using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.ViewModels;

/// <summary>Screen import: quests read off the in-game Tasks screen become active after the player confirms.</summary>
public sealed partial class MainViewModel
{
    QuestTextMatcher? _matcher;

    public ScreenScanner Scanner { get; }

    /// <summary>Read quests on the "Read screen" page; the ticks are the player's.</summary>
    public ObservableCollection<ImportRow> ImportRows { get; } = [];

    ScreenScanner CreateScanner()
    {
        var scanner = new ScreenScanner(() => _matcher ??= new QuestTextMatcher(_repo.Quests));
        scanner.Message += text => Toast = text;
        scanner.Changed += OnScannerChanged;
        return scanner;
    }

    void OnScannerChanged()
    {
        // keep the rows (and their ticks) that are there, add the newly read ones
        if (Scanner.Found.Count < ImportRows.Count) ImportRows.Clear();
        foreach (var q in Scanner.Found.Skip(ImportRows.Count))
            ImportRows.Add(new ImportRow(q,
                Progress.Active.Contains(q.Id) ? Loc.T("imp.already") : Progress.Completed.Contains(q.Id) ? Loc.T("imp.done") : ""));
        OnPropertyChanged(nameof(IsScanning));
        OnPropertyChanged(nameof(HasPendingImport));
        OnPropertyChanged(nameof(PendingImportText));
        OnPropertyChanged(nameof(ScanStatusText));
        OnPropertyChanged(nameof(ScanRemaining));
        OnPropertyChanged(nameof(ScanButtonText));
    }

    public bool IsScanning => Scanner.IsScanning;
    public bool HasPendingImport => Scanner.Found.Count > 0 && !Scanner.IsScanning;
    public string PendingImportText => Loc.F("imp.banner", Scanner.Found.Count);
    /// <summary>Seconds left, as a fraction of the whole scan (the countdown bar).</summary>
    public double ScanRemaining => Scanner.Remaining / ScreenScanner.MaxScan;
    public string ScanButtonText => Loc.T(Scanner.IsScanning ? "imp.stop" : "imp.start");
    public string ScanStatusText => Scanner.IsScanning
        ? Loc.F("imp.scanning", (int)Math.Ceiling(Scanner.Remaining.TotalSeconds), Scanner.Found.Count)
        : Scanner.Found.Count > 0 ? Loc.F("imp.found", Scanner.Found.Count) : Loc.F("imp.idle", (int)ScreenScanner.MaxScan.TotalSeconds);

    [RelayCommand]
    void ToggleScan() => Scanner.ToggleScan();

    /// <summary>Ticked quests become active; their prerequisite chains count as done (<see cref="QuestPlanner.Start"/>).</summary>
    [RelayCommand]
    void ApplyImport()
    {
        var list = ImportRows.Where(r => r.IsChecked && !Progress.Active.Contains(r.Quest.Id)).Select(r => r.Quest).ToList();
        Scanner.Clear();
        if (list.Count == 0) return;
        Snapshot();
        var marked = list.Sum(q => _planner.Start(Progress, q.Id).Count);
        Toast = Loc.F("t.scan.imported", list.Count, marked);
        SaveProgress();
        Refresh();
    }

    [RelayCommand]
    void ClearImport() => Scanner.Clear();

    ExtractMatcher? _extracts;

    /// <summary>Starts the raid from the in-game exfil list (double "O").</summary>
    public RaidDetector Detector { get; }

    RaidDetector CreateDetector()
    {
        var detector = new RaidDetector(() => _extracts ??= ExtractMatcher.Load(_dataDir));
        detector.Sighted += OnRaidSighted;
        detector.Glimpsed += () => OverlayPeek?.Invoke();
        return detector;
    }

    /// <summary>
    /// The exfil list was read. Same map: the raid is already on (the player checks the exfils mid-raid), so only
    /// the clock is set. Another map, or no raid: the raid starts on that map. Only the clock (a single "O"): it sets
    /// the time of the raid running; with no raid it says nothing about the map, so nothing happens.
    /// </summary>
    void OnRaidSighted(RaidDetector.Sighting s)
    {
        if (!Settings.AutoRaidDetect) return;  // the corner is only read for the auto-hide
        // under a minute left is far more likely a misread than a raid in its last minute, and it would end the raid
        var endsAt = s.TimeLeft is { } left && left >= TimeSpan.FromMinutes(1) ? s.ReadAt + left : (DateTime?)null;
        if (s.Map == null)
        {
            if (RaidMap != null && endsAt != null) SetRaidClock(endsAt);
            return;
        }
        if (s.Map == RaidMap)
        {
            if (endsAt != null) SetRaidClock(endsAt);
            return;
        }
        RaidMap = s.Map;
        SetRaidClock(endsAt);
        Toast = Loc.F("t.raidAuto", s.Map, OverlayQuests.Count);
    }

    public bool AutoRaidDetect
    {
        get => Settings.AutoRaidDetect;
        set
        {
            if (Settings.AutoRaidDetect == value) return;
            Settings.AutoRaidDetect = value;
            UpdateDetector();
            SaveSettings();
            OnPropertyChanged();
            OnPropertyChanged(nameof(AutoRaidDetectOff));
        }
    }

    /// <summary>Other side of <see cref="AutoRaidDetect"/> for the On | Off segmented switch.</summary>
    public bool AutoRaidDetectOff
    {
        get => !AutoRaidDetect;
        set { if (value) AutoRaidDetect = false; }
    }
}

/// <summary>One quest in the screen import confirm list.</summary>
public sealed class ImportRow(Quest quest, string state)
{
    public Quest Quest { get; } = quest;
    public string Name => Quest.DisplayName;
    public string Trader => Quest.Trader;
    /// <summary>"already active" / "marked done", empty for a new one.</summary>
    public string State { get; } = state;
    public bool HasState => State.Length > 0;
    /// <summary>New ones are ticked; an already done quest only if the player says so (the screen may list finished ones).</summary>
    public bool IsChecked { get; set; } = state.Length == 0;
}
