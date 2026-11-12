using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    readonly ProgressStore _store = new();
    readonly string _dataDir;
    QuestRepository _repo = null!;
    QuestPlanner _planner = null!;
    SearchService _search = null!;
    TicketPlanner _ticket = null!;

    /// <summary>"v0.0.2", from &lt;Version&gt; in TarkovQuestTracker.csproj.</summary>
    public static string AppVersion { get; } =
        typeof(MainViewModel).Assembly.GetName().Version is { } v ? $"v{v.Major}.{v.Minor}.{v.Build}" : "";

    public AppSettings Settings { get; private set; } = new();
    public Progress Progress { get; private set; } = new();
    /// <summary>Flea prices of both modes, kept as local files next to the profiles.</summary>
    public FleaMarket Flea { get; }

    public MainViewModel()
    {
        _dataDir = QuestRepository.FindDataDir(AppContext.BaseDirectory);
        Flea = new FleaMarket(Path.Combine(_store.Folder, "flea"));
        Scanner = CreateScanner();
        Detector = CreateDetector();
        Screenshots.Captured += OnNewScreenshot;
        _raidClock.Tick += OnRaidClockTick;
        _currentProfile = _store.Profile;
        _searchDelay.Tick += (_, _) =>
        {
            _searchDelay.Stop();
            RefreshSearch();
        };
        Loc.Changed += OnLocChanged;
        ReloadProfiles();
        ApplyProfileSettings();
    }


    /// <summary>Raised after switching profile, so the overlay can re-read position and hotkeys.</summary>
    public event Action? ProfileChanged;

    public ObservableCollection<string> Profiles { get; } = [];
    [ObservableProperty] private string _currentProfile;

    partial void OnCurrentProfileChanged(string value)
    {
        if (_loading || value == null || value == _store.Profile) return;
        SaveProgress();
        _store.SwitchProfile(value);
        ApplyProfileSettings();
        Toast = Loc.F("t.profile", value);
        ProfileChanged?.Invoke();
    }

    void ReloadProfiles()
    {
        _loading = true;
        Replace(Profiles, _store.ListProfiles());
        CurrentProfile = _store.Profile;
        _loading = false;
        OnPropertyChanged(nameof(CanDeleteProfile));
    }

    /// <summary>Loads the current profile's settings into the bindable properties, then its progress.</summary>
    void ApplyProfileSettings()
    {
        Settings = _store.LoadSettings();
        _loading = true;
        OverlayOpacity = Settings.OverlayOpacity;
        FleaOpacity = Math.Clamp(Settings.FleaOpacity, MinFleaOpacity, 1);
        OverlayWidth = Settings.OverlayWidth;
        OverlayScale = Math.Clamp(Settings.OverlayScale, MinOverlayScale, MaxOverlayScale);
        UiScale = Math.Clamp(Settings.UiScale, MinUiScale, MaxUiScale);
        OverlayPosition = Math.Clamp(Settings.OverlayPosition, 0, 1);
        OverlayPositionX = Math.Clamp(Settings.OverlayPositionX, 0, 1);
        OverlayDetailed = Settings.OverlayDetailed;
        IsPve = Settings.Mode == GameMode.Pve;
        UiLanguage = Loc.Languages.Contains(Settings.UiLanguage) ? Settings.UiLanguage : "tr";
        _loading = false;
        OnPropertyChanged(nameof(Settings));
        OnPropertyChanged(nameof(CheckForUpdates));
        OnPropertyChanged(nameof(AutoRaidDetect));
        OnPropertyChanged(nameof(AutoRaidDetectOff));
        ApplyOverlaySettings();
        ApplyMapSettings();
        ApplyBtrSettings();
        LoadData();
    }

    /// <summary>Returns an error message, or null on success.</summary>
    public string? CreateProfile(string name)
    {
        if (_store.ValidateName(name) is { } error) return Loc.T(error);
        _store.CreateProfile(name);
        ReloadProfiles();
        CurrentProfile = name.Trim();
        return null;
    }

    public string? RenameProfile(string newName)
    {
        if (_store.ValidateName(newName, ignore: CurrentProfile) is { } error) return Loc.T(error);
        _store.RenameProfile(CurrentProfile, newName);
        ReloadProfiles();
        Toast = Loc.F("t.profile", CurrentProfile);
        return null;
    }

    public bool CanDeleteProfile => Profiles.Count > 1;

    /// <summary>Deletes the current profile and switches to another one.</summary>
    public void DeleteCurrentProfile()
    {
        var doomed = CurrentProfile;
        CurrentProfile = Profiles.First(p => p != doomed);
        _store.DeleteProfile(doomed);
        ReloadProfiles();
        Toast = Loc.F("t.profileDeleted", doomed);
    }


    /// <summary>Quest data languages (tasks_{lang}.json files that exist).</summary>
    public ObservableCollection<Option> Languages { get; } = [];
    public IReadOnlyList<Option> UiLanguages { get; } = Loc.Languages.Select(l => Option.Fixed(l, Loc.LanguageName(l))).ToList();

    public IReadOnlyList<Option> Filters { get; } =
    [
        Option.Localized("all", "f.all"), Option.Localized("available", "f.available"), Option.Localized("active", "f.active"),
        Option.Localized("kappa", "f.kappa"), Option.Localized("notDone", "f.notDone"), Option.Localized("done", "f.done"),
    ];
    public IReadOnlyList<Option> Categories { get; } =
    [
        Option.Localized("", "cat.all"), Option.Localized("side", "cat.side"),
        Option.Localized("story", "cat.story"), Option.Localized("event", "cat.event"),
    ];
    /// <summary>Trader loyalty level the quest needs; key "" = any.</summary>
    public IReadOnlyList<Option> Loyalties { get; } =
    [
        Option.Localized("", "ll.all"), Option.Fixed("1", "LL1"), Option.Fixed("2", "LL2"),
        Option.Fixed("3", "LL3"), Option.Fixed("4", "LL4"),
    ];
    public IReadOnlyList<Option> KappaGroupings { get; } =
        [Option.Localized("map", "k.byMap"), Option.Localized("loyalty", "k.byLoyalty")];
    /// <summary>Key "" = all traders.</summary>
    public ObservableCollection<Option> Traders { get; } = [];
    public string DataInfo { get; private set; } = "";

    [ObservableProperty] private string _language = "en";
    [ObservableProperty] private string _uiLanguage = "tr";

    partial void OnUiLanguageChanged(string value)
    {
        if (value == null) return;
        if (Loc.Lang != value) Loc.Set(value);
        if (_loading || Settings.UiLanguage == value) return;
        Settings.UiLanguage = value;
        SaveSettings();
    }

    /// <summary>Called when the UI string table changed: rebuild every string made in code.</summary>
    void OnLocChanged()
    {
        if (_loading || _repo == null) return;  // a data load follows and builds everything anyway
        foreach (var o in Filters.Concat(Categories).Concat(Traders).Concat(Loyalties).Concat(KappaGroupings)) o.Relabel();
        foreach (var o in _containerGroups ?? []) o.RefreshLabel();
        DataInfo = BuildDataInfo();
        OnPropertyChanged(nameof(DataInfo));
        OnPropertyChanged(nameof(SelectedSources));
        OnPropertyChanged(nameof(GpsStatus));
        Refresh();
    }
    [ObservableProperty] private bool _isPve;

    partial void OnLanguageChanged(string value)
    {
        if (_loading || value == null || value == _repo.Language) return;
        Settings.Language = value;
        SaveSettings();
        LoadData();
    }

    partial void OnIsPveChanged(bool value)
    {
        OnPropertyChanged(nameof(IsPvp));
        OnPropertyChanged(nameof(ModeText));
        if (_loading) return;
        Settings.Mode = value ? GameMode.Pve : GameMode.Pvp;
        SaveSettings();
        LoadData();
        if (_goonTimer != null) _ = RefreshGoonsAsync();  // the Goons of the other mode
    }

    public string ModeText => IsPve ? "PvE" : "PvP";

    /// <summary>Other side of <see cref="IsPve"/> for the PvP | PvE segmented switch.</summary>
    public bool IsPvp
    {
        get => !IsPve;
        set { if (value) IsPve = false; }
    }

    bool _loading;

    void LoadData()
    {
        _loading = true;
        var selectedId = SelectedQuest?.Id;
        _repo = QuestRepository.Load(_dataDir, Settings.Language, Settings.Mode);
        _planner = new QuestPlanner(_repo);
        _search = new SearchService(_repo.Quests);
        _matcher = null;
        Scanner.Clear();  // quests read for the previous mode / profile
        _ticket = TicketPlanner.Load(_dataDir);
        _story = StoryPlanner.Load(_dataDir);
        Progress = _store.LoadProgress(Settings.Mode);
        _undo = null;
        OnPropertyChanged(nameof(CanUndo));

        Replace(Languages, _repo.Languages.Select(l => Option.Fixed(l, Loc.LanguageName(l))));
        Language = _repo.Language;
        Replace(Traders, _repo.Quests.Select(q => q.Trader).Distinct().Order()
                              .Select(t => Option.Fixed(t, t)).Prepend(Option.Localized("", "traders.all")));
        TraderFilter = "";
        DataInfo = BuildDataInfo();
        OnPropertyChanged(nameof(DataInfo));
        OnPropertyChanged(nameof(ModeText));
        OnPropertyChanged(nameof(ProgressFolder));
        SelectedGoal = null;
        _loading = false;

        SelectedQuest = selectedId != null ? _planner.Get(selectedId) : null;
        Refresh();
    }

    string BuildDataInfo()
    {
        var meta = Path.Combine(_dataDir, "meta.json");
        var q = _repo.Quests;
        return Loc.F("d.info", q.Count, ModeText, q.Count(x => x.Category == "side"), q.Count(x => x.Category == "story"),
                     q.Count(x => x.Category == "event"), string.Join(", ", _repo.Languages.Select(Loc.LanguageName)))
               + (q.Any(x => x.TarkovDevId != null) ? "" : "\n" + Loc.T("d.noTarkovDev"))
               + (File.Exists(meta) ? "\n" + File.ReadAllText(meta) : "");
    }


    [ObservableProperty] private string _searchText = "";
    // filter values are Option keys (language independent)
    [ObservableProperty] private string _filter = "all";
    [ObservableProperty] private string _traderFilter = "";
    [ObservableProperty] private string _loyaltyFilter = "";
    // story chapters by default; the full list is paged
    [ObservableProperty] private string _categoryFilter = "story";

    /// <summary>The page of results currently shown; more are appended while scrolling (<see cref="LoadMoreResults"/>).</summary>
    public BulkObservableCollection<QuestRow> SearchResults { get; } = [];
    List<QuestRow> _allResults = [];
    const int PageSize = 60;

    /// <summary>Total matches for the current search/filters (SearchResults may hold fewer).</summary>
    public int SearchTotal => _allResults.Count;

    bool _loadingMore;

    public void LoadMoreResults()
    {
        if (_loadingMore || SearchResults.Count >= _allResults.Count) return;
        _loadingMore = true;
        try
        {
            foreach (var row in _allResults.Skip(SearchResults.Count).Take(PageSize)) SearchResults.Add(row);
        }
        finally
        {
            _loadingMore = false;
        }
    }

    // debounce search input
    readonly DispatcherTimer _searchDelay = new() { Interval = TimeSpan.FromMilliseconds(180) };

    partial void OnSearchTextChanged(string value)
    {
        _searchDelay.Stop();
        _searchDelay.Start();
    }
    partial void OnFilterChanged(string value) => RefreshSearch();
    partial void OnTraderFilterChanged(string value) => RefreshSearch();
    partial void OnLoyaltyFilterChanged(string value) => RefreshSearch();
    partial void OnCategoryFilterChanged(string value) => RefreshSearch();

    void RefreshSearch()
    {
        if (_loading || _repo == null) return;
        var searching = !string.IsNullOrWhiteSpace(SearchText);
        IEnumerable<Quest> source = !searching
            ? _repo.Quests.OrderBy(q => q.Trader).ThenBy(q => q.MinLevel ?? 0).ThenBy(q => q.Name)
            : _search.Search(SearchText, 100);
        if (!string.IsNullOrEmpty(TraderFilter)) source = source.Where(q => q.Trader == TraderFilter);
        if (!string.IsNullOrEmpty(LoyaltyFilter)) source = source.Where(q => q.TraderLoyalty == LoyaltyFilter);
        // a typed name finds the quest whatever its type: a side quest still shows while "Event" is picked
        if (!searching && !string.IsNullOrEmpty(CategoryFilter)) source = source.Where(q => q.Category == CategoryFilter);
        var rows = source.Select(Row);
        rows = Filter switch
        {
            "available" => rows.Where(r => r.Status is QuestStatus.Available or QuestStatus.Active),
            "active" => rows.Where(r => r.Status == QuestStatus.Active),
            "kappa" => rows.Where(r => r.Quest.IsKappa),
            "notDone" => rows.Where(r => !r.IsDone),
            "done" => rows.Where(r => r.IsDone),
            _ => rows,
        };
        // keep already loaded rows so a refresh doesn't jump back to the top
        var shown = Math.Max(PageSize, SearchResults.Count);
        _allResults = rows.ToList();
        SearchResults.ReplaceAll(_allResults.Take(shown));
        OnPropertyChanged(nameof(SearchTotal));
    }

    QuestRow Row(Quest q)
    {
        var status = Progress.Completed.Contains(q.Id) ? QuestStatus.Done
            : Progress.Active.Contains(q.Id) ? QuestStatus.Active
            : !q.Prerequisites.All(Progress.Completed.Contains) ? QuestStatus.Locked
            : QuestStatus.Available;
        return new QuestRow(q, status, Progress.Goals.Contains(q.Id));
    }


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private Quest? _selectedQuest;

    public bool HasSelection => SelectedQuest != null;
    public QuestRow? SelectedRow => SelectedQuest == null ? null : Row(SelectedQuest);
    public IReadOnlyList<QuestRow> SelectedPrerequisites =>
        SelectedQuest?.Prerequisites.Select(id => Row(_repo.ById[id])).ToList() ?? [];
    public IReadOnlyList<QuestRow> SelectedUnlocks =>
        SelectedQuest == null ? [] : _planner.Dependents(SelectedQuest.Id).Select(Row).ToList();
    public string SelectedSources => SelectedQuest == null ? "" :
        Loc.T("s.sources") + string.Join(", ", SelectedQuest.Sources.Select(kv => $"{kv.Key}={JsonSerializer.Serialize(kv.Value)}"));
    public string SelectedOtherNames => SelectedQuest == null ? "" :
        string.Join(" / ", SelectedQuest.AllNames.Where(n => n != SelectedQuest.DisplayName));
    /// <summary>The detail panel shows a link to the endings guide for "The Ticket".</summary>
    public bool IsTicketSelected => SelectedQuest?.Id == TicketGuide.QuestId;

    partial void OnSelectedQuestChanged(Quest? value) => NotifyDetail();

    void NotifyDetail()
    {
        OnPropertyChanged(nameof(IsTicketSelected));
        OnPropertyChanged(nameof(SelectedRow));
        OnPropertyChanged(nameof(SelectedPrerequisites));
        OnPropertyChanged(nameof(SelectedUnlocks));
        OnPropertyChanged(nameof(SelectedSources));
        OnPropertyChanged(nameof(SelectedOtherNames));
    }

    [RelayCommand]
    void Select(object? arg)
    {
        var q = arg switch { Quest x => x, QuestRow r => r.Quest, string id => _planner.Get(id), _ => null };
        if (q != null) SelectedQuest = q;
    }

    [RelayCommand]
    void OpenWiki(Quest? q)
    {
        if (q?.WikiUrl is { Length: > 0 } url) Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }


    static Quest? AsQuest(object? arg) => arg as Quest ?? (arg as QuestRow)?.Quest ?? (arg as ActiveQuestVm)?.Quest;

    [RelayCommand]
    void ToggleComplete(object? arg)
    {
        if (AsQuest(arg) is not { } q) return;
        Snapshot();
        if (Progress.Completed.Contains(q.Id))
        {
            _planner.Uncomplete(Progress, q.Id);
            Toast = Loc.F("t.undone", q.DisplayName);
        }
        else Complete(q);
        SaveProgress();
        Refresh();
    }

    void Complete(Quest q)
    {
        var wasActive = Progress.Active.Contains(q.Id);
        _planner.Complete(Progress, q.Id);
        foreach (var o in q.Objectives) Progress.Objectives.Remove(Progress.ObjectiveKey(q.Id, o.Id));
        // handing in an active quest accepts what it unlocks; marking old quests done from the list does not
        var accepted = wasActive ? _planner.AcceptUnlocked(Progress, q.Id) : [];
        var unlocked = _planner.UnlockedBy(q.Id, Progress);
        Toast = accepted.Count > 0
            ? Loc.F("t.completedAccepted", q.DisplayName, string.Join(", ", accepted.Select(u => u.DisplayName)))
            : unlocked.Count == 0
                ? Loc.F("t.completed", q.DisplayName)
                : Loc.F("t.completedUnlocked", q.DisplayName, string.Join(", ", unlocked.Select(u => u.DisplayName)));
    }

    [RelayCommand]
    void ToggleActive(object? arg)
    {
        if (AsQuest(arg) is not { } q) return;
        if (Progress.Active.Remove(q.Id))
        {
            Progress.Tracked.Remove(q.Id);
            SaveProgress();
            Refresh();
            return;
        }
        Snapshot();
        // active ≠ on the overlay: the player pins quests there with "📌 Overlay"
        var marked = _planner.Start(Progress, q.Id);
        Toast = marked.Count == 0
            ? Loc.F("t.active", q.DisplayName)
            : Loc.F("t.activeChain", q.DisplayName, marked.Count,
                    string.Join(", ", marked.Take(4).Select(m => m.DisplayName)) + (marked.Count > 4 ? ", ..." : ""));
        SaveProgress();
        Refresh();
    }

    // one-step undo for actions that change many quests at once
    (HashSet<string> Completed, List<string> Active, List<string> Tracked)? _undo;

    public bool CanUndo => _undo != null;

    void Snapshot()
    {
        _undo = ([.. Progress.Completed], [.. Progress.Active], [.. Progress.Tracked]);
        OnPropertyChanged(nameof(CanUndo));
    }

    [RelayCommand]
    void Undo()
    {
        if (_undo is not { } u) return;
        Progress.Completed = u.Completed;
        Progress.Active = u.Active;
        Progress.Tracked = u.Tracked;
        _undo = null;
        OnPropertyChanged(nameof(CanUndo));
        Toast = Loc.T("t.undoDone");
        SaveProgress();
        Refresh();
    }

    [RelayCommand]
    void ToggleGoal(object? arg)
    {
        if (AsQuest(arg) is not { } q) return;
        if (!Progress.Goals.Remove(q.Id))
        {
            Snapshot();
            Progress.Goals.Add(q.Id);
            var started = _planner.ActivateGoalSteps(Progress, q.Id);
            Toast = started.Count == 0
                ? Loc.F("t.goal", q.DisplayName)
                : Loc.F("t.goalStarted", q.DisplayName, string.Join(", ", started.Select(s => s.DisplayName)));
            SaveProgress();
            Refresh();
            SelectedGoal = q;
            GoalSearchText = "";
            return;
        }
        SaveProgress();
        Refresh();
    }

    [RelayCommand]
    void ActivateAvailable(string? which)
    {
        var list = which == "kappa" ? KappaAvailable.SelectMany(g => g.Quests).Select(r => r.Quest)
                                     : GoalAvailable.Select(r => r.Quest);
        Snapshot();
        foreach (var q in list.ToList())
            if (!Progress.Active.Contains(q.Id)) Progress.Active.Add(q.Id);
        SaveProgress();
        Refresh();
    }

    /// <summary>Shows / hides an active quest on the overlay; it stays active either way.</summary>
    [RelayCommand]
    void ToggleTracked(object? arg)
    {
        if (AsQuest(arg) is not { } q || !Progress.Active.Contains(q.Id)) return;
        if (!Progress.Tracked.Remove(q.Id)) Progress.Tracked.Add(q.Id);
        SaveProgress();
        RefreshActive();
    }

    [RelayCommand]
    void UntrackAll()
    {
        Progress.Tracked.Clear();
        SaveProgress();
        RefreshActive();
    }

    [RelayCommand]
    void ClearActive()
    {
        Progress.Active.Clear();
        SaveProgress();
        Refresh();
    }

    [ObservableProperty] private string _toast = "";


    public ObservableCollection<Quest> Goals { get; } = [];
    [ObservableProperty] private Quest? _selectedGoal;
    [ObservableProperty] private string _goalSearchText = "";
    public BulkObservableCollection<Quest> GoalSearchResults { get; } = [];
    public BulkObservableCollection<QuestRow> GoalChain { get; } = [];
    public BulkObservableCollection<QuestRow> GoalAvailable { get; } = [];
    [ObservableProperty] private string _goalSummary = "";

    partial void OnGoalSearchTextChanged(string value) =>
        Replace(GoalSearchResults, string.IsNullOrWhiteSpace(value) ? [] : _search.Search(value, 15));

    /// <summary>The planner shows the endings guide under the chain of "The Ticket".</summary>
    public bool IsTicketGoal => SelectedGoal?.Id == TicketGuide.QuestId;

    partial void OnSelectedGoalChanged(Quest? value)
    {
        OnPropertyChanged(nameof(IsTicketGoal));
        if (!_loading) RefreshGoal();
    }

    void RefreshGoal()
    {
        if (SelectedGoal == null || !_repo.ById.ContainsKey(SelectedGoal.Id))
        {
            GoalChain.Clear();
            GoalAvailable.Clear();
            GoalSummary = Loc.T("g.none");
            return;
        }
        var plan = _planner.PlanFor(SelectedGoal.Id, Progress);
        Replace(GoalChain, plan.Chain.Select(Row));
        Replace(GoalAvailable, plan.AvailableNow.Select(Row));
        GoalSummary = plan.Remaining.Count == 0
            ? Loc.F("g.allDone", plan.Goal.DisplayName)
            : Loc.F("g.progress", plan.Goal.DisplayName, plan.Chain.Count - plan.Remaining.Count, plan.Chain.Count, plan.Remaining.Count);
    }


    public BulkObservableCollection<MapGroup> KappaAvailable { get; } = [];
    public BulkObservableCollection<QuestRow> KappaRemaining { get; } = [];
    [ObservableProperty] private double _kappaPercent;
    [ObservableProperty] private string _kappaSummary = "";
    /// <summary>"map" or "loyalty": how the available Kappa quests are grouped.</summary>
    [ObservableProperty] private string _kappaGroupBy = "map";

    partial void OnKappaGroupByChanged(string value)
    {
        if (!_loading && _repo != null) RefreshKappa();
    }

    void RefreshKappa()
    {
        var plan = _planner.KappaPlan(Progress);
        var done = plan.Chain.Count - plan.Remaining.Count;
        KappaPercent = plan.Chain.Count == 0 ? 0 : 100.0 * done / plan.Chain.Count;
        var unknown = _repo.Quests.Count(q => q.Category == "side" && q.KappaRequired == null);
        KappaSummary = Loc.F("k.summary", done, plan.Chain.Count, KappaPercent)
                       + (unknown > 0 ? Loc.F("k.unknown", unknown) : "");
        var available = plan.AvailableNow;
        Replace(KappaAvailable, KappaGroupBy == "loyalty"
            ? available.GroupBy(q => q.TraderLoyalty ?? "")
                       .OrderBy(g => g.Key == "" ? "9" : g.Key)
                       .Select(g => new MapGroup(g.Key == "" ? Loc.T("ll.none") : "LL" + g.Key, g.Select(Row).ToList()))
            : QuestPlanner.GroupByMap(available).Select(g => new MapGroup(g.Key, g.Select(Row).ToList())));
        Replace(KappaRemaining, plan.Remaining.Select(Row));
    }


    public BulkObservableCollection<TraderItems> ItemGroups { get; } = [];
    [ObservableProperty] private string _itemSearchText = "";
    [ObservableProperty] private bool _itemsHideDone = true;
    [ObservableProperty] private bool _itemsFirOnly;
    [ObservableProperty] private string _itemsSummary = "";

    partial void OnItemSearchTextChanged(string value) => RefreshItems();
    partial void OnItemsHideDoneChanged(bool value) => RefreshItems();
    partial void OnItemsFirOnlyChanged(bool value) => RefreshItems();

    readonly HashSet<string> _expandedTraders = [];

    void OnTraderExpandedChanged(string trader, bool expanded)
    {
        if (expanded) _expandedTraders.Add(trader);
        else _expandedTraders.Remove(trader);
    }

    void RefreshItems()
    {
        if (_loading || _repo == null) return;
        var text = ItemSearchText.Trim();
        bool Hit(string s) => s.Contains(text, StringComparison.OrdinalIgnoreCase);
        bool Match(ItemNeed n) => (!ItemsFirOnly || n.Fir)
                                  && (text.Length == 0 || Hit(n.Name) || Hit(n.ShortName) || n.Alternatives.Any(Hit));
        var rows = _repo.Quests
            .Where(q => q.ItemNeeds.Count > 0 && !(ItemsHideDone && Progress.Completed.Contains(q.Id)))
            .OrderBy(q => q.MinLevel ?? 0).ThenBy(q => q.DisplayName)
            .Select(q => new QuestItemsRow(Row(q), q.ItemNeeds.Where(Match).ToList()))
            .Where(r => r.Items.Count > 0)
            .ToList();
        var groups = rows.GroupBy(r => r.Row.Quest.Trader).OrderBy(g => g.Key)
            .Select(g => new TraderItems(g.Key, g.ToList(), OnTraderExpandedChanged)).ToList();
        Replace(ItemGroups, groups);
        // keep the traders the user opened open across refreshes (they reload in the background)
        foreach (var g in groups) g.IsExpanded = _expandedTraders.Contains(g.Trader);
        ItemsSummary = Loc.F("i.summary", rows.Count, rows.SelectMany(r => r.Items).Select(i => i.ItemId).Distinct().Count());
    }


    public BulkObservableCollection<ActiveQuestVm> ActiveQuests { get; } = [];
    public bool HasActive => ActiveQuests.Count > 0;
    /// <summary>The active quests shown on the overlay (same objects as in <see cref="ActiveQuests"/>).</summary>
    public BulkObservableCollection<ActiveQuestVm> TrackedQuests { get; } = [];
    public bool HasTracked => TrackedQuests.Count > 0;
    public string ActiveSummary => Loc.F("a.summary", ActiveQuests.Count, TrackedQuests.Count);

    /// <summary>The map the player is raiding (not saved): the overlay then shows that map's active quests.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInRaid))]
    private string? _raidMap;
    public bool IsInRaid => RaidMap != null;
    /// <summary>Map picker cards, with how many active quests each map has.</summary>
    public BulkObservableCollection<MapCardVm> RaidMaps { get; } = [];
    /// <summary>What the overlay shows: the pinned quests, or in a raid the map's quests (only their objectives there) + the pinned ones.</summary>
    public BulkObservableCollection<ActiveQuestVm> OverlayQuests { get; } = [];
    public bool HasOverlayQuests => OverlayQuests.Count > 0;
    public string RaidSummary => RaidMap == null ? ""
        : Loc.F("raid.active", RaidMap, OverlayQuests.Count) + (HasRaidTime ? " · " + Loc.F("raid.timeLeft", RaidTimeLeft) : "");

    partial void OnRaidMapChanged(string? value)
    {
        if (_repo != null) RefreshActive();
        FollowRaidMap(value);
        ResetBtr();
    }

    [RelayCommand]
    void StartRaid(string? map)
    {
        if (string.IsNullOrEmpty(map)) return;
        RaidMap = map;
        SetRaidClock(null);  // picked by hand: the time left is not known until the exfil list is read
        Toast = Loc.F("t.raidStart", map, OverlayQuests.Count);
    }

    [RelayCommand]
    void EndRaid()
    {
        if (RaidMap == null) return;
        RaidMap = null;
        SetRaidClock(null);
        Toast = Loc.T("t.raidEnd");
    }

    // raid clock: the time left the game showed on the exfil list, counted down here
    readonly DispatcherTimer _raidClock = new() { Interval = TimeSpan.FromSeconds(1) };
    DateTime? _raidEndsAt;

    public bool HasRaidTime => _raidEndsAt != null && RaidMap != null;
    /// <summary>"32:10" (or "1:02:05"), counting down.</summary>
    public string RaidTimeLeft
    {
        get
        {
            if (_raidEndsAt is not { } end) return "";
            var left = end - DateTime.UtcNow;
            if (left < TimeSpan.Zero) left = TimeSpan.Zero;
            return left.TotalHours >= 1 ? left.ToString(@"h\:mm\:ss") : left.ToString(@"mm\:ss");
        }
    }

    /// <summary>
    /// The same digit is misread the same way on every frame, so reads within one "O" press agreeing proves little.
    /// The clock may end the raid only once a separate press (this much later) read it the same.
    /// </summary>
    static readonly TimeSpan SeparatePress = TimeSpan.FromSeconds(15);
    /// <summary>Two reads of one raid end agree within this (a read without seconds is ±30 s).</summary>
    static readonly TimeSpan ClockAgree = TimeSpan.FromSeconds(35);
    /// <summary>A read this much earlier than the running clock waits for a second press before it takes over.</summary>
    static readonly TimeSpan ClockJump = TimeSpan.FromSeconds(90);

    DateTime _raidClockSetAt;
    /// <summary>A second "O" press read the running clock the same: it ends the raid when it runs out.</summary>
    bool _raidEndConfirmed;
    /// <summary>A read that would cut the running clock short, waiting for a second press that agrees.</summary>
    (DateTime EndsAt, DateTime ReadAt)? _earlierRaidEnd;

    void SetRaidClock(DateTime? endsAt)
    {
        OnBtrClockSet(_raidEndsAt, endsAt);
        _raidEndsAt = endsAt;
        _raidClockSetAt = DateTime.UtcNow;
        _raidEndConfirmed = false;
        _earlierRaidEnd = null;
        if (endsAt == null) _raidClock.Stop();
        else _raidClock.Start();
        OnRaidClockTick(null, EventArgs.Empty);
    }

    /// <summary>
    /// The raid clock was read again during the raid. Agreeing with the running clock on a later press confirms it;
    /// a later end is taken at once (a new raid on the same map); an end much earlier only once another press agrees,
    /// so one misread digit can't end the raid half an hour early.
    /// </summary>
    void ReadRaidClock(DateTime endsAt)
    {
        var now = DateTime.UtcNow;
        if (_raidEndsAt is { } current)
        {
            if ((endsAt - current).Duration() <= ClockAgree)
            {
                if (now - _raidClockSetAt >= SeparatePress) _raidEndConfirmed = true;
                return;
            }
            if (endsAt < current - ClockJump)
            {
                if (_earlierRaidEnd is { } earlier && (earlier.EndsAt - endsAt).Duration() <= ClockAgree)
                {
                    if (now - earlier.ReadAt < SeparatePress) return;  // the same press, still open
                    SetRaidClock(endsAt);
                    _raidEndConfirmed = true;
                    return;
                }
                _earlierRaidEnd = (endsAt, now);
                return;
            }
        }
        SetRaidClock(endsAt);
    }

    void OnRaidClockTick(object? sender, EventArgs e)
    {
        if (sender != null && _raidEndsAt is { } end && DateTime.UtcNow >= end)
        {
            if (!_raidEndConfirmed)
            {
                // read on one press only: maybe a misread, so the raid stays on (the next exfil list starts the next one)
                SetRaidClock(null);
                return;
            }
            // the raid time is up: the raid is over whatever happened in it
            RaidMap = null;
            SetRaidClock(null);
            Toast = Loc.T("t.raidTimeUp");
            return;
        }
        OnPropertyChanged(nameof(HasRaidTime));
        OnPropertyChanged(nameof(RaidTimeLeft));
        OnPropertyChanged(nameof(RaidSummary));
        UpdateBtr();
    }

    void RefreshActive()
    {
        Progress.Active.RemoveAll(id => !_repo.ById.ContainsKey(id));
        Progress.Tracked.RemoveAll(id => !Progress.Active.Contains(id));  // completed / removed quests leave the overlay
        Replace(ActiveQuests, Progress.Active.Select(id => _repo.ById[id]).Select(q => new ActiveQuestVm(q,
            q.Objectives.Select(o => new ObjectiveVm(q.Id, o,
                Progress.Objectives.GetValueOrDefault(Progress.ObjectiveKey(q.Id, o.Id)), OnObjectiveChanged)),
            Progress.Tracked.Contains(q.Id))));
        // overlay order follows the order the quests were tracked in
        Replace(TrackedQuests, Progress.Tracked.Select(id => ActiveQuests.First(a => a.Quest.Id == id)));

        // in a raid: the map's quests with only the objectives done there (same ObjectiveVm, counters stay in sync),
        // then the pinned quests that are not on the map
        var onMap = RaidMap is not { } map ? [] : ActiveQuests
            .Select(a => (a, show: RaidFilter.Objectives(a.Quest, map).ToHashSet()))
            .Where(x => x.show.Count > 0)
            .Select(x => new ActiveQuestVm(x.a.Quest, x.a.Objectives.Where(o => x.show.Contains(o.Objective)), x.a.IsTracked))
            .ToList();
        // too many for the overlay ("Any location" quests count on every map): the map's own quests win
        var all = onMap.Concat(TrackedQuests.Where(t => onMap.All(m => m.Quest.Id != t.Quest.Id)))
                       .OrderBy(a => RaidFilter.OverlayRank(a.Quest, RaidMap, a.IsTracked))
                       .ToList();
        var max = Settings.OverlayMaxQuests > 0 ? Settings.OverlayMaxQuests : all.Count;
        Replace(OverlayQuests, all.Take(max));
        OverlayHiddenCount = Math.Max(0, all.Count - max);
        RebuildRaidMaps();
        OnPropertyChanged(nameof(HasActive));
        OnPropertyChanged(nameof(HasTracked));
        OnPropertyChanged(nameof(HasOverlayQuests));
        OnPropertyChanged(nameof(RaidSummary));
        OnPropertyChanged(nameof(ActiveSummary));
        RefreshActiveFilter();
        MapChanged?.Invoke();  // the map's quest spots follow the active quests
    }

    void OnObjectiveChanged(ObjectiveVm o)
    {
        var key = Progress.ObjectiveKey(o.QuestId, o.Objective.Id);
        if (o.Current == 0) Progress.Objectives.Remove(key);
        else Progress.Objectives[key] = o.Current;
        SaveProgress();
        var card = ActiveQuests.FirstOrDefault(a => a.Quest.Id == o.QuestId);
        if (card != null && card.Objectives.Where(x => !x.Objective.Optional).All(x => x.IsDone))
            Toast = Loc.F("t.allObjectives", card.Quest.DisplayName);
        MapChanged?.Invoke();  // a done objective's spot leaves the map
    }


    public BulkObservableCollection<TicketEndingVm> TicketEndings { get; } = [];
    public BulkObservableCollection<TicketStepVm> TicketSteps { get; } = [];
    [ObservableProperty] private string _ticketSummary = "";
    /// <summary>Shown when the target ending is no longer reachable; empty otherwise.</summary>
    [ObservableProperty] private string _ticketWarning = "";
    [ObservableProperty] private string _ticketProgressText = "";

    void RefreshTicket()
    {
        var t = Progress.Ticket;
        var lang = Loc.Lang;
        var route = _ticket.Plan(t, Settings.Mode);
        string Names(IEnumerable<TicketEnding> endings) => string.Join(", ", endings.Select(e => e.Name.In(lang)));

        Replace(TicketEndings, _ticket.Guide.Endings.Select(e =>
        {
            var reachable = !TicketPlanner.Conflicts(e, route.Made);
            var isTarget = route.Target == e;
            var isRoute = route.Ending == e;
            var state = isTarget && !reachable ? Loc.T("tk.state.blocked")
                : isTarget ? Loc.T("tk.state.target")
                : !reachable ? Loc.T("tk.state.closed")
                : isRoute ? Loc.T("tk.state.route")
                : Loc.T("tk.state.open");
            return new TicketEndingVm(e, lang, state, isTarget, reachable, isRoute);
        }));

        Replace(TicketSteps, route.Steps.Select(s =>
        {
            IReadOnlyList<TicketOptionVm> options = [];
            var text = s.Text.In(lang);
            if (s.Kind == "decision" && _ticket.Decision(s.Decision) is { } d)
            {
                text = d.Title.In(lang);
                route.Made.TryGetValue(d.Id, out var chosen);
                route.Effective.TryGetValue(d.Id, out var planned);
                options = d.Options.Select(o => new TicketOptionVm(d.Id, o.Id, o.Label.In(lang),
                    "→ " + Names(_ticket.EndingsAfter(t, d.Id, o.Id)),
                    isChosen: chosen == o.Id, isSuggested: chosen == null && planned == o.Id)).ToList();
            }
            var tag = s.Kind == "step" ? "" : Loc.T("tk.kind." + s.Kind);
            return new TicketStepVm(s, text, tag, t.Done.Contains(s.Id), route.Current == s, options, OnTicketStepToggled);
        }));

        TicketSummary = route.Ending is { } ending
            ? Loc.F("tk.leadsTo", ending.Name.In(lang))
            : Loc.F("tk.stillOpen", Names(route.Possible));
        TicketWarning = route.TargetBlocked ? Loc.F("tk.blocked", route.Target!.Name.In(lang), Names(route.Possible)) : "";
        var checkable = route.Steps.Where(s => s.IsCheckable && s.Kind != "optional").ToList();
        TicketProgressText = Loc.F("tk.progress", checkable.Count(s => t.Done.Contains(s.Id)), checkable.Count)
                             + (route.Pending != null ? Loc.T("tk.progressOpen") : "");
    }

    void OnTicketStepToggled(TicketStepVm step, bool done)
    {
        if (done) Progress.Ticket.Done.Add(step.Step.Id);
        else Progress.Ticket.Done.Remove(step.Step.Id);
        SaveProgress();
        // rebuild after the checkbox binding has finished writing back
        Dispatcher.CurrentDispatcher.BeginInvoke(RefreshTicket);
    }

    [RelayCommand]
    void TicketTarget(string? endingId)
    {
        var t = Progress.Ticket;
        t.Target = t.Target == endingId ? null : endingId;
        SaveProgress();
        RefreshTicket();
    }

    /// <summary>Records a decision made in game; picking the chosen option again clears it.</summary>
    [RelayCommand]
    void TicketChoose(TicketOptionVm? option)
    {
        if (option == null) return;
        var choices = Progress.Ticket.Choices;
        if (option.IsChosen) choices.Remove(option.DecisionId);
        else choices[option.DecisionId] = option.OptionId;
        SaveProgress();
        RefreshTicket();
        RefreshStorySteps();  // a story choice may be this one (the armored case)
    }

    [RelayCommand]
    void TicketReset()
    {
        Progress.Ticket = new TicketProgress();
        SaveProgress();
        RefreshTicket();
        RefreshStorySteps();
        Toast = Loc.T("t.ticketReset");
    }


    [ObservableProperty] private double _overlayOpacity;
    [ObservableProperty] private double _overlayWidth;
    [ObservableProperty] private bool _overlayClickThrough;

    partial void OnOverlayOpacityChanged(double value)
    {
        if (_loading) return;
        Settings.OverlayOpacity = value;
        SaveSettings();
    }

    public const double MinFleaOpacity = 0.3;
    [ObservableProperty] private double _fleaOpacity = 0.9;

    partial void OnFleaOpacityChanged(double value)
    {
        if (_loading) return;
        Settings.FleaOpacity = value;
        SaveSettings();
    }

    partial void OnOverlayWidthChanged(double value)
    {
        if (_loading) return;
        Settings.OverlayWidth = value;
        SaveSettings();
    }

    /// <summary>0 = top of the screen, 1 = bottom. Set by dragging the overlay header or the settings slider.</summary>
    [ObservableProperty] private double _overlayPosition;
    /// <summary>0 = left edge, 1 = right edge. Set by dragging the overlay header or the settings slider.</summary>
    [ObservableProperty] private double _overlayPositionX = 1;
    /// <summary>true = title + details, false = titles only.</summary>
    [ObservableProperty] private bool _overlayDetailed = true;

    partial void OnOverlayPositionChanged(double value)
    {
        if (_loading) return;
        Settings.OverlayPosition = Math.Clamp(value, 0, 1);
        SaveSettings();
    }

    partial void OnOverlayPositionXChanged(double value)
    {
        if (_loading) return;
        Settings.OverlayPositionX = Math.Clamp(value, 0, 1);
        SaveSettings();
    }

    partial void OnOverlayDetailedChanged(bool value)
    {
        if (_loading) return;
        Settings.OverlayDetailed = value;
        SaveSettings();
    }

    /// <summary>Size of the whole main window (text, buttons, spacing), for small / large screens.</summary>
    public const double MinUiScale = 0.25, MaxUiScale = 2.0;
    [ObservableProperty] private double _uiScale = 1.0;

    partial void OnUiScaleChanged(double value)
    {
        var clamped = Math.Round(Math.Clamp(value, MinUiScale, MaxUiScale), 2);
        if (clamped != value)
        {
            UiScale = clamped;
            return;
        }
        if (_loading) return;
        Settings.UiScale = value;
        SaveSettings();
    }

    [RelayCommand]
    void ResetUiScale() => UiScale = 1.0;

    public const double MinOverlayScale = 0.6, MaxOverlayScale = 2.0;
    [ObservableProperty] private double _overlayScale;

    partial void OnOverlayScaleChanged(double value)
    {
        var clamped = Math.Round(Math.Clamp(value, MinOverlayScale, MaxOverlayScale), 2);
        if (clamped != value)
        {
            OverlayScale = clamped;
            return;
        }
        if (_loading) return;
        Settings.OverlayScale = value;
        SaveSettings();
    }

    public void ExportProgress(string path)
    {
        SaveProgress();
        _store.Export(Settings.Mode, path);
        Toast = Loc.F("t.exported", path);
    }

    public void ImportProgress(string path)
    {
        _store.Import(Settings.Mode, path);
        LoadData();
        Toast = Loc.T("t.imported");
    }

    public string ProgressFolder => _store.ProfileFolder;


    WikiUpdater? _updater;
    CancellationTokenSource? _updateCts;
    readonly Queue<string> _updateLines = new();
    const int MaxUpdateLines = 300;

    /// <summary>The scraper scripts are only next to a project checkout, not in a release zip.</summary>
    public bool CanUpdateFromWiki => (_updater ??= WikiUpdater.Find(_dataDir)) != null;
    [ObservableProperty] private bool _isUpdating;
    [ObservableProperty] private string _updateLog = "";

    /// <summary>Runs the scraper scripts, copies the new data in and reloads; clicking again while running stops it.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    async Task UpdateFromWiki()
    {
        if (IsUpdating)
        {
            _updateCts?.Cancel();
            return;
        }
        if (!CanUpdateFromWiki) return;
        IsUpdating = true;
        _updateLines.Clear();
        UpdateLog = "";
        _updateCts = new CancellationTokenSource();
        var dispatcher = Dispatcher.CurrentDispatcher;
        void Log(string line) => dispatcher.BeginInvoke(() =>
        {
            _updateLines.Enqueue(line);
            while (_updateLines.Count > MaxUpdateLines) _updateLines.Dequeue();
            UpdateLog = string.Join("\n", _updateLines);
        });
        try
        {
            if (await Task.Run(() => _updater!.RunAsync(Log, _updateCts.Token)))
            {
                var copied = _updater!.CopyInto(_dataDir);
                SaveProgress();
                LoadData();
                Toast = Loc.F("t.wikiUpdated", copied);
            }
            else Toast = Loc.T("t.wikiFailed");
        }
        catch (OperationCanceledException)
        {
            Toast = Loc.T("t.wikiCancelled");
        }
        finally
        {
            IsUpdating = false;
            _updateCts.Dispose();
            _updateCts = null;
        }
    }


    public void Refresh()
    {
        Replace(Goals, Progress.Goals.Where(_repo.ById.ContainsKey).Select(id => _repo.ById[id]));
        if (SelectedGoal == null || !Goals.Contains(SelectedGoal)) SelectedGoal = Goals.FirstOrDefault();
        RefreshGoal();
        RefreshSearch();
        RefreshKappa();
        RefreshItems();
        RefreshActive();
        RefreshTicket();
        RefreshStory();
        NotifyDetail();
    }

    public void SaveProgress()
    {
        Progress.UpdatedAt = DateTime.UtcNow;
        _store.SaveProgress(Settings.Mode, Progress);
    }

    public void SaveSettings() => _store.SaveSettings(Settings);

    static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        if (target is BulkObservableCollection<T> bulk)
        {
            bulk.ReplaceAll(items);
            return;
        }
        var list = items.ToList();
        target.Clear();
        foreach (var i in list) target.Add(i);
    }
}
