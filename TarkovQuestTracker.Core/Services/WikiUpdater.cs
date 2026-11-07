using System.ComponentModel;
using System.Diagnostics;

namespace TarkovQuestTracker.Core.Services;

/// <summary>
/// Runs the scraper scripts (tools/scraper) that rebuild data/ from the wiki and tarkov.dev,
/// then copies the result into the folder the app reads its data from.
/// Only available when the app runs inside the project folder (the scripts are not shipped).
/// </summary>
public sealed class WikiUpdater
{
    /// <summary>The scripts in the order they have to run (same as the README).</summary>
    public static readonly IReadOnlyList<(string Script, string Args)> Steps =
    [
        ("scrape_wiki.py", "--refresh"),
        ("fetch_tarkovdev.py", ""),
        ("merge.py", ""),
        ("fetch_quest_items.py", ""),
    ];

    // Python launchers to try; "py" covers installs where python.exe is not on PATH
    static readonly string[] Pythons = ["python", "py"];

    WikiUpdater(string root) => Root = root;

    /// <summary>Project folder (contains tools/scraper and data).</summary>
    public string Root { get; }
    public string ScraperDir => Path.Combine(Root, "tools", "scraper");
    /// <summary>Where the scripts write: &lt;project&gt;/data.</summary>
    public string SourceDataDir => Path.Combine(Root, "data");

    /// <summary>Walks up from <paramref name="startDir"/> to the project folder; null when the scripts are not there.</summary>
    public static WikiUpdater? Find(string startDir)
    {
        for (var dir = new DirectoryInfo(startDir); dir != null; dir = dir.Parent)
            if (Steps.All(s => File.Exists(Path.Combine(dir.FullName, "tools", "scraper", s.Script))))
                return new WikiUpdater(dir.FullName);
        return null;
    }

    /// <summary>Runs every step; false (with the reason logged) when one fails. Cancelling kills the running script.</summary>
    public async Task<bool> RunAsync(Action<string> log, CancellationToken ct)
    {
        foreach (var (script, args) in Steps)
        {
            log($"▶ {script} {args}".TrimEnd());
            var exit = await RunPythonAsync(Path.Combine(ScraperDir, script), args, log, ct);
            if (exit == null) return false;
            if (exit != 0)
            {
                log($"✖ {script} exit code {exit}");
                return false;
            }
        }
        return true;
    }

    /// <summary>Exit code, or null when no Python could be started.</summary>
    async Task<int?> RunPythonAsync(string script, string args, Action<string> log, CancellationToken ct)
    {
        foreach (var python in Pythons)
        {
            var info = new ProcessStartInfo(python)
            {
                WorkingDirectory = Root,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            info.ArgumentList.Add("-u");
            info.ArgumentList.Add(script);
            foreach (var a in args.Split(' ', StringSplitOptions.RemoveEmptyEntries)) info.ArgumentList.Add(a);
            info.Environment["PYTHONIOENCODING"] = "utf-8";
            info.StandardOutputEncoding = info.StandardErrorEncoding = System.Text.Encoding.UTF8;

            using var process = new Process { StartInfo = info };
            process.OutputDataReceived += (_, e) => { if (e.Data != null) log(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) log(e.Data); };
            try
            {
                process.Start();
            }
            catch (Win32Exception)
            {
                continue;  // this launcher is not installed, try the next one
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw;
            }
            return process.ExitCode;
        }
        log("✖ Python not found (python / py). Install Python 3 and add it to PATH.");
        return null;
    }

    /// <summary>
    /// Copies the refreshed data files into <paramref name="appDataDir"/> when the app reads a copy
    /// (build output) instead of &lt;project&gt;/data. Returns the number of files copied.
    /// </summary>
    public int CopyInto(string appDataDir)
    {
        if (Path.GetFullPath(appDataDir).TrimEnd('\\', '/')
                .Equals(Path.GetFullPath(SourceDataDir).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))
            return 0;
        var files = Directory.GetFiles(SourceDataDir, "tasks_*.json")
            .Concat(new[] { "meta.json", "quest_items.json" }.Select(f => Path.Combine(SourceDataDir, f)))
            .Where(File.Exists)
            .Select(f => (From: f, To: Path.Combine(appDataDir, Path.GetFileName(f))))
            .ToList();
        var icons = Path.Combine(SourceDataDir, "icons");
        if (Directory.Exists(icons))
            files.AddRange(Directory.GetFiles(icons, "*.png")
                .Select(f => (From: f, To: Path.Combine(appDataDir, "icons", Path.GetFileName(f)))));

        var copied = 0;
        foreach (var (from, to) in files)
        {
            if (File.Exists(to) && File.GetLastWriteTimeUtc(to) >= File.GetLastWriteTimeUtc(from)
                                && new FileInfo(to).Length == new FileInfo(from).Length)
                continue;
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(from, to, overwrite: true);
            copied++;
        }
        return copied;
    }
}
