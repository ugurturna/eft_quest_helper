using System.Text.Json;
using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>
/// Works out the route through "The Ticket": which endings the choices made so far still allow,
/// the steps to the target ending and the next decision when the route is still open.
/// </summary>
public sealed class TicketPlanner(TicketGuide guide)
{
    public const string FileName = "ticket_endings.json";

    public TicketGuide Guide { get; } = guide;

    /// <summary>Loads data/ticket_endings.json; an empty guide when the file is missing.</summary>
    public static TicketPlanner Load(string dataDir)
    {
        var path = Path.Combine(dataDir, FileName);
        var guide = File.Exists(path)
            ? JsonSerializer.Deserialize<TicketGuide>(File.ReadAllText(path), QuestRepository.Json)
            : null;
        return new TicketPlanner(guide ?? new TicketGuide());
    }

    public TicketEnding? Ending(string? id) => Guide.Endings.FirstOrDefault(e => e.Id == id);

    public TicketDecision? Decision(string? id) => Guide.Decisions.FirstOrDefault(d => d.Id == id);

    /// <summary>True when <paramref name="ending"/> needs a different option for a decision already in <paramref name="choices"/>.</summary>
    public static bool Conflicts(TicketEnding ending, IReadOnlyDictionary<string, string> choices) =>
        ending.Requires.Any(kv => choices.TryGetValue(kv.Key, out var chosen) && chosen != kv.Value);

    /// <summary>Endings that stay possible if <paramref name="optionId"/> is picked for <paramref name="decisionId"/>.</summary>
    public IReadOnlyList<TicketEnding> EndingsAfter(TicketProgress progress, string decisionId, string optionId)
    {
        var choices = ValidChoices(progress);
        choices[decisionId] = optionId;
        // endings that never ask this decision (Survivor at the evidence question) can't follow from it
        var decides = Guide.Endings.Any(e => e.Requires.ContainsKey(decisionId));
        return Guide.Endings.Where(e => !Conflicts(e, choices) && (!decides || e.Requires.ContainsKey(decisionId))).ToList();
    }

    public TicketRoute Plan(TicketProgress progress, GameMode mode)
    {
        var made = ValidChoices(progress);
        var target = Ending(progress.Target);
        var targetBlocked = target != null && Conflicts(target, made);

        // the target fills in the decisions not made yet
        var effective = new Dictionary<string, string>(made);
        if (target != null && !targetBlocked)
            foreach (var kv in target.Requires) effective.TryAdd(kv.Key, kv.Value);
        var possible = Guide.Endings.Where(e => !Conflicts(e, effective)).ToList();
        var possibleIds = possible.Select(e => e.Id).ToHashSet();

        var modeKey = mode == GameMode.Pve ? "pve" : "pvp";
        var steps = new List<TicketStep>();
        TicketStep? pending = null;
        foreach (var step in Guide.Steps)
        {
            if (step.Mode != null && step.Mode != modeKey) continue;
            if (step.Endings.Count > 0 && !step.Endings.Any(possibleIds.Contains)) continue;
            if (step.When.Any(kv => effective.GetValueOrDefault(kv.Key) != kv.Value)) continue;
            steps.Add(step);
            if (step.Kind == "decision" && step.Decision != null && !effective.ContainsKey(step.Decision))
            {
                pending = step;
                break;
            }
        }

        var current = steps.FirstOrDefault(s => s.IsCheckable && s.Kind != "optional" && !progress.Done.Contains(s.Id))
                      ?? pending;
        return new TicketRoute(steps, possible, made, effective, target, targetBlocked, pending, current);
    }

    /// <summary>Stored choices that still name an existing decision and option.</summary>
    Dictionary<string, string> ValidChoices(TicketProgress progress) =>
        progress.Choices.Where(kv => Decision(kv.Key)?.Options.Any(o => o.Id == kv.Value) == true)
                        .ToDictionary(kv => kv.Key, kv => kv.Value);
}

/// <param name="Steps">Steps of the route in order; ends at <paramref name="Pending"/> when a decision is still open.</param>
/// <param name="Possible">Endings still reachable with the choices made (and the target's, when it is reachable).</param>
/// <param name="Made">Decisions the player already made.</param>
/// <param name="Effective">Made choices plus the ones the target needs.</param>
/// <param name="TargetBlocked">The target needs an option the player did not pick.</param>
/// <param name="Pending">The first decision nothing decides yet.</param>
/// <param name="Current">The next step to do (or the pending decision).</param>
public sealed record TicketRoute(
    IReadOnlyList<TicketStep> Steps,
    IReadOnlyList<TicketEnding> Possible,
    IReadOnlyDictionary<string, string> Made,
    IReadOnlyDictionary<string, string> Effective,
    TicketEnding? Target,
    bool TargetBlocked,
    TicketStep? Pending,
    TicketStep? Current)
{
    /// <summary>The ending this route leads to, once only one is left.</summary>
    public TicketEnding? Ending => Possible.Count == 1 ? Possible[0] : null;
}
