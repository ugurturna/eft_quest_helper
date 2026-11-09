using System.Text.Json;
using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>Where the BTR is at <see cref="TimeLeft"/> (raid seconds left): a corner of its track.</summary>
public readonly record struct BtrPoint(double X, double Z, double TimeLeft);

/// <summary>A stop on the timetable: the BTR gets there at <see cref="Arrive"/> and drives off at <see cref="Depart"/> (time left, Arrive &gt; Depart).</summary>
public sealed record BtrVisit(string StopId, double Arrive, double Depart);

public enum BtrState { NotSpawned, Moving, Stopped, Done }

/// <summary>
/// The BTR at one moment: where, what it is doing, the stop it is at (<see cref="StopId"/>, when stopped) and the next one,
/// <see cref="Eta"/> seconds away; <see cref="Wait"/> is how long it still stays at the stop.
/// </summary>
public sealed record BtrFix(BtrState State, double X, double Z, string? StopId, string? NextStopId, double? Eta, double Wait);

/// <summary>Seconds until the BTR reaches a stop (0 while it is there), and on which route.</summary>
public sealed record BtrEta(string StopId, string Name, double? Seconds, string? RouteId)
{
    public bool IsHere => Seconds == 0;
}

/// <summary>
/// One route's timetable for a raid, in raid time left: the BTR drives off its spawn at the spawn's time, drives each leg at
/// the map's speed (or arrives when it was seen to, <see cref="BtrRoute.Observed"/>), waits at each stop, then goes round again
/// until the raid ends. The same model tarkovbtr.com uses.
/// </summary>
public sealed class BtrSchedule
{
    /// <summary>Rounds worked out at most (a raid ends long before).</summary>
    const int MaxRounds = 8;

    public BtrRoute Route { get; }
    public IReadOnlyList<BtrPoint> Track { get; }
    public IReadOnlyList<BtrVisit> Visits { get; }
    /// <summary>Seconds the timetable was moved by a sighting (+ = the BTR is ahead of the data).</summary>
    public double Shifted { get; }

    BtrSchedule(BtrRoute route, IReadOnlyList<BtrPoint> track, IReadOnlyList<BtrVisit> visits, double shifted)
    {
        Route = route;
        Track = track;
        Visits = visits;
        Shifted = shifted;
    }

    public static BtrSchedule Build(BtrMap map, BtrRoute route)
    {
        var track = new List<BtrPoint>();
        var visits = new List<BtrVisit>();
        if (!map.Spawns.TryGetValue(route.Spawn, out var spawn) || route.Stops.Count == 0 || map.Speed <= 0)
            return new BtrSchedule(route, track, visits, 0);

        var (x, z, t) = (spawn.Position[0], spawn.Position[1], spawn.TimeLeft);
        track.Add(new BtrPoint(x, z, t));
        var from = route.Spawn;
        for (var round = 0; round < MaxRounds && t > 0; round++)
        {
            for (var i = 0; i < route.Stops.Count && t > 0; i++)
            {
                var id = route.Stops[i];
                if (!map.Stops.TryGetValue(id, out var stop)) continue;
                var points = Leg(map, from, id).Append(stop.Position).Select(p => (X: p[0], Z: p[1])).ToList();
                var lengths = new List<double>();
                var (px, pz) = (x, z);
                foreach (var p in points)
                {
                    lengths.Add((lengths.Count > 0 ? lengths[^1] : 0) + Math.Sqrt((p.X - px) * (p.X - px) + (p.Z - pz) * (p.Z - pz)));
                    (px, pz) = p;
                }
                var total = lengths[^1];
                var arrive = t - total / map.Speed;
                if (round == 0 && route.Observed is { } seen && i < seen.Count && seen[i] is { } observed)
                    arrive = Math.Min(observed, t);  // never before it drove off
                // the bends in between at the same pace, so an observed arrival stretches or shrinks the whole leg
                for (var k = 0; k < points.Count; k++)
                    track.Add(new BtrPoint(points[k].X, points[k].Z, total > 0 ? t - lengths[k] / total * (t - arrive) : arrive));
                var depart = arrive - map.StopSeconds;
                track.Add(new BtrPoint(stop.Position[0], stop.Position[1], depart));
                visits.Add(new BtrVisit(id, arrive, depart));
                (x, z, t) = (stop.Position[0], stop.Position[1], depart);
                from = id;
            }
            if (!route.Loop) break;
        }
        return new BtrSchedule(route, track, visits, 0);
    }

    /// <summary>The bends between two stops (or a spawn and a stop): the road drawn either way, else none (straight).</summary>
    static IEnumerable<double[]> Leg(BtrMap map, string from, string to) =>
        map.Roads.TryGetValue($"{from}>{to}", out var road) ? road
        : map.Roads.TryGetValue($"{to}>{from}", out var back) ? back.Reverse()
        : [];

    /// <summary>The same timetable <paramref name="seconds"/> later in raid time left (+ = everything happens sooner).</summary>
    public BtrSchedule Shift(double seconds) => new(
        Route,
        Track.Select(p => p with { TimeLeft = p.TimeLeft + seconds }).ToList(),
        Visits.Select(v => new BtrVisit(v.StopId, v.Arrive + seconds, v.Depart + seconds)).ToList(),
        Shifted + seconds);

    public BtrFix PositionAt(double timeLeft)
    {
        if (Track.Count == 0) return new BtrFix(BtrState.Done, 0, 0, null, null, null, 0);
        var first = Track[0];
        if (timeLeft > first.TimeLeft)
        {
            var next = Visits.FirstOrDefault();
            return new BtrFix(BtrState.NotSpawned, first.X, first.Z, null, next?.StopId, next == null ? null : timeLeft - next.Arrive, 0);
        }
        var last = Track[^1];
        if (timeLeft <= last.TimeLeft) return new BtrFix(BtrState.Done, last.X, last.Z, null, null, null, 0);

        var (x, z) = (last.X, last.Z);
        for (var k = 0; k + 1 < Track.Count; k++)
        {
            var (a, b) = (Track[k], Track[k + 1]);
            if (timeLeft > a.TimeLeft || timeLeft < b.TimeLeft) continue;
            var f = a.TimeLeft - b.TimeLeft > 0 ? (a.TimeLeft - timeLeft) / (a.TimeLeft - b.TimeLeft) : 1;
            (x, z) = (a.X + (b.X - a.X) * f, a.Z + (b.Z - a.Z) * f);
            break;
        }
        for (var i = 0; i < Visits.Count; i++)
        {
            var v = Visits[i];
            if (timeLeft <= v.Arrive && timeLeft >= v.Depart)
            {
                var after = i + 1 < Visits.Count ? Visits[i + 1] : null;
                return new BtrFix(BtrState.Stopped, x, z, v.StopId, after?.StopId, after == null ? null : timeLeft - after.Arrive, timeLeft - v.Depart);
            }
            if (v.Arrive < timeLeft)
                return new BtrFix(BtrState.Moving, x, z, null, v.StopId, timeLeft - v.Arrive, 0);
        }
        return new BtrFix(BtrState.Done, x, z, null, null, null, 0);
    }

    /// <summary>Seconds until the BTR is next at <paramref name="stopId"/>: 0 while it is there, null when it won't come again.</summary>
    public double? EtaTo(string stopId, double timeLeft)
    {
        foreach (var v in Visits)
        {
            if (v.StopId != stopId || v.Depart > timeLeft) continue;
            return v.Arrive >= timeLeft ? 0 : timeLeft - v.Arrive;
        }
        return null;
    }

    /// <summary>
    /// How far the timetable is off when the BTR was seen at <paramref name="stopId"/> at <paramref name="timeLeft"/>:
    /// 0 when it should be there then, + when it came early (by the seconds to shift by), null when this route never stops there.
    /// </summary>
    public double? Residual(string stopId, double timeLeft)
    {
        double? best = null;
        foreach (var v in Visits.Where(v => v.StopId == stopId))
        {
            var off = timeLeft > v.Arrive ? timeLeft - v.Arrive : timeLeft < v.Depart ? timeLeft - v.Depart : 0;
            if (best == null || Math.Abs(off) < Math.Abs(best.Value)) best = off;
        }
        return best;
    }
}

/// <summary>
/// The BTR on one raid's map: every route it may drive, narrowed by the stops the player saw it at (each sighting also
/// moves the timetable onto what was seen) or by the route the player picked.
/// </summary>
public sealed class BtrTracker
{
    readonly IReadOnlyList<BtrSchedule> _all;
    readonly List<(string StopId, double TimeLeft)> _sightings = [];
    string? _picked;

    public BtrTracker(BtrMap map)
    {
        Map = map;
        _all = map.Routes.Select(r => BtrSchedule.Build(map, r)).Where(s => s.Visits.Count > 0).ToList();
        Candidates = _all;
    }

    public BtrMap Map { get; }
    public IReadOnlyList<BtrSchedule> Routes => _all;
    public IReadOnlyList<(string StopId, double TimeLeft)> Sightings => _sightings;
    /// <summary>The routes the BTR may be on now (timetables moved onto the last sighting).</summary>
    public IReadOnlyList<BtrSchedule> Candidates { get; private set; }

    /// <summary>The route the player picked; null = all that fit.</summary>
    public string? Picked
    {
        get => _picked;
        set
        {
            _picked = _all.Any(s => s.Route.Id == value) ? value : null;
            Recompute();
        }
    }

    public void Seen(string stopId, double timeLeft)
    {
        _sightings.Add((stopId, timeLeft));
        Recompute();
    }

    public void Reset()
    {
        _sightings.Clear();
        Recompute();
    }

    void Recompute()
    {
        var pool = _picked == null ? _all : _all.Where(s => s.Route.Id == _picked).ToList();
        if (_sightings.Count == 0)
        {
            Candidates = pool;
            return;
        }
        var (stopId, timeLeft) = _sightings[^1];
        var fits = pool.Select(s => (s, off: s.Residual(stopId, timeLeft)))
                       .Where(x => x.off != null)
                       .Select(x => (moved: x.s.Shift(x.off!.Value), off: x.off!.Value))
                       .ToList();
        var variance = Map.VarianceSeconds;
        // a route fits when it can be at the stop within twice the spawn's spread, and the earlier sightings still agree
        var good = fits.Where(f => Math.Abs(f.off) <= 2 * variance
                                   && _sightings.SkipLast(1).All(o => f.moved.Residual(o.StopId, o.TimeLeft) is { } e && Math.Abs(e) <= variance))
                       .Select(f => f.moved).ToList();
        // nothing fits (a far-off sighting): the closest route is still the best guess; a route that never stops there keeps its timetable
        Candidates = good.Count > 0 ? good
            : fits.Count > 0 ? [fits.MinBy(f => Math.Abs(f.off)).moved]
            : pool;
    }

    /// <summary>Every stop of the map with the soonest time any candidate route gets there; stops nobody comes to again last.</summary>
    public IReadOnlyList<BtrEta> Etas(double timeLeft) => Map.Stops
        .Select(kv =>
        {
            var best = Candidates.Select(c => (c.Route.Id, eta: c.EtaTo(kv.Key, timeLeft)))
                                 .Where(x => x.eta != null)
                                 .OrderBy(x => x.eta)
                                 .FirstOrDefault();
            return new BtrEta(kv.Key, kv.Value.Name, best.eta, best.eta == null ? null : best.Id);
        })
        .OrderBy(e => e.Seconds ?? double.MaxValue)
        .ToList();

    /// <summary>The candidate route whose BTR gets somewhere first (one already at a stop wins), with where it is.</summary>
    public (BtrSchedule Schedule, BtrFix Fix)? Nearest(double timeLeft) => Candidates
        .Select(c => (Schedule: c, Fix: c.PositionAt(timeLeft)))
        .Where(x => x.Fix.State != BtrState.Done)
        .OrderBy(x => x.Fix.State == BtrState.Stopped ? 0 : x.Fix.Eta ?? double.MaxValue)
        .Select(x => ((BtrSchedule, BtrFix)?)x)
        .FirstOrDefault();
}

/// <summary>data/btr.json: the BTR's stops, spawns and routes, by the map names the quests use.</summary>
public static class BtrRepository
{
    public const string FileName = "btr.json";

    sealed class BtrData
    {
        public Dictionary<string, BtrMap> Maps { get; set; } = [];
    }

    /// <summary>Loads data/btr.json; empty when the file is missing.</summary>
    public static IReadOnlyDictionary<string, BtrMap> Load(string dataDir)
    {
        var path = Path.Combine(dataDir, FileName);
        var data = File.Exists(path)
            ? JsonSerializer.Deserialize<BtrData>(File.ReadAllText(path), QuestRepository.Json)
            : null;
        var maps = data?.Maps ?? [];
        foreach (var (name, map) in maps) map.Name = name;
        return maps;
    }
}
