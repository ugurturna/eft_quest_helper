namespace TarkovQuestTracker.Core.Models;

/// <summary>
/// One map of data/maps.json (tools/scraper/fetch_maps.py): tarkov.dev's interactive map settings (how game
/// coordinates land on its SVG or tile picture, the floors) plus exfils, transits and quest objective spots, and the
/// optional layers from the game's own map data: spawns, bosses, locked doors, loot containers, loose loot, hazards.
/// Positions are game coordinates [x, y (height), z].
/// </summary>
public sealed class MapGeometry
{
    /// <summary>tarkov.dev map key ("customs").</summary>
    public string Key { get; set; } = "";
    /// <summary>The map name the quests use; set by the repository from the file's key.</summary>
    public string Name { get; set; } = "";
    /// <summary>[scaleX, marginX, scaleY, marginY] of tarkov.dev's Leaflet CRS.</summary>
    public double[] Transform { get; set; } = [1, 0, 1, 0];
    public double CoordinateRotation { get; set; }
    /// <summary>[[x, z], [x, z]] corners the picture covers.</summary>
    public double[][] Bounds { get; set; } = [];
    public string? SvgPath { get; set; }
    /// <summary>Corners of the SVG when they differ from <see cref="Bounds"/>.</summary>
    public double[][]? SvgBounds { get; set; }
    /// <summary>Top-level SVG group id of the ground floor.</summary>
    public string? SvgLayer { get; set; }
    /// <summary>Tile URL template with {z}/{x}/{y}.</summary>
    public string? TilePath { get; set; }
    public int TileSize { get; set; } = 256;
    public int MinZoom { get; set; }
    public int MaxZoom { get; set; } = 6;
    public List<MapLayer> Layers { get; set; } = [];
    public List<MapExtract> Extracts { get; set; } = [];
    public List<MapExtract> Transits { get; set; } = [];
    public List<MapObjectiveSpot> Objectives { get; set; } = [];
    public List<MapSpawn> Spawns { get; set; } = [];
    public List<MapBoss> Bosses { get; set; } = [];
    public List<MapLock> Locks { get; set; } = [];
    public List<MapContainer> Containers { get; set; } = [];
    public List<MapLooseSpot> Loose { get; set; } = [];
    public List<MapHazard> Hazards { get; set; } = [];
    /// <summary>Key, item, boss and container names; shared by every map of the file.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public MapNames Names { get; set; } = new();
}

/// <summary>A floor (2nd floor, underground): drawn instead of the ground floor where the player is inside it.</summary>
public sealed class MapLayer
{
    public string Name { get; set; } = "";
    public string? SvgLayer { get; set; }
    public string? TilePath { get; set; }
    public List<MapExtent> Extents { get; set; } = [];
}

/// <summary>Heights [from, to) and the [[x, z], [x, z]] rectangles (none = the whole map) a floor spans.</summary>
public sealed class MapExtent
{
    public double[] Height { get; set; } = [double.MinValue, double.MaxValue];
    public List<double[][]> Bounds { get; set; } = [];
}

public sealed class MapExtract
{
    public string Name { get; set; } = "";
    /// <summary>"pmc", "scav" or "shared" (transits leave it empty).</summary>
    public string Faction { get; set; } = "";
    public double[] Position { get; set; } = [0, 0, 0];
}

/// <summary>Where a quest objective is done (zone or quest item spot), by tarkov.dev quest id.</summary>
public sealed class MapObjectiveSpot
{
    public string Quest { get; set; } = "";
    public string Text { get; set; } = "";
    public List<double[]> Positions { get; set; } = [];
}

/// <summary>Where players or bots start: "pmc" (PMCs, also player scavs on Factory), "scav", "aipmc" (AI PMCs).</summary>
public sealed class MapSpawn
{
    public string Kind { get; set; } = "";
    /// <summary>The game's spawn zone ("ZoneDormitory").</summary>
    public string Zone { get; set; } = "";
    public double[] Position { get; set; } = [0, 0, 0];
}

/// <summary>A boss and the spots it can spawn on.</summary>
public sealed class MapBoss
{
    /// <summary>Mob id ("bossBully"); its name is in <see cref="MapNames"/>.</summary>
    public string Name { get; set; } = "";
    /// <summary>0..1 per raid.</summary>
    public double Chance { get; set; }
    /// <summary>Most guards it comes with.</summary>
    public int Escorts { get; set; }
    public List<double[]> Positions { get; set; } = [];
}

/// <summary>A locked door, car trunk or container and the key that opens it.</summary>
public sealed class MapLock
{
    /// <summary>"door", "trunk" or "container".</summary>
    public string Type { get; set; } = "";
    /// <summary>Key item id; its name is in <see cref="MapNames"/>.</summary>
    public string Key { get; set; } = "";
    /// <summary>Opens only with the power on.</summary>
    public bool Power { get; set; }
    public double[] Position { get; set; } = [0, 0, 0];
}

public sealed class MapContainer
{
    /// <summary>tarkov.dev's container type ("safe", "pc-block", "jacket").</summary>
    public string Type { get; set; } = "";
    public double[] Position { get; set; } = [0, 0, 0];
}

/// <summary>A loose loot spot and the items that can lie there (indexes into <see cref="MapNames.Items"/>).</summary>
public sealed class MapLooseSpot
{
    public double[] Position { get; set; } = [0, 0, 0];
    public int[] Items { get; set; } = [];
}

/// <summary>A minefield or a sniper zone: its outline as [x, z] corners.</summary>
public sealed class MapHazard
{
    /// <summary>"minefield" or "sniper".</summary>
    public string Type { get; set; } = "";
    public List<double[]> Outline { get; set; } = [];
}

/// <summary>English and Turkish names of the keys, loose items, bosses and container types (data/maps.json "names").</summary>
public sealed class MapNames
{
    /// <summary>Id → [en] or [en, tr]; an item's short name is under "id short".</summary>
    public Dictionary<string, string[]> Table { get; init; } = [];
    /// <summary>The loose loot item ids <see cref="MapLooseSpot.Items"/> point into.</summary>
    public IReadOnlyList<string> Items { get; init; } = [];

    /// <summary>The name in <paramref name="lang"/> ("tr" falls back to English), or the id itself.</summary>
    public string Of(string id, string lang) =>
        Table.TryGetValue(id, out var names) && names.Length > 0
            ? (lang == "tr" && names.Length > 1 ? names[1] : names[0])
            : id;

    /// <summary>Every name and short name of the item, both languages (for the search box).</summary>
    public IEnumerable<string> AllOf(string id)
    {
        if (Table.TryGetValue(id, out var names)) foreach (var n in names) yield return n;
        if (Table.TryGetValue(id + " short", out var shorts)) foreach (var n in shorts) yield return n;
    }
}
