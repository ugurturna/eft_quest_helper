using System.Net;
using System.Net.Http.Headers;
using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>
/// Keeps data/maps.json fresh without ever leaving the player without a map. At start-up the repository's copy
/// (rebuilt every day from tarkov.dev by .github/workflows/maps-data.yml, and only committed when the map tests pass)
/// is downloaded into the cache folder. What the map uses, best first:
/// the downloaded copy, the last good download, the copy that came with the app; a map missing from the newer
/// file is taken from the older one. A failed or broken download changes nothing.
/// </summary>
public static class MapDataUpdater
{
    public const string Url = $"https://raw.githubusercontent.com/{AppUpdater.Repo}/main/data/{MapRepository.FileName}";
    const string ETagFile = "maps.etag";
    /// <summary>Fewer maps than this means a broken file, not a smaller game.</summary>
    const int MinMaps = 8;

    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var http = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
        {
            Timeout = TimeSpan.FromSeconds(30),
        };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TarkovQuestTracker");
        return http;
    }

    /// <summary>The newest usable maps: the cached download when it is good and not older than the app's own file.</summary>
    public static IReadOnlyDictionary<string, MapGeometry> LoadBest(string dataDir, string cacheDir)
    {
        var bundled = MapRepository.TryLoad(Path.Combine(dataDir, MapRepository.FileName));
        var cached = MapRepository.TryLoad(Path.Combine(cacheDir, MapRepository.FileName));
        if (cached is not { } c || !IsUsable(c.Maps)) return bundled?.Maps ?? new Dictionary<string, MapGeometry>();
        if (bundled is not { } b) return c.Maps;
        return c.FetchedAt >= b.FetchedAt ? Merge(c.Maps, b.Maps) : Merge(b.Maps, c.Maps);
    }

    /// <summary><paramref name="primary"/>, plus the maps only <paramref name="fallback"/> has.</summary>
    public static IReadOnlyDictionary<string, MapGeometry> Merge(
        IReadOnlyDictionary<string, MapGeometry> primary, IReadOnlyDictionary<string, MapGeometry> fallback)
    {
        var maps = new Dictionary<string, MapGeometry>(primary);
        foreach (var (name, map) in fallback) maps.TryAdd(name, map);
        return maps;
    }

    /// <summary>Enough maps, each one drawable (the projection and a picture), Customs among them.</summary>
    public static bool IsUsable(IReadOnlyDictionary<string, MapGeometry> maps) =>
        maps.Count >= MinMaps && maps.ContainsKey("Customs")
        && maps.Values.All(m => m.Transform.Length == 4 && (m.SvgPath != null || m.TilePath != null));

    /// <summary>
    /// Downloads the repository's maps.json into <paramref name="cacheDir"/> when it changed (ETag); true when a new,
    /// usable file was saved. Never throws: no network, a bad file or a full disk keep what is there.
    /// </summary>
    public static async Task<bool> RefreshAsync(string cacheDir, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(cacheDir);
            var path = Path.Combine(cacheDir, MapRepository.FileName);
            var etagPath = Path.Combine(cacheDir, ETagFile);
            using var request = new HttpRequestMessage(HttpMethod.Get, Url);
            if (File.Exists(path) && File.Exists(etagPath)
                && EntityTagHeaderValue.TryParse(await File.ReadAllTextAsync(etagPath, ct), out var etag))
                request.Headers.IfNoneMatch.Add(etag);
            using var response = await Http.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.NotModified || !response.IsSuccessStatusCode) return false;
            var json = await response.Content.ReadAsStringAsync(ct);
            if (MapRepository.Parse(json) is not { } data || !IsUsable(data.Maps)) return false;
            // written next to the old one first: a crash halfway never leaves a broken file behind
            var temp = path + ".new";
            await File.WriteAllTextAsync(temp, json, ct);
            File.Move(temp, path, overwrite: true);
            if (response.Headers.ETag is { } newTag) await File.WriteAllTextAsync(etagPath, newTag.ToString(), ct);
            return true;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException
                                      or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
