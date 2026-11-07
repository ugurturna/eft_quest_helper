using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>What the overlay shows while the player is in a raid on one map.</summary>
public static class RaidFilter
{
    /// <summary>Picker order: the big maps first, then the small / new ones.</summary>
    static readonly string[] Order =
    [
        "Customs", "Woods", "Shoreline", "Interchange", "Reserve", "Lighthouse", "Streets of Tarkov",
        "Factory", "Ground Zero", "The Lab", "The Labyrinth", "Icebreaker", "Terminal",
    ];

    /// <summary>The quest can be worked on there (its maps or an objective's); quests without a map count on every map.</summary>
    public static bool IsOnMap(Quest q, string map) =>
        q.Map.Count == 0 || q.Map.Contains(QuestPlanner.AnyMap) || q.Map.Contains(map)
        || q.Objectives.Any(o => o.AllMaps.Contains(map));

    /// <summary>
    /// An objective tied to maps shows only on those ("Eliminate 2 PMCs on Factory" of a multi-map quest);
    /// one without a map shows everywhere, except handing items to the trader, which is not done in raid.
    /// </summary>
    public static bool ShowInRaid(Objective o, string map) =>
        o.AllMaps.Count > 0
            ? o.AllMaps.Contains(map)
            : !o.Text.StartsWith("Hand over", StringComparison.OrdinalIgnoreCase);

    /// <summary>The objectives of <paramref name="q"/> to show on <paramref name="map"/>; empty when the quest has nothing to do there.</summary>
    public static IReadOnlyList<Objective> Objectives(Quest q, string map) =>
        IsOnMap(q, map) ? q.Objectives.Where(o => ShowInRaid(o, map)).ToList() : [];

    /// <summary>Every real map the quests use (not "Any"), in picker order.</summary>
    public static IReadOnlyList<string> Maps(IEnumerable<Quest> quests) =>
        quests.SelectMany(q => q.Map).Where(m => m != QuestPlanner.AnyMap).Distinct()
              .OrderBy(m => Array.IndexOf(Order, m) is var i and >= 0 ? i : Order.Length).ThenBy(m => m)
              .ToList();
}
