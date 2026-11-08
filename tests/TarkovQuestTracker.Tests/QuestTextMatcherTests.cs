using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>OCR text of the in-game Tasks screen against the real quest names.</summary>
public sealed class QuestTextMatcherTests
{
    static readonly QuestRepository Repo =
        QuestRepository.Load(QuestRepository.FindDataDir(AppContext.BaseDirectory), "en", GameMode.Pvp);
    static readonly QuestTextMatcher Matcher = new(Repo.Quests);

    static string[] Ids(params string[] lines) => Matcher.Match(lines).Quests.Select(q => q.Id).ToArray();

    [Theory]
    [InlineData("Debut", "debut")]
    [InlineData("DEBUT", "debut")]
    [InlineData("Shortage", "shortage")]
    [InlineData("Shortaqe", "shortage")]
    [InlineData("The Punisher - Part 3", "the-punisher-part-3")]
    [InlineData("The Punisher - Part 4", "the-punisher-part-4")]
    [InlineData("Advertising Business - Part l", "advertising-business-part-1")]
    [InlineData("Advertising Business – Part 6", "advertising-business-part-6")]
    [InlineData("Gunsmith - HK MP5", "gunsmith-hk-mp5")]
    [InlineData("Debut Prapor Customs 0/5", "debut")]
    public void Reads_a_name(string line, string id) => Assert.Equal([id], Ids(line));

    [Fact]
    public void Joins_a_name_wrapped_onto_two_lines()
    {
        var name = Repo.ById["against-the-conscience-part-1"].Name;
        var cut = name.LastIndexOf(' ');
        Assert.Equal(["against-the-conscience-part-1"], Ids(name[..cut], name[(cut + 1)..]));
    }

    [Fact]
    public void Ignores_screen_text_that_is_no_quest()
    {
        var r = Matcher.Match(["TASKS", "Accept", "Active", "Completed", "Prapor", "Customs", "125,000 ₽", "LVL 15", "Debts"]);
        Assert.DoesNotContain(r.Quests, q => q.Id == "debut");
        Assert.True(r.Unmatched >= 5);
    }

    [Fact]
    public void Short_names_need_an_exact_read() => Assert.Empty(Ids("Debvt"));

    [Fact]
    public void Part_number_must_match() => Assert.DoesNotContain("the-punisher-part-3", Ids("The Punisher - Part 5"));

    [Fact]
    public void Same_quest_on_several_frames_is_listed_once() =>
        Assert.Equal(["debut", "shortage"], Ids("Debut", "Shortage", "Debut"));
}
