namespace TarkovQuestTracker.Core.Models;

/// <summary>Player progress for one game mode. Stored as progress_{pvp|pve}.json.</summary>
public sealed class Progress
{
    public HashSet<string> Completed { get; set; } = [];
    /// <summary>Quests the player is doing right now, in the order they were started.</summary>
    public List<string> Active { get; set; } = [];
    /// <summary>Active quests shown on the in-game overlay; only the ones the player pins (activating does not track).</summary>
    public List<string> Tracked { get; set; } = [];
    /// <summary>Objective counters, key "questId/objectiveId".</summary>
    public Dictionary<string, int> Objectives { get; set; } = [];
    /// <summary>Target quests (e.g. "the-punisher-part-3", "collector").</summary>
    public List<string> Goals { get; set; } = [];
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
    /// <summary>Main window zoom (text, buttons, spacing): 0.25 to 2.</summary>
    public double UiScale { get; set; } = 1.0;
    /// <summary>Vertical position as a fraction of the free screen height (0 = top, 1 = bottom), resolution independent.</summary>
    public double OverlayPosition { get; set; } = 0.05;
    /// <summary>Horizontal position as a fraction of the free screen width (0 = left, 1 = right edge, the old docked spot).</summary>
    public double OverlayPositionX { get; set; } = 1.0;
    /// <summary>true: quest title + trader/map + objectives; false: titles only.</summary>
    public bool OverlayDetailed { get; set; } = true;
    public bool OverlayVisible { get; set; } = true;
    /// <summary>At most this many quests on the overlay (0 = all): "Any location" quests can be dozens in a raid.</summary>
    public int OverlayMaxQuests { get; set; } = 10;
    /// <summary>Objectives already done are left off the overlay.</summary>
    public bool OverlayHideDoneObjectives { get; set; } = true;
    /// <summary>The overlay stays hidden and shows for <see cref="OverlayAutoHideSeconds"/> when "O" is pressed in game.</summary>
    public bool OverlayAutoHide { get; set; }
    public int OverlayAutoHideSeconds { get; set; } = 8;
    public string ClickThroughHotkey { get; set; } = "Ctrl+Shift+T";
    public string ToggleOverlayHotkey { get; set; } = "Ctrl+Shift+H";
    /// <summary>Opens the flea market price search in the middle of the screen.</summary>
    public string FleaSearchHotkey { get; set; } = "Ctrl+Shift+F";
    /// <summary>Opens the map list on the overlay to start a raid ("which map am I going to").</summary>
    public string RaidHotkey { get; set; } = "Ctrl+Shift+R";
    /// <summary>Reads the quest names on the in-game Tasks screen once (screen import).</summary>
    public string ScanHotkey { get; set; } = "Ctrl+Shift+I";
    /// <summary>Flea price search window opacity (lower = more of the game shows through).</summary>
    public double FleaOpacity { get; set; } = 0.9;
    /// <summary>Start the raid by itself when the in-game exfil list (double "O") shows a map's exfils.</summary>
    public bool AutoRaidDetect { get; set; } = true;
    /// <summary>Look for a newer release on GitHub when the app starts.</summary>
    public bool CheckForUpdates { get; set; } = true;
    /// <summary>Puts the player on the map window from the position in the game's screenshot file names.</summary>
    public bool MapTracking { get; set; } = true;
    /// <summary>The game's screenshot folder; empty = Documents\Escape from Tarkov\Screenshots.</summary>
    public string ScreenshotsFolder { get; set; } = "";
    /// <summary>A new screenshot (whatever key the player took it with) brings up the map window.</summary>
    public bool MapOpenOnScreenshot { get; set; } = true;
    public double MapOpacity { get; set; } = 0.95;
    public bool MapShowExtracts { get; set; } = true;
    public bool MapShowQuests { get; set; } = true;
    /// <summary>Map window place as fractions of the free space across all monitors (0.5 = middle), like the overlay's.</summary>
    public double MapPositionX { get; set; } = 0.5;
    public double MapPositionY { get; set; } = 0.5;
    /// <summary>Map window size in screen units; 0 = its default size.</summary>
    public double MapWindowWidth { get; set; }
    public double MapWindowHeight { get; set; }
    /// <summary>
    /// Whether each window shows in streams, recordings and screenshots (off = left out of screen pictures; the player
    /// still sees it). The overlay and the map are left out by default: the auto raid detection reads the screen
    /// corner they may cover, and streamers rarely want their quests on stream.
    /// </summary>
    public bool OverlayShowInCapture { get; set; }
    public bool FleaShowInCapture { get; set; } = true;
    public bool MapShowInCapture { get; set; }
}
