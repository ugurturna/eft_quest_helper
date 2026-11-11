namespace TarkovQuestTracker.Core.Models;

/// <summary>
/// data/story_choices.json: the choices inside the story chapters (keep the case or hand it over...), what each one
/// gives and, when The Ticket guide asks the same question, which of its options it is. Texts are keyed by language.
/// </summary>
public sealed class StoryChoices
{
    public const string FileName = "story_choices.json";

    public string Source { get; set; } = "";
    /// <summary>The chapters in the order the story page lists them (quest ids).</summary>
    public List<string> Order { get; set; } = [];
    public List<StoryDecision> Decisions { get; set; } = [];
}

public sealed class StoryDecision
{
    /// <summary>Unique over all chapters (also the key in <see cref="StoryProgress.Choices"/>).</summary>
    public string Id { get; set; } = "";
    /// <summary>Quest id of the chapter.</summary>
    public string Chapter { get; set; } = "";
    /// <summary>Objective id the choice is made at.</summary>
    public string Objective { get; set; } = "";
    /// <summary>The Ticket guide's decision this is (its choice is shared), or null.</summary>
    public string? TicketDecision { get; set; }
    public Dictionary<string, string> Title { get; set; } = [];
    public Dictionary<string, string> Note { get; set; } = [];
    public List<StoryOption> Options { get; set; } = [];
}

public sealed class StoryOption
{
    public string Id { get; set; } = "";
    public Dictionary<string, string> Label { get; set; } = [];
    public Dictionary<string, string> Rewards { get; set; } = [];
    /// <summary>The option of <see cref="StoryDecision.TicketDecision"/> this one is.</summary>
    public string? TicketOption { get; set; }
}

/// <summary>The player's story choices (decision id → option id), stored with the rest of the progress.</summary>
public sealed class StoryProgress
{
    public Dictionary<string, string> Choices { get; set; } = [];
}

/// <summary>One part of a chapter's walkthrough (wiki "Guide" section): its heading and what to do.</summary>
public sealed record GuideSection(string Title, string Body);

/// <summary>A chapter objective with the walkthrough parts that explain it.</summary>
public sealed record StoryStep(Objective Objective, IReadOnlyList<GuideSection> Help);
