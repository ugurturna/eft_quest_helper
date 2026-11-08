using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>The Active Quests page's search box and map choice.</summary>
public sealed class ActiveQuestFilterTests
{
    static readonly Quest OnCustoms = new()
    {
        Id = "c", Name = "Debut", DisplayName = "Debut", Trader = "Prapor", Map = ["Customs"],
        Objectives = [new Objective { Id = "o1", Text = "Eliminate 5 Scavs", DisplayText = "5 Scav öldür" }],
    };
    static readonly Quest ObjectiveOnWoods = new()
    {
        Id = "w", Name = "Shortage", DisplayName = "Shortage", Trader = "Therapist",
        Objectives = [new Objective { Id = "o1", Text = "Find Salewa", DisplayText = "Salewa bul", Maps = ["Woods"] }],
    };
    static readonly Quest Anywhere = new()
    {
        Id = "a", Name = "The Punisher - Part 1", DisplayName = "The Punisher - Part 1", Trader = "Prapor", Map = [QuestPlanner.AnyMap],
        Objectives = [new Objective { Id = "o1", Text = "Eliminate Scavs with an SKS" }],
    };
    static readonly Quest[] All = [OnCustoms, ObjectiveOnWoods, Anywhere];

    static string[] Ids(string? map, string? search) =>
        All.Where(q => ActiveQuestFilter.Matches(q, map, search)).Select(q => q.Id).ToArray();

    [Fact]
    public void No_filter_shows_everything() => Assert.Equal(["c", "w", "a"], Ids("", ""));

    [Fact]
    public void A_map_shows_only_its_own_quests_not_the_any_location_ones()
    {
        Assert.Equal(["c"], Ids("Customs", ""));
        Assert.Equal(["w"], Ids("Woods", ""));  // an objective on Woods is enough
    }

    [Fact]
    public void Any_location_shows_the_quests_done_anywhere() => Assert.Equal(["a"], Ids(QuestPlanner.AnyMap, null));

    [Theory]
    [InlineData("punisher", new[] { "a" })]
    [InlineData("prapor", new[] { "c", "a" })]       // trader
    [InlineData("salewa", new[] { "w" })]            // objective
    [InlineData("öldür", new[] { "c" })]             // translated objective
    [InlineData("SCAV sks", new[] { "a" })]          // every word, case ignored
    public void Search_looks_at_name_trader_and_objectives(string search, string[] ids) => Assert.Equal(ids, Ids("", search));

    [Fact]
    public void Map_and_search_combine() => Assert.Equal(["c"], Ids("Customs", "scav"));
}
