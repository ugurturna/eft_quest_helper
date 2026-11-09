using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.ViewModels;

/// <summary>
/// BTR tracker: on Woods, Streets and Lighthouse, where the BTR should be by the raid clock (data/btr.json) and how long until
/// it reaches each stop (ETA, "TVS" in Turkish). "Seen" on a stop moves the timetable onto what the player saw and drops
/// the routes that don't stop there then. Shown on the map window, the overlay and the raid page, each switchable.
/// </summary>
public sealed partial class MainViewModel
{
    IReadOnlyDictionary<string, BtrMap>? _btrMaps;
    BtrTracker? _btr;
    int _btrRouteIndex;

    /// <summary>data/btr.json, by the map names the quests use.</summary>
    public IReadOnlyDictionary<string, BtrMap> BtrData => _btrMaps ??= BtrRepository.Load(_dataDir);

    /// <summary>The raid's BTR; null out of a raid or on a map without one.</summary>
    public BtrTracker? Btr => _btr;
    public bool BtrAvailable => _btr != null;
    public bool BtrOverlayVisible => BtrAvailable && BtrShowInOverlay;
    public bool BtrCardVisible => BtrAvailable && BtrShowCard;

    /// <summary>Raid seconds left by the clock; null while the clock is not known.</summary>
    public double? BtrTimeLeft => HasRaidTime && _raidEndsAt is { } end ? Math.Max(0, (end - DateTime.UtcNow).TotalSeconds) : null;

    /// <summary>"BTR → Sawmill · ETA 1:20" (the overlay line and the card's title).</summary>
    public string BtrStatus { get; private set; } = "";
    /// <summary>"estimated ±1 min · 3 routes possible: press Seen at the stop you see it at".</summary>
    public string BtrNote { get; private set; } = "";
    public IReadOnlyList<BtrStopRowVm> BtrStops { get; private set; } = [];

    /// <summary>"Auto", "R1", "R2" … for the route picker.</summary>
    public IReadOnlyList<string> BtrRouteChoices =>
        _btr == null ? [] : [Loc.T("btr.auto"), .. _btr.Routes.Select(r => r.Route.Id)];

    /// <summary>0 = every route that fits, else the route picked (1-based in <see cref="Btr"/>.Routes).</summary>
    public int BtrRouteIndex
    {
        get => _btrRouteIndex;
        set
        {
            if (_btr == null || value == _btrRouteIndex || value < 0) return;
            _btrRouteIndex = value;
            _btr.Picked = value == 0 || value > _btr.Routes.Count ? null : _btr.Routes[value - 1].Route.Id;
            OnPropertyChanged();
            UpdateBtr();
        }
    }

    /// <summary>The BTR's guess changed (every second in a raid): the map window redraws it.</summary>
    public event Action? BtrChanged;

    /// <summary>A raid on another map (or none): a fresh BTR for it.</summary>
    void ResetBtr()
    {
        _btr = RaidMap is { } map && BtrData.TryGetValue(map, out var data) ? new BtrTracker(data) : null;
        _btrRouteIndex = 0;
        OnPropertyChanged(nameof(Btr));
        OnPropertyChanged(nameof(BtrAvailable));
        OnPropertyChanged(nameof(BtrOverlayVisible));
        OnPropertyChanged(nameof(BtrCardVisible));
        OnPropertyChanged(nameof(BtrRouteChoices));
        OnPropertyChanged(nameof(BtrRouteIndex));
        UpdateBtr();
    }

    /// <summary>The raid clock was set: a raid end far from the old one is a new raid, whose BTR starts over.</summary>
    void OnBtrClockSet(DateTime? before, DateTime? now)
    {
        if (_btr == null || now == null) return;
        if (before is { } old && (old - now.Value).Duration() < TimeSpan.FromMinutes(3)) return;
        _btr.Reset();
    }

    /// <summary>Works the texts and stop list out again for the clock now.</summary>
    void UpdateBtr()
    {
        var timeLeft = BtrTimeLeft;
        if (_btr is not { } btr)
        {
            (BtrStatus, BtrNote, BtrStops) = ("", "", []);
        }
        else if (timeLeft is not { } t)
        {
            BtrStatus = Loc.T("btr.noClock");
            BtrNote = Loc.T("btr.noClockNote");
            BtrStops = btr.Map.Stops.Select(kv => new BtrStopRowVm(kv.Key, kv.Value.Name, "—", false, "")).ToList();
        }
        else
        {
            BtrStatus = Status(btr, t);
            var notes = new List<string>();
            if (btr.Map.Estimated) notes.Add(Loc.F("btr.estimated", (int)Math.Round(btr.Map.VarianceSeconds / 60)));
            notes.Add(btr.Candidates.Count > 1 ? Loc.F("btr.routes", btr.Candidates.Count)
                      : Loc.F("btr.route", btr.Candidates.FirstOrDefault()?.Route.Id ?? "?"));
            if (btr.Sightings.Count > 0) notes.Add(Loc.F("btr.seenCount", btr.Sightings.Count));
            BtrNote = string.Join(" · ", notes);
            BtrStops = btr.Etas(t).Select(e => new BtrStopRowVm(e.StopId, e.Name,
                e.IsHere ? Loc.T("btr.here") : e.Seconds is { } s ? Clock(s) : "—", e.IsHere,
                btr.Candidates.Count > 1 ? e.RouteId ?? "" : "")).ToList();
        }
        OnPropertyChanged(nameof(BtrTimeLeft));
        OnPropertyChanged(nameof(BtrStatus));
        OnPropertyChanged(nameof(BtrNote));
        OnPropertyChanged(nameof(BtrStops));
        BtrChanged?.Invoke();
    }

    /// <summary>"BTR → Sawmill · ETA 1:20", "BTR at Sawmill (0:45) → Junction", "BTR spawns in 2:10".</summary>
    static string Status(BtrTracker btr, double timeLeft)
    {
        if (btr.Nearest(timeLeft) is not { } nearest) return Loc.T("btr.gone");
        var fix = nearest.Fix;
        string Name(string? id) => id != null && btr.Map.Stops.TryGetValue(id, out var s) ? s.Name : "?";
        return fix.State switch
        {
            BtrState.NotSpawned => Loc.F("btr.notSpawned", Clock(timeLeft - nearest.Schedule.Track[0].TimeLeft), Name(fix.NextStopId)),
            BtrState.Stopped => Loc.F("btr.at", Name(fix.StopId), Clock(fix.Wait), Name(fix.NextStopId)),
            _ => Loc.F("btr.to", Name(fix.NextStopId), Clock(fix.Eta ?? 0)),
        };
    }

    /// <summary>"1:05" (minutes:seconds).</summary>
    public static string Clock(double seconds)
    {
        var s = (int)Math.Max(0, Math.Round(seconds));
        return string.Create(CultureInfo.InvariantCulture, $"{s / 60}:{s % 60:00}");
    }

    /// <summary>The player sees the BTR at this stop now.</summary>
    [RelayCommand]
    void BtrSeen(string? stopId)
    {
        if (_btr == null || stopId == null) return;
        if (BtrTimeLeft is not { } t)
        {
            Toast = Loc.T("btr.noClock");
            return;
        }
        _btr.Seen(stopId, t);
        Toast = Loc.F("t.btr.seen", _btr.Map.Stops.TryGetValue(stopId, out var stop) ? stop.Name : stopId, Clock(t));
        UpdateBtr();
    }

    /// <summary>Forgets the sightings (and the route picked).</summary>
    [RelayCommand]
    void BtrReset()
    {
        if (_btr == null) return;
        _btr.Reset();
        _btrRouteIndex = 0;
        _btr.Picked = null;
        OnPropertyChanged(nameof(BtrRouteIndex));
        UpdateBtr();
    }

    // settings

    public bool BtrShowOnMap
    {
        get => Settings.BtrShowOnMap;
        set
        {
            if (Settings.BtrShowOnMap == value) return;
            Settings.BtrShowOnMap = value;
            SaveSettings();
            OnPropertyChanged();
            BtrChanged?.Invoke();
        }
    }

    public bool BtrShowInOverlay
    {
        get => Settings.BtrShowInOverlay;
        set
        {
            if (Settings.BtrShowInOverlay == value) return;
            Settings.BtrShowInOverlay = value;
            SaveSettings();
            OnPropertyChanged();
            OnPropertyChanged(nameof(BtrOverlayVisible));
        }
    }

    public bool BtrShowCard
    {
        get => Settings.BtrShowCard;
        set
        {
            if (Settings.BtrShowCard == value) return;
            Settings.BtrShowCard = value;
            SaveSettings();
            OnPropertyChanged();
            OnPropertyChanged(nameof(BtrCardVisible));
        }
    }

    /// <summary>After a profile's settings load.</summary>
    void ApplyBtrSettings()
    {
        OnPropertyChanged(nameof(BtrShowOnMap));
        OnPropertyChanged(nameof(BtrShowInOverlay));
        OnPropertyChanged(nameof(BtrShowCard));
        OnPropertyChanged(nameof(BtrOverlayVisible));
        OnPropertyChanged(nameof(BtrCardVisible));
        BtrChanged?.Invoke();
    }
}
