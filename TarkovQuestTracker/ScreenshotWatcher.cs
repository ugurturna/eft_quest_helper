using System.Globalization;
using System.IO;
using System.Windows.Threading;
using TarkovQuestTracker.Core.Services;
using TarkovQuestTracker.Interop;

namespace TarkovQuestTracker;

/// <summary>
/// Watches the game's screenshot folder: a screenshot taken in raid has the player's position in its file name
/// (<see cref="ScreenshotPosition"/>). Only the name is read, never the picture. Positions arrive on the UI thread.
/// With <see cref="CleanupMinutes"/> set (off by default), the raid screenshots seen while the app runs go to the
/// Recycle Bin that long after they came in. Pictures without a position (menus, hideout, desktop) and the ones from
/// before the app was watching are never touched. Antivirus programs read many deletes in Documents as ransomware, so
/// it goes slowly: a few files a minute, never a burst when the app closes (what is left is kept in
/// <see cref="PendingFile"/> for the next start), and a single refusal stops it for the session.
/// </summary>
public sealed class ScreenshotWatcher : IDisposable
{
    public static string DefaultFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Escape from Tarkov", "Screenshots");

    readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    FileSystemWatcher? _watcher;
    string _folder = DefaultFolder;
    bool _enabled;
    int _cleanupMinutes;
    /// <summary>Files per cleanup round (one round a minute): slow on purpose, see the class summary.</summary>
    const int PerRound = 3;
    /// <summary>Rounds a file may stay busy (open in a viewer) before it is left alone.</summary>
    const int MaxTries = 5;
    /// <summary>Raid screenshots seen by this app and still to clean up: when they came in, rounds tried.</summary>
    readonly Dictionary<string, (DateTime Seen, int Tries)> _pending = new(StringComparer.OrdinalIgnoreCase);
    readonly DispatcherTimer _cleanupTimer;
    string? _pendingFile;
    /// <summary>Windows or an antivirus refused: no more tries until the app starts again.</summary>
    bool _blocked;

    public ScreenshotWatcher()
    {
        _cleanupTimer = new DispatcherTimer(TimeSpan.FromMinutes(1), DispatcherPriority.Background, (_, _) => Cleanup(), _dispatcher);
        _cleanupTimer.Stop();
        _dispatcher.ShutdownStarted += (_, _) => SavePending();
    }

    public event Action<ScreenshotPosition>? Captured;

    /// <summary>Moving a screenshot to the Recycle Bin was refused (Controlled folder access, an antivirus); cleanup stops.</summary>
    public event Action? CleanupBlocked;

    /// <summary>The folder in use; empty = <see cref="DefaultFolder"/>.</summary>
    public string Folder
    {
        get => _folder;
        set
        {
            value = string.IsNullOrWhiteSpace(value) ? DefaultFolder : value.Trim();
            if (value == _folder) return;
            _folder = value;
            Restart();
        }
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (value == _enabled) return;
            _enabled = value;
            Restart();
        }
    }

    /// <summary>Recycles the raid screenshots this app saw this many minutes after they came in; 0 = keep them all.</summary>
    public int CleanupMinutes
    {
        get => _cleanupMinutes;
        set
        {
            _cleanupMinutes = Math.Max(0, value);
            if (_cleanupMinutes == 0)
            {
                // turned off: nothing is touched, not even what an earlier session left to do
                _pending.Clear();
                SavePending();
            }
            _cleanupTimer.IsEnabled = _cleanupMinutes > 0 && !_blocked;
        }
    }

    /// <summary>Where the screenshots still to clean up are kept between runs (path and the time each came in).</summary>
    public string? PendingFile
    {
        get => _pendingFile;
        set
        {
            if (value == _pendingFile) return;
            _pendingFile = value;
            LoadPending();
        }
    }

    void LoadPending()
    {
        if (_pendingFile == null || !File.Exists(_pendingFile)) return;
        try
        {
            foreach (var line in File.ReadAllLines(_pendingFile))
            {
                var parts = line.Split('\t', 2);
                if (parts.Length == 2 && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
                    && ScreenshotPosition.TryParse(parts[1], out _))
                    _pending.TryAdd(parts[1], (new DateTime(ticks, DateTimeKind.Utc), 0));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // an unreadable list only means those files stay
        }
    }

    void SavePending()
    {
        if (_pendingFile == null) return;
        try
        {
            if (_pending.Count == 0)
            {
                File.Delete(_pendingFile);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(_pendingFile)!);
            File.WriteAllLines(_pendingFile, _pending.Select(p => $"{p.Value.Seen.Ticks.ToString(CultureInfo.InvariantCulture)}\t{p.Key}"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    public bool FolderExists => Directory.Exists(_folder);

    /// <summary>The newest screenshot with a position in the folder, if any (the last spot before the app started).</summary>
    public ScreenshotPosition? Latest()
    {
        try
        {
            if (!FolderExists) return null;
            return new DirectoryInfo(_folder).EnumerateFiles()
                .OrderByDescending(f => f.CreationTimeUtc)
                .Take(50)
                .Select(f => ScreenshotPosition.TryParse(f.Name, out var p) ? p : null)
                .FirstOrDefault(p => p != null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Starts watching again (the folder may have been made by the game's first screenshot since).</summary>
    public void Restart()
    {
        _watcher?.Dispose();
        _watcher = null;
        if (!_enabled || !FolderExists) return;
        try
        {
            _watcher = new FileSystemWatcher(_folder) { NotifyFilter = NotifyFilters.FileName };
            _watcher.Created += OnFile;
            _watcher.Renamed += OnFile;
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _watcher = null;
        }
    }

    void OnFile(object sender, FileSystemEventArgs e)
    {
        if (!ScreenshotPosition.TryParse(e.Name ?? "", out var position)) return;
        var path = e.FullPath;
        _dispatcher.BeginInvoke(() =>
        {
            if (_cleanupMinutes > 0 && !_blocked) _pending.TryAdd(path, (DateTime.UtcNow, 0));
            Captured?.Invoke(position);
        });
    }

    /// <summary>A few of the screenshots older than <see cref="CleanupMinutes"/> to the Recycle Bin, oldest first.</summary>
    void Cleanup()
    {
        if (_cleanupMinutes <= 0 || _blocked || _pending.Count == 0) return;
        var cutoff = DateTime.UtcNow.AddMinutes(-_cleanupMinutes);
        var due = _pending.Where(p => p.Value.Seen <= cutoff).OrderBy(p => p.Value.Seen).Take(PerRound).ToList();
        foreach (var (path, (seen, tries)) in due)
        {
            switch (RecycleBin.Send(path))
            {
                case RecycleResult.Done:
                    _pending.Remove(path);
                    break;
                case RecycleResult.Busy:
                    if (tries + 1 >= MaxTries) _pending.Remove(path);  // left alone: someone keeps it open
                    else _pending[path] = (seen, tries + 1);
                    break;
                case RecycleResult.Blocked:
                    // asked once, refused: no more tries this session (an antivirus would see them as an attack)
                    _blocked = true;
                    _cleanupTimer.Stop();
                    SavePending();
                    CleanupBlocked?.Invoke();
                    return;
            }
        }
        if (due.Count > 0) SavePending();
    }

    public void Dispose()
    {
        _cleanupTimer.Stop();
        _watcher?.Dispose();
        _watcher = null;
    }
}
