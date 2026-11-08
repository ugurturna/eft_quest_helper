using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>The Active Quests page's search box and map choice.</summary>
public static class ActiveQuestFilter
{
    /// <summary>
    /// <paramref name="map"/>: empty = every map; <see cref="QuestPlanner.AnyMap"/> = the quests with no map of their
    /// own (done anywhere); a map = the quests with work on that very map. <paramref name="search"/>: every word in
    /// the name, the trader or an objective (any language, accents and case ignored).
    /// </summary>
    public static bool Matches(Quest q, string? map, string? search) => OnMap(q, map) && HasWords(q, search);

    static bool OnMap(Quest q, string? map) =>
        string.IsNullOrEmpty(map) ? true
        : map == QuestPlanner.AnyMap ? q.Map.All(m => m == QuestPlanner.AnyMap) && q.Objectives.All(o => o.AllMaps.Count == 0)
        : RaidFilter.IsMapSpecific(q, map);

    static bool HasWords(Quest q, string? search)
    {
        var words = SearchService.Normalize(search ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return true;
        var text = SearchService.Normalize(string.Join(' ',
            new[] { q.DisplayName, q.Name, q.Trader }.Concat(q.Objectives.Select(o => o.DisplayText + " " + o.Text))));
        return words.All(text.Contains);
    }
}
