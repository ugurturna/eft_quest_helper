using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>The story page against the real data: walkthrough parts land on their objectives, the choices load.</summary>
public sealed class StoryPlannerTests
{
    static readonly string DataDir = QuestRepository.FindDataDir(AppContext.BaseDirectory);
    static readonly QuestRepository En = QuestRepository.Load(DataDir, "en", GameMode.Pvp);
    static readonly QuestRepository Tr = QuestRepository.Load(DataDir, "tr", GameMode.Pvp);
    static readonly StoryPlanner Planner = StoryPlanner.Load(DataDir);

    static string Help(QuestRepository repo, string chapter, string objective) =>
        string.Join("\n", StoryPlanner.Steps(repo.ById[chapter]).Single(s => s.Objective.Id == objective).Help.Select(h => h.Title + "\n" + h.Body));

    [Fact]
    public void Lists_every_chapter_in_story_order()
    {
        var chapters = Planner.Chapters(En.Quests).Select(q => q.Id).ToList();
        Assert.Equal(10, chapters.Count);
        Assert.Equal("tour", chapters[0]);
        Assert.Equal("the-ticket", chapters[^1]);
    }

    [Fact]
    public void Walkthrough_parts_land_on_their_objectives()
    {
        // Falling Skies: the G-Wagon by the Tunnel exfil, the recorder's stash on the island
        Assert.Contains("Tunnel", Help(En, "falling-skies", "o5"));
        Assert.Contains("island", Help(En, "falling-skies", "o9"));
        // Blue Fire: the note's three places stay with "Find a lead on Item 1156"
        var lead = Help(En, "blue-fire", "o7");
        Assert.Contains("Ground Zero", lead);
        Assert.Contains("Lighthouse", lead);
        Assert.Contains("O22", lead);
    }

    [Fact]
    public void Choice_parts_are_left_out_of_the_walkthrough()
    {
        foreach (var chapter in Planner.Chapters(En.Quests))
            Assert.All(StoryPlanner.Steps(chapter).SelectMany(s => s.Help), h => Assert.DoesNotMatch("^Decision:", h.Title));
    }

    [Fact]
    public void Every_walkthrough_part_has_a_step_in_turkish_too()
    {
        foreach (var chapter in Planner.Chapters(Tr.Quests).Where(q => q.DisplayGuide.Length > 0))
        {
            var sections = StoryPlanner.Sections(chapter.DisplayGuide).Count;
            Assert.Equal(sections, StoryPlanner.Steps(chapter).Sum(s => s.Help.Count));
        }
        Assert.Contains("Tunnel", Help(Tr, "falling-skies", "o5"));
    }

    [Fact]
    public void Choices_point_at_existing_objectives_and_the_ticket_decision()
    {
        var ticket = TicketPlanner.Load(DataDir).Guide;
        Assert.NotEmpty(Planner.Choices.Decisions);
        foreach (var d in Planner.Choices.Decisions)
        {
            Assert.Contains(En.ById[d.Chapter].Objectives, o => o.Id == d.Objective);
            Assert.All(d.Options, o => Assert.False(string.IsNullOrEmpty(o.Label.In("tr"))));
            if (d.TicketDecision == null) continue;
            var asked = ticket.Decisions.Single(t => t.Id == d.TicketDecision);
            Assert.All(d.Options, o => Assert.Contains(asked.Options, t => t.Id == o.TicketOption));
        }
    }

    [Fact]
    public void Names_and_numbers_in_objectives_are_not_counters()
    {
        Assert.Equal(1, En.ById["blue-fire"].Objectives.Single(o => o.Id == "o7").Count);  // "Item 1156"
        Assert.Equal(1, En.ById["golden-swag"].Objectives[0].Count);                       // "Dorm room 303"
        Assert.Equal(3, En.ById["tour"].Objectives.Single(o => o.Id == "o5").Count);        // "3 times"
        Assert.Equal(30, En.ById["get-a-foothold"].Objectives[3].Count);                    // "30 M.U.L.E."
    }
}
