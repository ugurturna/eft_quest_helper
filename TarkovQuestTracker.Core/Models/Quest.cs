using System.Text.Json.Serialization;

namespace TarkovQuestTracker.Core.Models;

public enum GameMode { Pvp, Pve }

/// <summary>One record of data/tasks_en.json (structural fields + English text).</summary>
public sealed class Quest
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Trader { get; set; } = "";
    /// <summary>"side" (trader quest), "story" (story chapter) or "event".</summary>
    public string Category { get; set; } = "side";
    public string? EventName { get; set; }
    /// <summary>Events only: false when the wiki marks the event as historical (no longer doable).</summary>
    public bool? EventActive { get; set; }
    public List<string> Map { get; set; } = [];
    public int? MinLevel { get; set; }
    public string? TraderLoyalty { get; set; }
    public bool? KappaRequired { get; set; }
    public bool? LightkeeperRequired { get; set; }
    public List<string> GameModes { get; set; } = ["pvp", "pve"];
    public List<string> Prerequisites { get; set; } = [];
    public List<string> LeadsTo { get; set; } = [];
    public List<string> RequirementsText { get; set; } = [];
    public List<Objective> Objectives { get; set; } = [];
    public List<RequiredItem> RequiredItems { get; set; } = [];
    public Rewards Rewards { get; set; } = new();
    public string Guide { get; set; } = "";
    public string Notes { get; set; } = "";
    public Dictionary<string, List<string>> ModeNotes { get; set; } = [];
    public Dictionary<string, ModeOverride> ModeOverrides { get; set; } = [];
    public string WikiUrl { get; set; } = "";
    public Dictionary<string, object> Sources { get; set; } = [];
    public string? TarkovDevId { get; set; }

    // set by QuestRepository, not stored in the JSON

    /// <summary>The in-game English name, whatever the quest language (translations only find it in search).</summary>
    [JsonIgnore] public string DisplayName { get; set; } = "";
    /// <summary>Every known name (all languages) for search.</summary>
    [JsonIgnore] public List<string> AllNames { get; set; } = [];
    [JsonIgnore] public string DisplayNotes { get; set; } = "";
    [JsonIgnore] public string DisplayGuide { get; set; } = "";
    /// <summary>PvE notes / differing objectives for the active mode, shown as a warning.</summary>
    [JsonIgnore] public List<string> ModeWarnings { get; set; } = [];
    /// <summary>Items to hand over / plant (data/quest_items.json) for the active mode.</summary>
    [JsonIgnore] public List<ItemNeed> ItemNeeds { get; set; } = [];
    /// <summary>What to bring into the raid (keys, markers, items to plant, weapon, clothes), by kind.</summary>
    [JsonIgnore] public List<RaidNeedGroup> RaidNeeds { get; set; } = [];
    [JsonIgnore] public IEnumerable<ItemNeed> RaidNeedItems => RaidNeeds.SelectMany(g => g.Items);

    [JsonIgnore] public bool IsKappa => KappaRequired == true;
    [JsonIgnore] public bool IsPastEvent => Category == "event" && EventActive == false;
    [JsonIgnore] public string MapText => Map.Count == 0 ? "-" : string.Join(", ", Map);
    [JsonIgnore] public string LevelText => MinLevel is > 1 ? $"Lv {MinLevel}" : "";
    /// <summary>"LL2" (trader loyalty level the quest needs, from the wiki), empty when unknown.</summary>
    [JsonIgnore] public string LoyaltyText => string.IsNullOrEmpty(TraderLoyalty) ? "" : $"LL{TraderLoyalty}";

    public override string ToString() => DisplayName;
}

public sealed class Objective
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public int Count { get; set; } = 1;
    public bool Optional { get; set; }
    public bool Fir { get; set; }
    public string? Map { get; set; }
    /// <summary>Every map the objective counts on ("on Woods, Ground Zero, Interchange, Customs"); Map is the first.</summary>
    public List<string> Maps { get; set; } = [];
    public bool PveOnly { get; set; }
    public List<string> Details { get; set; } = [];

    [JsonIgnore] public string DisplayText { get; set; } = "";
    [JsonIgnore] public List<string> DisplayDetails { get; set; } = [];
    /// <summary>The item this objective is about (icon in the UI), from data/quest_items.json.</summary>
    [JsonIgnore] public ItemNeed? Item { get; set; }
    /// <summary><see cref="Maps"/>, or <see cref="Map"/> for data written before Maps existed.</summary>
    [JsonIgnore] public IReadOnlyList<string> AllMaps => Maps.Count > 0 ? Maps : Map is { } m ? [m] : [];
}

public sealed class RequiredItem
{
    public string Name { get; set; } = "";
    public int Count { get; set; } = 1;
    public string Requirement { get; set; } = "";
    public bool Fir { get; set; }

    public override string ToString() => $"{Count}× {Name}{(Fir ? " (FIR)" : "")} - {Requirement}";
}

public sealed class Rewards
{
    public int Xp { get; set; }
    public List<RepReward> Rep { get; set; } = [];
    public List<string> Items { get; set; } = [];
}

public sealed class RepReward
{
    public string Trader { get; set; } = "";
    public double Value { get; set; }
}

/// <summary>Fields that differ in a game mode (currently only "pve").</summary>
public sealed class ModeOverride
{
    public int? MinLevel { get; set; }
    public List<string>? Prerequisites { get; set; }
    public bool? KappaRequired { get; set; }
    public List<string>? ObjectivesTarkovDev { get; set; }
}

/// <summary>One record of data/tasks_{lang}.json for non-English languages.</summary>
public sealed class QuestTranslation
{
    public string Id { get; set; } = "";
    public string? Name { get; set; }
    public List<TranslatedObjective> Objectives { get; set; } = [];
    public string? Notes { get; set; }
    public string? Guide { get; set; }
}

public sealed class TranslatedObjective
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    /// <summary>Sub-steps, same order as <see cref="Objective.Details"/>; missing ones fall back to English.</summary>
    public List<string> Details { get; set; } = [];
}
