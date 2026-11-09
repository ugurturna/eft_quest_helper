using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>The BTR timetable (raid time left), its position along the track, sightings narrowing the routes, and data/btr.json.</summary>
public sealed class BtrScheduleTests
{
    /// <summary>
    /// A straight line: spawn at x=0 drives off at 1000 s left, stop A at x=100, stop B at x=300; 10 m/s, 50 s at a stop.
    /// A: arrive 990, leave 940. B: arrive 920, leave 870. Back to A (200 m): arrive 850, leave 800 …
    /// </summary>
    static BtrMap Line(List<double?>? observed = null) => new()
    {
        StopSeconds = 50,
        Speed = 10,
        VarianceSeconds = 30,
        Stops =
        {
            ["a"] = new BtrStop { Name = "A", Position = [100, 0] },
            ["b"] = new BtrStop { Name = "B", Position = [300, 0] },
        },
        Spawns = { ["s"] = new BtrSpawn { TimeLeft = 1000, Position = [0, 0] } },
        Routes = { new BtrRoute { Id = "R1", Spawn = "s", Stops = ["a", "b"], Observed = observed } },
    };

    [Fact]
    public void Timetable_drives_each_leg_at_the_speed_and_waits_at_stops()
    {
        var map = Line();
        var s = BtrSchedule.Build(map, map.Routes[0]);
        Assert.Equal(new BtrVisit("a", 990, 940), s.Visits[0]);
        Assert.Equal(new BtrVisit("b", 920, 870), s.Visits[1]);
        Assert.Equal(new BtrVisit("a", 850, 800), s.Visits[2]);  // round again
        Assert.True(s.Visits[^1].Arrive > 0);
        Assert.True(s.Visits[^1].Depart <= 0 || s.Visits.Count > 20);
    }

    [Fact]
    public void Observed_arrivals_win_over_the_speed()
    {
        var map = Line([null, 900]);
        var s = BtrSchedule.Build(map, map.Routes[0]);
        Assert.Equal(990, s.Visits[0].Arrive);
        Assert.Equal(900, s.Visits[1].Arrive);
        Assert.Equal(850, s.Visits[1].Depart);
    }

    [Fact]
    public void Roads_bend_the_leg_and_count_its_length()
    {
        var map = Line();
        map.Roads["a>b"] = [[200, 100]];  // 141 + 141 m instead of 200
        var s = BtrSchedule.Build(map, map.Routes[0]);
        Assert.Equal(940 - Math.Sqrt(2) * 200 / 10, s.Visits[1].Arrive, 6);
        // the way back uses the same road reversed
        Assert.Contains(s.Track, p => p is { X: 200, Z: 100 } && p.TimeLeft < s.Visits[1].Depart);
    }

    [Fact]
    public void Position_before_spawn_at_a_stop_and_between_stops()
    {
        var map = Line();
        var s = BtrSchedule.Build(map, map.Routes[0]);

        var waiting = s.PositionAt(1100);
        Assert.Equal(BtrState.NotSpawned, waiting.State);
        Assert.Equal("a", waiting.NextStopId);
        Assert.Equal(110, waiting.Eta);

        var halfway = s.PositionAt(995);  // 5 s of the 10 s leg to A
        Assert.Equal(BtrState.Moving, halfway.State);
        Assert.Equal(50, halfway.X, 6);
        Assert.Equal("a", halfway.NextStopId);
        Assert.Equal(5, halfway.Eta);

        var atA = s.PositionAt(960);
        Assert.Equal(BtrState.Stopped, atA.State);
        Assert.Equal("a", atA.StopId);
        Assert.Equal(100, atA.X, 6);
        Assert.Equal(20, atA.Wait);
        Assert.Equal("b", atA.NextStopId);
        Assert.Equal(40, atA.Eta);

        var toB = s.PositionAt(930);  // 10 s after leaving A at 10 m/s
        Assert.Equal(200, toB.X, 6);
        Assert.Equal("b", toB.NextStopId);
        Assert.Equal(10, toB.Eta);
    }

    [Fact]
    public void Eta_to_a_stop_is_zero_while_there_and_the_next_visit_after()
    {
        var map = Line();
        var s = BtrSchedule.Build(map, map.Routes[0]);
        Assert.Equal(0, s.EtaTo("a", 960));
        Assert.Equal(930 - 850, s.EtaTo("a", 930));  // left A: the next round
        Assert.Equal(10, s.EtaTo("b", 930));
        Assert.Null(s.EtaTo("nowhere", 930));
    }

    [Fact]
    public void A_sighting_moves_the_timetable_onto_it()
    {
        var map = Line();
        var tracker = new BtrTracker(map);
        tracker.Seen("a", 1010);  // there 20 s before the data says it arrives
        var moved = Assert.Single(tracker.Candidates);
        Assert.Equal(20, moved.Shifted);
        Assert.Equal(new BtrVisit("a", 1010, 960), moved.Visits[0]);
        Assert.Equal(BtrState.Stopped, moved.PositionAt(1000).State);
    }

    [Fact]
    public void Sightings_and_picks_narrow_the_routes()
    {
        var map = Line();
        map.Stops["c"] = new BtrStop { Name = "C", Position = [-100, 0] };
        map.Routes.Add(new BtrRoute { Id = "R2", Spawn = "s", Stops = ["c", "a"] });
        var tracker = new BtrTracker(map);
        Assert.Equal(2, tracker.Candidates.Count);

        tracker.Seen("b", 920);  // only R1 stops at B
        Assert.Equal("R1", Assert.Single(tracker.Candidates).Route.Id);

        tracker.Reset();
        Assert.Equal(2, tracker.Candidates.Count);
        tracker.Picked = "R2";
        Assert.Equal("R2", Assert.Single(tracker.Candidates).Route.Id);
        tracker.Picked = "nope";
        Assert.Null(tracker.Picked);
    }

    [Fact]
    public void Etas_list_the_soonest_route_per_stop_first()
    {
        var map = Line();
        var etas = new BtrTracker(map).Etas(995);
        Assert.Equal(["a", "b"], etas.Select(e => e.StopId));
        Assert.Equal(5, etas[0].Seconds);
        Assert.Equal("R1", etas[0].RouteId);
        Assert.Equal(75, etas[1].Seconds);
    }

    [Fact]
    public void Unproject_undoes_project()
    {
        var map = new MapGeometry { Transform = [0.1855, 112.95, 0.1855, 167.85], CoordinateRotation = 180 };
        var (x, z) = MapProjection.Unproject(map, MapProjection.Project(map, -123.4, 456.7));
        Assert.Equal(-123.4, x, 6);
        Assert.Equal(456.7, z, 6);
    }

    [Fact]
    public void Real_data_routes_are_whole()
    {
        var dataDir = QuestRepository.FindDataDir(AppContext.BaseDirectory);
        var btr = BtrRepository.Load(dataDir);
        var maps = MapRepository.Load(dataDir);
        Assert.Equal(["Lighthouse", "Streets of Tarkov", "Woods"], btr.Keys.Order());
        foreach (var (name, map) in btr)
        {
            Assert.True(maps.ContainsKey(name), name);
            Assert.True(map.Speed > 0 && map.StopSeconds > 0, name);
            Assert.NotEmpty(map.Routes);
            foreach (var route in map.Routes)
            {
                Assert.True(map.Spawns.ContainsKey(route.Spawn), $"{name} {route.Id} spawn {route.Spawn}");
                Assert.All(route.Stops, s => Assert.True(map.Stops.ContainsKey(s), $"{name} {route.Id} stop {s}"));
                var schedule = BtrSchedule.Build(map, route);
                Assert.True(schedule.Visits.Count >= route.Stops.Count, $"{name} {route.Id}");
                Assert.True(schedule.Visits.Zip(schedule.Visits.Skip(1)).All(p => p.First.Depart >= p.Second.Arrive), $"{name} {route.Id} order");
            }
            foreach (var key in map.Roads.Keys)
                Assert.All(key.Split('>'), id => Assert.True(map.Stops.ContainsKey(id) || map.Spawns.ContainsKey(id), $"{name} road {key}"));
        }
    }
}
