using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>OCR text of the in-game exfil list (double "O") against data/extracts.json.</summary>
public sealed class ExtractMatcherTests
{
    static readonly string DataDir = QuestRepository.FindDataDir(AppContext.BaseDirectory);
    static readonly ExtractMatcher Matcher = ExtractMatcher.Load(DataDir);

    [Theory]
    [InlineData("Customs", "EXFIL01 Crossroads", "EXFIL02 Trailer Park", "EXFIL03 ZB-1011")]
    [InlineData("Woods", "EXFIL01 Outskirts", "EXFIL02 UN Roadblock", "EXFIL03 ZB-014")]
    [InlineData("Shoreline", "EXFIL01 Road to Customs", "EXFIL02 Tunnel", "EXFIL03 Pier Boat")]
    [InlineData("Interchange", "EXFIL01 Emercom Checkpoint", "EXFIL02 Railway Exfil", "EXFIL03 Hole in the Fence")]
    [InlineData("Streets of Tarkov", "EXFIL01 Collapsed Crane", "EXFIL02 Crash Site")]
    [InlineData("Factory", "EXFIL01 Gate 3", "EXFIL02 Cellars", "EXFIL03 Gate 0")]
    [InlineData("Ground Zero", "EXFIL01 Emercom Checkpoint", "EXFIL02 Nakatani Basement Stairs")]
    [InlineData("The Labyrinth", "EXFIL01 The Way Up")]
    public void Recognizes_the_map(string map, params string[] lines) => Assert.Equal(map, Matcher.Map(lines));

    /// <summary>What Windows OCR made of a real double "O" on Customs (auto_raid_test.png): slashed zeros, a 1 read as I.</summary>
    static readonly string[] CustomsScreen =
    [
        "Find an extraction point", "EXFILØI Dorms V-Ex", "EXFILØ2", "EXFILØ3", "EXFILØ4", "EXFILØ5", "EXFILØ6", "EXFILØ7",
        "ZB-1Ø13", "ZB-IØII", "Old Gas Station", "Railroad to Reserve", "Railroad Passage (Flare)",
        "Boiler Room Basement (Co-op)", "TRANSITØI Transit to Reserve", "TRANSITØ2 Transit to Factory",
        "TRANSITØ3 Transit to Interchange", "TRANSITØ4 Transit to Shoreline",
    ];

    [Fact]
    public void Reads_a_real_exfil_list()
    {
        Assert.Equal("Customs", Matcher.Map(CustomsScreen));
        Assert.Equal(7, Matcher.Hits(CustomsScreen));  // ZB-1013 and ZB-1011 too, despite the slashed zeros
    }

    [Fact]
    public void Survives_an_ocr_typo_in_a_long_name() =>
        Assert.Equal("Ground Zero", Matcher.Map(["Nakatani Basernent Stairs", "Tartowers Sales Office"]));

    [Fact]
    public void Exfils_shared_by_two_maps_alone_tell_nothing() =>
        // Emercom Checkpoint: Interchange and Ground Zero
        Assert.Null(Matcher.Map(["EXFIL01 Emercom Checkpoint"]));

    [Fact]
    public void One_exfil_is_not_enough_on_a_big_map() => Assert.Null(Matcher.Map(["Crossroads"]));

    [Fact]
    public void Other_screen_text_is_no_map() =>
        Assert.Null(Matcher.Map(["Escape from Tarkov", "Character", "Traders", "Flea Market", "Hideout", "12:45:03"]));

    [Fact]
    public void Every_map_in_the_file_is_a_quest_map()
    {
        var questMaps = RaidFilter.Maps(QuestRepository.Load(DataDir, "en", GameMode.Pvp).Quests);
        var file = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(DataDir, ExtractMatcher.FileName)));
        foreach (var map in file.RootElement.GetProperty("maps").EnumerateObject())
            Assert.Contains(map.Name, questMaps);
    }

    [Fact]
    public void Counts_the_exfils_drawn_so_far()
    {
        Assert.Equal(1, Matcher.Hits(["EXFIL01 Crossroads"]));
        Assert.Equal(3, Matcher.Hits(["EXFIL01 Crossroads", "EXFIL02 Trailer Park", "EXFIL03 ZB-1011"]));
        Assert.Equal(0, Matcher.Hits([]));
    }

    [Fact]
    public void A_missing_file_gives_an_empty_matcher()
    {
        var empty = ExtractMatcher.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        Assert.True(empty.IsEmpty);
        Assert.Null(empty.Map(["Crossroads", "Trailer Park"]));
    }
}
