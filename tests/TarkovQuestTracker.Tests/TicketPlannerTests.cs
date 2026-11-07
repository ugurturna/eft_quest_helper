using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

/// <summary>Routes through "The Ticket" against data/ticket_endings.json.</summary>
public sealed class TicketPlannerTests
{
    static readonly TicketPlanner Planner = TicketPlanner.Load(QuestRepository.FindDataDir(AppContext.BaseDirectory));

    static TicketProgress Progress(string? target = null, params (string Decision, string Option)[] choices) => new()
    {
        Target = target,
        Choices = choices.ToDictionary(c => c.Decision, c => c.Option),
    };

    static List<string> Ids(TicketRoute route) => route.Steps.Select(s => s.Id).ToList();

    [Fact]
    public void Data_references_only_known_decisions_options_and_endings()
    {
        var guide = Planner.Guide;
        Assert.Equal(["survivor", "savior", "debtor", "fallen"], guide.Endings.Select(e => e.Id));
        Assert.Equal(guide.Steps.Count, guide.Steps.Select(s => s.Id).Distinct().Count());

        bool Valid(string decision, string option) =>
            guide.Decisions.Any(d => d.Id == decision && d.Options.Any(o => o.Id == option));
        Assert.All(guide.Endings, e => Assert.All(e.Requires, kv => Assert.True(Valid(kv.Key, kv.Value), e.Id)));
        Assert.All(guide.Steps, s =>
        {
            Assert.All(s.When, kv => Assert.True(Valid(kv.Key, kv.Value), s.Id));
            Assert.All(s.Endings, id => Assert.Contains(guide.Endings, e => e.Id == id));
            Assert.Contains(s.Mode, new string?[] { null, "pvp", "pve" });
            if (s.Kind == "decision") Assert.NotNull(Planner.Decision(s.Decision));
            else
            {
                Assert.False(string.IsNullOrWhiteSpace(s.Text.In("en")), s.Id);
                Assert.False(string.IsNullOrWhiteSpace(s.Text.In("tr")), s.Id);
            }
        });
    }

    [Fact]
    public void Without_choices_the_route_stops_at_the_case_decision()
    {
        var route = Planner.Plan(Progress(), GameMode.Pvp);
        Assert.Equal("case", route.Pending?.Decision);
        Assert.Equal(["recover-case", "case"], Ids(route));
        Assert.Equal(4, route.Possible.Count);
        Assert.Null(route.Ending);
        Assert.Equal("recover-case", route.Current?.Id);
    }

    [Theory]
    [InlineData("survivor"), InlineData("savior"), InlineData("debtor"), InlineData("fallen")]
    public void Every_ending_is_reachable_from_both_case_choices_in_both_modes(string ending)
    {
        foreach (var caseChoice in new[] { "keep", "prapor" })
        foreach (var mode in new[] { GameMode.Pvp, GameMode.Pve })
        {
            var route = Planner.Plan(Progress(ending, ("case", caseChoice)), mode);
            Assert.Null(route.Pending);
            Assert.False(route.TargetBlocked);
            Assert.Equal(ending, route.Ending?.Id);
            Assert.Equal("end-" + ending, route.Steps[^1].Id);
            Assert.Contains("escape", Ids(route));
            // no step of another ending leaks into the route
            Assert.All(route.Steps, s => Assert.True(s.Endings.Count == 0 || s.Endings.Contains(ending), s.Id));
        }
    }

    [Fact]
    public void Survivor_pays_less_when_the_case_went_to_prapor()
    {
        var kept = Ids(Planner.Plan(Progress("survivor", ("case", "keep")), GameMode.Pvp));
        var given = Ids(Planner.Plan(Progress("survivor", ("case", "prapor")), GameMode.Pvp));
        Assert.Contains("sv-pay-500", kept);
        Assert.DoesNotContain("sv-pay-300", kept);
        Assert.Contains("sv-pay-300", given);
        Assert.Contains("prapor-collect-case", given);
        Assert.DoesNotContain("prapor-collect-case", kept);
    }

    [Fact]
    public void Fallen_skips_prapors_extra_tasks_when_he_got_the_case()
    {
        var kept = Ids(Planner.Plan(Progress("fallen", ("case", "keep")), GameMode.Pvp));
        var given = Ids(Planner.Plan(Progress("fallen", ("case", "prapor")), GameMode.Pvp));
        Assert.Contains("fa-repair-kits", kept);
        Assert.DoesNotContain("fa-repair-kits", given);
        Assert.Contains("fa-skip", given);
    }

    [Fact]
    public void Game_mode_picks_the_matching_objectives()
    {
        var pvp = Ids(Planner.Plan(Progress("savior", ("case", "prapor")), GameMode.Pvp));
        var pve = Ids(Planner.Plan(Progress("savior", ("case", "prapor")), GameMode.Pve));
        Assert.Contains("sa-coop", pvp);
        Assert.DoesNotContain("sa-5-pmc", pvp);
        Assert.Contains("sa-5-pmc", pve);
        Assert.Contains("prapor-5-pmc", pve);
        Assert.DoesNotContain("prapor-15-kills", pve);
    }

    [Fact]
    public void Refusing_kerman_locks_in_survivor_and_blocks_other_targets()
    {
        var route = Planner.Plan(Progress("savior", ("case", "keep"), ("kerman", "refuse")), GameMode.Pvp);
        Assert.True(route.TargetBlocked);
        Assert.Equal("survivor", route.Ending?.Id);
        Assert.Equal("end-survivor", route.Steps[^1].Id);
    }

    [Fact]
    public void Working_with_kerman_without_target_stops_at_the_evidence_decision()
    {
        var progress = Progress(null, ("case", "keep"), ("kerman", "work"));
        var route = Planner.Plan(progress, GameMode.Pvp);
        Assert.Equal("evidence", route.Pending?.Decision);
        Assert.Equal(["savior", "debtor", "fallen"], route.Possible.Select(e => e.Id));
        Assert.DoesNotContain(route.Steps, s => s.Id.StartsWith("sv-"));
        Assert.Equal(["debtor"], Planner.EndingsAfter(progress, "evidence", "uturn").Select(e => e.Id));
    }

    [Fact]
    public void Evidence_options_never_list_survivor()
    {
        var progress = Progress("savior", ("case", "keep"));
        Assert.Equal(["savior"], Planner.EndingsAfter(progress, "evidence", "savior").Select(e => e.Id));
        Assert.Equal(4, Planner.EndingsAfter(progress, "case", "prapor").Count);
    }

    [Fact]
    public void Current_step_is_the_first_unticked_mandatory_step()
    {
        var progress = Progress("debtor", ("case", "keep"));
        progress.Done.UnionWith(["recover-case", "jammer"]);
        Assert.Equal("open-case", Planner.Plan(progress, GameMode.Pvp).Current?.Id);
    }

    [Fact]
    public void Unknown_stored_choices_are_ignored()
    {
        var route = Planner.Plan(Progress("nope", ("case", "lost"), ("ghost", "x")), GameMode.Pvp);
        Assert.Empty(route.Made);
        Assert.Null(route.Target);
        Assert.Equal("case", route.Pending?.Decision);
    }
}
