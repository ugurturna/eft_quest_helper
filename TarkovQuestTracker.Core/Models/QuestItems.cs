namespace TarkovQuestTracker.Core.Models;

/// <summary>data/quest_items.json, written by tools/scraper/fetch_quest_items.py.</summary>
public sealed class QuestItemData
{
    public Dictionary<string, ItemInfo> Items { get; set; } = [];
    /// <summary>mode ("pvp"/"pve") → quest id → hand-over / plant objectives.</summary>
    public Dictionary<string, Dictionary<string, List<ItemObjective>>> Modes { get; set; } = [];
}

public sealed class ItemInfo
{
    /// <summary>language → name</summary>
    public Dictionary<string, string> Name { get; set; } = [];
    public Dictionary<string, string> Short { get; set; } = [];
}

public sealed class ItemObjective
{
    /// <summary>"giveItem", "plantItem" or "findItem" (find only labels its objective).</summary>
    public string Type { get; set; } = "";
    /// <summary>Id of our matching <see cref="Models.Objective"/>, when the texts could be matched.</summary>
    public string? Objective { get; set; }
    /// <summary>Any one of these counts; usually a single item.</summary>
    public List<string> Items { get; set; } = [];
    public int Count { get; set; } = 1;
    public bool Fir { get; set; }
}

/// <summary>One item a quest needs, resolved for the UI language.</summary>
public sealed record ItemNeed(string ItemId, string Name, string ShortName, int Count, bool Fir,
                              IReadOnlyList<string> Alternatives, string? IconPath)
{
    public string CountText => $"×{Count}";
    public string MoreText => Alternatives.Count > 0 ? $"+{Alternatives.Count}" : "";
    public string ToolTip => string.Join("\n", new[] { Name }.Concat(Alternatives.Take(12))
        .Append(Alternatives.Count > 12 ? "…" : null).OfType<string>());
}
