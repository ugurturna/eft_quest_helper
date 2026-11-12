using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.ViewModels;

/// <summary>A container group in the GPS settings' container filter.</summary>
public sealed partial class MapContainerGroupOption(string group, Action<string, bool> changed) : ObservableObject
{
    public string Group { get; } = group;
    public string Label => Loc.T("s.gps.cont." + Group);

    [ObservableProperty] private bool _isChecked;

    partial void OnIsCheckedChanged(bool value) => changed(Group, value);

    public void RefreshLabel() => OnPropertyChanged(nameof(Label));
}

/// <summary>An item the map's search box found: its name, and on how many loose loot spots of the map it can lie.</summary>
public sealed record MapItemChoice(string Id, string Name, int Spots)
{
    public override string ToString() => Name;
}

/// <summary>
/// The map's optional layers from the game's own map data (In-Game GPS settings): PMC / scav spawns, bosses, locked doors
/// with their keys, loot containers by group, minefields and sniper zones, and the loose loot spots of a searched item.
/// The map window, the app's map page and the mini map all draw them.
/// </summary>
public sealed partial class MainViewModel
{
    void MapLayerChanged([CallerMemberName] string? name = null)
    {
        SaveSettings();
        OnPropertyChanged(name);
        MapChanged?.Invoke();
    }

    public bool MapShowPmcSpawns
    {
        get => Settings.MapShowPmcSpawns;
        set { if (Settings.MapShowPmcSpawns == value) return; Settings.MapShowPmcSpawns = value; MapLayerChanged(); }
    }

    public bool MapShowScavSpawns
    {
        get => Settings.MapShowScavSpawns;
        set { if (Settings.MapShowScavSpawns == value) return; Settings.MapShowScavSpawns = value; MapLayerChanged(); }
    }

    public bool MapShowBosses
    {
        get => Settings.MapShowBosses;
        set { if (Settings.MapShowBosses == value) return; Settings.MapShowBosses = value; MapLayerChanged(); }
    }

    public bool MapShowLocks
    {
        get => Settings.MapShowLocks;
        set { if (Settings.MapShowLocks == value) return; Settings.MapShowLocks = value; MapLayerChanged(); }
    }

    public bool MapShowContainers
    {
        get => Settings.MapShowContainers;
        set { if (Settings.MapShowContainers == value) return; Settings.MapShowContainers = value; MapLayerChanged(); }
    }

    public bool MapShowHazards
    {
        get => Settings.MapShowHazards;
        set { if (Settings.MapShowHazards == value) return; Settings.MapShowHazards = value; MapLayerChanged(); }
    }

    List<MapContainerGroupOption>? _containerGroups;
    bool _fillingContainerGroups;

    /// <summary>The container filter's check boxes.</summary>
    public IReadOnlyList<MapContainerGroupOption> MapContainerGroupOptions
    {
        get
        {
            if (_containerGroups == null)
            {
                _containerGroups = MapContainerGroups.All.Select(g => new MapContainerGroupOption(g.Group, OnContainerGroupChanged)).ToList();
                FillContainerGroups();
            }
            return _containerGroups;
        }
    }

    void FillContainerGroups()
    {
        if (_containerGroups == null) return;
        _fillingContainerGroups = true;
        foreach (var option in _containerGroups) option.IsChecked = Settings.MapContainerGroups.Contains(option.Group);
        _fillingContainerGroups = false;
    }

    void OnContainerGroupChanged(string group, bool on)
    {
        if (_fillingContainerGroups) return;
        Settings.MapContainerGroups.Remove(group);
        if (on) Settings.MapContainerGroups.Add(group);
        SaveSettings();
        MapChanged?.Invoke();
    }

    /// <summary>Whether the map draws a container of tarkov.dev type <paramref name="type"/>.</summary>
    public bool ShowsContainer(string type) => Settings.MapContainerGroups.Contains(MapContainerGroups.Of(type));

    // item search: where an item can lie as loose loot

    /// <summary>The item whose loose loot spots the map marks (tarkov.dev item id); null = none.</summary>
    [ObservableProperty] private MapItemChoice? _mapSearchItem;

    partial void OnMapSearchItemChanged(MapItemChoice? value) => MapChanged?.Invoke();

    /// <summary>
    /// Items whose name (English or Turkish, long or short) holds <paramref name="text"/>, those lying on the shown map
    /// first, then the most spots; at most <paramref name="max"/>.
    /// </summary>
    public IReadOnlyList<MapItemChoice> FindMapItems(string text, int max = 12)
    {
        text = text.Trim();
        if (text.Length < 2 || MapData.Values.FirstOrDefault() is not { } any) return [];
        var names = any.Names;
        var map = MapName is { } n && MapData.TryGetValue(n, out var m) ? m : null;
        var spots = new int[names.Items.Count];
        foreach (var spot in map?.Loose ?? [])
            foreach (var i in spot.Items)
                if (i >= 0 && i < spots.Length) spots[i]++;
        return Enumerable.Range(0, names.Items.Count)
            .Where(i => names.AllOf(names.Items[i]).Any(s => s.Contains(text, StringComparison.CurrentCultureIgnoreCase)))
            .Select(i => new MapItemChoice(names.Items[i], names.Of(names.Items[i], Loc.Lang), spots[i]))
            .OrderByDescending(c => c.Spots > 0)
            .ThenByDescending(c => c.Spots)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(max)
            .ToList();
    }

    /// <summary>The searched item's index in the map file's item list; -1 = none.</summary>
    public int MapSearchIndex(MapGeometry map) =>
        MapSearchItem is { } item ? map.Names.Items.ToList().IndexOf(item.Id) : -1;

    /// <summary>After a profile's settings load.</summary>
    void ApplyMapLayerSettings()
    {
        FillContainerGroups();
        OnPropertyChanged(nameof(MapShowPmcSpawns));
        OnPropertyChanged(nameof(MapShowScavSpawns));
        OnPropertyChanged(nameof(MapShowBosses));
        OnPropertyChanged(nameof(MapShowLocks));
        OnPropertyChanged(nameof(MapShowContainers));
        OnPropertyChanged(nameof(MapShowHazards));
    }
}
