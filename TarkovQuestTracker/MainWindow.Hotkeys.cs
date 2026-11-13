using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker;

/// <summary>Settings → Shortcuts: click a key, press the new combination; Esc or clicking away keeps the old one.</summary>
public partial class MainWindow
{
    Button? _recording;
    BindingBase? _recordingCaption;

    /// <summary>The bottom bar's short names, for "already used by …".</summary>
    static readonly Dictionary<string, string> HotkeyNames = new()
    {
        [Hotkeys.ClickThrough] = "s.keys.barLock",
        [Hotkeys.ToggleOverlay] = "s.keys.barToggle",
        [Hotkeys.FleaSearch] = "s.keys.barFlea",
        [Hotkeys.Raid] = "s.keys.barRaid",
        [Hotkeys.Scan] = "s.keys.barScan",
        [Hotkeys.ItemCheck] = "s.keys.barItem",
    };

    void Hotkey_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var again = button == _recording;
        StopRecording();
        if (again) return;  // a second click cancels
        _recording = button;
        _recordingCaption = BindingOperations.GetBindingBase(button, ContentControl.ContentProperty);
        button.Content = Loc.T("s.keys.press");
        // the current keys would fire (and never reach this window) while the new one is pressed
        _overlay.PauseHotkeys();
        PreviewKeyDown += RecordHotkey;
        button.LostKeyboardFocus += RecordingLostFocus;
    }

    void RecordHotkey(object sender, KeyEventArgs e)
    {
        if (_recording is not { } button) return;
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var mods = Keyboard.Modifiers;
        if (key == Key.Escape && mods == ModifierKeys.None)
        {
            StopRecording();
            return;
        }
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt
            or Key.LWin or Key.RWin or Key.System or Key.ImeProcessed or Key.DeadCharProcessed or Key.None)
            return;  // only a modifier so far: wait for the key itself

        var id = (string)button.Tag;
        var old = Hotkeys.Get(_vm.Settings, id);
        var gesture = Hotkeys.Format((mods & ModifierKeys.Control) != 0, (mods & ModifierKeys.Shift) != 0,
            (mods & ModifierKeys.Alt) != 0, (mods & ModifierKeys.Windows) != 0, key.ToString());
        var problem = _vm.SetHotkey(id, gesture);
        var failed = StopRecording();
        if (problem != HotkeyProblem.None)
        {
            _vm.Toast = problem == HotkeyProblem.InUse
                ? Loc.F("t.key.inUse", gesture, Loc.T(HotkeyNames[Hotkeys.UsedBy(_vm.Settings, id, gesture)!]))
                : Loc.F(problem == HotkeyProblem.Reserved ? "t.key.reserved" : "t.key.needsMod", gesture);
            return;
        }
        if (failed.Any(f => Hotkeys.Same(f, gesture)))
        {
            // another program holds it: keep the key that worked
            _vm.RestoreHotkey(id, old);
            _overlay.RegisterHotkeys();
            _vm.Toast = Loc.F("t.key.taken", gesture, old);
            return;
        }
        _vm.Toast = Loc.F("t.key.saved", gesture);
    }

    void RecordingLostFocus(object sender, KeyboardFocusChangedEventArgs e) => StopRecording();

    /// <summary>Ends recording (if any): the key caption comes back and the hotkeys work again.</summary>
    List<string> StopRecording()
    {
        if (_recording is not { } button) return [];
        _recording = null;
        PreviewKeyDown -= RecordHotkey;
        button.LostKeyboardFocus -= RecordingLostFocus;
        if (_recordingCaption != null) button.SetBinding(ContentControl.ContentProperty, _recordingCaption);
        return _overlay.RegisterHotkeys();
    }

    void ResetHotkeys_Click(object sender, RoutedEventArgs e)
    {
        StopRecording();
        _vm.ResetHotkeys();
        if (_overlay.RegisterHotkeys().Count == 0) _vm.Toast = Loc.T("t.key.reset");
    }
}
