using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace TarkovQuestTracker.Interop;

static class Win32
{
    const int GWL_EXSTYLE = -20;
    const int WS_EX_TRANSPARENT = 0x00000020;
    const int WS_EX_TOOLWINDOW = 0x00000080;
    const int WS_EX_LAYERED = 0x00080000;
    const int WS_EX_NOACTIVATE = 0x08000000;
    static readonly IntPtr HWND_TOPMOST = new(-1);
    const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr value);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);

    [DllImport("user32.dll")]
    static extern bool AttachThreadInput(uint attach, uint attachTo, bool on);

    [DllImport("user32.dll")]
    static extern bool BringWindowToTop(IntPtr hwnd);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    /// <summary>
    /// Gives the window the keyboard even when this app is in the background (a screenshot opened the map, no hotkey):
    /// Windows refuses SetForegroundWindow then, unless our thread shares input with the foreground one for a moment.
    /// Nothing is sent to the game.
    /// </summary>
    public static void TakeForeground(IntPtr hwnd)
    {
        var foreground = GetForegroundWindow();
        if (foreground == hwnd) return;
        var theirs = GetWindowThreadProcessId(foreground, out _);
        var ours = GetCurrentThreadId();
        var attached = theirs != 0 && theirs != ours && AttachThreadInput(ours, theirs, true);
        try
        {
            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attached) AttachThreadInput(ours, theirs, false);
        }
    }

    [DllImport("user32.dll")]
    static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

    [DllImport("user32.dll")]
    static extern bool GetWindowDisplayAffinity(IntPtr hwnd, out uint affinity);

    const uint WDA_NONE = 0, WDA_EXCLUDEFROMCAPTURE = 0x11;

    /// <summary>
    /// Leaves the window out of screen pictures (Windows 10 2004+): it stays on the monitor for the player, but a
    /// capture shows what is under it. So the overlay can sit on the exfil list and the list is still readable.
    /// Returns false when Windows can't do it.
    /// </summary>
    public static bool ExcludeFromCapture(IntPtr hwnd, bool on) =>
        SetWindowDisplayAffinity(hwnd, on ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE);

    public static bool IsExcludedFromCapture(IntPtr hwnd) =>
        GetWindowDisplayAffinity(hwnd, out var affinity) && affinity == WDA_EXCLUDEFROMCAPTURE;

    static IntPtr _lastWindow;
    static bool _lastIsGame;

    /// <summary>The window belongs to the game (EscapeFromTarkov.exe). Remembers the last window asked about.</summary>
    public static bool IsGameWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        if (hwnd == _lastWindow) return _lastIsGame;
        _lastWindow = hwnd;
        _lastIsGame = false;
        if (GetWindowThreadProcessId(hwnd, out var pid) == 0) return false;
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById((int)pid);
            _lastIsGame = process.ProcessName.Equals("EscapeFromTarkov", StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException) { }  // the process exited meanwhile
        return _lastIsGame;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct MONITORINFO { public int Size; public RECT Monitor, Work; public uint Flags; }

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

    /// <summary>
    /// Centers the window on the monitor showing <paramref name="reference"/> (the game), in physical pixels,
    /// so display scaling (125 %, 150 %) can't push it off center.
    /// </summary>
    public static void CenterOnMonitorOf(IntPtr hwnd, IntPtr reference)
    {
        const uint MONITOR_DEFAULTTOPRIMARY = 1, SWP_NOZORDER = 0x0004;
        var info = new MONITORINFO { Size = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(MonitorFromWindow(reference, MONITOR_DEFAULTTOPRIMARY), ref info) || !GetWindowRect(hwnd, out var r)) return;
        var m = info.Monitor;
        int w = r.Right - r.Left, h = r.Bottom - r.Top;
        SetWindowPos(hwnd, IntPtr.Zero, m.Left + (m.Right - m.Left - w) / 2, m.Top + (m.Bottom - m.Top - h) / 2, 0, 0,
                     SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int X, Y; }

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromPoint(POINT point, uint flags);

    /// <summary>The mouse position on the screen, physical pixels.</summary>
    public static (int X, int Y)? Cursor() => GetCursorPos(out var p) ? (p.X, p.Y) : null;

    /// <summary>The window's size in physical pixels.</summary>
    public static (int Width, int Height) Size(IntPtr hwnd) =>
        GetWindowRect(hwnd, out var r) ? (r.Right - r.Left, r.Bottom - r.Top) : (0, 0);

    /// <summary>The whole monitor the point is on (left, top, right, bottom), physical pixels.</summary>
    public static (int Left, int Top, int Right, int Bottom) MonitorAt(int x, int y)
    {
        const uint MONITOR_DEFAULTTONEAREST = 2;
        var info = new MONITORINFO { Size = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(MonitorFromPoint(new POINT { X = x, Y = y }, MONITOR_DEFAULTTONEAREST), ref info))
            return (0, 0, 1920, 1080);
        return (info.Monitor.Left, info.Monitor.Top, info.Monitor.Right, info.Monitor.Bottom);
    }

    /// <summary>Moves the window to a point in physical pixels, above the game, without focusing it.</summary>
    public static void MoveTo(IntPtr hwnd, int x, int y) =>
        SetWindowPos(hwnd, HWND_TOPMOST, x, y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);

    /// <summary>No taskbar/alt-tab entry, but can take keyboard focus (for typing).</summary>
    public static void MakeToolWindow(IntPtr hwnd) => SetExStyle(hwnd, Ex(hwnd) | WS_EX_TOOLWINDOW);

    /// <summary>Overlay base style: no taskbar/alt-tab entry and never steals focus from the game.</summary>
    public static void MakeOverlay(IntPtr hwnd) =>
        SetExStyle(hwnd, Ex(hwnd) | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED);

    /// <summary>Mouse clicks pass through to the window below (the game).</summary>
    public static void SetClickThrough(IntPtr hwnd, bool on) =>
        SetExStyle(hwnd, on ? Ex(hwnd) | WS_EX_TRANSPARENT | WS_EX_LAYERED : Ex(hwnd) & ~WS_EX_TRANSPARENT);

    /// <summary>Borderless games can push themselves above; re-assert topmost periodically.</summary>
    public static void BringToTop(IntPtr hwnd) =>
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

    static long Ex(IntPtr hwnd) => GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();

    static void SetExStyle(IntPtr hwnd, long style) => SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(style));
}

/// <summary>System-wide hotkeys (work while the game has focus).</summary>
sealed class HotkeyManager : IDisposable
{
    const int WM_HOTKEY = 0x0312;
    const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_SHIFT = 4, MOD_WIN = 8, MOD_NOREPEAT = 0x4000;

    readonly IntPtr _hwnd;
    readonly Dictionary<int, Action> _actions = [];
    int _nextId = 0x5100;

    public HotkeyManager(IntPtr hwnd)
    {
        _hwnd = hwnd;
        HwndSource.FromHwnd(hwnd)!.AddHook(Hook);
    }

    /// <summary>Registers e.g. "Ctrl+Shift+T". Returns false if the text is invalid or the key is taken.</summary>
    public bool Register(string gesture, Action action)
    {
        uint mods = MOD_NOREPEAT;
        Key? key = null;
        foreach (var part in gesture.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl" or "control": mods |= MOD_CONTROL; break;
                case "shift": mods |= MOD_SHIFT; break;
                case "alt": mods |= MOD_ALT; break;
                case "win": mods |= MOD_WIN; break;
                default:
                    if (Enum.TryParse<Key>(part, true, out var k)) key = k;
                    else return false;
                    break;
            }
        }
        if (key == null) return false;
        var id = _nextId++;
        if (!Win32.RegisterHotKey(_hwnd, id, mods, (uint)KeyInterop.VirtualKeyFromKey(key.Value))) return false;
        _actions[id] = action;
        return true;
    }

    IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && _actions.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        foreach (var id in _actions.Keys) Win32.UnregisterHotKey(_hwnd, id);
        _actions.Clear();
    }
}
