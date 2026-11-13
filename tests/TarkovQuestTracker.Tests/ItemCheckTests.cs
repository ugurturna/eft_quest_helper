using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

public class ItemCheckTests
{
    static FleaItem Item(string id, string name, string shortName, int? flea = null, int? trader = null,
                         bool banned = false, int offers = 10, string? local = null) => new()
    {
        Id = id, Name = local ?? name, ShortName = shortName, EnglishName = name, EnglishShortName = shortName,
        LastLowPrice = flea, BestTrader = trader == null ? null : "Therapist", BestTraderPrice = trader,
        FleaBanned = banned, OfferCount = offers,
    };

    static readonly FleaItem Gpu = Item("gpu", "Graphics card", "GPU", 480000, 310000, local: "Ekran kartı");
    static readonly FleaItem Ledx = Item("ledx", "LEDX Skin Transilluminator", "LEDX", 1000000, 300000);
    static readonly FleaItem Bolts = Item("bolts", "Bolts", "Bolts", 9000, 8500);
    static readonly FleaItem Tag = Item("tag", "Dogtag", "Tag", trader: 5000, banned: true);
    static readonly ItemTextMatcher Matcher = new([Gpu, Ledx, Bolts, Tag]);

    static FleaItem? Read(params (string Text, double Distance)[] texts) =>
        Matcher.Match(texts.Select(t => new ScreenText(t.Text, t.Distance)));

    [Fact]
    public void Match_FullNameEnglishOrLocal()
    {
        Assert.Same(Gpu, Read(("Graphics card", 40)));
        Assert.Same(Gpu, Read(("EKRAN KARTI", 40)));
        Assert.Same(Ledx, Read(("LEDX Skin Transilluminator", 10)));
    }

    [Fact]
    public void Match_IgnoresNearerShortName()
    {
        // the neighbour cell's short name sits closer to the mouse than the tooltip
        Assert.Same(Gpu, Read(("LEDX", 5), ("Graphics card", 60)));
    }

    [Fact]
    public void Match_NearestOfTwoFullNames()
    {
        Assert.Same(Ledx, Read(("Graphics card", 200), ("LEDX Skin Transilluminator", 30)));
    }

    [Fact]
    public void Match_MisreadLetterAndExtraText()
    {
        Assert.Same(Gpu, Read(("Graphlcs card", 20)));
        Assert.Same(Gpu, Read(("Graphics card 2/2", 20)));
    }

    [Fact]
    public void Match_WholeNameBeatsNameInsideLongerText()
    {
        Assert.Same(Ledx, Read(("Graphics card 2/2", 5), ("LEDX Skin Transilluminator", 50)));
    }

    [Fact]
    public void Match_TooltipBeatsNearerCellLabelThatIsAlsoAName()
    {
        // "Bolts" is both the item's name and its short name: a neighbour cell's label, nearer than the tooltip
        Assert.Same(Gpu, Read(("Bolts", 4), ("Graphics card", 60)));
        Assert.Same(Bolts, Read(("Bolts", 4), ("Search", 30)));
    }

    [Fact]
    public void Match_ShortNamesAloneAreNotEnough()
    {
        Assert.Null(Read(("LEDX", 15), ("GPU", 5)));
    }

    [Fact]
    public void Match_NothingForOtherText()
    {
        Assert.Null(Read(("Search", 10), ("Examine", 20), ("Ug", 1)));
    }

    static Quest Quest(string id, ItemNeed need, int level = 1, params string[] prerequisites) => new()
    {
        Id = id, Name = id, DisplayName = id, MinLevel = level, Prerequisites = [.. prerequisites], ItemNeeds = [need],
    };

    static ItemNeed Need(string itemId, int count = 1, bool fir = false, params string[] alternatives) =>
        new(itemId, itemId, itemId, count, fir, alternatives, null) { AlternativeIds = alternatives };

    [Fact]
    public void Advise_ListsOpenQuestsActiveFirstAndSkipsDone()
    {
        var quests = new[]
        {
            Quest("locked", Need("gpu"), 30, "first"),
            Quest("open", Need("gpu", 2, fir: true), 20),
            Quest("active", Need("gpu"), 40),
            Quest("done", Need("gpu")),
            Quest("other", Need("ledx")),
        };
        var progress = new Progress { Completed = ["done"], Active = ["active"] };

        var advice = ItemAdvisor.Advise(Gpu, quests, progress);

        Assert.True(advice.Keep);
        Assert.Equal(["active", "open", "locked"], advice.Quests.Select(u => u.Quest.Id));
        Assert.False(advice.Quests[2].Available);
        Assert.True(advice.Quests[1].Need.Fir);
    }

    [Fact]
    public void Advise_FindsAlternativeItems()
    {
        var quests = new[] { Quest("any", Need("ledx", 1, false, "gpu")) };
        Assert.Single(ItemAdvisor.QuestUses("gpu", quests, new Progress()));
    }

    [Fact]
    public void BestSale_FleaUnlessTraderPaysAboutTheSame()
    {
        Assert.Equal((SellPlace.Flea, 480000), ItemAdvisor.BestSale(Gpu));
        Assert.Equal((SellPlace.Trader, 8500), ItemAdvisor.BestSale(Bolts));
        Assert.Equal((SellPlace.Trader, 5000), ItemAdvisor.BestSale(Tag));
        Assert.Equal((SellPlace.None, (int?)null), ItemAdvisor.BestSale(Item("x", "X thing", "X")));
    }
}
