using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TarkovQuestTracker;

/// <summary>
/// Trader name → portrait embedded from Assets/Traders/{name-slug}.png (e.g. "BTR Driver" → btr-driver.png).
/// Returns null when the file is not part of the build; the UI then shows the trader's initial.
/// </summary>
public sealed class TraderImageConverter : IValueConverter
{
    static readonly Dictionary<string, ImageSource?> Cache = [];

    public object? Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string trader || trader.Length == 0) return null;
        if (Cache.TryGetValue(trader, out var cached)) return cached;
        var uri = new Uri($"pack://application:,,,/Assets/Traders/{trader.ToLowerInvariant().Replace(' ', '-')}.png");
        ImageSource? image = null;
        try
        {
            using (Application.GetResourceStream(uri)?.Stream) { }
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            image = bitmap;
        }
        catch (IOException)
        {
            // portrait not bundled
        }
        return Cache[trader] = image;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Icon file path (data/icons/{itemId}.png) → cached image; null path → no image.</summary>
public sealed class ItemIconConverter : IValueConverter
{
    static readonly Dictionary<string, ImageSource> Cache = [];

    public object? Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path) return null;
        if (Cache.TryGetValue(path, out var cached)) return cached;
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(path);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        // icons are drawn at ≤32 px; decoding at 64 keeps them sharp on 200% DPI and is cheaper
        bitmap.DecodePixelWidth = 64;
        bitmap.EndInit();
        bitmap.Freeze();
        return Cache[path] = bitmap;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>"Prapor" → "P"; fallback badge when no portrait is bundled ("all traders" gets a star).</summary>
public sealed class InitialConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string { Length: > 0 } s ? s[..1].ToUpperInvariant() : "★";

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>true ↔ false; lets a RadioButton bind to the "off" side of a bool.</summary>
public sealed class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) => value is false;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => value is false;
}
