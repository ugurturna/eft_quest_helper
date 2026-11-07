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
