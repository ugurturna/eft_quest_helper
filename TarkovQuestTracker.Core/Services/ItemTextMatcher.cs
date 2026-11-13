namespace TarkovQuestTracker.Core.Services;

/// <summary>A piece of text read off the screen and how far it is from the mouse (pixels of the read picture).</summary>
public readonly record struct ScreenText(string Text, double Distance);

/// <summary>
/// Finds the item the game names next to the mouse: the name tooltip of the hovered item (or an inspect window's
/// title), in English or the app language; the text nearest to the mouse wins. Short names are not used: every
/// inventory cell shows one (a neighbour's), and many are plain interface words ("Stash", "Sort"). A name that is
/// also some item's short name ("Bolts") may be such a cell label, so any other name beats it.
/// </summary>
public sealed class ItemTextMatcher
{
    /// <summary>Shorter keys are not compared loosely: one wrong letter makes another real name.</summary>
    const int MinFuzzyLength = 6;

    /// <summary>Lines with more words are interface text, not a name to compare loosely.</summary>
    const int MaxFuzzyWords = 6;

    /// <summary>Roubles, dollars, euros: nothing to keep or sell, and their names are labels all over the trader screens.</summary>
    static readonly HashSet<string> Currencies = ["5449016a4bdc2d6f028b456f", "5696686a4bdc2da3298b456a", "569668774bdc2da2298b4568"];

    readonly Dictionary<string, FleaItem> _byName = [];
    /// <summary>Every short name: what the inventory cells show.</summary>
    readonly HashSet<string> _shortNames = [];

    public ItemTextMatcher(IEnumerable<FleaItem> items)
    {
        // the most traded item keeps a name several share (a weapon and its presets)
        foreach (var i in items.Where(i => !Currencies.Contains(i.Id)).OrderByDescending(i => i.OfferCount))
        {
            foreach (var name in new[] { i.Name, i.EnglishName })
                if (Key(name) is { Length: > 0 } k) _byName.TryAdd(k, i);
            foreach (var name in new[] { i.ShortName, i.EnglishShortName })
                if (Key(name) is { Length: > 0 } k) _shortNames.Add(k);
        }
    }

    /// <summary>Lower case, Turkish letters folded, single spaces (see <see cref="SearchService.Normalize"/>).</summary>
    public static string Key(string text) =>
        string.Join(' ', SearchService.Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>The named item, or null when no text near the mouse is an item name.</summary>
    public FleaItem? Match(IEnumerable<ScreenText> texts)
    {
        FleaItem? best = null;
        var bestRank = (Tier: 0, Distance: double.MaxValue);
        foreach (var t in texts)
        {
            var key = Key(t.Text);
            if (key.Length < 2) continue;
            var (item, tier) = Find(key);
            if (item == null) continue;
            if (tier > bestRank.Tier || (tier == bestRank.Tier && t.Distance < bestRank.Distance))
            {
                best = item;
                bestRank = (tier, t.Distance);
            }
        }
        return best;
    }

    /// <summary>
    /// Tier 3: the whole text is a name; 2: a name inside longer text, or with a misread letter; 1: a name that may
    /// be a cell's short name.
    /// </summary>
    (FleaItem? Item, int Tier) Find(string key)
    {
        if (_byName.TryGetValue(key, out var exact)) return (exact, _shortNames.Contains(key) ? 1 : 3);
        var words = key.Split(' ');
        // the tooltip read together with something next to it: the longest run of words that is a name
        for (var length = words.Length - 1; length >= 1; length--)
            for (var start = 0; start + length <= words.Length; start++)
            {
                var part = string.Join(' ', words, start, length);
                if ((length >= 2 || part.Length >= MinFuzzyLength) && _byName.TryGetValue(part, out var inside))
                    return (inside, _shortNames.Contains(part) ? 1 : 2);
            }
        if (key.Length >= MinFuzzyLength && words.Length <= MaxFuzzyWords && Nearest(key) is { } close) return (close, 2);
        return (null, 0);
    }

    /// <summary>The full name at most one wrong letter per 8 away ("Graphlcs card"); the first letter must be right.</summary>
    FleaItem? Nearest(string key)
    {
        var allowed = Math.Max(1, key.Length / 8);
        FleaItem? best = null;
        var bestDistance = allowed + 1;
        foreach (var (name, item) in _byName)
        {
            if (name[0] != key[0] || Math.Abs(name.Length - key.Length) > allowed) continue;
            var d = SearchService.Levenshtein(name, key);
            if (d < bestDistance)
            {
                best = item;
                bestDistance = d;
            }
        }
        return best;
    }
}
