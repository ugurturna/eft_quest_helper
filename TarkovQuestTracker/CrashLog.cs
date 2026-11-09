using System.IO;
using System.Text;
using System.Windows.Media;

namespace TarkovQuestTracker;

/// <summary>
/// crash.log next to the profiles: every error nothing else caught, with what is needed to tell one PC from another
/// (app and Windows version, the graphics tier). Without it a crash on someone else's PC leaves nothing to go on.
/// </summary>
static class CrashLog
{
    /// <summary>Past this size the file is started again: the newest crashes are the ones that matter.</summary>
    const long MaxBytes = 512 * 1024;
    /// <summary>The same error again within this is not written again (one failing picture can fail every frame).</summary>
    static readonly TimeSpan Repeat = TimeSpan.FromSeconds(30);

    static readonly object Gate = new();
    static string? _lastKey;
    static DateTime _lastAt;

    /// <summary>The app's storage folder once the profiles are found; the temp folder before that.</summary>
    public static string Folder { get; set; } = Path.GetTempPath();

    public static string FilePath => Path.Combine(Folder, "crash.log");

    /// <summary>Writes the error; false when it is the same as the last one just now (not written again).</summary>
    public static bool Write(string where, Exception? error)
    {
        var key = $"{where}|{error?.GetType().FullName}|{error?.Message}";
        lock (Gate)
        {
            var now = DateTime.UtcNow;
            if (key == _lastKey && now - _lastAt < Repeat) return false;
            (_lastKey, _lastAt) = (key, now);
            try
            {
                var path = FilePath;
                if (File.Exists(path) && new FileInfo(path).Length > MaxBytes) File.Delete(path);
                var text = new StringBuilder()
                    .AppendLine($"==== {DateTime.Now:yyyy-MM-dd HH:mm:ss} · {where}")
                    .AppendLine($"app {ViewModels.MainViewModel.AppVersion} · {Environment.OSVersion.VersionString} · .NET {Environment.Version}"
                                + $" · render tier {RenderTier()}")
                    .AppendLine(error?.ToString() ?? "(no exception)")
                    .AppendLine();
                Directory.CreateDirectory(Folder);
                File.AppendAllText(path, text.ToString());
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // nowhere to write: nothing more to do
            }
            return true;
        }
    }

    /// <summary>0 = software drawing, 1 = partial, 2 = full graphics card drawing; "?" off the UI thread.</summary>
    static string RenderTier()
    {
        try
        {
            return (RenderCapability.Tier >> 16).ToString();
        }
        catch (InvalidOperationException)
        {
            return "?";
        }
    }
}
