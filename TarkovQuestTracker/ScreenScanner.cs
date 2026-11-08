using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Threading;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;
using TarkovQuestTracker.Interop;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace TarkovQuestTracker;

/// <summary>
/// Reads quest names off the in-game Tasks screen: one frame per hotkey press, or a frame every
/// <see cref="Interval"/> while scanning (the player scrolls the list). Uses the OCR built into Windows;
/// pictures stay in memory, nothing is saved or sent anywhere. Found quests wait for the player to confirm.
/// </summary>
public sealed class ScreenScanner
{
    static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(700);
    /// <summary>A scan stops by itself after this long (counted down on screen), so a forgotten one can't keep the PC busy.</summary>
    public static readonly TimeSpan MaxScan = TimeSpan.FromSeconds(30);
    /// <summary>Frames are enlarged to about this height: Tarkov's list text is small at 1080p.</summary>
    const int TargetHeight = 1620;

    readonly Func<QuestTextMatcher> _matcher;
    readonly DispatcherTimer _timer = new() { Interval = Interval };
    readonly List<Quest> _found = [];
    OcrEngine? _ocr;
    bool _ocrChecked, _busy;
    int _lastFingerprint;
    byte[]? _buffer;
    DateTime _scanStarted;

    public ScreenScanner(Func<QuestTextMatcher> matcher)
    {
        _matcher = matcher;
        _timer.Tick += async (_, _) =>
        {
            if (Remaining <= TimeSpan.Zero)
            {
                StopScan();
                return;
            }
            Changed?.Invoke();  // the countdown moved
            await ReadFrameAsync(single: false);
        };
    }

    /// <summary>Quests read so far, in the order they were seen.</summary>
    public IReadOnlyList<Quest> Found => _found;
    public bool IsScanning => _timer.IsEnabled;
    /// <summary>Time left before the running scan stops by itself.</summary>
    public TimeSpan Remaining => IsScanning ? Max(TimeSpan.Zero, MaxScan - (DateTime.UtcNow - _scanStarted)) : TimeSpan.Zero;

    static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;

    /// <summary>Toast text for the player.</summary>
    public event Action<string>? Message;
    /// <summary><see cref="Found"/> or <see cref="IsScanning"/> changed.</summary>
    public event Action? Changed;

    public async Task ReadOnceAsync()
    {
        if (IsScanning) return;
        await ReadFrameAsync(single: true);
    }

    public void ToggleScan()
    {
        if (IsScanning)
        {
            StopScan();
            return;
        }
        if (Engine() == null) return;
        _scanStarted = DateTime.UtcNow;
        _lastFingerprint = 0;
        _timer.Start();
        Message?.Invoke(Loc.F("t.scan.started", (int)MaxScan.TotalSeconds));
        Changed?.Invoke();
    }

    public void StopScan()
    {
        if (!IsScanning) return;
        _timer.Stop();
        _buffer = null;  // ~20 MB, not needed until the next scan
        Message?.Invoke(_found.Count == 0 ? Loc.T("t.scan.none") : Loc.F("t.scan.stopped", _found.Count));
        Changed?.Invoke();
    }

    public void Clear()
    {
        _timer.Stop();
        _found.Clear();
        _buffer = null;
        Changed?.Invoke();
    }

    /// <summary>The English recognizer (quest names are matched in English); null + a message when Windows has none.</summary>
    OcrEngine? Engine()
    {
        if (!_ocrChecked)
        {
            _ocrChecked = true;
            var english = OcrEngine.AvailableRecognizerLanguages
                                   .FirstOrDefault(l => l.LanguageTag.StartsWith("en", StringComparison.OrdinalIgnoreCase));
            _ocr = english == null ? null : OcrEngine.TryCreateFromLanguage(english);
        }
        if (_ocr == null) Message?.Invoke(Loc.T("t.scan.noOcr"));
        return _ocr;
    }

    async Task ReadFrameAsync(bool single)
    {
        if (_busy) return;  // the previous frame is still being read
        _busy = true;
        try
        {
            if (Engine() is not { } ocr) return;
            var game = Win32.GetForegroundWindow();
            if (ScreenCapture.IsOwnWindow(game))
            {
                if (single) Message?.Invoke(Loc.T("t.scan.focusGame"));
                return;
            }
            var maxSide = (int)OcrEngine.MaxImageDimension;
            var frame = await Task.Run(() => ScreenCapture.Capture(game, TargetHeight, maxSide, _buffer));
            if (frame == null) return;
            _buffer = frame.Pixels;
            if (ScreenCapture.IsBlank(frame))
            {
                Message?.Invoke(Loc.T("t.scan.black"));
                return;
            }
            var fingerprint = ScreenCapture.Fingerprint(frame);
            if (!single && fingerprint == _lastFingerprint) return;  // list not scrolled since the last frame
            _lastFingerprint = fingerprint;

            var cells = await ReadCellsAsync(ocr, frame);
            var matcher = _matcher();
            var result = await Task.Run(() => matcher.Match(cells));
            var added = result.Quests.Where(q => !_found.Contains(q)).ToList();
            _found.AddRange(added);
            if (added.Count > 0) Changed?.Invoke();

            if (_found.Count == 0 && result.Unmatched >= 5) Message?.Invoke(Loc.T("t.scan.english"));
            else if (single) Message?.Invoke(Loc.F("t.scan.read", added.Count, _found.Count));
            else if (added.Count > 0) Message?.Invoke(Loc.F("t.scan.progress", _found.Count, (int)Math.Ceiling(Remaining.TotalSeconds)));
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>Text pieces (table cells) top to bottom, left to right.</summary>
    internal static async Task<List<string>> ReadCellsAsync(OcrEngine ocr, ScreenCapture.Frame frame)
    {
        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(frame.Pixels.AsBuffer(), BitmapPixelFormat.Bgra8,
                                                               frame.Width, frame.Height, BitmapAlphaMode.Ignore);
        var text = await ocr.RecognizeAsync(bitmap);
        return Cells(text.Lines.SelectMany(l => l.Words).Select(w => (w.Text, w.BoundingRect)));
    }

    /// <summary>
    /// Windows' own lines can't be trusted for a table: it splits "Against the Conscience" from "- Part 2" and lists
    /// the pieces far apart. So rows are rebuilt from the word boxes (same height on screen = same row), and a row is
    /// cut into cells where the gap between two words is wider than the text is high (the next column starts there).
    /// </summary>
    internal static List<string> Cells(IEnumerable<(string Text, Windows.Foundation.Rect Box)> words)
    {
        var rows = new List<List<(string Text, Windows.Foundation.Rect Box)>>();
        foreach (var w in words.OrderBy(w => w.Box.Y + w.Box.Height / 2))
        {
            var mid = w.Box.Y + w.Box.Height / 2;
            var row = rows.Count > 0 ? rows[^1] : null;
            // a dash is a flat box: compare with the row's tallest word, not with the dash
            var rowHeight = row?.Max(x => x.Box.Height) ?? 0;
            if (row != null && Math.Abs(mid - row.Average(x => x.Box.Y + x.Box.Height / 2)) < rowHeight / 2) row.Add(w);
            else rows.Add([w]);
        }
        var cells = new List<string>();
        foreach (var row in rows)
        {
            var height = row.Max(x => x.Box.Height);
            var cell = new List<string>();
            double? lastRight = null;
            foreach (var (word, box) in row.OrderBy(x => x.Box.X))
            {
                if (lastRight is { } right && box.X - right > 1.5 * height && cell.Count > 0)
                {
                    cells.Add(string.Join(' ', cell));
                    cell.Clear();
                }
                cell.Add(word);
                lastRight = box.X + box.Width;
            }
            if (cell.Count > 0) cells.Add(string.Join(' ', cell));
        }
        return cells;
    }
}
