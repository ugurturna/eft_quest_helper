using System.IO;
using System.Windows.Threading;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker;

/// <summary>
/// Watches the game's screenshot folder: a screenshot taken in raid has the player's position in its file name
/// (<see cref="ScreenshotPosition"/>). Only the name is read, never the picture. Positions arrive on the UI thread.
/// </summary>
public sealed class ScreenshotWatcher : IDisposable
{
    public static string DefaultFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Escape from Tarkov", "Screenshots");

    readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    FileSystemWatcher? _watcher;
    string _folder = DefaultFolder;
    bool _enabled;

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
        _dispatcher.BeginInvoke(() => Captured?.Invoke(position));
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
}
