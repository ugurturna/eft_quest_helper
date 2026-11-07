using System.Text.Json;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

public sealed class PlannerTests : IDisposable
{
    readonly string _dir = Directory.CreateTempSubdirectory("tqt").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    static Quest Q(string id, string name, int? level = null, bool kappa = false, params string[] pre) => new()
    {
        Id = id, Name = name, Trader = "Prapor", MinLevel = level, KappaRequired = kappa,
        Prerequisites = [.. pre], Objectives = [new Objective { Id = "o1", Text = "Do " + name }],
    };

    QuestRepository Repo(GameMode mode = GameMode.Pvp, string lang = "en")
    {
        var pve = Q("pve-only", "PvE Thing");
        pve.GameModes = ["pve"];
        var lvl = Q("prestige", "New Beginning", level: 30);
        lvl.ModeOverrides["pve"] = new ModeOverride { MinLevel = 20 };
        Quest[] quests =
        [
            Q("shootout-picnic", "Shootout Picnic"),
            Q("punisher-1", "The Punisher - Part 1", 10, false, "shootout-picnic"),
            Q("punisher-2", "The Punisher - Part 2", null, false, "punisher-1"),
            Q("punisher-3", "The Punisher - Part 3", null, true, "punisher-2"),
            Q("debut", "Debut", null, true),
            Q("collector", "Collector", 40, false, "punisher-3"),
            pve, lvl,
        ];
        File.WriteAllText(Path.Combine(_dir, "tasks_en.json"), JsonSerializer.Serialize(quests, QuestRepository.Json));
        File.WriteAllText(Path.Combine(_dir, "tasks_tr.json"), JsonSerializer.Serialize(new[]
        {
            new QuestTranslation { Id = "punisher-3", Name = "Cezalandırıcı - Bölüm 3",
                Objectives = [new TranslatedObjective { Id = "o1", Text = "Yap" }] },
        }, QuestRepository.Json));
        return QuestRepository.Load(_dir, lang, mode);
    }

    [Fact]
    public void Plan_for_punisher_3_lists_whole_chain_in_order()
    {
        var planner = new QuestPlanner(Repo());
        var plan = planner.PlanFor("punisher-3", new Progress());
        Assert.Equal(["shootout-picnic", "punisher-1", "punisher-2", "punisher-3"], plan.Chain.Select(q => q.Id));
        Assert.Equal(["shootout-picnic"], plan.AvailableNow.Select(q => q.Id));
    }

    [Fact]
    public void Completing_a_quest_completes_its_prerequisites_and_unlocks_next()
    {
        var planner = new QuestPlanner(Repo());
        var p = new Progress { Active = ["punisher-1"] };
        planner.Complete(p, "punisher-1");
        Assert.Contains("shootout-picnic", p.Completed);
        Assert.Empty(p.Active);
        Assert.Equal(["punisher-2"], planner.UnlockedBy("punisher-1", p).Select(q => q.Id));
        var plan = planner.PlanFor("punisher-3", p);
        Assert.Equal(["punisher-2", "punisher-3"], plan.Remaining.Select(q => q.Id));
    }

    [Fact]
    public void Handing_in_accepts_the_next_quests_without_tracking_them()
    {
        var planner = new QuestPlanner(Repo());
        var p = new Progress { Active = ["punisher-1"], Tracked = ["punisher-1"] };
        planner.Complete(p, "punisher-1");
        Assert.Equal(["punisher-2"], planner.AcceptUnlocked(p, "punisher-1").Select(q => q.Id));
        Assert.Equal(["punisher-2"], p.Active);
        Assert.DoesNotContain("punisher-2", p.Tracked);   // not on the overlay until pinned

        p = new Progress { Completed = ["shootout-picnic", "punisher-1", "punisher-2"], Active = ["punisher-3"] };
        planner.Complete(p, "punisher-3");
        Assert.Equal(["collector"], planner.AcceptUnlocked(p, "punisher-3").Select(q => q.Id));   // level 40 is only shown
    }

    [Fact]
    public void Raid_filter_keeps_raid_objectives_of_the_map()
    {
        var quest = new Quest
        {
            Id = "q", Name = "Multi", Map = ["Customs", "Woods"], Objectives =
            [
                new Objective { Id = "o1", Text = "Find the folder on Customs", Map = "Customs", Maps = ["Customs"] },
                new Objective { Id = "o2", Text = "Stash it on Woods", Map = "Woods" },   // old data: Map only
                new Objective { Id = "o3", Text = "Survive and extract from the location" },
                new Objective { Id = "o4", Text = "Hand over the folder" },
            ],
        };
        Assert.Equal(["o1", "o3"], RaidFilter.Objectives(quest, "Customs").Select(o => o.Id));
        Assert.Equal(["o2", "o3"], RaidFilter.Objectives(quest, "Woods").Select(o => o.Id));
        Assert.Empty(RaidFilter.Objectives(quest, "Factory"));

        // no map = any map; a quest that only hands items over has nothing to do in the raid
        var anywhere = new Quest { Id = "a", Objectives = [new Objective { Id = "o1", Text = "Eliminate 5 PMCs" }] };
        var handIn = new Quest { Id = "h", Objectives = [new Objective { Id = "o1", Text = "Hand over 3 Salewa" }] };
        Assert.Single(RaidFilter.Objectives(anywhere, "Factory"));
        Assert.Empty(RaidFilter.Objectives(handIn, "Factory"));

        Assert.Equal(["Customs", "Woods", "Factory"],
            RaidFilter.Maps([quest, new Quest { Map = ["Factory", QuestPlanner.AnyMap] }]));
    }

    [Fact]
    public void A_new_goal_activates_the_step_the_player_is_at()
    {
        var planner = new QuestPlanner(Repo());
        var p = new Progress();
        Assert.Equal(["shootout-picnic"], planner.ActivateGoalSteps(p, "punisher-3").Select(q => q.Id));   // nothing done: first quest

        p = new Progress { Completed = ["shootout-picnic", "punisher-1"] };
        Assert.Equal(["punisher-2"], planner.ActivateGoalSteps(p, "punisher-3").Select(q => q.Id));        // part of it done: next step
        Assert.Equal(["punisher-2"], p.Active);
        Assert.Empty(p.Tracked);
        Assert.Empty(planner.ActivateGoalSteps(p, "punisher-3"));                                          // already active
    }

    [Fact]
    public void Starting_a_quest_marks_its_whole_chain_done()
    {
        var planner = new QuestPlanner(Repo());
        var p = new Progress { Active = ["punisher-1"] };
        var marked = planner.Start(p, "punisher-3");
        Assert.Equal(["shootout-picnic", "punisher-1", "punisher-2"], marked.Select(q => q.Id));
        Assert.Equal(["punisher-3"], p.Active);
        Assert.DoesNotContain("punisher-3", p.Completed);
        Assert.Empty(planner.Start(p, "punisher-3"));  // nothing new the second time
    }

    [Fact]
    public void Uncompleting_removes_dependents()
    {
        var planner = new QuestPlanner(Repo());
        var p = new Progress();
        planner.Complete(p, "punisher-3");
        planner.Uncomplete(p, "punisher-1");
        Assert.Equal(["shootout-picnic"], p.Completed);
    }

    [Fact]
    public void Kappa_set_includes_flagged_quests_collector_and_their_prerequisites()
    {
        var set = new QuestPlanner(Repo()).KappaSet();
        Assert.Equal(["collector", "debut", "punisher-1", "punisher-2", "punisher-3", "shootout-picnic"], set.Order());
    }

    [Fact]
    public void Required_level_does_not_lock_a_quest()
    {
        var repo = Repo();
        var p = new Progress { Completed = ["shootout-picnic"] };
        Assert.True(new QuestPlanner(repo).IsAvailable(repo.ById["punisher-1"], p));   // needs level 10
    }

    [Fact]
    public void Game_mode_filters_quests_and_applies_overrides()
    {
        Assert.False(Repo(GameMode.Pvp).ById.ContainsKey("pve-only"));
        var pve = Repo(GameMode.Pve);
        Assert.True(pve.ById.ContainsKey("pve-only"));
        Assert.Equal(20, pve.ById["prestige"].MinLevel);
        Assert.Equal(30, Repo(GameMode.Pvp).ById["prestige"].MinLevel);
    }

    [Fact]
    public void Translation_sets_display_text_and_both_names_are_searchable()
    {
        var repo = Repo(lang: "tr");
        var q = repo.ById["punisher-3"];
        Assert.Equal("Cezalandırıcı - Bölüm 3", q.DisplayName);
        Assert.Equal("Yap", q.Objectives[0].DisplayText);
        Assert.Equal("Debut", repo.ById["debut"].DisplayName);  // falls back to English

        var search = new SearchService(repo.Quests);
        Assert.Equal("punisher-3", search.Search("cezalandirici 3")[0].Id);
        Assert.Equal("punisher-3", search.Search("punisher part 3")[0].Id);
        Assert.Equal("punisher-3", search.Search("punişır 3")[0].Id);
        Assert.DoesNotContain(search.Search("punisher 4"), x => x.Id == "punisher-3");
    }

    [Fact]
    public void Profiles_keep_progress_and_settings_separate()
    {
        var root = Path.Combine(_dir, "profiles-test");
        var store = new ProgressStore(root);
        Assert.Equal([ProgressStore.DefaultProfile], store.ListProfiles());
        store.SaveProgress(GameMode.Pvp, new Progress { Completed = ["debut"] });
        store.SaveSettings(new AppSettings { OverlayScale = 1.5 });

        store.CreateProfile("Kardeşim");
        store.SwitchProfile("Kardeşim");
        Assert.Empty(store.LoadProgress(GameMode.Pvp).Completed);
        Assert.Equal(1.5, store.LoadSettings().OverlayScale);  // new profile copies current settings
        store.SaveProgress(GameMode.Pvp, new Progress { Completed = ["shootout-picnic"] });

        // current profile survives a restart
        var reopened = new ProgressStore(root);
        Assert.Equal("Kardeşim", reopened.Profile);
        reopened.SwitchProfile(ProgressStore.DefaultProfile);
        Assert.Equal(["debut"], reopened.LoadProgress(GameMode.Pvp).Completed);

        Assert.NotNull(reopened.ValidateName("kardeşim"));   // duplicate, case-insensitive
        Assert.NotNull(reopened.ValidateName("a/b"));
        reopened.RenameProfile("Kardeşim", "Ali");
        Assert.Throws<InvalidOperationException>(() => reopened.DeleteProfile(ProgressStore.DefaultProfile));
        reopened.DeleteProfile("Ali");
        Assert.Equal([ProgressStore.DefaultProfile], reopened.ListProfiles());
    }

    [Fact]
    public void Old_single_user_files_move_into_default_profile()
    {
        var root = Path.Combine(_dir, "legacy-test");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "progress_pvp.json"), """{"completed":["debut"]}""");
        var store = new ProgressStore(root);
        Assert.Equal(ProgressStore.DefaultProfile, store.Profile);
        Assert.Equal(["debut"], store.LoadProgress(GameMode.Pvp).Completed);
        Assert.False(File.Exists(Path.Combine(root, "progress_pvp.json")));
    }

    [Fact]
    public void Progress_round_trips()
    {
        var store = new ProgressStore(_dir);
        var p = new Progress { Completed = ["a"], Active = ["b"], Goals = ["collector"] };
        p.Objectives[Progress.ObjectiveKey("b", "o1")] = 3;
        store.SaveProgress(GameMode.Pve, p);
        var back = store.LoadProgress(GameMode.Pve);
        Assert.Equal(["collector"], back.Goals);
        Assert.Equal(3, back.Objectives["b/o1"]);
        Assert.Empty(store.LoadProgress(GameMode.Pvp).Completed);
    }
}
