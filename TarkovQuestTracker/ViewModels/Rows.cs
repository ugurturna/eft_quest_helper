using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.ViewModels;

public enum QuestStatus { Done, Active, Available, LevelLocked, Locked }

/// <summary>ObservableCollection that swaps its whole content with a single Reset notification.</summary>
public sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    public void ReplaceAll(IEnumerable<T> items)
    {
        var list = items.ToList();
        Items.Clear();
        foreach (var item in list) Items.Add(item);
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}

/// <summary>A quest in a list, with its state for the current progress.</summary>
public sealed class QuestRow(Quest quest, QuestStatus status, bool isGoal)
{
    public Quest Quest { get; } = quest;
    public QuestStatus Status { get; } = status;
    public bool IsGoal { get; } = isGoal;
    public bool IsDone => Status == QuestStatus.Done;

    public string StatusText => Status switch
    {
        QuestStatus.Done => Loc.T("st.done"),
        QuestStatus.Active => Loc.T("st.active"),
        QuestStatus.Available => Loc.T("st.available"),
        QuestStatus.LevelLocked => Loc.F("st.level", Quest.MinLevel),
        _ => Loc.T("st.locked"),
    };

    /// <summary>"STORY" / "EVENT" (UI language), empty for side quests.</summary>
    public string CategoryBadge => Quest.Category switch
    {
        "story" => Loc.T("badge.story"),
        "event" => Loc.T("badge.event"),
        _ => "",
    };
}

public sealed class MapGroup(string map, IReadOnlyList<QuestRow> quests)
{
    public string Map { get; } = map == QuestPlanner.AnyMap ? Loc.T("map.any") : map;
    public IReadOnlyList<QuestRow> Quests { get; } = quests;
    public string Header => $"{Map}  ({Quests.Count})";
}

/// <summary>"Items" tab: one trader's quests that need items.</summary>
public sealed class TraderItems(string trader, IReadOnlyList<QuestItemsRow> quests)
{
    public string Trader { get; } = trader;
    public IReadOnlyList<QuestItemsRow> Quests { get; } = quests;
    public string Header => $"{Trader}  ({Quests.Count})";
}

public sealed class QuestItemsRow(QuestRow row, IReadOnlyList<ItemNeed> items)
{
    public QuestRow Row { get; } = row;
    public IReadOnlyList<ItemNeed> Items { get; } = items;
}

/// <summary>"Ticket endings" tab: one ending card.</summary>
public sealed class TicketEndingVm(TicketEnding ending, string lang, string state, bool isTarget, bool isReachable, bool isRoute)
{
    public string Id { get; } = ending.Id;
    public string Name { get; } = ending.Name.In(lang);
    public string Summary { get; } = ending.Summary.In(lang);
    /// <summary>The ending's achievement picture.</summary>
    public string Image { get; } = TicketImages.For(ending.Id);
    public string StateText { get; } = state;
    public bool IsTarget { get; } = isTarget;
    /// <summary>Still possible with the decisions already made.</summary>
    public bool IsReachable { get; } = isReachable;
    /// <summary>The ending the current route leads to.</summary>
    public bool IsRoute { get; } = isRoute;
}

/// <summary>"Ticket endings" tab: one line of the route (step, decision, achievement, note...).</summary>
public sealed class TicketStepVm(TicketStep step, string text, string tag, bool isDone, bool isCurrent,
                                 IReadOnlyList<TicketOptionVm> options, Action<TicketStepVm, bool> toggled)
{
    public TicketStep Step { get; } = step;
    public string Kind => Step.Kind;
    public string Text { get; } = text;
    /// <summary>Short label in front of the text ("NO RETURN", "ACHIEVEMENT"...), empty for plain steps.</summary>
    public string Tag { get; } = tag;
    public bool HasTag => Tag.Length > 0;
    public bool IsCheckable => Step.IsCheckable;
    public bool IsPlain => !Step.IsCheckable;
    public bool IsCurrent { get; } = isCurrent;
    public IReadOnlyList<TicketOptionVm> Options { get; } = options;
    public bool IsDecision => Options.Count > 0;
    /// <summary>Ending steps show the ending's picture.</summary>
    public string? Image { get; } = step.Kind == "ending" && step.Endings.Count == 1 ? TicketImages.For(step.Endings[0]) : null;
    public bool HasImage => Image != null;

    public bool IsDone
    {
        get => isDone;
        set
        {
            if (value == isDone) return;
            isDone = value;
            toggled(this, value);
        }
    }
}

static class TicketImages
{
    /// <summary>Assets/Endings/{id}.png, cut from the "Story Endings" flowchart.</summary>
    public static string For(string endingId) => $"pack://application:,,,/Assets/Endings/{endingId}.png";
}

/// <summary>One answer of a Ticket decision, with the endings it keeps open.</summary>
public sealed class TicketOptionVm(string decisionId, string optionId, string label, string endings, bool isChosen, bool isSuggested)
{
    public string DecisionId { get; } = decisionId;
    public string OptionId { get; } = optionId;
    public string Label { get; } = label;
    /// <summary>"→ Savior, Debtor"</summary>
    public string EndingsText { get; } = endings;
    /// <summary>The player made this choice in game.</summary>
    public bool IsChosen { get; } = isChosen;
    /// <summary>Not chosen yet, but the target ending needs it.</summary>
    public bool IsSuggested { get; } = isSuggested;
}

/// <summary>An active quest card (main window "Aktif" tab and the overlay).</summary>
public sealed class ActiveQuestVm : ObservableObject
{
    public ActiveQuestVm(Quest quest, IEnumerable<ObjectiveVm> objectives, bool isTracked)
    {
        Quest = quest;
        IsTracked = isTracked;
        Objectives = objectives.ToList();
        foreach (var o in Objectives) o.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Progress));
    }

    public Quest Quest { get; }
    public IReadOnlyList<ObjectiveVm> Objectives { get; }
    /// <summary>Shown on the overlay.</summary>
    public bool IsTracked { get; }
    public string Progress => $"{Objectives.Count(o => o.IsDone)}/{Objectives.Count}";
}

public sealed partial class ObjectiveVm : ObservableObject
{
    readonly Action<ObjectiveVm> _changed;

    public ObjectiveVm(string questId, Objective objective, int current, Action<ObjectiveVm> changed)
    {
        QuestId = questId;
        Objective = objective;
        _current = current;
        _changed = changed;
    }

    public string QuestId { get; }
    public Objective Objective { get; }
    public string Text => Objective.DisplayText;
    public int Count => Math.Max(1, Objective.Count);
    public bool IsCounter => Count > 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDone), nameof(CounterText))]
    private int _current;

    public bool IsDone
    {
        get => Current >= Count;
        set => Set(value ? Count : 0);
    }

    public string CounterText => $"{Current}/{Count}";

    [RelayCommand] void Increment() => Set(Current + 1);
    [RelayCommand] void Decrement() => Set(Current - 1);

    void Set(int value)
    {
        value = Math.Clamp(value, 0, Count);
        if (value == Current) return;
        Current = value;
        _changed(this);
    }
}
