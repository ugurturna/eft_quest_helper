namespace TarkovQuestTracker.Core.Models;

/// <summary>
/// data/ticket_endings.json: the endings of the story chapter "The Ticket", the point-of-no-return
/// decisions and every step of every route. Texts are keyed by language ("en", "tr").
/// </summary>
public sealed class TicketGuide
{
    public const string QuestId = "the-ticket";

    public string Source { get; set; } = "";
    public List<TicketEnding> Endings { get; set; } = [];
    public List<TicketDecision> Decisions { get; set; } = [];
    public List<TicketStep> Steps { get; set; } = [];
}

public sealed class TicketEnding
{
    public string Id { get; set; } = "";
    public Dictionary<string, string> Name { get; set; } = [];
    public Dictionary<string, string> Summary { get; set; } = [];
    /// <summary>Decision id → option id this ending needs.</summary>
    public Dictionary<string, string> Requires { get; set; } = [];
}

public sealed class TicketDecision
{
    public string Id { get; set; } = "";
    public Dictionary<string, string> Title { get; set; } = [];
    public List<TicketOption> Options { get; set; } = [];
}

public sealed class TicketOption
{
    public string Id { get; set; } = "";
    public Dictionary<string, string> Label { get; set; } = [];
}

public sealed class TicketStep
{
    public string Id { get; set; } = "";
    /// <summary>"step", "decision", "achievement", "optional", "warning", "note", "escape" or "ending".</summary>
    public string Kind { get; set; } = "step";
    public Dictionary<string, string> Text { get; set; } = [];
    /// <summary>Decision steps: the decision asked here.</summary>
    public string? Decision { get; set; }
    /// <summary>Only on routes with these choices (decision id → option id).</summary>
    public Dictionary<string, string> When { get; set; } = [];
    /// <summary>Only on routes that can still end in one of these endings; empty = every route.</summary>
    public List<string> Endings { get; set; } = [];
    /// <summary>"pvp" or "pve" when the step differs per game mode.</summary>
    public string? Mode { get; set; }

    /// <summary>Steps the player ticks off; warnings, notes and decisions are not.</summary>
    public bool IsCheckable => Kind is "step" or "achievement" or "optional" or "escape";
}

/// <summary>The player's Ticket choices, stored with the rest of the progress.</summary>
public sealed class TicketProgress
{
    /// <summary>Decision id → option id, for decisions already made in game.</summary>
    public Dictionary<string, string> Choices { get; set; } = [];
    /// <summary>The ending the player is going for, or null.</summary>
    public string? Target { get; set; }
    public HashSet<string> Done { get; set; } = [];
}

public static class LocalizedText
{
    /// <summary>The text in <paramref name="lang"/>, falling back to English.</summary>
    public static string In(this Dictionary<string, string> text, string lang) =>
        text.GetValueOrDefault(lang) ?? text.GetValueOrDefault("en") ?? "";
}
