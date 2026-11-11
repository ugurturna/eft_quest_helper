using System.Text.Json;
using System.Text.RegularExpressions;
using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>
/// The story page: each chapter's objectives with the walkthrough parts that explain them, and the choices made
/// inside the chapters (data/story_choices.json).
/// </summary>
public sealed partial class StoryPlanner(StoryChoices choices)
{
    public StoryChoices Choices { get; } = choices;

    /// <summary>Loads data/story_choices.json; no choices when the file is missing.</summary>
    public static StoryPlanner Load(string dataDir)
    {
        var path = Path.Combine(dataDir, StoryChoices.FileName);
        var data = File.Exists(path)
            ? JsonSerializer.Deserialize<StoryChoices>(File.ReadAllText(path), QuestRepository.Json)
            : null;
        return new StoryPlanner(data ?? new StoryChoices());
    }

    /// <summary>The story chapters in the page's order; chapters missing from the order go last, by name.</summary>
    public IReadOnlyList<Quest> Chapters(IEnumerable<Quest> quests)
    {
        var story = quests.Where(q => q.Category == "story").ToList();
        return story.OrderBy(q => Choices.Order.IndexOf(q.Id) is var i and >= 0 ? i : int.MaxValue)
                    .ThenBy(q => q.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
    }

    public IEnumerable<StoryDecision> DecisionsOf(string chapterId) => Choices.Decisions.Where(d => d.Chapter == chapterId);

    /// <summary>
    /// The chapter's objectives, each with the walkthrough sections about it. The walkthrough follows the objectives'
    /// order, so each section goes to the next objective its heading names (looking a few ahead) and the parts in
    /// between (a map's spot, the rewards) stay with the objective before them. The choice parts are left out:
    /// the page shows them as choices.
    /// </summary>
    public static IReadOnlyList<StoryStep> Steps(Quest chapter)
    {
        var objectives = chapter.Objectives;
        var help = objectives.Select(_ => new List<GuideSection>()).ToList();
        if (objectives.Count == 0) return [];
        var at = 0;
        foreach (var section in Sections(chapter.DisplayGuide.Length > 0 ? chapter.DisplayGuide : chapter.Guide))
        {
            var words = Words(section.Title);
            var best = -1;
            var bestScore = 0.0;
            for (var j = at; j < Math.Min(objectives.Count, at + 6); j++)
            {
                var score = Overlap(words, Words(Text(objectives[j])));
                if (score > bestScore + 1e-9) (best, bestScore) = (j, score);
            }
            if (best >= 0 && bestScore >= 0.6) at = best;
            help[at].Add(section);
        }
        return objectives.Select((o, i) => new StoryStep(o, help[i])).ToList();
    }

    static string Text(Objective o) => o.DisplayText.Length > 0 ? o.DisplayText : o.Text;

    /// <summary>The walkthrough's "▸ heading" parts, without the choice parts ("Decision: …" and the rewards under it).</summary>
    public static IReadOnlyList<GuideSection> Sections(string guide)
    {
        var sections = new List<GuideSection>();
        string? title = null;
        var body = new List<string>();
        var skipRewards = false;
        void Flush()
        {
            if (title == null) return;
            var isDecision = DecisionTitle().IsMatch(title);
            var isRewards = RewardsTitle().IsMatch(title);
            if (isDecision) skipRewards = true;
            else if (isRewards && skipRewards) { }  // the rewards of a choice: on the choice
            else
            {
                skipRewards = false;
                sections.Add(new GuideSection(title, string.Join("\n", body).Trim()));
            }
            body.Clear();
        }
        foreach (var line in guide.Split('\n'))
        {
            if (line.StartsWith('▸'))
            {
                Flush();
                title = line.TrimStart('▸').Trim();
            }
            else if (title != null) body.Add(line.TrimEnd());
        }
        Flush();
        return sections;
    }

    [GeneratedRegex(@"^(Decision|Karar)\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex DecisionTitle();

    [GeneratedRegex(@"^(Rewards|Ödüller)$", RegexOptions.IgnoreCase)]
    private static partial Regex RewardsTitle();

    static readonly HashSet<string> Small =
    [
        "the", "a", "an", "to", "of", "for", "in", "on", "at", "with", "and", "or", "from", "your", "you", "any", "one",
        "ve", "ile", "ya", "da", "de", "bir", "için", "olan",
    ];

    static HashSet<string> Words(string text) =>
        WordPattern().Matches(text.ToLowerInvariant()).Select(m => m.Value).Where(w => !Small.Contains(w)).ToHashSet();

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex WordPattern();

    /// <summary>Shared words over the shorter text's words (a heading may say more or less than the objective).</summary>
    static double Overlap(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0;
        return a.Count(b.Contains) / (double)Math.Min(a.Count, b.Count);
    }
}
