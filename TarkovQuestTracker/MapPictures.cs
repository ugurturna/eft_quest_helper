using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker;

/// <summary>A piece of the map picture and where it goes, in <see cref="MapPoint"/> units (tarkov.dev's zoom-0 pixels).</summary>
public sealed record MapPicture(ImageSource Source, Rect Rect, double Opacity);

/// <summary>
/// tarkov.dev's map pictures: the SVG (most maps; one file with every floor as a group) or, where there is none,
/// the tiles at one zoom. Downloaded once into the cache folder, then read from there (works offline).
/// A floor is drawn over the ground floor, which is dimmed, as on tarkov.dev.
/// </summary>
public static class MapPictures
{
    static readonly HttpClient Http = CreateClient();

    /// <summary>Ground floor behind a floor.</summary>
    const double OffLevel = 0.3;
    /// <summary>Tile pictures aim for about this many pixels across the map.</summary>
    const double TileTargetWidth = 2048;

    static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TarkovQuestTracker");
        return client;
    }

    /// <summary>Pictures made this run, by map and floor: made once, shared by the map window, the mini map and the app's map page.</summary>
    static readonly Dictionary<(string Map, string? Floor), Task<IReadOnlyList<MapPicture>>> Made = [];

    /// <summary>
    /// The map's pictures for <paramref name="layer"/> (null = ground floor): from memory when they were made already,
    /// else from the disk cache (downloaded when missing), drawn off the UI thread. A failed attempt is tried again next time.
    /// </summary>
    public static Task<IReadOnlyList<MapPicture>> LoadAsync(MapGeometry map, MapLayer? layer, string cacheDir)
    {
        var key = (map.Name, layer?.Name);
        lock (Made)
        {
            if (Made.TryGetValue(key, out var made) && !made.IsFaulted && !made.IsCanceled) return made;
            return Made[key] = MakeAsync(map, layer, cacheDir);
        }
    }

    /// <summary>Makes the map's ground floor ahead (the raid's map is known): the first screenshot then shows it at once.</summary>
    public static async Task WarmAsync(MapGeometry map, string cacheDir)
    {
        try
        {
            await LoadAsync(map, null, cacheDir);
        }
        catch (Exception ex) when (IsDownloadError(ex) || ex is InvalidOperationException)
        {
            // tried again when the map is shown
        }
    }

    static async Task<IReadOnlyList<MapPicture>> MakeAsync(MapGeometry map, MapLayer? layer, string cacheDir)
    {
        var pictures = new List<MapPicture>();
        if (map.SvgPath != null)
        {
            var svgFloor = layer?.SvgLayer;
            pictures.Add(await SvgAsync(map, svgFloor, cacheDir));
            // a floor only in tiles (Customs' 4th floor): its tiles over the dimmed SVG
            if (layer != null && svgFloor == null && layer.TilePath != null)
            {
                pictures[0] = pictures[0] with { Opacity = OffLevel };
                pictures.AddRange(await TilesAsync(map, layer.TilePath, cacheDir, 1));
            }
        }
        else if (map.TilePath != null)
        {
            var floor = layer?.TilePath;
            pictures.AddRange(await TilesAsync(map, map.TilePath, cacheDir, floor != null ? OffLevel : 1));
            if (floor != null) pictures.AddRange(await TilesAsync(map, floor, cacheDir, 1));
        }
        return pictures;
    }

    // SVG

    static async Task<MapPicture> SvgAsync(MapGeometry map, string? floor, string cacheDir)
    {
        var path = await CachedAsync(map.SvgPath!, cacheDir);
        // reading and drawing a big SVG takes a second or two: not on the UI thread (the picture comes back frozen)
        return await OnStaThread(() => Svg(map, floor, path));
    }

    /// <summary>Runs <paramref name="work"/> on its own STA thread (WPF drawing objects want one).</summary>
    static Task<T> OnStaThread<T>(Func<T> work)
    {
        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                done.SetResult(work());
            }
            catch (Exception ex)
            {
                done.SetException(ex);
            }
        }) { IsBackground = true, Name = "map picture" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return done.Task;
    }

    static MapPicture Svg(MapGeometry map, string? floor, string path)
    {
        var doc = XDocument.Load(path);
        var viewBox = ShowFloor(doc, map.SvgLayer, floor);
        InlineClasses(doc);
        using var stream = new MemoryStream();
        doc.Save(stream);
        stream.Position = 0;
        var reader = new FileSvgReader(new WpfDrawingSettings { IncludeRuntime = false, TextAsGeometry = true });
        var drawing = reader.Read(stream) ?? new DrawingGroup();
        // the picture spans the SVG's viewBox (what tarkov.dev stretches over the bounds), not just what is drawn
        var frame = new DrawingGroup { ClipGeometry = new RectangleGeometry(viewBox) };
        frame.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(viewBox)));
        frame.Children.Add(drawing);
        var image = new DrawingImage(frame);
        image.Freeze();
        var (left, top, right, bottom) = MapProjection.Rect(map, map.SvgBounds ?? map.Bounds);
        // tarkov.dev's SVG keeps its aspect ratio inside the bounds (centred), like Stretch.Uniform
        var scale = Math.Min((right - left) / viewBox.Width, (bottom - top) / viewBox.Height);
        var (w, h) = (viewBox.Width * scale, viewBox.Height * scale);
        var rect = new Rect(left + (right - left - w) / 2, top + (bottom - top - h) / 2, w, h);
        return new MapPicture(image, rect, 1);
    }

    /// <summary>
    /// Keeps the ground floor's groups (dimmed under a floor) and the floor's, drops the other floors.
    /// Floors are the SVG's top-level groups with an id; data-keep-with-group ties a group to one. Returns the viewBox.
    /// </summary>
    static Rect ShowFloor(XDocument doc, string? ground, string? floor)
    {
        var root = doc.Root!;
        foreach (var group in root.Elements().Where(e => e.Name.LocalName == "g" && e.Attribute("id") != null).ToList())
        {
            var id = (string?)group.Attribute("id");
            var keepWith = (string?)group.Attribute("data-keep-with-group");
            var owner = keepWith ?? id;
            if (owner == floor && floor != null) continue;
            if (owner == ground || ground == null)
            {
                if (floor != null) group.SetAttributeValue("opacity", OffLevel.ToString(System.Globalization.CultureInfo.InvariantCulture));
                continue;
            }
            group.Remove();
        }
        var box = ((string?)root.Attribute("viewBox") ?? "").Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(v => double.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        return box.Length == 4 ? new Rect(box[0], box[1], box[2], box[3]) : new Rect(0, 0, 1000, 1000);
    }

    /// <summary>
    /// The maps style everything with <c>.class { fill: …; stroke: … }</c> rules, which SharpVectors only partly applies
    /// (roads, trees and buildings went missing). Writes them onto the elements as attributes instead: a class wins
    /// over an attribute, an inline style over both, as in a browser. Filters (drop shadows) are left out.
    /// </summary>
    static void InlineClasses(XDocument doc)
    {
        var rules = new Dictionary<string, List<(string Name, string Value)>>();
        foreach (var style in doc.Descendants().Where(e => e.Name.LocalName == "style").ToList())
        {
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(style.Value, @"\.([\w-]+)\s*\{([^}]*)\}"))
            {
                var props = m.Groups[2].Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(p => p.Split(':', 2, StringSplitOptions.TrimEntries))
                    .Where(p => p.Length == 2 && p[0] != "filter")
                    .Select(p => (p[0], p[1]));
                if (!rules.TryGetValue(m.Groups[1].Value, out var list)) rules[m.Groups[1].Value] = list = [];
                list.AddRange(props);
            }
            style.Remove();
        }
        if (rules.Count == 0) return;
        foreach (var element in doc.Descendants())
        {
            if ((string?)element.Attribute("class") is not { } classes) continue;
            var inline = (string?)element.Attribute("style") ?? "";
            foreach (var name in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                foreach (var (prop, value) in rules.GetValueOrDefault(name) ?? [])
                    if (!inline.Contains(prop + ":", StringComparison.Ordinal)) element.SetAttributeValue(prop, value);
            element.Attribute("class")!.Remove();
        }
    }

    // tiles

    /// <summary>The tiles covering the map at the zoom the window uses, with where each goes (map units).</summary>
    static IEnumerable<(string Url, Rect Rect)> Tiles(MapGeometry map, string template)
    {
        var (left, top, right, bottom) = MapProjection.Rect(map, map.Bounds);
        var zoom = (int)Math.Clamp(Math.Round(Math.Log2(TileTargetWidth / Math.Max(1, right - left))), map.MinZoom, map.MaxZoom);
        var size = map.TileSize > 0 ? map.TileSize : 256;
        var factor = Math.Pow(2, zoom);
        int x0 = (int)Math.Floor(left * factor / size), x1 = (int)Math.Floor(right * factor / size);
        int y0 = (int)Math.Floor(top * factor / size), y1 = (int)Math.Floor(bottom * factor / size);
        var unit = size / factor;  // one tile in map units
        for (var x = x0; x <= x1; x++)
            for (var y = y0; y <= y1; y++)
                yield return (template.Replace("{z}", zoom.ToString()).Replace("{x}", x.ToString()).Replace("{y}", y.ToString()),
                              new Rect(x * unit, y * unit, unit, unit));
    }

    static async Task<IReadOnlyList<MapPicture>> TilesAsync(MapGeometry map, string template, string cacheDir, double opacity)
    {
        using var limit = new SemaphoreSlim(8);
        var tasks = new List<Task<MapPicture?>>();
        foreach (var (url, rect) in Tiles(map, template))
                tasks.Add(Task.Run(async () =>
                {
                    await limit.WaitAsync();
                    try
                    {
                        var file = await CachedAsync(url, cacheDir);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(file);
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return new MapPicture(bitmap, rect, opacity);
                    }
                    catch (Exception ex) when (ex is HttpRequestException or IOException or NotSupportedException or TaskCanceledException)
                    {
                        return null;  // outside the picture (404) or unreadable: that square stays empty
                    }
                    finally
                    {
                        limit.Release();
                    }
                }));
        var tiles = await Task.WhenAll(tasks);
        return tiles.OfType<MapPicture>().ToList();
    }

    static string CachePath(string url, string cacheDir)
    {
        var uri = new Uri(url);
        return Path.Combine(cacheDir, uri.Host, uri.AbsolutePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>The local copy of <paramref name="url"/> (the URL's path under the cache folder), downloaded when missing.</summary>
    static async Task<string> CachedAsync(string url, string cacheDir)
    {
        var path = CachePath(url, cacheDir);
        if (File.Exists(path)) return path;
        await SaveAsync(path, await Http.GetByteArrayAsync(url), null);
        return path;
    }

    static async Task SaveAsync(string path, byte[] bytes, DateTimeOffset? modified)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = $"{path}.{Guid.NewGuid():N}.tmp";  // the start-up download and an open map may fetch the same file
        await File.WriteAllBytesAsync(tmp, bytes);
        if (modified is { } when) File.SetLastWriteTimeUtc(tmp, when.UtcDateTime);
        File.Move(tmp, path, overwrite: true);
    }

    /// <summary>
    /// The local copy brought up to date: tarkov.dev is asked whether its file changed since ours (If-Modified-Since),
    /// so an unchanged map costs one small request and a changed one is downloaded again.
    /// </summary>
    static async Task RefreshAsync(string url, string cacheDir)
    {
        var path = CachePath(url, cacheDir);
        if (!File.Exists(path))
        {
            await CachedAsync(url, cacheDir);
            return;
        }
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.IfModifiedSince = File.GetLastWriteTimeUtc(path);
        using var response = await Http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.NotModified) return;
        response.EnsureSuccessStatusCode();
        await SaveAsync(path, await response.Content.ReadAsByteArrayAsync(), response.Content.Headers.LastModified);
    }

    // at start-up: missing files downloaded, changed SVGs downloaded again

    const string VersionFile = "version.txt";

    /// <summary>
    /// Every start, in the background: the maps are brought up to date (the map window then opens at once, also offline).
    /// The SVG maps are checked against tarkov.dev and downloaded again only when they changed; tiles (they don't change)
    /// only when missing. The cache is kept across app updates (files are stored by their URL).
    /// Errors only mean a map is fetched when it is opened.
    /// </summary>
    public static async Task PrepareAsync(IEnumerable<MapGeometry> maps, string cacheDir, string appVersion)
    {
        try
        {
            // the cache outlives app updates: files are kept by their URL, and the SVGs are checked below
            Directory.CreateDirectory(cacheDir);
            await File.WriteAllTextAsync(Path.Combine(cacheDir, VersionFile), appVersion);
            foreach (var map in maps)
            {
                // the picture the window opens with; floors (Icebreaker has 16) come when they are first shown
                var urls = map.SvgPath != null ? [map.SvgPath]
                         : map.TilePath != null ? Tiles(map, map.TilePath).Select(t => t.Url).ToList()
                         : new List<string>();
                using var limit = new SemaphoreSlim(4);
                await Task.WhenAll(urls.Select(async url =>
                {
                    await limit.WaitAsync();
                    try
                    {
                        if (url == map.SvgPath) await RefreshAsync(url, cacheDir);
                        else await CachedAsync(url, cacheDir);
                    }
                    catch (Exception ex) when (IsDownloadError(ex) || ex is UnauthorizedAccessException)
                    {
                        // a tile outside the picture (404) or no network: the window tries again when it opens
                    }
                    finally
                    {
                        limit.Release();
                    }
                }));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // the cache folder can't be written: maps are fetched when they are opened
        }
    }

    public static bool IsDownloadError(Exception ex) =>
        ex is HttpRequestException or IOException or TaskCanceledException or System.Xml.XmlException;
}
