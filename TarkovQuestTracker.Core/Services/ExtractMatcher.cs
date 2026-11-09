using System.Text.Json;

namespace TarkovQuestTracker.Core.Services;

/// <summary>
/// Recognizes the map from the exfil list the game shows on a double "O" press (OCR text): exfil names are
/// mostly unique to one map. Strict on purpose: no map is better than a wrong one starting a raid.
/// </summary>
public sealed class ExtractMatcher
{
    public const string FileName = "extracts.json";

    sealed class ExtractData
    {
        public Dictionary<string, List<string>> Maps { get; set; } = [];
    }

    /// <summary>One exfil name (cleaned) and the maps that have it.</summary>
    readonly List<(string[] Words, string Text, HashSet<string> Maps)> _index;
    readonly Dictionary<string, int> _counts;

    public ExtractMatcher(IReadOnlyDictionary<string, List<string>> maps)
    {
        var byName = new Dictionary<string, HashSet<string>>();
        foreach (var (map, names) in maps)
            foreach (var name in names.Select(Clean).Where(n => n.Length >= 3))
            {
                if (!byName.TryGetValue(name, out var set)) byName[name] = set = [];
                set.Add(map);
            }
        _index = byName.Select(kv => (kv.Key.Split(' '), kv.Key, kv.Value)).ToList();
        _counts = maps.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
    }

    public bool IsEmpty => _index.Count == 0;

    /// <summary>Loads data/extracts.json; an empty matcher (never finds a map) when the file is missing.</summary>
    public static ExtractMatcher Load(string dataDir)
    {
        var path = Path.Combine(dataDir, FileName);
        var data = File.Exists(path)
            ? JsonSerializer.Deserialize<ExtractData>(File.ReadAllText(path), QuestRepository.Json)
            : null;
        return new ExtractMatcher(data?.Maps ?? []);
    }

    /// <summary>
    /// The map whose exfils are in <paramref name="lines"/>, or null. It needs a name only that map has, more of
    /// those than any other map, and two exfils in all (one on maps that have only one or two).
    /// </summary>
    public string? Map(IEnumerable<string> lines)
    {
        var found = Found(lines);
        var scores = found.SelectMany(e => e.Maps.Select(m => (Map: m, Unique: e.Maps.Count == 1)))
                          .GroupBy(x => x.Map)
                          .Select(g => (Map: g.Key, Unique: g.Count(x => x.Unique), Total: g.Count()))
                          .OrderByDescending(s => s.Unique).ThenByDescending(s => s.Total)
                          .ToList();
        if (scores.Count == 0 || scores[0].Unique == 0) return null;
        var best = scores[0];
        if (scores.Count > 1 && scores[1].Unique == best.Unique) return null;
        var needed = _counts.GetValueOrDefault(best.Map, 3) <= 2 ? 1 : 2;
        return best.Total >= needed ? best.Map : null;
    }

    /// <summary>How many exfil names are readable: grows while a slow PC is still drawing the list.</summary>
    public int Hits(IEnumerable<string> lines) => Found(lines).Count;

    /// <summary>
    /// Exfil names that appear in the lines (other text on the line, such as "EXFIL01" or a timer, is fine). A name
    /// inside a longer one found on the same line is not counted: "Side Tunnel" (Lighthouse) is not Shoreline's "Tunnel",
    /// "Courtyard Gate" (Factory) not Streets' "Courtyard".
    /// </summary>
    List<(string[] Words, string Text, HashSet<string> Maps)> Found(IEnumerable<string> lines)
    {
        var found = new List<(string[] Words, string Text, HashSet<string> Maps)>();
        foreach (var line in lines.Select(Clean).Where(l => l != ""))
        {
            var words = line.Split(' ');
            var hits = _index.Select(e => (Entry: e, Start: Find(words, e.Words, e.Text)))
                             .Where(h => h.Start >= 0).ToList();
            foreach (var (entry, start) in hits)
            {
                var end = start + entry.Words.Length;
                var inside = hits.Any(o => o.Entry.Words.Length > entry.Words.Length
                                           && o.Start <= start && end <= o.Start + o.Entry.Words.Length);
                if (!inside && !found.Contains(entry)) found.Add(entry);
            }
        }
        return found;
    }

    /// <summary>The word the name starts at in the line, or -1.</summary>
    static int Find(string[] words, string[] name, string text)
    {
        var allowed = Allowed(text.Length);
        for (var i = 0; i + name.Length <= words.Length; i++)
        {
            var window = string.Join(' ', words, i, name.Length);
            if (window == text) return i;
            if (allowed > 0 && Math.Abs(window.Length - text.Length) <= allowed
                && SearchService.Levenshtein(window, text) <= allowed) return i;
        }
        return -1;
    }

    /// <summary>
    /// <see cref="QuestTextMatcher.Clean"/>, plus the game font's look-alikes in words with digits: its zero is
    /// slashed ("ZB-1Ø13") and OCR takes a 1 for I or l ("ZB-IØII").
    /// </summary>
    static string Clean(string text) =>
        string.Join(' ', QuestTextMatcher.Clean(text).Split(' ').Select(w =>
            w.Any(c => char.IsDigit(c) || c == 'ø')
                ? w.Replace('ø', '0').Replace('o', '0').Replace('i', '1').Replace('l', '1')
                : w));

    /// <summary>OCR typos: none in short names ("ZB-014" vs "ZB-016"), about 1 in 8 letters in long ones.</summary>
    static int Allowed(int length) => length <= 7 ? 0 : length / 8;
}
