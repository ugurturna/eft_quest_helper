using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.Tests;

public class HotkeysTests
{
    [Fact]
    public void Format_puts_modifiers_in_a_fixed_order()
    {
        Assert.Equal("Ctrl+Shift+Alt+Win+F9", Hotkeys.Format(true, true, true, true, "F9"));
        Assert.Equal("Alt+K", Hotkeys.Format(false, false, true, false, "K"));
        Assert.Equal("F8", Hotkeys.Format(false, false, false, false, "F8"));
    }

    [Theory]
    [InlineData("Ctrl+Shift+T", "shift+ctrl+t")]
    [InlineData("Control+Alt+F9", "Alt + Ctrl + F9")]
    public void Same_ignores_order_case_and_spaces(string a, string b) => Assert.True(Hotkeys.Same(a, b));

    [Theory]
    [InlineData("Ctrl+Shift+T", "Ctrl+T")]
    [InlineData("Ctrl+T", "Ctrl+Y")]
    [InlineData("Ctrl+Shift", "Ctrl+Shift")]
    public void Same_needs_the_same_key_and_modifiers(string a, string b) => Assert.False(Hotkeys.Same(a, b));

    [Theory]
    [InlineData("Ctrl+K")]
    [InlineData("Alt+D1")]
    [InlineData("Ctrl+Shift+F9")]
    [InlineData("F8")]
    [InlineData("Shift+F12")]
    public void Check_accepts_keys_held_with_ctrl_alt_or_win_and_lone_f_keys(string gesture) =>
        Assert.Equal(HotkeyProblem.None, Hotkeys.Check(new AppSettings(), Hotkeys.Raid, gesture));

    [Theory]
    [InlineData("K")]
    [InlineData("Shift+K")]
    [InlineData("Space")]
    public void Check_refuses_keys_the_game_would_also_get(string gesture) =>
        Assert.Equal(HotkeyProblem.NeedsModifier, Hotkeys.Check(new AppSettings(), Hotkeys.Raid, gesture));

    [Fact]
    public void Check_refuses_a_lone_modifier_and_alt_f4()
    {
        var s = new AppSettings();
        Assert.Equal(HotkeyProblem.NoKey, Hotkeys.Check(s, Hotkeys.Raid, "Ctrl+LeftShift"));
        Assert.Equal(HotkeyProblem.NoKey, Hotkeys.Check(s, Hotkeys.Raid, "Ctrl+Shift"));
        Assert.Equal(HotkeyProblem.Reserved, Hotkeys.Check(s, Hotkeys.Raid, "Alt+F4"));
    }

    [Fact]
    public void Check_refuses_another_hotkeys_combination_but_not_its_own()
    {
        var s = new AppSettings();
        Assert.Equal(HotkeyProblem.InUse, Hotkeys.Check(s, Hotkeys.Raid, "shift+ctrl+t"));
        Assert.Equal(Hotkeys.ClickThrough, Hotkeys.UsedBy(s, Hotkeys.Raid, "Ctrl+Shift+T"));
        Assert.Equal(HotkeyProblem.None, Hotkeys.Check(s, Hotkeys.ClickThrough, "Ctrl+Shift+T"));
    }

    [Fact]
    public void Set_and_Get_reach_every_hotkey_and_defaults_stay()
    {
        var s = new AppSettings();
        foreach (var id in Hotkeys.Ids)
        {
            Assert.Equal(Hotkeys.Default(id), Hotkeys.Get(s, id));
            Hotkeys.Set(s, id, "Ctrl+Alt+" + id);
            Assert.Equal("Ctrl+Alt+" + id, Hotkeys.Get(s, id));
        }
        Assert.Equal("Ctrl+Shift+T", Hotkeys.Default(Hotkeys.ClickThrough));
        Assert.Equal(Hotkeys.Ids.Count, Hotkeys.Ids.Select(id => Hotkeys.Get(s, id)).Distinct().Count());
    }

    [Fact]
    public void Defaults_do_not_clash()
    {
        var s = new AppSettings();
        foreach (var id in Hotkeys.Ids)
            Assert.Equal(HotkeyProblem.None, Hotkeys.Check(s, id, Hotkeys.Get(s, id)));
    }
}
