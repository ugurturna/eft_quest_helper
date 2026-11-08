using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.ViewModels;

/// <summary>Active Quests page: a search box and a map choice over the active quests.</summary>
public sealed partial class MainViewModel
{
    /// <summary>The active quests the search and the map choice let through (same objects as <see cref="ActiveQuests"/>).</summary>
    public BulkObservableCollection<ActiveQuestVm> ActiveShown { get; } = [];

    /// <summary>"All maps", the maps the active quests use, "Any location".</summary>
    public ObservableCollection<Option> ActiveMaps { get; } = [];

    [ObservableProperty] private string _activeSearch = "";
    /// <summary>Key of <see cref="ActiveMaps"/>: "" = all, a map, or <see cref="QuestPlanner.AnyMap"/>.</summary>
    [ObservableProperty] private string _activeMap = "";

    partial void OnActiveSearchChanged(string value) => RefreshActiveShown();
    partial void OnActiveMapChanged(string value) => RefreshActiveShown();

    public bool IsActiveFiltered => !string.IsNullOrWhiteSpace(ActiveSearch) || !string.IsNullOrEmpty(ActiveMap);
    /// <summary>"5 of 23 shown" while filtering.</summary>
    public string ActiveShownText => Loc.F("a.shown", ActiveShown.Count, ActiveQuests.Count);
    public bool HasNoActiveShown => ActiveQuests.Count > 0 && ActiveShown.Count == 0;

    [RelayCommand]
    void ClearActiveFilter()
    {
        ActiveSearch = "";
        ActiveMap = "";
    }

    /// <summary>After the active quests changed: the map list follows them, the choice stays when it still exists.</summary>
    void RefreshActiveFilter()
    {
        var chosen = ActiveMap ?? "";
        Replace(ActiveMaps, RaidFilter.Maps(ActiveQuests.Select(a => a.Quest))
                                      .Select(m => Option.Fixed(m, m))
                                      .Prepend(Option.Localized("", "a.allMaps"))
                                      .Append(Option.Localized(QuestPlanner.AnyMap, "a.anyMap")));
        if (ActiveMaps.All(o => o.Key != chosen)) chosen = "";
        if (chosen != ActiveMap)
        {
            ActiveMap = chosen;  // refreshes the list
            return;
        }
        OnPropertyChanged(nameof(ActiveMap));  // the combo box lost its selection with the old items
        RefreshActiveShown();
    }

    void RefreshActiveShown()
    {
        Replace(ActiveShown, ActiveQuests.Where(a => ActiveQuestFilter.Matches(a.Quest, ActiveMap, ActiveSearch)));
        OnPropertyChanged(nameof(IsActiveFiltered));
        OnPropertyChanged(nameof(ActiveShownText));
        OnPropertyChanged(nameof(HasNoActiveShown));
    }
}
