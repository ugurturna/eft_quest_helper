using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>Why a pressed key combination cannot be a hotkey.</summary>
public enum HotkeyProblem { None, NoKey, NeedsModifier, Reserved, InUse }

/// <summary>
/// The app's global hotkeys as the player sets them in Settings: which ones there are, their defaults,
/// and whether a pressed combination ("Ctrl+Shift+F9") can be one.
/// </summary>
public static class Hotkeys
{
    public const string ClickThrough = "clickThrough", ToggleOverlay = "toggleOverlay", FleaSearch = "fleaSearch",
        Raid = "raid", Scan = "scan", ItemCheck = "itemCheck";

    public static readonly IReadOnlyList<string> Ids = [ClickThrough, ToggleOverlay, FleaSearch, Raid, Scan, ItemCheck];

    static readonly AppSettings Defaults = new();

    public static string Get(AppSettings s, string id) => id switch
    {
        ClickThrough => s.ClickThroughHotkey,
        ToggleOverlay => s.ToggleOverlayHotkey,
        FleaSearch => s.FleaSearchHotkey,
        Raid => s.RaidHotkey,
        Scan => s.ScanHotkey,
        ItemCheck => s.ItemCheckHotkey,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
    };

    public static void Set(AppSettings s, string id, string gesture)
    {
        switch (id)
        {
            case ClickThrough: s.ClickThroughHotkey = gesture; break;
            case ToggleOverlay: s.ToggleOverlayHotkey = gesture; break;
            case FleaSearch: s.FleaSearchHotkey = gesture; break;
            case Raid: s.RaidHotkey = gesture; break;
            case Scan: s.ScanHotkey = gesture; break;
            case ItemCheck: s.ItemCheckHotkey = gesture; break;
            default: throw new ArgumentOutOfRangeException(nameof(id), id, null);
        }
    }

    public static string Default(string id) => Get(Defaults, id);

    /// <summary>
    /// "Ctrl+Shift+F9" from the held modifiers and the key's name (WPF <c>Key</c> names, which the
    /// hotkey parser reads back). Modifiers always come in the same order, so equal combinations read the same.
    /// </summary>
    public static string Format(bool ctrl, bool shift, bool alt, bool win, string key)
    {
        var parts = new List<string>(5);
        if (ctrl) parts.Add("Ctrl");
        if (shift) parts.Add("Shift");
        if (alt) parts.Add("Alt");
        if (win) parts.Add("Win");
        parts.Add(key);
        return string.Join("+", parts);
    }

    /// <summary>
    /// Whether <paramref name="gesture"/> can be the hotkey <paramref name="id"/>: a real key, held with
    /// Ctrl / Alt / Win (an F key may be alone; letters alone or with Shift would fire while typing or playing),
    /// not one Windows keeps for itself, and not another of the app's hotkeys.
    /// </summary>
    public static HotkeyProblem Check(AppSettings s, string id, string gesture)
    {
        var (mods, key) = Split(gesture);
        if (key == null || IsModifier(key)) return HotkeyProblem.NoKey;
        if (mods.SetEquals(["alt"]) && key == "f4") return HotkeyProblem.Reserved;
        if (!mods.Overlaps(["ctrl", "alt", "win"]) && !IsFunctionKey(key)) return HotkeyProblem.NeedsModifier;
        foreach (var other in Ids)
            if (other != id && Same(Get(s, other), gesture)) return HotkeyProblem.InUse;
        return HotkeyProblem.None;
    }

    /// <summary>The hotkey that already uses <paramref name="gesture"/>, other than <paramref name="id"/>.</summary>
    public static string? UsedBy(AppSettings s, string id, string gesture) =>
        Ids.FirstOrDefault(other => other != id && Same(Get(s, other), gesture));

    /// <summary>Same combination whatever the order or case ("shift+ctrl+t" = "Ctrl+Shift+T").</summary>
    public static bool Same(string a, string b)
    {
        var (ma, ka) = Split(a);
        var (mb, kb) = Split(b);
        return ka != null && ka == kb && ma.SetEquals(mb);
    }

    static (HashSet<string> Mods, string? Key) Split(string gesture)
    {
        var mods = new HashSet<string>();
        string? key = null;
        foreach (var part in gesture.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var p = part.ToLowerInvariant();
            if (p is "ctrl" or "control") mods.Add("ctrl");
            else if (p is "shift" or "alt" or "win") mods.Add(p);
            else key = p;
        }
        return (mods, key);
    }

    static bool IsModifier(string key) => key is "leftctrl" or "rightctrl" or "leftshift" or "rightshift"
        or "leftalt" or "rightalt" or "lwin" or "rwin" or "system";

    static bool IsFunctionKey(string key) =>
        key.Length is 2 or 3 && key[0] == 'f' && int.TryParse(key[1..], out var n) && n is >= 1 and <= 24;
}
