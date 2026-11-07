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

    /// <summary>"STORY" / "EVENT" / "PAST EVENT" (UI language), empty for side quests.</summary>
    public string CategoryBadge => Quest.Category switch
    {
        "story" => Loc.T("badge.story"),
        "event" => Loc.T(Quest.IsPastEvent ? "badge.pastEvent" : "badge.event"),
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

/// <summary>An active quest card (main window "Aktif" tab and the overlay).</summary>
public sealed class ActiveQuestVm : ObservableObject
{
    public ActiveQuestVm(Quest quest, IEnumerable<ObjectiveVm> objectives)
    {
        Quest = quest;
        Objectives = objectives.ToList();
        foreach (var o in Objectives) o.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Progress));
    }

    public Quest Quest { get; }
    public IReadOnlyList<ObjectiveVm> Objectives { get; }
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
