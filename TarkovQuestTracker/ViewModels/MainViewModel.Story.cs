using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.ViewModels;

/// <summary>
/// Story page: every story chapter step by step, like The Ticket guide. Each objective has its walkthrough under it,
/// the choices inside a chapter (keep the case or hand it over...) are asked where they come up, and the ticks are
/// the same objective progress the active quests and the overlay use.
/// </summary>
public sealed partial class MainViewModel
{
    StoryPlanner _story = null!;

    public BulkObservableCollection<StoryChapterVm> StoryChapters { get; } = [];
    public BulkObservableCollection<StoryStepVm> StorySteps { get; } = [];

    [ObservableProperty] private StoryChapterVm? _selectedStoryChapter;
    [ObservableProperty] private string _storyProgressText = "";

    /// <summary>The Ticket has its own page (endings and routes): the story page links there.</summary>
    public bool StoryIsTicket => SelectedStoryChapter?.Quest.Id == TicketGuide.QuestId;
    public bool StoryShowsSteps => SelectedStoryChapter != null && !StoryIsTicket;
    public bool StoryChapterActive => SelectedStoryChapter is { } c && Progress.Active.Contains(c.Quest.Id);

    /// <summary>"Open The Ticket guide": the main window goes to that page.</summary>
    public event Action? TicketPageRequested;

    partial void OnSelectedStoryChapterChanged(StoryChapterVm? value) => RefreshStorySteps();

    void RefreshStory()
    {
        var selected = SelectedStoryChapter?.Quest.Id;
        Replace(StoryChapters, _story.Chapters(_repo.ById.Values).Select(ChapterVm));
        // the list is rebuilt (new rows): pick the same chapter again, which also draws its steps
        SelectedStoryChapter = StoryChapters.FirstOrDefault(c => c.Quest.Id == selected) ?? StoryChapters.FirstOrDefault();
    }

    StoryChapterVm ChapterVm(Quest q)
    {
        var required = q.Objectives.Where(o => !o.Optional).ToList();
        var done = required.Count(o => IsObjectiveDone(q.Id, o));
        return new StoryChapterVm(q, done, required.Count, Progress.Completed.Contains(q.Id), Progress.Active.Contains(q.Id));
    }

    bool IsObjectiveDone(string questId, Objective o) =>
        Progress.Completed.Contains(questId)
        || Progress.Objectives.GetValueOrDefault(Progress.ObjectiveKey(questId, o.Id)) >= Math.Max(1, o.Count);

    void RefreshStorySteps()
    {
        OnPropertyChanged(nameof(StoryIsTicket));
        OnPropertyChanged(nameof(StoryShowsSteps));
        OnPropertyChanged(nameof(StoryChapterActive));
        if (SelectedStoryChapter is not { } chapter || StoryIsTicket)
        {
            Replace(StorySteps, []);
            StoryProgressText = "";
            return;
        }
        var q = chapter.Quest;
        var lang = Loc.Lang;
        var decisions = _story.DecisionsOf(q.Id).ToList();
        var steps = StoryPlanner.Steps(q).Select(s =>
        {
            var objective = new ObjectiveVm(q.Id, s.Objective,
                Progress.Completed.Contains(q.Id) ? Math.Max(1, s.Objective.Count)
                    : Progress.Objectives.GetValueOrDefault(Progress.ObjectiveKey(q.Id, s.Objective.Id)),
                OnStoryObjectiveChanged);
            // a part named like the objective needs no heading (it's right above); others (a map, the rewards) keep theirs
            var help = string.Join("\n\n", s.Help
                .Where(h => h.Body.Length > 0 || !string.Equals(h.Title, objective.Text, StringComparison.CurrentCultureIgnoreCase))
                .Select(h =>
                h.Body.Length == 0 ? h.Title
                : string.Equals(h.Title, objective.Text, StringComparison.CurrentCultureIgnoreCase) ? h.Body
                : $"{h.Title}\n{h.Body}"));
            var decision = decisions.FirstOrDefault(d => d.Objective == s.Objective.Id) is { } d ? DecisionVm(d, lang) : null;
            return new StoryStepVm(objective, help, decision);
        }).ToList();
        if (steps.FirstOrDefault(s => !s.Objective.Objective.Optional && !s.Objective.IsDone) is { } current) current.IsCurrent = true;
        Replace(StorySteps, steps);
        StoryProgressText = Loc.F("st.progress", chapter.Done, chapter.Total);
    }

    StoryDecisionVm DecisionVm(StoryDecision d, string lang)
    {
        var chosen = StoryChoice(d);
        return new StoryDecisionVm(d.Title.In(lang), d.Note.In(lang),
            d.Options.Select(o => new StoryOptionVm(d.Id, o.Id, o.Label.In(lang), o.Rewards.In(lang), chosen == o.Id)).ToList());
    }

    /// <summary>
    /// The option picked for <paramref name="d"/>. A choice The Ticket guide also asks is kept in step with it:
    /// picked there, the first matching option shows here.
    /// </summary>
    string? StoryChoice(StoryDecision d)
    {
        Progress.Story.Choices.TryGetValue(d.Id, out var own);
        if (d.TicketDecision == null) return own;
        if (!Progress.Ticket.Choices.TryGetValue(d.TicketDecision, out var ticket)) return null;
        var mine = d.Options.FirstOrDefault(o => o.Id == own);
        return mine?.TicketOption == ticket ? own : d.Options.FirstOrDefault(o => o.TicketOption == ticket)?.Id;
    }

    /// <summary>Records a choice made in game; picking the chosen option again clears it.</summary>
    [RelayCommand]
    void StoryChoose(StoryOptionVm? option)
    {
        if (option == null || _story.Choices.Decisions.FirstOrDefault(d => d.Id == option.DecisionId) is not { } d) return;
        var choices = Progress.Story.Choices;
        if (option.IsChosen) choices.Remove(d.Id);
        else choices[d.Id] = option.OptionId;
        if (d.TicketDecision != null)
        {
            if (option.IsChosen) Progress.Ticket.Choices.Remove(d.TicketDecision);
            else if (d.Options.First(o => o.Id == option.OptionId).TicketOption is { } ticketOption)
                Progress.Ticket.Choices[d.TicketDecision] = ticketOption;
            RefreshTicket();
        }
        SaveProgress();
        RefreshStorySteps();
    }

    void OnStoryObjectiveChanged(ObjectiveVm o)
    {
        OnObjectiveChanged(o);
        // the active quests (and the overlay) show the same objectives: their counters follow
        if (Progress.Active.Contains(o.QuestId)) RefreshActive();
        // after the checkbox binding has finished writing back: the chapter's count and the step to do next
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(() =>
        {
            if (SelectedStoryChapter is { } c && c.Quest.Id == o.QuestId)
            {
                var fresh = ChapterVm(c.Quest);
                c.Update(fresh.Done, fresh.Total);
                StoryProgressText = Loc.F("st.progress", c.Done, c.Total);
                foreach (var step in StorySteps) step.IsCurrent = false;
                if (StorySteps.FirstOrDefault(s => !s.Objective.Objective.Optional && !s.Objective.IsDone) is { } current)
                    current.IsCurrent = true;
            }
        });
    }

    /// <summary>Adds the chapter to the active quests (overlay, raid page) or takes it off.</summary>
    [RelayCommand]
    void StoryToggleActive()
    {
        if (SelectedStoryChapter is not { } c) return;
        ToggleActive(c.Quest);
        RefreshStory();
    }

    [RelayCommand]
    void OpenTicketGuide() => TicketPageRequested?.Invoke();
}
