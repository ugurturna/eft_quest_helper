namespace TarkovQuestTracker.Core.Models;

/// <summary>
/// tarkov.dev's 27 container types in a few groups, for the GPS settings' container filter
/// (<see cref="AppSettings.MapContainerGroups"/>). A type not listed here falls in <see cref="Other"/>.
/// </summary>
public static class MapContainerGroups
{
    public const string Other = "other";

    /// <summary>Group key → container types, in the settings' order.</summary>
    public static readonly IReadOnlyList<(string Group, string[] Types)> All =
    [
        ("safe", ["safe", "bank-safe"]),
        ("pc", ["pc-block"]),
        ("weapon", ["weapon-box", "wooden-ammo-box", "grenade-box", "wooden-crate"]),
        ("medical", ["medcase", "medbag", "medical-supply-crate"]),
        ("bags", ["jacket", "duffle-bag", "plastic-suitcase", "drawer"]),
        ("cash", ["cash-register", "bank-cash-register"]),
        ("tools", ["toolbox", "technical-supply-crate", "ration-supply-crate"]),
        ("caches", ["buried-barrel-cache", "ground-cache", "shturmans-stash"]),
        ("bodies", ["civilian-body", "dead-scav", "scav-body", "pmc-body", "lab-technician-body"]),
        (Other, []),
    ];

    static readonly Dictionary<string, string> ByType =
        All.SelectMany(g => g.Types.Select(t => (t, g.Group))).ToDictionary(x => x.t, x => x.Group);

    public static string Of(string type) => ByType.GetValueOrDefault(type, Other);

    /// <summary>What the filter starts with: where the good loot is, not every drawer and body.</summary>
    public static List<string> Defaults() => ["safe", "pc", "weapon", "medical", "caches"];
}
