using System.IO;
using System.Windows.Threading;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker;

/// <summary>
/// Watches the game's screenshot folder: a screenshot taken in raid has the player's position in its file name
/// (<see cref="ScreenshotPosition"/>). Only the name is read, never the picture. Positions arrive on the UI thread.
/// With <see cref="CleanupMinutes"/> set, the raid screenshots seen while the app runs are deleted that long after
/// they came in (and the rest when the app closes); pictures without a position (menus, hideout, desktop) and the
/// ones from before the app started are never touched.
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
    /// <summary>Raid screenshots seen this session that are still on disk, with when they came in.</summary>
    readonly Dictionary<string, DateTime> _seen = new(StringComparer.OrdinalIgnoreCase);
    readonly DispatcherTimer _cleanupTimer;

    public ScreenshotWatcher()
    {
        _cleanupTimer = new DispatcherTimer(TimeSpan.FromMinutes(1), DispatcherPriority.Background, (_, _) => Cleanup(false), _dispatcher);
        _cleanupTimer.Stop();
        _dispatcher.ShutdownStarted += (_, _) => Cleanup(true);
    }

    public event Action<ScreenshotPosition>? Captured;

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

    /// <summary>Deletes this session's raid screenshots this many minutes after they came in; 0 = keep them.</summary>
    public int CleanupMinutes
    {
        get => _cleanupMinutes;
        set
        {
            _cleanupMinutes = Math.Max(0, value);
            if (_cleanupMinutes == 0) _seen.Clear();
            _cleanupTimer.IsEnabled = _cleanupMinutes > 0;
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
            if (_cleanupMinutes > 0) _seen.TryAdd(path, DateTime.UtcNow);
            Captured?.Invoke(position);
        });
    }

    /// <summary>Deletes the session's raid screenshots older than <see cref="CleanupMinutes"/> (all of them when <paramref name="all"/>).</summary>
    void Cleanup(bool all)
    {
        if (_cleanupMinutes <= 0 || _seen.Count == 0) return;
        var cutoff = DateTime.UtcNow.AddMinutes(-_cleanupMinutes);
        foreach (var (path, seen) in _seen.ToList())
        {
            if (!all && seen > cutoff) continue;
            try
            {
                File.Delete(path);
                _seen.Remove(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // still open (the game writing it, a viewer): next round
            }
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Stop();
        _watcher?.Dispose();
        _watcher = null;
    }
}
