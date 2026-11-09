using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.ViewModels;

/// <summary>Goon tracker: where the Goons were last seen in the mode being played (tarkov-goon-tracker.com).</summary>
public sealed partial class MainViewModel
{
    /// <summary>The site's reports are fetched again after this long; the "… ago" texts tick every minute.</summary>
    static readonly TimeSpan GoonMaxAge = TimeSpan.FromMinutes(5);
    const int GoonRows = 5;

    DispatcherTimer? _goonTimer;
    DateTime _goonFetchedAt;
    GameMode? _goonMode;

    [ObservableProperty] private IReadOnlyList<GoonSighting> _goonSightings = [];
    [ObservableProperty] private string _goonStatus = "";
    [ObservableProperty] private bool _isLoadingGoons;

    public bool HasGoons => GoonSightings.Count > 0;
    public bool HasGoonStatus => GoonStatus.Length > 0;

    partial void OnGoonStatusChanged(string value) => OnPropertyChanged(nameof(HasGoonStatus));
    /// <summary>"Customs", the newest report's map; "" before the first one.</summary>
    public string GoonLastMap => GoonSightings.FirstOrDefault()?.MapName ?? "";
    public string GoonLastSeenText => GoonSightings.FirstOrDefault() is { } s ? Ago(s.SeenAt) : "";
    public IReadOnlyList<GoonRowVm> GoonRecent =>
        GoonSightings.Take(GoonRows).Select(s => new GoonRowVm(s.MapName, Ago(s.SeenAt), s.Reporter)).ToList();

    partial void OnGoonSightingsChanged(IReadOnlyList<GoonSighting> value)
    {
        RelabelGoons();
        RebuildRaidMaps();  // the Goons' map gets its badge
    }

    void RelabelGoons()
    {
        OnPropertyChanged(nameof(HasGoons));
        OnPropertyChanged(nameof(GoonLastMap));
        OnPropertyChanged(nameof(GoonLastSeenText));
        OnPropertyChanged(nameof(GoonRecent));
    }

    /// <summary>The "entering a raid" map cards: maps with active quests first, the Goons' map badged.</summary>
    void RebuildRaidMaps()
    {
        if (_repo == null) return;
        Replace(RaidMaps, RaidFilter.Maps(_repo.Quests)
            .Select(m => new MapCardVm(m, ActiveQuests.Count(a => RaidFilter.Objectives(a.Quest, m).Count > 0), m == RaidMap, IsGoonMap(m)))
            .OrderByDescending(c => c.ActiveCount > 0));
    }

    bool IsGoonMap(string map) =>
        GoonSightings.FirstOrDefault() is { } s && string.Equals(s.MapName, map, StringComparison.OrdinalIgnoreCase);

    /// <summary>App start: first fetch now, then one-minute ticks (texts every tick, the site every <see cref="GoonMaxAge"/>).</summary>
    public void StartGoonTracking()
    {
        if (_goonTimer != null) return;
        _goonTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _goonTimer.Tick += (_, _) =>
        {
            if (DateTime.UtcNow - _goonFetchedAt >= GoonMaxAge) _ = RefreshGoonsAsync();
            else RelabelGoons();
        };
        _goonTimer.Start();
        _ = RefreshGoonsAsync();
    }

    [RelayCommand]
    Task RefreshGoons() => RefreshGoonsAsync();

    async Task RefreshGoonsAsync()
    {
        if (IsLoadingGoons) return;
        var mode = Settings.Mode;
        if (_goonMode != mode) GoonSightings = [];  // the other mode's reports would point at the wrong map
        _goonMode = mode;
        IsLoadingGoons = true;
        GoonStatus = HasGoons ? "" : Loc.T("goon.loading");
        try
        {
            var sightings = await GoonTracker.FetchAsync(mode);
            if (Settings.Mode != mode) return;  // switched while loading: fetched again below
            _goonFetchedAt = DateTime.UtcNow;
            GoonSightings = sightings;
            GoonStatus = sightings.Count == 0 ? Loc.T("goon.none") : "";
        }
        catch (Exception e) when (GoonTracker.IsFetchError(e))
        {
            // keep what is shown; try again on the next fetch
            _goonFetchedAt = DateTime.UtcNow;
            GoonStatus = Loc.F("goon.failed", e.Message);
        }
        finally
        {
            IsLoadingGoons = false;
            if (Settings.Mode != mode && _goonTimer != null) _ = RefreshGoonsAsync();
        }
    }

    [RelayCommand]
    void OpenGoonSite() => OpenUrl(GoonTracker.PageUrl(Settings.Mode));

    /// <summary>"just now", "12 min ago", "2 h 5 min ago", "3 days ago".</summary>
    static string Ago(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span < TimeSpan.FromMinutes(1)) return Loc.T("goon.justNow");
        if (span < TimeSpan.FromHours(1)) return Loc.F("goon.minAgo", (int)span.TotalMinutes);
        if (span < TimeSpan.FromDays(1)) return Loc.F("goon.hourAgo", (int)span.TotalHours, span.Minutes);
        return Loc.F("goon.dayAgo", (int)span.TotalDays);
    }
}
