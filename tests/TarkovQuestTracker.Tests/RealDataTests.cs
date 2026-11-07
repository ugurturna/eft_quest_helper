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
    public void Has_story_chapters_and_events_and_hides_past_events_by_default()
    {
        Assert.Contains(Repo.Quests, q => q.Category == "story" && q.Name == "Tour");
        Assert.Contains(Repo.Quests, q => q.Category == "event" && q.EventActive == true);
        Assert.DoesNotContain(Repo.Quests, q => q.IsPastEvent);

        var withPast = QuestRepository.Load(QuestRepository.FindDataDir(AppContext.BaseDirectory), "en", GameMode.Pvp, includePastEvents: true);
        Assert.Contains(withPast.Quests, q => q.IsPastEvent);
    }

    [Fact]
    public void Story_chapters_are_translated_to_turkish_including_sub_steps()
    {
        var tour = Repo.ById["tour"];
        Assert.Equal("Tur", tour.DisplayName);
        Assert.Equal("Ground Zero'dan kaç", tour.Objectives[0].DisplayText);
        Assert.Equal("Parayı Therapist'e teslim et", tour.Objectives[2].DisplayDetails[0]);
        Assert.StartsWith("▸ Ground Zero'dan kaç", tour.DisplayGuide);
        // names like "Batya" are not translated, objectives always are
        Assert.All(Repo.Quests.Where(q => q.Category == "story"),
            q => Assert.NotEqual(q.Objectives[0].Text, q.Objectives[0].DisplayText));
        // both languages stay searchable
        Assert.Equal("blue-fire", new SearchService(Repo.Quests).Search("mavi ateş")[0].Id);
        Assert.Equal("blue-fire", new SearchService(Repo.Quests).Search("blue fire")[0].Id);
    }

    [Theory]
    [InlineData("punisher part 3")]
    [InlineData("punişır 3")]
    [InlineData("The Punisher - Part 3")]
    public void Search_finds_punisher_3(string query) =>
        Assert.Equal("the-punisher-part-3", new SearchService(Repo.Quests).Search(query)[0].Id);
}
