using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>One report of the Goons on a map.</summary>
public sealed record GoonSighting(string MapSlug, string MapName, DateTime SeenAt, string Reporter);

/// <summary>
/// Where the Goons were last seen, from tarkov-goon-tracker.com (separate PvP and PvE pages). The site has no API:
/// its Next.js pages carry the reports in the <c>__NEXT_DATA__</c> script, which is read here.
/// </summary>
public static partial class GoonTracker
{
    const string Base = "https://www.tarkov-goon-tracker.com/tr";
    /// <summary>The maps the Goons roam; the PvP feed also has bot rows with stream titles as the "map".</summary>
    static readonly HashSet<string> Maps = ["customs", "woods", "shoreline", "lighthouse"];

    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var http = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
        {
            Timeout = TimeSpan.FromSeconds(15),
        };
        // the site answers plain browsers; a bare app name may get a bot page
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) TarkovQuestTracker");
        return http;
    }

    public static string PageUrl(GameMode mode) => mode == GameMode.Pve ? $"{Base}/pve" : Base;

    /// <summary>The mode's reports, newest first.</summary>
    public static async Task<IReadOnlyList<GoonSighting>> FetchAsync(GameMode mode, CancellationToken cancel = default) =>
        Parse(await Http.GetStringAsync(PageUrl(mode), cancel));

    [GeneratedRegex("""<script id="__NEXT_DATA__"[^>]*>(.*?)</script>""", RegexOptions.Singleline)]
    private static partial Regex NextData();

    /// <summary>
    /// The reports in a page: <c>props.pageProps.trackings[]</c> with <c>currentDate</c>, <c>map.slug</c>, <c>map.name</c>
    /// and <c>username</c>. Rows without a real date or a Goon map are dropped. Empty when the page has none.
    /// </summary>
    public static IReadOnlyList<GoonSighting> Parse(string html)
    {
        var match = NextData().Match(html);
        if (!match.Success) return [];
        using var doc = JsonDocument.Parse(match.Groups[1].Value);
        if (!doc.RootElement.TryGetProperty("props", out var props)
            || !props.TryGetProperty("pageProps", out var page)
            || !page.TryGetProperty("trackings", out var rows)
            || rows.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<GoonSighting>();
        foreach (var row in rows.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object
                || !DateTime.TryParse(Str(row, "currentDate"), CultureInfo.InvariantCulture,
                                      DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at)
                || !row.TryGetProperty("map", out var map) || map.ValueKind != JsonValueKind.Object
                || Str(map, "slug")?.ToLowerInvariant() is not { } slug || !Maps.Contains(slug))
                continue;
            list.Add(new GoonSighting(slug, Str(map, "name") ?? slug, at, (Str(row, "username") ?? "").Trim()));
        }
        return list.OrderByDescending(s => s.SeenAt).ToList();
    }

    static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>Network or broken-page failures of a fetch.</summary>
    public static bool IsFetchError(Exception e) =>
        e is HttpRequestException or TaskCanceledException or JsonException;
}
