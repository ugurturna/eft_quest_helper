using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

public sealed record GoalPlan(
    Quest Goal,
    IReadOnlyList<Quest> Chain,          // every prerequisite + the goal, in doable order
    IReadOnlyList<Quest> Remaining,      // chain minus completed
    IReadOnlyList<Quest> AvailableNow);  // remaining quests whose prerequisites are all done

/// <summary>Prerequisite graph queries over a loaded <see cref="QuestRepository"/>.</summary>
public sealed class QuestPlanner
{
    public const string CollectorId = "collector";

    readonly QuestRepository _repo;
    readonly Dictionary<string, List<string>> _dependents = [];

    public QuestPlanner(QuestRepository repo)
    {
        _repo = repo;
        foreach (var q in repo.Quests)
            foreach (var p in q.Prerequisites)
                (_dependents.TryGetValue(p, out var list) ? list : _dependents[p] = []).Add(q.Id);
    }

    public Quest? Get(string id) => _repo.ById.GetValueOrDefault(id);

    public HashSet<string> Ancestors(string id) => Walk(id, x => Get(x)?.Prerequisites ?? []);

    public HashSet<string> Descendants(string id) => Walk(id, x => _dependents.GetValueOrDefault(x) ?? []);

    public IReadOnlyList<Quest> Dependents(string id) =>
        (_dependents.GetValueOrDefault(id) ?? []).Select(x => _repo.ById[x]).ToList();

    static HashSet<string> Walk(string start, Func<string, IEnumerable<string>> next)
    {
        var seen = new HashSet<string>();
        var stack = new Stack<string>([start]);
        while (stack.Count > 0)
            foreach (var n in next(stack.Pop()))
                if (seen.Add(n)) stack.Push(n);
        return seen;
    }

    /// <summary>Kahn topological order inside <paramref name="ids"/>; ties by level, trader, name.</summary>
    public List<Quest> TopoSort(IEnumerable<string> ids)
    {
        var set = ids.Where(_repo.ById.ContainsKey).ToHashSet();
        var indegree = set.ToDictionary(id => id, id => _repo.ById[id].Prerequisites.Count(set.Contains));
        var ready = new PriorityQueue<Quest, (int, string, string)>();
        foreach (var (id, deg) in indegree)
            if (deg == 0) Enqueue(id);

        var result = new List<Quest>();
        while (ready.TryDequeue(out var q, out _))
        {
            result.Add(q);
            foreach (var d in _dependents.GetValueOrDefault(q.Id) ?? [])
                if (set.Contains(d) && --indegree[d] == 0) Enqueue(d);
        }
        return result;

        void Enqueue(string id)
        {
            var q = _repo.ById[id];
            ready.Enqueue(q, (q.MinLevel ?? 0, q.Trader, q.Name));
        }
    }

    public bool IsAvailable(Quest q, Progress p, bool ignoreLevel = false) =>
        !p.Completed.Contains(q.Id)
        && q.Prerequisites.All(p.Completed.Contains)
        && (ignoreLevel || (q.MinLevel ?? 0) <= p.PlayerLevel);

    public bool IsLevelLocked(Quest q, Progress p) => (q.MinLevel ?? 0) > p.PlayerLevel;

    public GoalPlan PlanFor(string goalId, Progress p)
    {
        var goal = _repo.ById[goalId];
        var chain = TopoSort(Ancestors(goalId).Append(goalId));
        var remaining = chain.Where(q => !p.Completed.Contains(q.Id)).ToList();
        var available = remaining.Where(q => IsAvailable(q, p, ignoreLevel: true)).ToList();
        return new GoalPlan(goal, chain, remaining, available);
    }

    /// <summary>Collector + every kappa-required quest + all their prerequisites.</summary>
    public HashSet<string> KappaSet()
    {
        var roots = _repo.Quests.Where(q => q.IsKappa).Select(q => q.Id).ToList();
        if (_repo.ById.ContainsKey(CollectorId)) roots.Add(CollectorId);
        var set = new HashSet<string>(roots);
        foreach (var r in roots) set.UnionWith(Ancestors(r));
        return set;
    }

    public GoalPlan KappaPlan(Progress p)
    {
        var chain = TopoSort(KappaSet());
        var remaining = chain.Where(q => !p.Completed.Contains(q.Id)).ToList();
        var available = remaining.Where(q => IsAvailable(q, p, ignoreLevel: true)).ToList();
        var goal = Get(CollectorId) ?? chain.LastOrDefault() ?? new Quest { Id = CollectorId, Name = "Collector" };
        return new GoalPlan(goal, chain, remaining, available);
    }

    /// <summary>Marks the quest done; a quest can only be done if its prerequisites are, so they are marked too.</summary>
    public void Complete(Progress p, string id)
    {
        foreach (var x in Ancestors(id).Append(id))
        {
            p.Completed.Add(x);
            p.Active.Remove(x);
        }
        p.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Puts the quest in the active list. Being on a quest means its whole prerequisite chain is done,
    /// so those are marked completed. Returns the quests that were newly marked.
    /// </summary>
    public IReadOnlyList<Quest> Start(Progress p, string id)
    {
        var newlyDone = TopoSort(Ancestors(id).Where(a => !p.Completed.Contains(a)));
        foreach (var q in newlyDone)
        {
            p.Completed.Add(q.Id);
            p.Active.Remove(q.Id);
        }
        p.Completed.Remove(id);
        if (!p.Active.Contains(id)) p.Active.Add(id);
        p.UpdatedAt = DateTime.UtcNow;
        return newlyDone;
    }

    /// <summary>Un-marks the quest; anything that depends on it cannot be done either.</summary>
    public void Uncomplete(Progress p, string id)
    {
        foreach (var x in Descendants(id).Append(id)) p.Completed.Remove(x);
        p.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Quests that become available once <paramref name="id"/> is completed.</summary>
    public IReadOnlyList<Quest> UnlockedBy(string id, Progress p) =>
        Dependents(id).Where(q => IsAvailable(q, p, ignoreLevel: true)).ToList();

    public const string AnyMap = "Any";

    /// <summary>Groups by map; quests without a specific map go under <see cref="AnyMap"/>.</summary>
    public static IReadOnlyList<IGrouping<string, Quest>> GroupByMap(IEnumerable<Quest> quests) =>
        quests.SelectMany(q => (q.Map.Count == 0 ? [AnyMap] : q.Map)
                  .Select(m => (m, q)))
              .GroupBy(x => x.m, x => x.q)
              .OrderByDescending(g => g.Count())
              .ToList();
}
