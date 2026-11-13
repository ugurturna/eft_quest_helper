using System.Net;
using System.Runtime;
using System.Text.Json;
using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>One item with its flea / trader prices for one game mode.</summary>
public sealed class FleaItem
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string ShortName { get; init; } = "";
    /// <summary>The game's English names: the screen reader matches them whatever the app language.</summary>
    public string EnglishName { get; init; } = "";
    public string EnglishShortName { get; init; } = "";
    public string? IconLink { get; init; }
    public int Width { get; init; } = 1;
    public int Height { get; init; } = 1;
    public int? LastLowPrice { get; init; }
    public int? Avg24hPrice { get; init; }
    public double? Change48hPercent { get; init; }
    public int OfferCount { get; init; }
    public int? MinLevelForFlea { get; init; }
    /// <summary>Can't be listed on the flea market at all.</summary>
    public bool FleaBanned { get; init; }
    public string? BestTrader { get; init; }
    public int? BestTraderPrice { get; init; }

    public int Slots => Math.Max(1, Width * Height);
    /// <summary>Last flea price, or the 24 h average when nobody is selling right now.</summary>
    public int? FleaPrice => LastLowPrice ?? Avg24hPrice;

    // normalized search keys (local + English names), built once
    internal string[] Words { get; init; } = [];
    internal string[] ShortKeys { get; init; } = [];
}

/// <summary>
/// Flea market prices from json.tarkov.dev (the same dumps tools/scraper uses), per game mode:
/// "regular" for PvP, "pve" for PvE. The dumps are saved under <see cref="Folder"/>/&lt;mode&gt;/ with the
/// download time in meta.json; searches always read those local files.
/// </summary>
public sealed class FleaMarket(string folder)
{
    const string Base = "https://json.tarkov.dev";
    /// <summary>Older local prices are downloaded again when the search opens.</summary>
    public static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(15);
    public const int MinQueryLength = 3;
    public static readonly GameMode[] Modes = [GameMode.Pvp, GameMode.Pve];

    static readonly HttpClient Http = CreateClient();

    // all members are used from one thread (the UI thread); the work itself runs on the thread pool
    readonly Dictionary<GameMode, Task> _refreshing = [];
    readonly Dictionary<string, (DateTime? Stamp, IReadOnlyList<FleaItem> Items)> _parsed = [];
    readonly Dictionary<string, Task<IReadOnlyList<FleaItem>?>> _loading = [];

    sealed class Meta
    {
        public DateTime UpdatedAt { get; set; }
    }

    static HttpClient CreateClient()
    {
        var http = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
        {
            Timeout = TimeSpan.FromSeconds(90),
        };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TarkovQuestTracker");
        return http;
    }

    public string Folder { get; } = folder;

    public static string ModePath(GameMode mode) => mode == GameMode.Pve ? "pve" : "regular";

    string ModeFolder(GameMode mode) => Path.Combine(Folder, ModePath(mode));
    string MetaPath(GameMode mode) => Path.Combine(ModeFolder(mode), "meta.json");

    /// <summary>When the mode's local files were downloaded (UTC); null when there are none.</summary>
    public DateTime? UpdatedAt(GameMode mode)
    {
        try
        {
            return File.Exists(MetaPath(mode)) ? JsonSerializer.Deserialize<Meta>(File.ReadAllText(MetaPath(mode)))?.UpdatedAt : null;
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Older than <see cref="MaxAge"/>, never downloaded, or missing this language's names.</summary>
    public bool IsStale(GameMode mode, string lang) =>
        UpdatedAt(mode) is not { } at || DateTime.UtcNow - at > MaxAge
        || (lang != "en" && !File.Exists(Path.Combine(ModeFolder(mode), $"items_{lang}.json")));

    public bool IsRefreshing(GameMode mode) => _refreshing.ContainsKey(mode);

    /// <summary>
    /// Background update of the mode being played (app start), then parses it so the first search is instant;
    /// the other mode loads when a search opens in it. Failures only mean the old files stay.
    /// </summary>
    public async Task PrepareAsync(GameMode mode, string lang)
    {
        try { await RefreshAsync(mode, lang); }
        catch (Exception e) when (IsDownloadError(e)) { }
        try { await LoadAsync(mode, lang); }
        catch (Exception e) when (IsDownloadError(e)) { }
        // parsing the ~17 MB dump peaks at ~150 MB of short-lived objects; give that back to Windows once
        // instead of keeping the peak as the app's footprint while the game runs
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    /// <summary>Downloads the mode's dumps to disk and stamps the time; a refresh already running is shared.</summary>
    public Task RefreshAsync(GameMode mode, string lang)
    {
        if (_refreshing.TryGetValue(mode, out var running)) return running;
        var task = RunRefreshAsync(mode, lang);
        _refreshing[mode] = task;
        return task;
    }

    async Task RunRefreshAsync(GameMode mode, string lang)
    {
        try
        {
            await Task.Run(() => DownloadAsync(mode, lang));
        }
        finally
        {
            _refreshing.Remove(mode);
        }
    }

    async Task DownloadAsync(GameMode mode, string lang)
    {
        var dir = ModeFolder(mode);
        Directory.CreateDirectory(dir);
        var path = ModePath(mode);
        Task Get(string name) => DownloadFileAsync($"{Base}/{path}/{name}", Path.Combine(dir, name + ".json"));
        // translations are optional: a failed one leaves English names / trader ids
        async Task Optional(string name)
        {
            try { await Get(name); }
            catch (Exception e) when (IsDownloadError(e)) { }
        }
        await Task.WhenAll(Get("items"), Get("items_en"), Optional($"traders_{lang}"),
                           lang == "en" ? Task.CompletedTask : Optional($"items_{lang}"));
        // stamp last, so a half-finished download never looks fresh
        File.WriteAllText(MetaPath(mode), JsonSerializer.Serialize(new Meta { UpdatedAt = DateTime.UtcNow }));
    }

    /// <summary>Writes to a temp file first: a search reading the old file never sees a half-written one.</summary>
    static async Task DownloadFileAsync(string url, string target)
    {
        var temp = target + ".tmp";
        await using (var stream = await Http.GetStreamAsync(url))
        await using (var file = File.Create(temp))
            await stream.CopyToAsync(file);
        File.Move(temp, target, overwrite: true);
    }

    /// <summary>
    /// The mode's items parsed from the local files (re-read after each download); null when nothing was downloaded yet.
    /// </summary>
    public async Task<IReadOnlyList<FleaItem>?> LoadAsync(GameMode mode, string lang)
    {
        var key = $"{ModePath(mode)}/{lang}";
        var stamp = UpdatedAt(mode);
        if (stamp == null) return null;
        if (_parsed.TryGetValue(key, out var p) && p.Stamp == stamp) return p.Items;
        if (!_loading.TryGetValue(key, out var task))
            _loading[key] = task = Task.Run(() => ReadLocalAsync(mode, lang));
        try
        {
            var items = await task;
            if (items != null) _parsed[key] = (stamp, items);
            return items;
        }
        finally
        {
            _loading.Remove(key);
        }
    }

    async Task<IReadOnlyList<FleaItem>?> ReadLocalAsync(GameMode mode, string lang)
    {
        var dir = ModeFolder(mode);
        using var items = await ReadJsonAsync(Path.Combine(dir, "items.json"));
        using var en = await ReadJsonAsync(Path.Combine(dir, "items_en.json"));
        if (items == null || en == null) return null;
        // translations are optional: English names / trader ids when missing
        using var local = lang == "en" ? null : await ReadJsonAsync(Path.Combine(dir, $"items_{lang}.json"));
        using var traders = await ReadJsonAsync(Path.Combine(dir, $"traders_{lang}.json"));
        return Parse(items.RootElement, en.RootElement, local?.RootElement, traders?.RootElement);
    }

    static async Task<JsonDocument?> ReadJsonAsync(string path)
    {
        if (!File.Exists(path)) return null;
        // parse the file's own bytes: ParseAsync(stream) rents a pooled copy of the whole file, and the shared
        // pool keeps those big arrays alive after the document is disposed
        return JsonDocument.Parse(await File.ReadAllBytesAsync(path));
    }

    /// <summary>Network, disk or broken-JSON failures of a download / local read.</summary>
    public static bool IsDownloadError(Exception e) =>
        e is HttpRequestException or TaskCanceledException or JsonException or IOException or UnauthorizedAccessException;

    /// <summary>
    /// Builds the list from the dumps: <c>{"data":{"items":{id:{...}}}}</c> plus the
    /// <c>{"data":{"&lt;id&gt; Name": "..."}}</c> translation tables.
    /// </summary>
    public static IReadOnlyList<FleaItem> Parse(JsonElement items, JsonElement english, JsonElement? local, JsonElement? traders)
    {
        var en = english.GetProperty("data");
        var loc = local?.GetProperty("data");
        var tr = traders?.GetProperty("data");

        static string Text(JsonElement? table, string key) =>
            table is { } t && t.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

        var list = new List<FleaItem>();
        foreach (var p in items.GetProperty("data").GetProperty("items").EnumerateObject())
        {
            var i = p.Value;
            var nameKey = Str(i, "name") ?? "";
            var shortKey = Str(i, "shortName") ?? "";
            var enName = Text(en, nameKey) is { Length: > 0 } n ? n : nameKey;
            var enShort = Text(en, shortKey) is { Length: > 0 } s ? s : enName;
            var name = Text(loc, nameKey) is { Length: > 0 } ln ? ln : enName;
            var shortName = Text(loc, shortKey) is { Length: > 0 } ls ? ls : enShort;

            string? bestTrader = null;
            int? bestPrice = null;
            if (i.TryGetProperty("sellToTrader", out var sells) && sells.ValueKind == JsonValueKind.Array)
                foreach (var sell in sells.EnumerateArray())
                {
                    var price = Int(sell, "priceRUB") ?? 0;
                    if (price <= 0 || price <= bestPrice) continue;
                    bestPrice = price;
                    var trader = Str(sell, "trader") ?? "";
                    bestTrader = Text(tr, $"{trader} Nickname") is { Length: > 0 } tn ? tn : trader;
                }

            var banned = i.TryGetProperty("types", out var types) && types.ValueKind == JsonValueKind.Array
                         && types.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && x.GetString() == "noFlea");

            list.Add(new FleaItem
            {
                Id = Str(i, "id") ?? p.Name,
                Name = name,
                ShortName = shortName,
                EnglishName = enName,
                EnglishShortName = enShort,
                IconLink = Str(i, "iconLink"),
                Width = Int(i, "width") ?? 1,
                Height = Int(i, "height") ?? 1,
                LastLowPrice = Positive(Int(i, "lastLowPrice")),
                Avg24hPrice = Positive(Int(i, "avg24hPrice")),
                Change48hPercent = i.TryGetProperty("changeLast48hPercent", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetDouble() : null,
                OfferCount = Int(i, "lastOfferCount") ?? 0,
                MinLevelForFlea = Int(i, "minLevelForFlea"),
                FleaBanned = banned,
                BestTrader = bestTrader,
                BestTraderPrice = bestPrice,
                Words = new[] { name, enName, shortName, enShort }
                    .SelectMany(x => SearchService.Normalize(x).Split(' ', StringSplitOptions.RemoveEmptyEntries)).Distinct().ToArray(),
                ShortKeys = new[] { shortName, enShort }.Select(x => SearchService.Normalize(x).Replace(" ", "")).Distinct().ToArray(),
            });
        }
        return list;
    }

    static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    static int? Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? (int)Math.Round(v.GetDouble()) : null;

    static int? Positive(int? value) => value > 0 ? value : null;

    /// <summary>
    /// Items whose names contain every word of the query (word starts rank higher), case / Turkish-letter insensitive.
    /// Empty until the query has <see cref="MinQueryLength"/> letters.
    /// </summary>
    public static IReadOnlyList<FleaItem> Search(IEnumerable<FleaItem> items, string query, int limit = 12)
    {
        var words = SearchService.Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var joined = string.Concat(words);
        if (joined.Length < MinQueryLength) return [];
        return items
            .Select(i => (Item: i, Score: Score(i, words, joined)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Item.OfferCount)
            .ThenBy(x => x.Item.Name.Length)
            .Take(limit)
            .Select(x => x.Item)
            .ToList();
    }

    static int Score(FleaItem item, string[] words, string joined)
    {
        if (item.ShortKeys.Contains(joined)) return 100;  // "ledx", "gpu", "m4a1"
        var score = 0;
        foreach (var w in words)
        {
            if (item.Words.Any(k => k.StartsWith(w, StringComparison.Ordinal))) score += 3;
            else if (item.Words.Any(k => k.Contains(w, StringComparison.Ordinal))
                     || item.ShortKeys.Any(k => k.Contains(w, StringComparison.Ordinal))) score += 1;
            else return 0;  // every word must match
        }
        if (item.ShortKeys.Any(k => k.StartsWith(joined, StringComparison.Ordinal))) score += 5;
        return score;
    }
}
