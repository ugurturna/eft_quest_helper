using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace TarkovQuestTracker.Core.Services;

/// <summary>A newer GitHub release with the win-x64 zip attached.</summary>
public sealed record ReleaseInfo(Version Version, string Tag, string Notes, string PageUrl,
                                 string AssetUrl, long AssetSize, string? Sha256);

/// <summary>
/// Self-update from the GitHub releases (the zip release.yml publishes: the exe plus data\).
/// The running exe can't be overwritten but can be renamed, so files are swapped in place:
/// old ones become *.old (deleted on the next start). The player's files (<see cref="Keep"/>) are never touched,
/// and are copied to data\backups first (<see cref="BackupUserData"/>).
/// </summary>
public sealed class AppUpdater(string installDir)
{
    public const string Repo = "ugurturna/eft_quest_helper";
    const string LatestUrl = $"https://api.github.com/repos/{Repo}/releases/latest";
    public const string ReleasesPage = $"https://github.com/{Repo}/releases/latest";
    const string OldSuffix = ".old";

    /// <summary>
    /// Kept across updates (relative to the install folder): progress, settings, pre-profile files that were
    /// not migrated yet, the update backups and the flea price cache. An update never writes or deletes these.
    /// </summary>
    static readonly string[] Keep =
    [
        Path.Combine("data", "profiles"), Path.Combine("data", "app.json"),
        Path.Combine("data", "settings.json"), Path.Combine("data", "progress_pvp.json"), Path.Combine("data", "progress_pve.json"),
        Path.Combine("data", BackupFolder), Path.Combine("data", "flea"), "portable.txt",
    ];

    const string BackupFolder = "backups";
    /// <summary>How many pre-update backups stay in data\backups (oldest are pruned).</summary>
    public const int BackupsKept = 5;

    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TarkovQuestTracker");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return http;
    }

    public string InstallDir { get; } = installDir;

    /// <summary>The latest release when it is newer than <paramref name="current"/> and has the zip; otherwise null.</summary>
    public static async Task<ReleaseInfo?> CheckAsync(Version current, CancellationToken ct = default)
    {
        await using var stream = await Http.GetStreamAsync(LatestUrl, ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var release = ParseRelease(doc.RootElement);
        return release != null && IsNewer(release.Version, current) ? release : null;
    }

    /// <summary>Reads a GitHub "latest release" response; null for drafts, pre-releases or a release without the win-x64 zip.</summary>
    public static ReleaseInfo? ParseRelease(JsonElement r)
    {
        if (Bool(r, "draft") || Bool(r, "prerelease")) return null;
        var tag = Str(r, "tag_name") ?? "";
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out var version)) return null;
        if (!r.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) return null;
        foreach (var a in assets.EnumerateArray())
        {
            var name = Str(a, "name") ?? "";
            if (!name.EndsWith("-win-x64.zip", StringComparison.OrdinalIgnoreCase)) continue;
            var digest = Str(a, "digest");
            return new ReleaseInfo(version, tag, Str(r, "body") ?? "", Str(r, "html_url") ?? ReleasesPage,
                Str(a, "browser_download_url") ?? "", a.TryGetProperty("size", out var s) ? s.GetInt64() : 0,
                digest != null && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ? digest[7..].ToLowerInvariant() : null);
        }
        return null;
    }

    static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    static bool Bool(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

    /// <summary>Compares major.minor.build only (assembly versions carry a 4th part, tags don't).</summary>
    public static bool IsNewer(Version candidate, Version current) => Norm(candidate) > Norm(current);

    static Version Norm(Version v) => new(v.Major, Math.Max(0, v.Minor), Math.Max(0, v.Build));

    /// <summary>Downloads and checks (size, SHA-256) the zip, then unpacks it; returns the folder that holds the new exe.</summary>
    public async Task<string> DownloadAsync(ReleaseInfo release, IProgress<double>? progress, CancellationToken ct = default)
    {
        var work = Path.Combine(Path.GetTempPath(), "TarkovQuestTracker-update");
        if (Directory.Exists(work)) Directory.Delete(work, recursive: true);
        Directory.CreateDirectory(work);
        var zip = Path.Combine(work, "release.zip");

        using (var response = await Http.GetAsync(release.AssetUrl, HttpCompletionOption.ResponseHeadersRead, ct))
        {
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? release.AssetSize;
            await using var source = await response.Content.ReadAsStreamAsync(ct);
            await using var file = File.Create(zip);
            var buffer = new byte[81920];
            long done = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, ct)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read), ct);
                done += read;
                if (total > 0) progress?.Report((double)done / total);
            }
        }

        if (release.AssetSize > 0 && new FileInfo(zip).Length != release.AssetSize)
            throw new InvalidDataException("The download is incomplete.");
        if (release.Sha256 != null)
        {
            await using var file = File.OpenRead(zip);
            var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(file, ct));
            if (hash != release.Sha256) throw new InvalidDataException("The download does not match the release checksum.");
        }

        var staging = Path.Combine(work, "files");
        ZipFile.ExtractToDirectory(zip, staging);
        return FindAppRoot(staging) ?? throw new InvalidDataException("TarkovQuestTracker.exe is not in the release zip.");
    }

    /// <summary>The zip has the files at its root; tolerate one wrapping folder.</summary>
    static string? FindAppRoot(string staging)
    {
        if (File.Exists(Path.Combine(staging, "TarkovQuestTracker.exe"))) return staging;
        var dirs = Directory.GetDirectories(staging);
        return dirs.Length == 1 && File.Exists(Path.Combine(dirs[0], "TarkovQuestTracker.exe")) ? dirs[0] : null;
    }

    /// <summary>
    /// Copies the new files over the install folder. A file in use (the running exe) is renamed to *.old first.
    /// The player's own files (<see cref="Keep"/>) are skipped even if a release ever ships them.
    /// </summary>
    public void Apply(string newRoot)
    {
        // (target, its *.old backup or null when the file is new) to roll back if a copy fails half way
        var done = new List<(string Target, string? Old)>();
        try
        {
            foreach (var source in Directory.EnumerateFiles(newRoot, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(newRoot, source);
                if (IsKept(relative)) continue;
                var target = Path.Combine(InstallDir, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                string? old = null;
                if (File.Exists(target))
                {
                    old = target + OldSuffix;
                    if (File.Exists(old)) File.Delete(old);
                    File.Move(target, old);  // works for the running exe too
                }
                done.Add((target, old));
                File.Copy(source, target);
            }
        }
        catch
        {
            for (var i = done.Count - 1; i >= 0; i--)
            {
                var (target, old) = done[i];
                try
                {
                    if (File.Exists(target)) File.Delete(target);
                    if (old != null) File.Move(old, target);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
            }
            throw;
        }
    }

    /// <summary>
    /// Copies the player's files (<see cref="Keep"/>, minus the re-downloadable flea cache and older backups) to
    /// data\backups\{timestamp}\ before an update; returns that folder, or null when there was nothing to back up.
    /// Throws on a failed copy so the update is not applied without a backup.
    /// </summary>
    public string? BackupUserData(DateTime? now = null)
    {
        var sources = Keep
            .Where(k => k != Path.Combine("data", BackupFolder) && k != Path.Combine("data", "flea"))
            .Select(k => (Relative: k, Full: Path.Combine(InstallDir, k)))
            .Where(s => File.Exists(s.Full) || Directory.Exists(s.Full))
            .ToList();
        if (sources.Count == 0) return null;

        var root = Path.Combine(InstallDir, "data", BackupFolder);
        var target = Path.Combine(root, (now ?? DateTime.Now).ToString("yyyyMMdd-HHmmss"));
        foreach (var (relative, full) in sources)
        {
            var dest = Path.Combine(target, relative);
            if (File.Exists(full))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(full, dest, overwrite: true);
                continue;
            }
            foreach (var file in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
            {
                var fileDest = Path.Combine(dest, Path.GetRelativePath(full, file));
                Directory.CreateDirectory(Path.GetDirectoryName(fileDest)!);
                File.Copy(file, fileDest, overwrite: true);
            }
        }

        // timestamped names sort chronologically; prune beyond the newest few (best effort)
        foreach (var old in Directory.GetDirectories(root).Order(StringComparer.Ordinal).SkipLast(BackupsKept))
        {
            try { Directory.Delete(old, recursive: true); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
        return target;
    }

    public static bool IsKept(string relative) =>
        Keep.Any(k => relative.Equals(k, StringComparison.OrdinalIgnoreCase)
                      || relative.StartsWith(k + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

    /// <summary>Deletes the *.old files a previous update left behind (call once the old process has exited).</summary>
    public void CleanupOldFiles()
    {
        if (!Directory.Exists(InstallDir)) return;
        foreach (var old in Directory.EnumerateFiles(InstallDir, "*" + OldSuffix, SearchOption.AllDirectories))
        {
            if (IsKept(Path.GetRelativePath(InstallDir, old))) continue;
            try { File.Delete(old); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }  // next start
        }
    }

    /// <summary>Network, disk, permission or bad-download failures of a check / update.</summary>
    public static bool IsUpdateError(Exception e) =>
        e is HttpRequestException or TaskCanceledException or JsonException or IOException
          or UnauthorizedAccessException or InvalidDataException;
}
