namespace TarkovQuestTracker.Core.Models;

/// <summary>Player progress for one game mode. Stored as progress_{pvp|pve}.json.</summary>
public sealed class Progress
{
    public HashSet<string> Completed { get; set; } = [];
    /// <summary>Quests the player is doing right now, in overlay order.</summary>
    public List<string> Active { get; set; } = [];
    /// <summary>Objective counters, key "questId/objectiveId".</summary>
    public Dictionary<string, int> Objectives { get; set; } = [];
    /// <summary>Target quests (e.g. "the-punisher-part-3", "collector").</summary>
    public List<string> Goals { get; set; } = [];
    public int PlayerLevel { get; set; } = 1;
    /// <summary>"The Ticket" endings guide: choices, target ending, ticked steps.</summary>
    public TicketProgress Ticket { get; set; } = new();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static string ObjectiveKey(string questId, string objectiveId) => $"{questId}/{objectiveId}";
}

public sealed class AppSettings
{
    /// <summary>Quest data language (tasks_{lang}.json): names, objectives, notes.</summary>
    public string Language { get; set; } = "en";
    /// <summary>App UI language (menus, buttons, messages): "tr" or "en".</summary>
    public string UiLanguage { get; set; } = "tr";
    public GameMode Mode { get; set; } = GameMode.Pvp;
    public double OverlayOpacity { get; set; } = 0.85;
    public double OverlayWidth { get; set; } = 340;
    /// <summary>Overlay zoom (text, buttons, width) so it reads well at any resolution.</summary>
    public double OverlayScale { get; set; } = 1.0;
    /// <summary>Vertical position as a fraction of the free screen height (0 = top, 1 = bottom), resolution independent.</summary>
    public double OverlayPosition { get; set; } = 0.05;
    /// <summary>true: quest title + trader/map + objectives; false: titles only.</summary>
    public bool OverlayDetailed { get; set; } = true;
    public bool OverlayVisible { get; set; } = true;
    public string ClickThroughHotkey { get; set; } = "Ctrl+Shift+T";
    public string ToggleOverlayHotkey { get; set; } = "Ctrl+Shift+H";
}
