using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>Game coordinates → map picture (tarkov.dev's math) and floors, against data/maps.json.</summary>
public sealed class MapProjectionTests
{
    static readonly IReadOnlyDictionary<string, MapGeometry> Maps =
        MapRepository.Load(QuestRepository.FindDataDir(AppContext.BaseDirectory));

    [Fact]
    public void Every_raid_map_can_be_drawn()
    {
        foreach (var name in new[] { "Customs", "Woods", "Shoreline", "Interchange", "Reserve", "Lighthouse",
                                     "Streets of Tarkov", "Factory", "Ground Zero", "The Lab", "Icebreaker" })
        {
            Assert.True(Maps.TryGetValue(name, out var map), name);
            Assert.Equal(name, map.Name);
            Assert.True(map.SvgPath != null || map.TilePath != null, name);
            Assert.Equal(4, map.Transform.Length);
        }
    }

    [Fact]
    public void Matches_tarkov_dev_math()
    {
        // Customs: transform [0.239, 168.65, 0.239, 136.35], rotation 180 → (x, z) turns into (-x, -z)
        var customs = new MapGeometry { Transform = [0.239, 168.65, 0.239, 136.35], CoordinateRotation = 180 };
        var p = MapProjection.Project(customs, 100, 50);
        Assert.Equal(0.239 * -100 + 168.65, p.X, 6);
        Assert.Equal(-0.239 * -50 + 136.35, p.Y, 6);

        // rotation 90: (x, z) → (-z, x)
        var quarter = new MapGeometry { Transform = [2, 10, 2, 20], CoordinateRotation = 90 };
        var q = MapProjection.Project(quarter, 3, 4);
        Assert.Equal(2 * -4 + 10, q.X, 6);
        Assert.Equal(-2 * 3 + 20, q.Y, 6);
    }

    [Theory]
    [InlineData("Customs")]
    [InlineData("Woods")]
    [InlineData("Shoreline")]
    [InlineData("Reserve")]
    [InlineData("Interchange")]
    [InlineData("Streets of Tarkov")]
    [InlineData("Lighthouse")]
    public void Exfils_land_on_the_picture(string name)
    {
        var map = Maps[name];
        var (left, top, right, bottom) = MapProjection.Rect(map, map.SvgBounds ?? map.Bounds);
        var margin = (right - left) * 0.05;
        Assert.NotEmpty(map.Extracts);
        foreach (var e in map.Extracts)
        {
            var p = MapProjection.Project(map, e.Position);
            Assert.InRange(p.X, left - margin, right + margin);
            Assert.InRange(p.Y, top - margin, bottom + margin);
        }
    }

    [Fact]
    public void Picks_the_floor_by_height_and_place()
    {
        var map = Maps["Customs"];
        Assert.Null(MapProjection.LayerFor(map, 200, 1, 150));               // dorms, ground floor
        Assert.Equal("2nd Floor", MapProjection.LayerFor(map, 200, 4, 150)?.Name);  // dorms, 2nd floor
        Assert.Equal("Underground", MapProjection.LayerFor(map, 336, -3, -60)?.Name);  // a cellar
        Assert.Null(MapProjection.LayerFor(map, 0, -3, 0));                  // low ground, no cellar there
    }

    [Theory]
    [InlineData(180, 0, 180)]
    [InlineData(180, 90, 270)]
    [InlineData(90, 0, 270)]   // quarter-turned maps add 180 more, like tarkov.dev
    [InlineData(270, 10, 100)]
    public void Turns_the_player_marker(double rotation, double yaw, double expected) =>
        Assert.Equal(expected, MapProjection.Heading(new MapGeometry { CoordinateRotation = rotation }, yaw), 6);
}
