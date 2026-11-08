namespace TarkovQuestTracker.Core.Services;

/// <summary>
/// Reads the raid time left ("0:31:55") the game shows at the top right on "O" (and above the exfil list on a
/// double "O"). Windows OCR can't: it skips that cell, and even in a plain font it drops the seconds. So the digits
/// are matched against their shapes in the game's font, cut from real screenshots (the EXFIL01–07 labels share the
/// font; 8 and 9 from clocks on screen). Strict: a digit that matches no shape well means no time, never a guessed
/// one. Only the seconds may be missing (the FPS counter's text runs over them): then the time is ±30 s.
/// </summary>
public static class RaidClockReader
{
    const int GridW = 10, GridH = 14;
    /// <summary>
    /// Mean cell difference (+ aspect difference) a digit may have from its shape: about 0.05 at the size the shapes
    /// were cut at, up to 0.13 on a smaller / enlarged screen. Different digits are at least 0.17 apart.
    /// </summary>
    const double MaxDistance = 0.16;
    /// <summary>The runner-up must be this much worse (real digits: 0.09+), or the digit is ambiguous.</summary>
    const double MinMargin = 0.08;

    /// <summary>Digit, width / height, 10×14 fill per cell (0–9), cut from real screenshots (single and double "O").</summary>
    static readonly (char Digit, double Aspect, double[] Cells)[] Shapes =
    [
        Shape('0', 0.737, "01899998100799999992899533799899500199999940059999994029949999407962999943981299994795029999998102999999300299999711269828999999920289999820"),
        Shape('1', 0.481, "00017993000038999400269999940069999994000262799400000079940000007994000000799400000079940000007994000000799400001189960007999999990799999999"),
        Shape('2', 0.706, "47999999209999999992463333389800000003990000000399000000049900000049940000399970000499960003799620007999300000997000000099999999999999999998"),
        Shape('3', 0.725, "69999999996999999999233333799400000499400000599400000599400000099999400008999996000000079900000002990000000299253000069869999999923699999820"),
        Shape('4', 0.794, "00000029600000029960000007996000004999600004994960002994096001896009600899200960299200096099866689969999999999233333397300000009600000000960"),
        Shape('5', 0.699, "79999999977999999997798333333279700000007970000000799444441079999999823444444998000000049900000002990000000299263000169879999999933689999830"),
        Shape('6', 0.735, "02999999730899999998699433336399400000009940000000997444440099999999819974444898994000049999400003999940000399798200069808999999930189999830"),
        Shape('7', 0.686, "99999999999999999999233333349900000007980000002993000000898000000499200000099600000049910000029950000004991000000498000000049800000004980000"),
        Shape('8', 0.686, "00499998000499999981999744499899700004999960000399998333369907999999922899999993999200089999600003999960000399998333369929999999940289999940"),
        Shape('9', 0.706, "00899999200799999992898333369899400000999930000099996000009989943335990799999999008999989900000000990000000099342000069979999999943689999940"),
    ];

    static (char, double, double[]) Shape(char digit, double aspect, string cells) =>
        (digit, aspect, cells.Select(c => (c - '0') / 9.0).ToArray());

    /// <summary>A text piece on screen: the box of one or more touching / stacked blobs (a colon is two dots).</summary>
    readonly record struct Glyph(int Left, int Top, int Right, int Bottom)
    {
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    /// <summary>
    /// The raid clock sits right at the top of the screen (its top about 1.4 digit heights down); the exfil rows
    /// below have countdowns of their own ("0:00:45" while an exfil opens), the first one about 4.6 digit heights
    /// down. A clock lower than this is one of those, never the raid's.
    /// </summary>
    const double MaxTopInDigits = 2.5;

    /// <summary>
    /// The raid clock in the picture (BGRA, top row first, its top the top of the screen), or null. Only light grey
    /// pixels count as text, so a coloured FPS / ping counter on top of the clock is not read as digits. When the
    /// raid clock can't be read, an exfil's countdown further down is not taken for it.
    /// </summary>
    public static TimeSpan? Read(byte[] bgra, int width, int height)
    {
        // only the top rows: the clock is at the top (<see cref="MaxTopInDigits"/>), and this runs every second
        height = Math.Min(height, TopRows);
        return Read(Mask(bgra, width, height, minLight: 140, maxTint: 60), width, height)
               // the FPS counter's box darkens the clock under it to about a fifth: read it again with a low bar
               ?? Read(InDark(Mask(bgra, width, height, minLight: 24, maxTint: 25), bgra, width, height), width, height);
    }

    /// <summary>Rows that can hold the clock: its digits are at most ~80 px high even on an enlarged 4K corner.</summary>
    const int TopRows = 320;

    /// <summary>Text pixels: grey (not the green / red FPS text) and at least this light.</summary>
    static bool[] Mask(byte[] bgra, int width, int height, int minLight, int maxTint)
    {
        var mask = new bool[width * height];
        for (int i = 0, p = 0; i < mask.Length; i++, p += 4)
        {
            byte b = bgra[p], g = bgra[p + 1], r = bgra[p + 2];
            int lo = Math.Min(r, Math.Min(g, b)), hi = Math.Max(r, Math.Max(g, b));
            mask[i] = lo > minLight && hi - lo < maxTint;
        }
        return mask;
    }

    /// <summary>
    /// Keeps the pixels with near-black within a stroke's reach left / right or up / down: dimmed digits sit in the
    /// FPS counter's black box, a grey wall of the game has no black next to it.
    /// </summary>
    static bool[] InDark(bool[] mask, byte[] bgra, int width, int height)
    {
        const int Reach = 8;
        // dark pixels counted per row / per column, so "any dark within reach" is a subtraction
        var rows = new int[height * (width + 1)];
        var cols = new int[width * (height + 1)];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var p = (y * width + x) * 4;
                var dark = Math.Max(bgra[p], Math.Max(bgra[p + 1], bgra[p + 2])) < 14 ? 1 : 0;
                rows[y * (width + 1) + x + 1] = rows[y * (width + 1) + x] + dark;
                cols[x * (height + 1) + y + 1] = cols[x * (height + 1) + y] + dark;
            }
        var kept = new bool[mask.Length];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (!mask[y * width + x]) continue;
                int x0 = Math.Max(0, x - Reach), x1 = Math.Min(width, x + Reach + 1);
                int y0 = Math.Max(0, y - Reach), y1 = Math.Min(height, y + Reach + 1);
                kept[y * width + x] = rows[y * (width + 1) + x1] - rows[y * (width + 1) + x0] > 0
                                      || cols[x * (height + 1) + y1] - cols[x * (height + 1) + y0] > 0;
            }
        return kept;
    }

    static TimeSpan? Read(bool[] mask, int width, int height)
    {
        var glyphs = Glyphs(mask, width, height);
        foreach (var start in glyphs.Where(g => g.Top <= g.Height * MaxTopInDigits).OrderBy(g => g.Top))
            if (Clock(start, glyphs, mask, width) is { } time) return time;
        return null;
    }

    /// <summary>Blobs of text pixels, merged where they stack (colon dots, the slash in the zero).</summary>
    static List<Glyph> Glyphs(bool[] mask, int width, int height)
    {
        var seen = new bool[mask.Length];
        var blobs = new List<Glyph>();
        var stack = new Stack<int>();
        for (var i = 0; i < mask.Length; i++)
        {
            if (!mask[i] || seen[i]) continue;
            int left = int.MaxValue, top = int.MaxValue, right = 0, bottom = 0, count = 0;
            seen[i] = true;
            stack.Push(i);
            while (stack.Count > 0)
            {
                var p = stack.Pop();
                int x = p % width, y = p / width;
                left = Math.Min(left, x); right = Math.Max(right, x + 1);
                top = Math.Min(top, y); bottom = Math.Max(bottom, y + 1);
                count++;
                if (x > 0) Visit(p - 1);
                if (x + 1 < width) Visit(p + 1);
                if (y > 0) Visit(p - width);
                if (y + 1 < height) Visit(p + width);
            }
            if (count >= 3 && right - left <= 300 && bottom - top <= 300) blobs.Add(new Glyph(left, top, right, bottom));
        }
        // stacked blobs (overlapping columns, close rows) are one glyph; a colon's two dots are further apart
        blobs.Sort((a, b) => a.Left.CompareTo(b.Left));
        var merged = new List<Glyph>();
        foreach (var blob in blobs)
        {
            var at = merged.FindLastIndex(m =>
            {
                if (Overlap(m.Left, m.Right, blob.Left, blob.Right) * 2 < Math.Min(m.Width, blob.Width)) return false;
                var gap = Math.Max(m.Top, blob.Top) - Math.Min(m.Bottom, blob.Bottom);
                // pieces of one glyph touch or nearly (the zero's slash); text of the next row is further away
                return gap <= Math.Max(m.Height, blob.Height) / 3 || AreColonDots(m, blob) && gap <= 3 * Math.Max(m.Height, blob.Height);
            });
            if (at < 0) merged.Add(blob);
            else
            {
                var m = merged[at];
                merged[at] = new Glyph(Math.Min(m.Left, blob.Left), Math.Min(m.Top, blob.Top), Math.Max(m.Right, blob.Right), Math.Max(m.Bottom, blob.Bottom));
            }
        }
        return merged;

        void Visit(int q)
        {
            if (!mask[q] || seen[q]) return;
            seen[q] = true;
            stack.Push(q);
        }
    }

    static int Overlap(int a0, int a1, int b0, int b1) => Math.Max(0, Math.Min(a1, b1) - Math.Max(a0, b0));

    /// <summary>Two roughly square blobs of about the same size: the dots of a colon (not a digit and a dot).</summary>
    static bool AreColonDots(Glyph a, Glyph b) =>
        IsSquare(a) && IsSquare(b) && a.Height * 3 >= b.Height * 2 && b.Height * 3 >= a.Height * 2;

    /// <summary>Dots are square; digits are about 1.4 times as high as wide.</summary>
    static bool IsSquare(Glyph g) => g.Width * 13 >= g.Height * 10 && g.Height * 13 >= g.Width * 10;

    /// <summary>
    /// "h:mm:ss" (or "hh:mm:ss") starting at <paramref name="first"/>, read digit by digit; null if anything is off.
    /// Seconds that can't be read (FPS text over them) are taken as :30, after "h:mm:" was read for sure.
    /// </summary>
    static TimeSpan? Clock(Glyph first, List<Glyph> glyphs, bool[] mask, int width)
    {
        if (!IsDigit(first, first.Height) || Classify(first, mask, width) is not { } hour) return null;
        var digits = new List<char> { hour };
        var current = first;
        // an optional second hour digit
        if (NextOnLine(current, first, glyphs) is { } second && IsDigit(second, first.Height))
        {
            if (Classify(second, mask, width) is not { } d) return null;
            digits.Add(d);
            current = second;
        }
        var h = int.Parse(new string(digits.ToArray()));
        if (ReadPair(ref current, first, glyphs, mask, width) is not { } m) return null;
        // the colon after the minutes must be there: "0:18" alone could be any text
        if (NextOnLine(current, first, glyphs) is not { } colon || !IsColon(colon, first)) return null;
        var s = ReadPair(ref current, first, glyphs, mask, width) ?? 30;
        return m < 60 && s < 60 && h <= 3 ? new TimeSpan(h, m, s) : null;
    }

    /// <summary>":dd" after <paramref name="current"/> (which moves to its last digit), or null.</summary>
    static int? ReadPair(ref Glyph current, Glyph line, List<Glyph> glyphs, bool[] mask, int width)
    {
        if (NextOnLine(current, line, glyphs) is not { } colon || !IsColon(colon, line)) return null;
        var at = colon;
        var value = 0;
        for (var i = 0; i < 2; i++)
        {
            if (NextOnLine(at, line, glyphs) is not { } g || !IsDigit(g, line.Height) || Classify(g, mask, width) is not { } d)
                return null;
            value = value * 10 + (d - '0');
            at = g;
        }
        current = at;
        return value;
    }

    static bool IsDigit(Glyph g, int lineHeight) =>
        g.Height >= 12 && Math.Abs(g.Height - lineHeight) <= lineHeight * 0.15
        && g.Width >= g.Height * 0.3 && g.Width <= g.Height * 1.0;

    /// <summary>Two dots: short and narrow, inside the line's height.</summary>
    static bool IsColon(Glyph g, Glyph line) =>
        g.Height >= line.Height * 0.3 && g.Height <= line.Height * 0.85 && g.Width <= line.Height * 0.35
        && g.Top >= line.Top - line.Height * 0.1 && g.Bottom <= line.Bottom + line.Height * 0.1;

    /// <summary>The closest glyph to the right on the same line, not further than a character away.</summary>
    static Glyph? NextOnLine(Glyph current, Glyph line, List<Glyph> glyphs)
    {
        Glyph? best = null;
        foreach (var g in glyphs)
        {
            if (g.Left < current.Right || g.Left - current.Right > line.Height * 0.7) continue;
            var middle = (g.Top + g.Bottom) / 2;
            if (middle < line.Top || middle > line.Bottom) continue;
            if (best == null || g.Left < best.Value.Left) best = g;
        }
        return best;
    }

    /// <summary>The digit whose shape is clearly the closest, or null.</summary>
    static char? Classify(Glyph g, bool[] mask, int width)
    {
        var cells = new double[GridW * GridH];
        for (var gy = 0; gy < GridH; gy++)
        {
            int y0 = g.Top + gy * g.Height / GridH, y1 = Math.Max(y0 + 1, g.Top + (gy + 1) * g.Height / GridH);
            for (var gx = 0; gx < GridW; gx++)
            {
                int x0 = g.Left + gx * g.Width / GridW, x1 = Math.Max(x0 + 1, g.Left + (gx + 1) * g.Width / GridW);
                var on = 0;
                for (var y = y0; y < y1; y++)
                    for (var x = x0; x < x1; x++)
                        if (mask[y * width + x]) on++;
                cells[gy * GridW + gx] = (double)on / ((x1 - x0) * (y1 - y0));
            }
        }
        var aspect = (double)g.Width / g.Height;
        var ranked = Shapes.Select(s => (s.Digit, Distance: cells.Zip(s.Cells, (a, b) => Math.Abs(a - b)).Average() + Math.Abs(aspect - s.Aspect)))
                           .OrderBy(x => x.Distance).ToList();
        return ranked[0].Distance <= MaxDistance && ranked[1].Distance - ranked[0].Distance >= MinMargin ? ranked[0].Digit : null;
    }
}
