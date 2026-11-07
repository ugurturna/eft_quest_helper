using System.Text.Json;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

public class FleaMarketTests
{
    const string Items = """
        {"data":{"items":{
          "led":{"id":"led","name":"led Name","shortName":"led ShortName","iconLink":"https://assets.tarkov.dev/led-icon.webp",
                 "width":1,"height":1,"lastLowPrice":1000000,"avg24hPrice":990000,"changeLast48hPercent":-1.5,"lastOfferCount":40,
                 "types":["barter"],"sellToTrader":[{"trader":"t1","priceRUB":300000},{"trader":"t2","priceRUB":250000}]},
          "gpu":{"id":"gpu","name":"gpu Name","shortName":"gpu ShortName","width":2,"height":1,"lastLowPrice":0,"avg24hPrice":400000,
                 "lastOfferCount":0,"types":["barter"],"sellToTrader":[]},
          "dog":{"id":"dog","name":"dog Name","shortName":"dog ShortName","width":1,"height":1,"types":["quest","noFlea"]}
        }}}
        """;

    const string English = """
        {"data":{"led Name":"LEDX Skin Transilluminator","led ShortName":"LEDX",
                 "gpu Name":"Graphics card","gpu ShortName":"GPU",
                 "dog Name":"Dogtag","dog ShortName":"Tag"}}
        """;

    const string Turkish = """{"data":{"gpu Name":"Ekran kartı"}}""";
    const string Traders = """{"data":{"t1 Nickname":"Therapist","t2 Nickname":"Fence"}}""";

    static IReadOnlyList<FleaItem> Load(bool turkish = false)
    {
        using var items = JsonDocument.Parse(Items);
        using var en = JsonDocument.Parse(English);
        using var tr = JsonDocument.Parse(Turkish);
        using var traders = JsonDocument.Parse(Traders);
        return FleaMarket.Parse(items.RootElement, en.RootElement, turkish ? tr.RootElement : null, traders.RootElement);
    }

    [Fact]
    public void Parse_ResolvesNamesPricesAndBestTrader()
    {
        var led = Load().Single(i => i.Id == "led");
        Assert.Equal("LEDX Skin Transilluminator", led.Name);
        Assert.Equal("LEDX", led.ShortName);
        Assert.Equal(1000000, led.FleaPrice);
        Assert.Equal("Therapist", led.BestTrader);
        Assert.Equal(300000, led.BestTraderPrice);
        Assert.False(led.FleaBanned);
    }

    [Fact]
    public void Parse_FallsBackToAverageAndMarksBanned()
    {
        var items = Load();
        var gpu = items.Single(i => i.Id == "gpu");
        Assert.Null(gpu.LastLowPrice);
        Assert.Equal(400000, gpu.FleaPrice);
        Assert.Equal(2, gpu.Slots);
        Assert.True(items.Single(i => i.Id == "dog").FleaBanned);
    }

    [Fact]
    public void Search_NeedsThreeLetters()
    {
        Assert.Empty(FleaMarket.Search(Load(), "le"));
        Assert.Equal("led", FleaMarket.Search(Load(), "led").First().Id);
    }

    [Fact]
    public void Search_ShortNameAndWordStarts()
    {
        Assert.Equal("gpu", FleaMarket.Search(Load(), "GPU").First().Id);
        Assert.Equal("led", FleaMarket.Search(Load(), "skin trans").Single().Id);
        Assert.Equal("gpu", FleaMarket.Search(Load(), "graph").Single().Id);
    }

    [Fact]
    public void Search_TurkishAndEnglishNames()
    {
        var items = Load(turkish: true);
        Assert.Equal("Ekran kartı", items.Single(i => i.Id == "gpu").Name);
        Assert.Equal("gpu", FleaMarket.Search(items, "ekran karti").Single().Id);
        Assert.Equal("gpu", FleaMarket.Search(items, "graphics").Single().Id);
    }

    /// <summary>A FleaMarket folder in temp with the PvE dumps already "downloaded" at <paramref name="updatedAt"/>.</summary>
    static FleaMarket LocalMarket(DateTime? updatedAt, out string folder)
    {
        folder = Path.Combine(Path.GetTempPath(), "tqt-flea-" + Guid.NewGuid().ToString("N"));
        var pve = Path.Combine(folder, "pve");
        Directory.CreateDirectory(pve);
        File.WriteAllText(Path.Combine(pve, "items.json"), Items);
        File.WriteAllText(Path.Combine(pve, "items_en.json"), English);
        File.WriteAllText(Path.Combine(pve, "items_tr.json"), Turkish);
        File.WriteAllText(Path.Combine(pve, "traders_tr.json"), Traders);
        if (updatedAt is { } at)
            File.WriteAllText(Path.Combine(pve, "meta.json"), JsonSerializer.Serialize(new { UpdatedAt = at }));
        return new FleaMarket(folder);
    }

    [Fact]
    public async Task LoadAsync_ReadsTheLocalFilesOfTheMode()
    {
        var market = LocalMarket(DateTime.UtcNow, out var folder);
        try
        {
            var pve = await market.LoadAsync(GameMode.Pve, "tr");
            Assert.NotNull(pve);
            Assert.Equal("Ekran kartı", pve!.Single(i => i.Id == "gpu").Name);
            Assert.Null(await market.LoadAsync(GameMode.Pvp, "tr"));  // PvP never downloaded
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void IsStale_AfterFifteenMinutesOrWithoutTimestamp()
    {
        var fresh = LocalMarket(DateTime.UtcNow.AddMinutes(-5), out var f1);
        var old = LocalMarket(DateTime.UtcNow.AddMinutes(-16), out var f2);
        var none = LocalMarket(null, out var f3);
        try
        {
            Assert.False(fresh.IsStale(GameMode.Pve, "tr"));
            Assert.True(fresh.IsStale(GameMode.Pvp, "tr"));
            Assert.True(fresh.IsStale(GameMode.Pve, "ru"));  // names of that language not downloaded
            Assert.True(old.IsStale(GameMode.Pve, "tr"));
            Assert.True(none.IsStale(GameMode.Pve, "tr"));
            Assert.Null(none.UpdatedAt(GameMode.Pve));
        }
        finally
        {
            foreach (var f in new[] { f1, f2, f3 }) Directory.Delete(f, true);
        }
    }

    [Theory]
    [InlineData(GameMode.Pvp, "regular")]
    [InlineData(GameMode.Pve, "pve")]
    public void ModePath_FollowsGameMode(GameMode mode, string path) =>
        Assert.Equal(path, FleaMarket.ModePath(mode));
}
