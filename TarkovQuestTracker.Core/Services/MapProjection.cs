using System.Text.Json;
using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>A point on the map picture, in tarkov.dev's zoom-0 pixels (tile pixels at zoom z = this × 2^z).</summary>
public readonly record struct MapPoint(double X, double Y);

/// <summary>
/// Game coordinates → map picture, the same math as tarkov.dev's Leaflet map (pages/map getCRS + applyRotation):
/// the game's (x, z) is rotated by the map's coordinateRotation, then Transformation(scaleX, marginX, -scaleY, marginY).
/// The SVG and the tiles live in that same pixel space, so markers land on them without any per-map tuning.
/// </summary>
public static class MapProjection
{
    public static MapPoint Project(MapGeometry map, double x, double z)
    {
        var angle = map.CoordinateRotation * Math.PI / 180;
        var (cos, sin) = (Math.Cos(angle), Math.Sin(angle));
        var rx = x * cos - z * sin;
        var ry = x * sin + z * cos;
        var t = map.Transform;
        return new MapPoint(t[0] * rx + t[1], -t[2] * ry + t[3]);
    }

    public static MapPoint Project(MapGeometry map, double[] position) => Project(map, position[0], position[2]);

    /// <summary>Back from the map picture to game (x, z): <see cref="Project(MapGeometry, double, double)"/> undone.</summary>
    public static (double X, double Z) Unproject(MapGeometry map, MapPoint point)
    {
        var t = map.Transform;
        var rx = (point.X - t[1]) / t[0];
        var ry = -(point.Y - t[3]) / t[2];
        var angle = map.CoordinateRotation * Math.PI / 180;
        var (cos, sin) = (Math.Cos(angle), Math.Sin(angle));
        return (rx * cos + ry * sin, -rx * sin + ry * cos);
    }

    /// <summary>
    /// The picture's rectangle (left, top, right, bottom) for [[x, z], [x, z]] corners, as Leaflet places an overlay:
    /// its north-west and south-east corners, projected.
    /// </summary>
    public static (double Left, double Top, double Right, double Bottom) Rect(MapGeometry map, double[][] bounds)
    {
        var (minX, maxX) = (Math.Min(bounds[0][0], bounds[1][0]), Math.Max(bounds[0][0], bounds[1][0]));
        var (minZ, maxZ) = (Math.Min(bounds[0][1], bounds[1][1]), Math.Max(bounds[0][1], bounds[1][1]));
        var a = Project(map, minX, maxZ);
        var b = Project(map, maxX, minZ);
        return (Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
    }

    /// <summary>
    /// Clockwise screen rotation (0 = up) of a marker facing <paramref name="yaw"/>, as tarkov.dev turns its
    /// player marker: the yaw plus the map's rotation (+180 on the maps turned a quarter).
    /// </summary>
    public static double Heading(MapGeometry map, double yaw)
    {
        var add = map.CoordinateRotation;
        if (add is 90 or 270) add += 180;
        return ((yaw + add) % 360 + 360) % 360;
    }

    /// <summary>
    /// The floor the position is on, or null for the ground floor: a floor whose height range and (if it has any)
    /// rectangles hold it; when several do, the one with the tightest height range (a building's own floor
    /// rather than "everything above 5 m").
    /// </summary>
    public static MapLayer? LayerFor(MapGeometry map, double x, double y, double z)
    {
        MapLayer? best = null;
        var bestSpan = double.MaxValue;
        foreach (var layer in map.Layers)
            foreach (var e in layer.Extents)
            {
                if (e.Height.Length < 2 || y < e.Height[0] || y >= e.Height[1]) continue;
                if (e.Bounds.Count > 0 && !e.Bounds.Any(b => Contains(b, x, z))) continue;
                var span = e.Height[1] - e.Height[0];
                if (span < bestSpan) (best, bestSpan) = (layer, span);
            }
        return best;
    }

    static bool Contains(double[][] rect, double x, double z) =>
        x >= Math.Min(rect[0][0], rect[1][0]) && x <= Math.Max(rect[0][0], rect[1][0])
        && z >= Math.Min(rect[0][1], rect[1][1]) && z <= Math.Max(rect[0][1], rect[1][1]);
}

/// <summary>data/maps.json: the maps the map window can draw, by the map names the quests use.</summary>
public static class MapRepository
{
    public const string FileName = "maps.json";

    sealed class MapData
    {
        public Dictionary<string, MapGeometry> Maps { get; set; } = [];
    }

    /// <summary>Loads data/maps.json; empty when the file is missing.</summary>
    public static IReadOnlyDictionary<string, MapGeometry> Load(string dataDir)
    {
        var path = Path.Combine(dataDir, FileName);
        var data = File.Exists(path)
            ? JsonSerializer.Deserialize<MapData>(File.ReadAllText(path), QuestRepository.Json)
            : null;
        var maps = data?.Maps ?? [];
        foreach (var (name, map) in maps) map.Name = name;
        return maps;
    }
}
