using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Threading;
using TarkovQuestTracker.Core.Services;
using TarkovQuestTracker.Interop;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace TarkovQuestTracker;

/// <summary>
/// "Keep or sell?" for the item under the mouse: reads the name the game shows next to the cursor (the item's
/// tooltip) with the Windows OCR, from a small picture around the mouse. Once per hotkey press, or by itself while
/// <see cref="Auto"/> is on and the mouse rests over the game. Pictures stay in memory; the game is never touched.
/// </summary>
public sealed class ItemChecker
{
    // the picture around the mouse at 1080p (bigger at higher resolutions): the tooltip opens next to the cursor
    const int AreaLeft = 320, AreaRight = 560, AreaUp = 160, AreaDown = 180;
    /// <summary>Hand tremor is not a move (pixels).</summary>
    const int MoveSlack = 6;
    /// <summary>The card closes once the mouse is this far from where it opened (pixels at 1080p).</summary>
    const int AwaySlack = 60;
    /// <summary>The mouse rests this long before an automatic read; the game's tooltip needs a moment too.</summary>
    static readonly TimeSpan Rest = TimeSpan.FromMilliseconds(450);
    /// <summary>Reads per resting place: a second one for a tooltip that was late.</summary>
    const int ReadsPerRest = 2;
    /// <summary>Hotkey reads: tries while the tooltip may still be appearing.</summary>
    const int HotkeyTries = 3;
    static readonly TimeSpan HotkeyCardTime = TimeSpan.FromSeconds(12);

    readonly Func<Task<ItemTextMatcher?>> _matcher;
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    OcrEngine? _ocr;
    bool _ocrChecked, _busy, _auto;
    (int X, int Y) _last;
    DateTime _restSince;
    int _readsHere;
    (int X, int Y)? _shownAt;
    DateTime _shownTime;
    bool _shownByHotkey;
    double _scale = 1;

    public ItemChecker(Func<Task<ItemTextMatcher?>> matcher)
    {
        _matcher = matcher;
        _timer.Tick += async (_, _) => await TickAsync();
    }

    /// <summary>Read by itself whenever the mouse rests over the game.</summary>
    public bool Auto
    {
        get => _auto;
        set
        {
            _auto = value;
            UpdateTimer();
        }
    }

    /// <summary>An item was read: show its card next to this mouse position (physical pixels).</summary>
    public event Action<FleaItem, (int X, int Y)>? Found;
    /// <summary>The mouse left the item (or the game): hide the card.</summary>
    public event Action? Lost;
    /// <summary>Toast text for the player.</summary>
    public event Action<string>? Message;

    void UpdateTimer()
    {
        if (_auto || _shownAt != null) _timer.Start();
        else _timer.Stop();
    }

    OcrEngine? Engine()
    {
        if (!_ocrChecked)
        {
            _ocrChecked = true;
            _ocr = OcrEngines.Create();
        }
        return _ocr;
    }

    /// <summary>The hotkey: reads the item under the mouse in whatever window is in front (not only the game).</summary>
    public async Task CheckOnceAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            if (Engine() is not { } ocr)
            {
                Message?.Invoke(Loc.T("t.scan.noOcr"));
                return;
            }
            var window = Win32.GetForegroundWindow();
            if (ScreenCapture.IsOwnWindow(window))
            {
                Message?.Invoke(Loc.T("t.item.focusGame"));
                return;
            }
            if (await _matcher() is not { } matcher)
            {
                Message?.Invoke(Loc.T("t.item.noPrices"));
                return;
            }
            for (var i = 0; i < HotkeyTries; i++)
            {
                if (i > 0) await Task.Delay(300);
                if (Win32.Cursor() is not { } at) return;
                if (await ReadAsync(ocr, matcher, window, at) is { } item)
                {
                    Show(item, at, byHotkey: true);
                    return;
                }
            }
            Message?.Invoke(Loc.T("t.item.none"));
        }
        catch (Exception ex)
        {
            Message?.Invoke(Loc.F("t.scan.failed", ex.Message));
        }
        finally
        {
            _busy = false;
        }
    }

    async Task TickAsync()
    {
        if (_busy) return;
        var window = Win32.GetForegroundWindow();
        // the hotkey card stays over any window; the automatic one only reads the game
        if (Win32.Cursor() is not { } at || (_auto && !_shownByHotkey && !Win32.IsGameWindow(window)))
        {
            Hide();
            return;
        }
        if (Math.Abs(at.X - _last.X) > MoveSlack || Math.Abs(at.Y - _last.Y) > MoveSlack)
        {
            _last = at;
            _restSince = DateTime.UtcNow;
            _readsHere = 0;
            if (_shownAt is { } shown && Math.Max(Math.Abs(at.X - shown.X), Math.Abs(at.Y - shown.Y)) > AwaySlack * _scale) Hide();
            return;
        }
        if (_shownAt != null)
        {
            if (_shownByHotkey && DateTime.UtcNow - _shownTime > HotkeyCardTime) Hide();
            return;
        }
        // in a raid the game keeps the mouse still: one resting place, read at most twice, then quiet
        if (!_auto || !Win32.IsGameWindow(window) || _readsHere >= ReadsPerRest
            || DateTime.UtcNow - _restSince < Rest * (_readsHere + 1)) return;
        _readsHere++;
        _busy = true;
        try
        {
            if (Engine() is not { } ocr || await _matcher() is not { } matcher) return;
            if (await ReadAsync(ocr, matcher, window, at) is { } item) Show(item, at, byHotkey: false);
        }
        catch (Exception)
        {
            // a failed automatic read stays quiet: the next resting place tries again
        }
        finally
        {
            _busy = false;
        }
    }

    void Show(FleaItem item, (int X, int Y) at, bool byHotkey)
    {
        _shownAt = at;
        _last = at;
        _shownTime = DateTime.UtcNow;
        _shownByHotkey = byHotkey;
        UpdateTimer();
        Found?.Invoke(item, at);
    }

    public void Hide()
    {
        if (_shownAt == null) return;
        _shownAt = null;
        _shownByHotkey = false;
        UpdateTimer();
        Lost?.Invoke();
    }

    /// <summary>The item named in the picture around the mouse, or null.</summary>
    async Task<FleaItem?> ReadAsync(OcrEngine ocr, ItemTextMatcher matcher, IntPtr window, (int X, int Y) at)
    {
        var height = Win32.Size(window).Height;
        if (height < 200) return null;
        _scale = Math.Max(0.75, height / 1080.0);
        // small tooltip text reads better enlarged: about 2× at 1080p, as is at 4K
        var zoom = Math.Clamp(2160.0 / height, 1, 2);
        var area = await Task.Run(() => ScreenCapture.CaptureArea(window,
            at.X - (int)(AreaLeft * _scale), at.Y - (int)(AreaUp * _scale),
            at.X + (int)(AreaRight * _scale), at.Y + (int)(AreaDown * _scale), zoom));
        if (area is not { } a || ScreenCapture.IsBlank(a.Frame)) return null;

        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(a.Frame.Pixels.AsBuffer(), BitmapPixelFormat.Bgra8,
                                                               a.Frame.Width, a.Frame.Height, BitmapAlphaMode.Ignore);
        var text = await ocr.RecognizeAsync(bitmap);
        var cells = ScreenScanner.CellBoxes(text.Lines.SelectMany(l => l.Words).Select(w => (w.Text, w.BoundingRect)));
        // the mouse inside the picture
        double mx = (at.X - a.Left) * zoom, my = (at.Y - a.Top) * zoom;
        var texts = cells.Select(c => new ScreenText(c.Text, Distance(c.Box, mx, my))).ToList();
        return await Task.Run(() => matcher.Match(texts));
    }

    static double Distance(Windows.Foundation.Rect box, double x, double y)
    {
        var dx = Math.Max(0, Math.Max(box.X - x, x - (box.X + box.Width)));
        var dy = Math.Max(0, Math.Max(box.Y - y, y - (box.Y + box.Height)));
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
