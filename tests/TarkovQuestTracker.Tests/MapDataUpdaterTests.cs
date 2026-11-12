using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>The map data fallbacks: downloaded copy, last good download, the app's own file; never no map at all.</summary>
public sealed class MapDataUpdaterTests : IDisposable
{
    static readonly string DataDir = QuestRepository.FindDataDir(AppContext.BaseDirectory);
    readonly string _cache = Path.Combine(Path.GetTempPath(), "tqt-maps-" + Guid.NewGuid().ToString("N"));

    public MapDataUpdaterTests() => Directory.CreateDirectory(_cache);

    public void Dispose()
    {
        try { Directory.Delete(_cache, true); } catch (IOException) { }
    }

    string CachedFile => Path.Combine(_cache, MapRepository.FileName);

    [Fact]
    public void The_shipped_file_is_usable()
    {
        Assert.True(MapDataUpdater.IsUsable(MapRepository.Load(DataDir)));
    }

    [Fact]
    public void No_download_uses_the_shipped_file()
    {
        var maps = MapDataUpdater.LoadBest(DataDir, _cache);
        Assert.Equal(MapRepository.Load(DataDir).Keys.Order(), maps.Keys.Order());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json {")]
    [InlineData("{\"maps\":{}}")]
    [InlineData("{\"fetchedAt\":\"2099-01-01T00:00:00+00:00\",\"maps\":{\"Customs\":{\"transform\":[1,0,1,0],\"svgPath\":\"x\"}}}")]
    public void A_broken_or_partial_download_falls_back(string cached)
    {
        File.WriteAllText(CachedFile, cached);
        var maps = MapDataUpdater.LoadBest(DataDir, _cache);
        Assert.True(MapDataUpdater.IsUsable(maps));
        Assert.NotEmpty(maps["Customs"].Spawns);
    }

    [Fact]
    public void A_newer_download_wins_and_keeps_maps_it_lacks()
    {
        var shipped = File.ReadAllText(Path.Combine(DataDir, MapRepository.FileName));
        var doc = System.Text.Json.Nodes.JsonNode.Parse(shipped)!.AsObject();
        doc["fetchedAt"] = "2099-01-01T00:00:00+00:00";
        doc["maps"]!.AsObject().Remove("Woods");
        doc["maps"]!["Customs"]!["extracts"] = new System.Text.Json.Nodes.JsonArray();
        File.WriteAllText(CachedFile, doc.ToJsonString());

        var maps = MapDataUpdater.LoadBest(DataDir, _cache);
        Assert.Empty(maps["Customs"].Extracts);   // from the download
        Assert.NotEmpty(maps["Woods"].Extracts);  // only the shipped file has it
    }

    [Fact]
    public void An_older_download_loses_to_a_newer_app()
    {
        var shipped = File.ReadAllText(Path.Combine(DataDir, MapRepository.FileName));
        var doc = System.Text.Json.Nodes.JsonNode.Parse(shipped)!.AsObject();
        doc["fetchedAt"] = "2000-01-01T00:00:00+00:00";
        doc["maps"]!["Customs"]!["extracts"] = new System.Text.Json.Nodes.JsonArray();
        File.WriteAllText(CachedFile, doc.ToJsonString());

        Assert.NotEmpty(MapDataUpdater.LoadBest(DataDir, _cache)["Customs"].Extracts);
    }

    [Fact]
    public void Null_layers_read_as_empty()
    {
        var loaded = MapRepository.Parse("{\"maps\":{\"Customs\":{\"transform\":[1,0,1,0],\"svgPath\":\"x\",\"spawns\":null,\"locks\":null}}}");
        Assert.NotNull(loaded);
        Assert.Empty(loaded.Maps["Customs"].Spawns);
        Assert.Empty(loaded.Maps["Customs"].Locks);
    }
}
