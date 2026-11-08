using System.Runtime.InteropServices;

namespace TarkovQuestTracker.Interop;

/// <summary>
/// Copies what the screen shows inside a window (the game) with plain GDI, like any screenshot tool.
/// Never touches the game process: no reading its memory, no hooks, no input sent to it.
/// </summary>
static class ScreenCapture
{
    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFOHEADER
    {
        public int Size, Width, Height;
        public short Planes, BitCount;
        public int Compression, SizeImage, XPelsPerMeter, YPelsPerMeter, ClrUsed, ClrImportant;
    }

    delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] static extern int SetStretchBltMode(IntPtr dc, int mode);
    [DllImport("gdi32.dll")] static extern bool SetBrushOrgEx(IntPtr dc, int x, int y, IntPtr previous);
    [DllImport("gdi32.dll")]
    static extern bool StretchBlt(IntPtr dest, int dx, int dy, int dw, int dh, IntPtr src, int sx, int sy, int sw, int sh, uint rop);
    [DllImport("gdi32.dll")]
    static extern IntPtr CreateDIBSection(IntPtr dc, ref BITMAPINFOHEADER info, uint usage, out IntPtr bits, IntPtr section, uint offset);

    const int HALFTONE = 4;
    const uint SRCCOPY = 0x00CC0020;

    /// <summary>A captured picture: BGRA, top row first.</summary>
    public sealed record Frame(byte[] Pixels, int Width, int Height);

    /// <summary>True for windows of this app (main window, overlay, flea search).</summary>
    public static bool IsOwnWindow(IntPtr hwnd)
    {
        GetWindowThreadProcessId(hwnd, out var pid);
        return pid == Environment.ProcessId;
    }

    /// <summary>
    /// The window's area, scaled so its height is about <paramref name="targetHeight"/> (small game text reads
    /// better enlarged), at most <paramref name="maxSide"/> pixels a side. This app's own windows on top of it
    /// (the overlay) are blanked out, so their quest names are not read as the game's; one left out of captures
    /// (<see cref="Win32.ExcludeFromCapture"/>) is not in the picture anyway, and the game under it is kept.
    /// <paramref name="reuse"/>: the previous frame's pixels, filled again when the size still fits (no new 20 MB array per frame).
    /// <paramref name="part"/>: only this part of the window, as fractions of its width / height (the exfil list corner).
    /// </summary>
    public static Frame? Capture(IntPtr hwnd, int targetHeight, int maxSide, byte[]? reuse = null,
                                 (double Left, double Top, double Right, double Bottom)? part = null)
    {
        if (!GetWindowRect(hwnd, out var r)) return null;
        if (r.Right - r.Left < 200 || r.Bottom - r.Top < 200) return null;
        if (part is { } p)
        {
            int ww = r.Right - r.Left, wh = r.Bottom - r.Top;
            r = new RECT { Left = r.Left + (int)(p.Left * ww), Top = r.Top + (int)(p.Top * wh),
                           Right = r.Left + (int)(p.Right * ww), Bottom = r.Top + (int)(p.Bottom * wh) };
        }
        int w = r.Right - r.Left, h = r.Bottom - r.Top;
        if (w < 20 || h < 20) return null;
        var scale = Math.Min(Math.Clamp((double)targetHeight / h, 1, 2), (double)maxSide / Math.Max(w, h));
        int tw = (int)(w * scale), th = (int)(h * scale);

        var screen = GetDC(IntPtr.Zero);
        var mem = CreateCompatibleDC(screen);
        // negative height = top-down rows
        var info = new BITMAPINFOHEADER { Size = Marshal.SizeOf<BITMAPINFOHEADER>(), Width = tw, Height = -th, Planes = 1, BitCount = 32 };
        var bitmap = CreateDIBSection(mem, ref info, 0, out var bits, IntPtr.Zero, 0);
        try
        {
            if (bitmap == IntPtr.Zero) return null;
            var old = SelectObject(mem, bitmap);
            SetStretchBltMode(mem, HALFTONE);
            SetBrushOrgEx(mem, 0, 0, IntPtr.Zero);
            var ok = StretchBlt(mem, 0, 0, tw, th, screen, r.Left, r.Top, w, h, SRCCOPY);
            SelectObject(mem, old);
            if (!ok) return null;
            var pixels = reuse?.Length == tw * th * 4 ? reuse : new byte[tw * th * 4];
            Marshal.Copy(bits, pixels, 0, pixels.Length);
            foreach (var own in OwnVisibleWindows())
                if (GetWindowRect(own, out var o))
                    Blank(pixels, tw, th, (int)((o.Left - r.Left) * scale), (int)((o.Top - r.Top) * scale),
                          (int)Math.Ceiling((o.Right - r.Left) * scale), (int)Math.Ceiling((o.Bottom - r.Top) * scale));
            return new Frame(pixels, tw, th);
        }
        finally
        {
            if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
            DeleteDC(mem);
            ReleaseDC(IntPtr.Zero, screen);
        }
    }

    /// <summary>All black: exclusive fullscreen hands GDI an empty picture.</summary>
    public static bool IsBlank(Frame f)
    {
        for (var i = 0; i < f.Pixels.Length; i += 4 * 97)
            if (f.Pixels[i] > 8 || f.Pixels[i + 1] > 8 || f.Pixels[i + 2] > 8) return false;
        return true;
    }

    /// <summary>Cheap fingerprint of a frame (every 61st pixel) to skip ones that did not change.</summary>
    public static int Fingerprint(Frame f)
    {
        var hash = new HashCode();
        for (var i = 0; i + 3 < f.Pixels.Length; i += 4 * 61) hash.Add(BitConverter.ToInt32(f.Pixels, i));
        return hash.ToHashCode();
    }

    static List<IntPtr> OwnVisibleWindows()
    {
        var list = new List<IntPtr>();
        var pid = (uint)Environment.ProcessId;
        EnumWindows((hwnd, _) =>
        {
            // a window left out of captures (the overlay) is not in the picture: the game under it is, keep that
            if (IsWindowVisible(hwnd) && GetWindowThreadProcessId(hwnd, out var p) != 0 && p == pid
                && !Win32.IsExcludedFromCapture(hwnd)) list.Add(hwnd);
            return true;
        }, IntPtr.Zero);
        return list;
    }

    static void Blank(byte[] pixels, int width, int height, int left, int top, int right, int bottom)
    {
        left = Math.Max(0, left);
        top = Math.Max(0, top);
        right = Math.Min(width, right);
        bottom = Math.Min(height, bottom);
        if (left >= right) return;
        for (var y = top; y < bottom; y++) Array.Clear(pixels, (y * width + left) * 4, (right - left) * 4);
    }
}
