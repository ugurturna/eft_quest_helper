using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>The optional map layers in data/maps.json: spawns, bosses, locks with key names, containers, loose loot, hazards.</summary>
public sealed class MapLayersTests
{
    static readonly IReadOnlyDictionary<string, MapGeometry> Maps =
        MapRepository.Load(QuestRepository.FindDataDir(AppContext.BaseDirectory));

    [Fact]
    public void Customs_has_every_layer()
    {
        var customs = Maps["Customs"];
        Assert.Contains(customs.Spawns, s => s.Kind == "pmc");
        Assert.Contains(customs.Spawns, s => s.Kind == "scav");
        Assert.NotEmpty(customs.Locks);
        Assert.NotEmpty(customs.Containers);
        Assert.NotEmpty(customs.Loose);
        Assert.All(customs.Locks, l => Assert.Equal(3, l.Position.Length));
    }

    [Fact]
    public void Bosses_have_a_name_a_chance_and_spots()
    {
        var reshala = Assert.Single(Maps["Customs"].Bosses, b => b.Name == "bossBully");
        Assert.Equal("Reshala", Maps["Customs"].Names.Of(reshala.Name, "en"));
        Assert.InRange(reshala.Chance, 0.01, 1);
        Assert.True(reshala.Escorts > 0);
        Assert.NotEmpty(reshala.Positions);
    }

    [Fact]
    public void Keys_are_named_in_both_languages()
    {
        var customs = Maps["Customs"];
        var names = customs.Names;
        Assert.All(customs.Locks, l => Assert.NotEqual(l.Key, names.Of(l.Key, "en")));
        // the military checkpoint key: English and Turkish names differ
        Assert.Equal("Military checkpoint key", names.Of("5913915886f774123603c392", "en"));
        Assert.NotEqual(names.Of("5913915886f774123603c392", "en"), names.Of("5913915886f774123603c392", "tr"));
    }

    [Fact]
    public void Loose_loot_points_into_the_item_list()
    {
        foreach (var map in Maps.Values)
            Assert.All(map.Loose, spot => Assert.All(spot.Items, i => Assert.InRange(i, 0, map.Names.Items.Count - 1)));
        var names = Maps["Customs"].Names;
        Assert.All(names.Items, id => Assert.NotEqual(id, names.Of(id, "en")));
    }

    [Fact]
    public void Hazards_are_outlines()
    {
        var lighthouse = Maps["Lighthouse"];
        Assert.Contains(lighthouse.Hazards, h => h.Type == "minefield");
        Assert.All(lighthouse.Hazards, h => Assert.True(h.Outline.Count >= 3 && h.Outline.All(p => p.Length == 2)));
    }

    [Fact]
    public void Every_container_type_has_a_group()
    {
        var types = Maps.Values.SelectMany(m => m.Containers).Select(c => c.Type).Distinct().ToList();
        Assert.NotEmpty(types);
        Assert.All(types, t => Assert.NotEqual(MapContainerGroups.Other, MapContainerGroups.Of(t)));
        Assert.All(MapContainerGroups.Defaults(), g => Assert.Contains(MapContainerGroups.All, x => x.Group == g));
    }

    [Fact]
    public void Names_fall_back_to_english_then_the_id()
    {
        var names = new MapNames { Table = new() { ["a"] = ["Apple"], ["b"] = ["Bread", "Ekmek"] } };
        Assert.Equal("Apple", names.Of("a", "tr"));
        Assert.Equal("Ekmek", names.Of("b", "tr"));
        Assert.Equal("Bread", names.Of("b", "en"));
        Assert.Equal("zzz", names.Of("zzz", "tr"));
    }
}
