namespace TarkovQuestTracker.Core.Models;

/// <summary>
/// One map of data/maps.json (tools/scraper/fetch_maps.py): tarkov.dev's interactive map settings (how game
/// coordinates land on its SVG or tile picture, the floors) plus exfils, transits and quest objective spots.
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
