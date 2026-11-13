using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.ViewModels;

/// <summary>Global hotkeys the player sets in Settings: saved in the profile, registered again by the overlay.</summary>
public sealed partial class MainViewModel
{
    /// <summary>Sets hotkey <paramref name="id"/> to <paramref name="gesture"/> if it can be one; saved at once.</summary>
    public HotkeyProblem SetHotkey(string id, string gesture)
    {
        var problem = Hotkeys.Check(Settings, id, gesture);
        if (problem != HotkeyProblem.None) return problem;
        Hotkeys.Set(Settings, id, gesture);
        HotkeysSaved();
        return problem;
    }

    /// <summary>Puts back the key a hotkey had (Windows would not take the new one), without checking it again.</summary>
    public void RestoreHotkey(string id, string gesture)
    {
        Hotkeys.Set(Settings, id, gesture);
        HotkeysSaved();
    }

    public void ResetHotkeys()
    {
        foreach (var id in Hotkeys.Ids) Hotkeys.Set(Settings, id, Hotkeys.Default(id));
        HotkeysSaved();
    }

    void HotkeysSaved()
    {
        SaveSettings();
        OnPropertyChanged(nameof(Settings));  // the key captions in Settings and the bottom bar
    }
}
