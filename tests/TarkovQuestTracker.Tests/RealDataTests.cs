using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>Sanity checks against the scraped data/tasks_en.json.</summary>
public sealed class RealDataTests
{
    static readonly QuestRepository Repo =
        QuestRepository.Load(QuestRepository.FindDataDir(AppContext.BaseDirectory), "tr", GameMode.Pvp);

    [Fact]
    public void Has_all_quests_and_no_dangling_prerequisites()
    {
        Assert.True(Repo.Quests.Count > 400);
        foreach (var q in Repo.Quests)
            Assert.All(q.Prerequisites, p => Assert.True(Repo.ById.ContainsKey(p), $"{q.Id} -> {p}"));
    }

    [Fact]
    public void Quest_items_are_loaded_with_names_and_icons()
    {
        var needs = Repo.Quests.SelectMany(q => q.ItemNeeds).ToList();
        Assert.True(Repo.Quests.Count(q => q.ItemNeeds.Count > 0) > 100);
        Assert.All(needs, n => Assert.NotEqual(n.ItemId, n.Name));
        Assert.All(needs, n => Assert.True(n.IconPath != null && File.Exists(n.IconPath), n.ItemId));
    }

    [Fact]
    public void Item_objectives_show_their_item()
    {
        var collector = Repo.ById["collector"];
        Assert.All(collector.Objectives, o => Assert.NotNull(o.Item));
        var linked = Repo.Quests.SelectMany(q => q.Objectives).Count(o => o.Item != null);
        Assert.True(linked > 400, $"{linked} objectives with an item");
    }

    [Fact]
    public void Raid_needs_list_keys_markers_and_weapons()
    {
        RaidNeedGroup Group(string quest, string kind) => Repo.ById[quest].RaidNeeds.Single(g => g.Kind == kind);

        Assert.Contains(Group("bad-rep-evidence", "key").Items, n => n.ItemId == "5938144586f77473c2087145");
        Assert.Equal(6, Group("route-deviation", "mark").Items.Sum(n => n.Count));
        Assert.NotEmpty(Group("the-punisher-part-1", "useWeapon").Items);
        Assert.NotNull(Repo.ById["tigr-safari"].Objectives.Single(o => o.Id == "o1").Item);
        // keys / weapons are not handed over, so they stay off the items tab
        Assert.DoesNotContain(Repo.ById["bad-rep-evidence"].ItemNeeds, n => n.ItemId == "5938144586f77473c2087145");
    }

    [Theory]
    // an objective that counts on several maps knows all of them, not only the first link
    [InlineData("debut", "Customs", new[] { "o1" })]
    [InlineData("debut", "Woods", new[] { "o1" })]
    // a multi-map quest shows only the line of the map you are on
    [InlineData("escort", "Factory", new[] { "o1" })]
    [InlineData("escort", "The Lab", new[] { "o7" })]
    [InlineData("delivery-from-the-past", "Factory", new[] { "o2" })]
    [InlineData("delivery-from-the-past", "Customs", new[] { "o1" })]
    [InlineData("delivery-from-the-past", "Woods", new string[0])]
    public void Raid_shows_the_objectives_of_that_map(string quest, string map, string[] objectives) =>
        Assert.Equal(objectives, RaidFilter.Objectives(Repo.ById[quest], map).Select(o => o.Id));

    [Theory]
    // a number inside the item name is not the amount
    [InlineData("collector", "o1", 1)]          // "42 Signature Blend English Tea"
    [InlineData("battery-change", "o2", 1)]     // "6-STEN-140-M military battery"
    [InlineData("golden-swag", "o2", 1)]        // "Dorm room 303"
    // "Hand over the items" takes the amount of the find objective
    [InlineData("the-punisher-part-3", "o3", 7)]
    // an amount the text states itself wins
    [InlineData("crisis", "o8", 2)]             // "Hand over the 2 LEDX"
    [InlineData("crisis", "o10", 10)]           // "Hand over the 10 multivitamins"
    public void Item_objective_counts_are_the_real_amounts(string quest, string objective, int count) =>
        Assert.Equal(count, Repo.ById[quest].Objectives.Single(o => o.Id == objective).Count);

    [Fact]
    public void Kappa_is_the_collector_chain()
    {
        var kappa = Repo.Quests.Where(q => q.IsKappa).Select(q => q.Id).ToHashSet();
        Assert.Contains("collector", kappa);
        Assert.Contains("chemical-part-1", kappa);
        Assert.Equal(kappa, new QuestPlanner(Repo).KappaSet());
    }

    [Fact]
    public void Graph_is_acyclic()
    {
        var planner = new QuestPlanner(Repo);
        Assert.Equal(Repo.Quests.Count, planner.TopoSort(Repo.Quests.Select(q => q.Id)).Count);
    }

    [Fact]
    public void Punisher_3_chain_contains_parts_1_and_2()
    {
        var plan = new QuestPlanner(Repo).PlanFor("the-punisher-part-3", new Progress());
        var ids = plan.Chain.Select(q => q.Id).ToList();
        Assert.True(ids.IndexOf("the-punisher-part-1") < ids.IndexOf("the-punisher-part-2"));
        Assert.Equal("the-punisher-part-3", ids[^1]);
    }

    [Fact]
    public void Has_story_chapters_and_events_but_never_past_events()
    {
        Assert.Contains(Repo.Quests, q => q.Category == "story" && q.Name == "Tour");
        Assert.Contains(Repo.Quests, q => q.Category == "event" && q.EventActive == true);
        Assert.DoesNotContain(Repo.Quests, q => q.IsPastEvent);
    }

    [Fact]
    public void Story_chapters_are_translated_to_turkish_including_sub_steps()
    {
        var tour = Repo.ById["tour"];
        Assert.Equal("Tour", tour.DisplayName);  // chapter names stay as in game
        Assert.Equal("Ground Zero'dan kaç", tour.Objectives[0].DisplayText);
        Assert.Equal("Parayı Therapist'e teslim et", tour.Objectives[2].DisplayDetails[0]);
        Assert.StartsWith("▸ Ground Zero'dan kaç", tour.DisplayGuide);
        // names like "Batya" are not translated, objectives always are
        Assert.All(Repo.Quests.Where(q => q.Category == "story"),
            q => Assert.NotEqual(q.Objectives[0].Text, q.Objectives[0].DisplayText));
        // chapter names are not translated (game terms stay as in game)
        Assert.All(Repo.Quests.Where(q => q.Category == "story"), q => Assert.Equal([q.Name], q.AllNames.Distinct()));
        Assert.Equal("blue-fire", new SearchService(Repo.Quests).Search("blue fire")[0].Id);
    }

    [Theory]
    [InlineData("punisher part 3")]
    [InlineData("punişır 3")]
    [InlineData("The Punisher - Part 3")]
    public void Search_finds_punisher_3(string query) =>
        Assert.Equal("the-punisher-part-3", new SearchService(Repo.Quests).Search(query)[0].Id);
}
