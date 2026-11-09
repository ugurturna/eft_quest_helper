using System.Windows.Threading;
using TarkovQuestTracker.Core.Services;
using TarkovQuestTracker.Interop;
using Windows.Media.Ocr;

namespace TarkovQuestTracker;

/// <summary>
/// Starts the raid by itself. A double "O" in game lists the exfils at the top right: their names tell the map
/// (Windows OCR) and the clock above them the raid time left (<see cref="RaidClockReader"/>). A single "O" shows
/// only the clock: that just sets the time of the raid already running. No keyboard hook: while the game is in
/// front, the top-right corner of the screen is read now and then (picture kept in memory only). An empty corner
/// just means "not in a raid" (or nothing open): nothing happens and nothing is reported.
/// </summary>
public sealed class RaidDetector
{
    /// <summary>
    /// What a read found: the map (null when only the clock was on screen, a single "O") and the raid time left as
    /// of <see cref="ReadAt"/> (null when it could not be read twice the same, e.g. an FPS counter covers it).
    /// </summary>
    public sealed record Sighting(string? Map, TimeSpan? TimeLeft, DateTime ReadAt);

    static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(1200);
    /// <summary>
    /// A slow PC draws the list a few rows at a time: once exfils show up, read again after this pause until no
    /// new ones appear (at most <see cref="MaxSettleReads"/> times), then decide on the fullest read.
    /// </summary>
    static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(700);
    const int MaxSettleReads = 4;
    /// <summary>After a sighting the list is still open for a while: don't read it again and again.</summary>
    static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(5);
    /// <summary>Where the game puts the exfil list, as fractions of the game window.</summary>
    static readonly (double, double, double, double) Corner = (0.55, 0, 1, 0.45);
    /// <summary>The corner is enlarged to about this height: the exfil text is small at 1080p.</summary>
    const int TargetHeight = 900;

    readonly Func<ExtractMatcher> _matcher;
    readonly DispatcherTimer _timer = new() { Interval = Interval };
    OcrEngine? _ocr;
    bool _ocrChecked, _busy;
    byte[]? _buffer;
    DateTime _quietUntil;
    /// <summary>The last clock read on its own (single "O"), waiting for a second read that agrees.</summary>
    (TimeSpan Left, DateTime At)? _lastClock;

    public RaidDetector(Func<ExtractMatcher> matcher)
    {
        _matcher = matcher;
        _timer.Tick += async (_, _) => await TickAsync();
    }

    /// <summary>A map was recognized from its exfil list, or the raid clock was read.</summary>
    public event Action<Sighting>? Sighted;

    /// <summary>Something "O" shows is on screen (the clock or exfils), even before it is fully read: "O" was pressed.</summary>
    public event Action? Glimpsed;

    public bool Enabled
    {
        get => _timer.IsEnabled;
        set
        {
            if (value) _timer.Start();
            else
            {
                _timer.Stop();
                _buffer = null;
            }
        }
    }

    async Task TickAsync()
    {
        if (_busy || DateTime.UtcNow < _quietUntil) return;
        var game = Win32.GetForegroundWindow();
        if (!Win32.IsGameWindow(game)) return;
        _busy = true;
        try
        {
            if (Engine() is not { } ocr || _matcher() is not { IsEmpty: false } matcher) return;
            if (await ReadAsync(ocr, game) is not { } read) return;
            var hits = matcher.Hits(read.Cells);
            if (read.Clock != null || hits > 0) Glimpsed?.Invoke();
            if (hits == 0)
            {
                // no exfil list: a single "O" (just the clock), or nothing at all
                OnClockAlone(read.Clock, read.At);
                return;
            }

            // the list is coming up: give it time to draw completely, keep the fullest read
            var best = read.Cells;
            var clocks = new List<(TimeSpan Left, DateTime At)>();
            if (read.Clock is { } first) clocks.Add((first, read.At));
            for (var i = 0; i < MaxSettleReads; i++)
            {
                await Task.Delay(SettleDelay);
                if (Win32.GetForegroundWindow() != game) break;
                if (await ReadAsync(ocr, game) is not { } next) break;
                if (next.Clock is { } c) clocks.Add((c, next.At));
                var nextHits = matcher.Hits(next.Cells);
                if (next.Clock != null || nextHits > 0) Glimpsed?.Invoke();  // still open: keep the overlay up
                if (nextHits <= hits && clocks.Count != 1) break;  // complete (one more read when the clock needs a second)
                if (nextHits > hits)
                {
                    best = next.Cells;
                    hits = nextHits;
                }
            }
            var map = matcher.Map(best);
            var time = Confirmed(clocks);
            _lastClock = null;
            if (map == null)
            {
                // exfils but no sure map: the clock above them still sets the time of the raid running
                if (time is { } t) Sighted?.Invoke(new Sighting(null, t.Left, t.At));
                return;
            }
            _quietUntil = DateTime.UtcNow + Cooldown;
            Sighted?.Invoke(new Sighting(map, time?.Left, time?.At ?? DateTime.UtcNow));
        }
        catch (Exception)
        {
            // a frame that can't be read (game closing, display change): try again on the next tick
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>A clock without exfils (single "O"): reported once two reads a tick apart agree.</summary>
    void OnClockAlone(TimeSpan? clock, DateTime at)
    {
        if (clock is not { } left)
        {
            _lastClock = null;
            return;
        }
        if (_lastClock is { } last && at - last.At < TimeSpan.FromSeconds(4) && Confirmed([last, (left, at)]) is { } time)
            Sighted?.Invoke(new Sighting(null, time.Left, time.At));
        _lastClock = (left, at);
    }

    /// <summary>
    /// The latest clock read that another read agrees with (both point at the same raid end, give or take 2 s):
    /// one misread digit can't set a wrong time, let alone end the raid early.
    /// </summary>
    static (TimeSpan Left, DateTime At)? Confirmed(List<(TimeSpan Left, DateTime At)> reads)
    {
        for (var i = reads.Count - 1; i > 0; i--)
            for (var j = i - 1; j >= 0; j--)
                if (((reads[i].At + reads[i].Left) - (reads[j].At + reads[j].Left)).Duration() <= TimeSpan.FromSeconds(2))
                    return reads[i];
        return null;
    }

    /// <summary>
    /// The corner's text pieces (OCR) and its raid clock, or null when there is no usable picture (minimized,
    /// exclusive fullscreen).
    /// </summary>
    async Task<(List<string> Cells, TimeSpan? Clock, DateTime At)?> ReadAsync(OcrEngine ocr, IntPtr game)
    {
        var at = DateTime.UtcNow;
        var frame = await Task.Run(() => ScreenCapture.Capture(game, TargetHeight, (int)OcrEngine.MaxImageDimension, _buffer, Corner));
        if (frame == null) return null;
        _buffer = frame.Pixels;
        if (ScreenCapture.IsBlank(frame)) return null;
        var clock = await Task.Run(() => RaidClockReader.Read(frame.Pixels, frame.Width, frame.Height));
        return (await ScreenScanner.ReadCellsAsync(ocr, frame), clock, at);
    }

    /// <summary>A Latin-script recognizer (<see cref="OcrEngines.Create"/>); null when Windows has none (the detector then does nothing).</summary>
    OcrEngine? Engine()
    {
        if (!_ocrChecked)
        {
            _ocrChecked = true;
            _ocr = OcrEngines.Create();
        }
        return _ocr;
    }
}
