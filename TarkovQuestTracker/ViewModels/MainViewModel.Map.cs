using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.ViewModels;

/// <summary>An active quest's objective spot on the map window.</summary>
public sealed record MapQuestSpot(string Quest, string Objective, IReadOnlyList<double[]> Positions);

/// <summary>
/// Map window: the player's position from the game's screenshot file names (press the screenshot key in raid),
/// on tarkov.dev's map of the raid's map, with exfils and the active quests' objective spots.
/// </summary>
public sealed partial class MainViewModel
{
    IReadOnlyDictionary<string, MapGeometry>? _maps;

    /// <summary>data/maps.json, by the map names the quests use.</summary>
    public IReadOnlyDictionary<string, MapGeometry> MapData => _maps ??= MapRepository.Load(_dataDir);

    /// <summary>Maps the window can draw, in the raid picker's order.</summary>
    public IReadOnlyList<string> MapNames => RaidFilter.InOrder(MapData.Keys);

    /// <summary>Downloaded map pictures (SVG, tiles), next to the profiles.</summary>
    public string MapCacheFolder => Path.Combine(_store.Folder, "maps");

    public ScreenshotWatcher Screenshots { get; } = new();

    /// <summary>The map shown on the map window: the raid's map, or the one picked there.</summary>
    [ObservableProperty] private string? _mapName;

    /// <summary>Where the last screenshot on <see cref="MapName"/> was taken; null before the first one.</summary>
    [ObservableProperty] private ScreenshotPosition? _playerPosition;

    /// <summary>The screenshots of this raid on this map, oldest first (the path walked).</summary>
    public List<ScreenshotPosition> PlayerTrail { get; } = [];

    /// <summary>What the map window draws changed: position, trail, quests, marker settings.</summary>
    public event Action? MapChanged;

    /// <summary>The map buttons: the overlay owns the window.</summary>
    public event Action? MapToggleRequested;

    /// <summary>A new screenshot came in and <see cref="MapOpenOnScreenshot"/> is on: the window comes up (without focus).</summary>
    public event Action? MapShowRequested;

    [RelayCommand]
    void ToggleMap() => MapToggleRequested?.Invoke();

    bool _followingCapture;

    partial void OnMapNameChanged(string? value)
    {
        OnPropertyChanged(nameof(GpsStatus));
        if (_followingCapture) return;
        // another map: the old position and path were on the old one
        PlayerTrail.Clear();
        PlayerPosition = null;
        MapChanged?.Invoke();
    }

    /// <summary>A raid started (picked or detected): the map window follows it with a fresh path.</summary>
    void FollowRaidMap(string? map)
    {
        if (map == null || !MapData.ContainsKey(map)) return;
        if (MapName == map)
        {
            PlayerTrail.Clear();
            PlayerPosition = null;
            MapChanged?.Invoke();
        }
        else MapName = map;
    }

    void OnScreenshot(ScreenshotPosition p)
    {
        _followingCapture = true;
        try
        {
            var map = RaidMap is { } raid && MapData.ContainsKey(raid) ? raid : MapName ?? GuessMap(p);
            if (map != MapName)
            {
                PlayerTrail.Clear();
                MapName = map;
            }
        }
        finally
        {
            _followingCapture = false;
        }
        PlayerTrail.Add(p);
        if (PlayerTrail.Count > 200) PlayerTrail.RemoveAt(0);
        PlayerPosition = p;
        MapChanged?.Invoke();
        if (MapName == null) Toast = Loc.T("t.map.pick");
    }

    /// <summary>A screenshot taken just now in game (the folder watcher, not the "read again" button).</summary>
    void OnNewScreenshot(ScreenshotPosition p)
    {
        OnScreenshot(p);
        if (Settings.MapOpenOnScreenshot) MapShowRequested?.Invoke();
    }

    /// <summary>With no raid and no map picked: the only map whose picture holds the point, if just one does.</summary>
    string? GuessMap(ScreenshotPosition p)
    {
        var holding = MapData.Values.Where(m =>
        {
            var b = m.Bounds;
            return b.Length == 2
                   && p.X >= Math.Min(b[0][0], b[1][0]) && p.X <= Math.Max(b[0][0], b[1][0])
                   && p.Z >= Math.Min(b[0][1], b[1][1]) && p.Z <= Math.Max(b[0][1], b[1][1]);
        }).ToList();
        return holding.Count == 1 ? holding[0].Name : null;
    }

    /// <summary>Reads the newest screenshot in the folder again (the window's "last position" button).</summary>
    [RelayCommand]
    void ReloadScreenshot()
    {
        Screenshots.Restart();
        OnPropertyChanged(nameof(GpsStatus));  // the folder may exist by now
        if (Screenshots.Latest() is { } p) OnScreenshot(p);
        else Toast = Loc.F(Screenshots.FolderExists ? "t.map.noShot" : "t.map.noFolder", Screenshots.Folder);
    }

    /// <summary>Objective spots of the active quests on <paramref name="map"/>, minus the objectives already done.</summary>
    public IReadOnlyList<MapQuestSpot> QuestSpots(MapGeometry map)
    {
        var active = ActiveQuests.Where(a => a.Quest.TarkovDevId != null)
                                 .GroupBy(a => a.Quest.TarkovDevId!)
                                 .ToDictionary(g => g.Key, g => g.First());
        var spots = new List<MapQuestSpot>();
        foreach (var spot in map.Objectives)
        {
            if (!active.TryGetValue(spot.Quest, out var quest)) continue;
            var objective = quest.Objectives.FirstOrDefault(o =>
                string.Equals(o.Objective.Text.Trim(), spot.Text.Trim(), StringComparison.OrdinalIgnoreCase));
            if (objective?.IsDone == true) continue;
            spots.Add(new MapQuestSpot(quest.Quest.DisplayName, objective?.Objective.DisplayText ?? spot.Text, spot.Positions));
        }
        return spots;
    }

    // settings

    public bool MapTracking
    {
        get => Settings.MapTracking;
        set
        {
            if (Settings.MapTracking == value) return;
            Settings.MapTracking = value;
            Screenshots.Enabled = value;
            if (value) _ = MapPictures.PrepareAsync(MapData.Values, MapCacheFolder, AppVersion);
            SaveSettings();
            OnPropertyChanged();
            OnPropertyChanged(nameof(MapTrackingOff));
            OnPropertyChanged(nameof(GpsStatus));
        }
    }

    /// <summary>Other side of <see cref="MapTracking"/> for the GPS page's On | Off segmented switch.</summary>
    public bool MapTrackingOff
    {
        get => !MapTracking;
        set { if (value) MapTracking = false; }
    }

    /// <summary>GPS page status line: off, no folder, waiting for a screenshot, or the last position.</summary>
    public string GpsStatus =>
        !MapTracking ? Loc.T("s.map.off")
        : !Screenshots.FolderExists ? Loc.F("s.map.noFolder", Screenshots.Folder)
        : PlayerPosition is not { } p ? Loc.T("s.gps.waiting")
        : Loc.F("s.gps.pos", p.Taken.ToString("HH:mm"), MapName ?? Loc.T("s.gps.noMap"),
                p.X.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                p.Z.ToString("0", System.Globalization.CultureInfo.InvariantCulture));

    partial void OnPlayerPositionChanged(ScreenshotPosition? value) => OnPropertyChanged(nameof(GpsStatus));

    public string ScreenshotsFolder
    {
        get => Screenshots.Folder;
        set
        {
            value = value?.Trim() ?? "";
            Settings.ScreenshotsFolder = value == ScreenshotWatcher.DefaultFolder ? "" : value;
            Screenshots.Folder = value;
            SaveSettings();
            OnPropertyChanged();
            OnPropertyChanged(nameof(GpsStatus));
        }
    }

    public bool MapOpenOnScreenshot
    {
        get => Settings.MapOpenOnScreenshot;
        set
        {
            if (Settings.MapOpenOnScreenshot == value) return;
            Settings.MapOpenOnScreenshot = value;
            SaveSettings();
            OnPropertyChanged();
        }
    }

    public bool MapShowExtracts
    {
        get => Settings.MapShowExtracts;
        set
        {
            if (Settings.MapShowExtracts == value) return;
            Settings.MapShowExtracts = value;
            SaveSettings();
            OnPropertyChanged();
            MapChanged?.Invoke();
        }
    }

    public bool MapShowQuests
    {
        get => Settings.MapShowQuests;
        set
        {
            if (Settings.MapShowQuests == value) return;
            Settings.MapShowQuests = value;
            SaveSettings();
            OnPropertyChanged();
            MapChanged?.Invoke();
        }
    }

    public const double MinMapOpacity = 0.3;

    public double MapOpacity
    {
        get => Math.Clamp(Settings.MapOpacity, MinMapOpacity, 1);
        set
        {
            value = Math.Clamp(value, MinMapOpacity, 1);
            if (Settings.MapOpacity == value) return;
            Settings.MapOpacity = value;
            SaveSettings();
            OnPropertyChanged();
        }
    }

    /// <summary>Map window place, 0–1 across the free space of all monitors (sliders on the GPS page; dragging sets it too).</summary>
    public double MapPositionX
    {
        get => Math.Clamp(Settings.MapPositionX, 0, 1);
        set => SetMapPosition(value, MapPositionY);
    }

    public double MapPositionY
    {
        get => Math.Clamp(Settings.MapPositionY, 0, 1);
        set => SetMapPosition(MapPositionX, value);
    }

    /// <summary>
    /// Both at once; saved right away, like the overlay's position. A drag passes <paramref name="clamp"/> false:
    /// past 0–1 is another monitor (the sliders show the nearest edge).
    /// </summary>
    public void SetMapPosition(double x, double y, bool clamp = true)
    {
        if (clamp) (x, y) = (Math.Clamp(x, 0, 1), Math.Clamp(y, 0, 1));
        if (Settings.MapPositionX == x && Settings.MapPositionY == y) return;
        (Settings.MapPositionX, Settings.MapPositionY) = (x, y);
        SaveSettings();
        OnPropertyChanged(nameof(MapPositionX));
        OnPropertyChanged(nameof(MapPositionY));
    }

    [RelayCommand]
    void CenterMap() => SetMapPosition(0.5, 0.5);

    /// <summary>After a profile's settings load.</summary>
    void ApplyMapSettings()
    {
        OnPropertyChanged(nameof(MapPositionX));
        OnPropertyChanged(nameof(MapPositionY));
        Screenshots.Folder = Settings.ScreenshotsFolder;
        Screenshots.Enabled = Settings.MapTracking;
        OnPropertyChanged(nameof(MapTracking));
        OnPropertyChanged(nameof(MapTrackingOff));
        OnPropertyChanged(nameof(GpsStatus));
        OnPropertyChanged(nameof(MapOpenOnScreenshot));
        OnPropertyChanged(nameof(ScreenshotsFolder));
        OnPropertyChanged(nameof(MapShowExtracts));
        OnPropertyChanged(nameof(MapShowQuests));
        OnPropertyChanged(nameof(MapOpacity));
    }
}
