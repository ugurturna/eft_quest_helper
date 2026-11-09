using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using TarkovQuestTracker.Interop;
using TarkovQuestTracker.ViewModels;

namespace TarkovQuestTracker;

/// <summary>Always-on-top tracker; drag its header to put it anywhere on the screen (starts at the right edge).</summary>
public partial class OverlayWindow : Window
{
    readonly MainViewModel _vm;
    readonly DispatcherTimer _topmostTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    readonly DispatcherTimer _autoHideTimer = new();
    HotkeyManager? _hotkeys;
    FleaSearchWindow? _flea;
    MapWindow? _map;
    IntPtr _hwnd;
    Point? _dragStart;

    public OverlayWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        // content height changes (quests added, detail mode, scale) must not push the overlay off screen
        SizeChanged += (_, _) => ApplyPosition();
        _topmostTimer.Tick += (_, _) => { if (IsVisible) Win32.BringToTop(_hwnd); };
        _autoHideTimer.Tick += (_, _) => AutoHideTick();
        vm.OverlayPeek += Peek;
        vm.MapToggleRequested += ToggleMap;
        vm.MapShowRequested += () => (_map ??= new MapWindow(_vm)).ShowMap();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.OverlayScale)) FitHeight();
            if (e.PropertyName is nameof(MainViewModel.OverlayPosition) or nameof(MainViewModel.OverlayPositionX)
                && _dragStart == null) ApplyPosition();
            if (e.PropertyName == nameof(MainViewModel.RaidMap) && vm.RaidMap != null) LockForRaid();
            if (e.PropertyName == nameof(MainViewModel.ReadsScreen)) ApplyCaptureExclusion();
            if (e.PropertyName == nameof(MainViewModel.OverlayAutoHide)) OnAutoHideChanged();
        };
    }

    // auto-hide: hidden until "O" is pressed in game (or the hotkey), then gone again after a few seconds

    /// <summary>Shows the auto-hidden overlay for the set time (again from the start when it is already up).</summary>
    void Peek()
    {
        if (!_vm.OverlayAutoHide) return;
        if (!IsVisible) Show();
        _autoHideTimer.Stop();
        _autoHideTimer.Interval = TimeSpan.FromSeconds(_vm.OverlayAutoHideSeconds);
        _autoHideTimer.Start();
    }

    void AutoHideTick()
    {
        // not while the player is using it: the mouse on it, the map list open, dragging
        if (IsMouseOver || MapPicker.Visibility == Visibility.Visible || _dragStart != null) return;
        _autoHideTimer.Stop();
        Hide();
    }

    void OnAutoHideChanged()
    {
        if (_vm.OverlayAutoHide)
        {
            Peek();  // shows what it does: up now, gone in a few seconds
            return;
        }
        _autoHideTimer.Stop();
        if (_vm.Settings.OverlayVisible) Show();
    }

    /// <summary>
    /// While the corner is read (auto raid detection / auto-hide), the overlay is left out of screen pictures: it may sit right on the exfil list
    /// (top right, where most players keep it) and the list is still read. The player keeps seeing it as always;
    /// only recordings / streams / screenshots don't show it.
    /// </summary>
    void ApplyCaptureExclusion()
    {
        if (_hwnd != IntPtr.Zero) Win32.ExcludeFromCapture(_hwnd, _vm.ReadsScreen);
    }

    /// <summary>
    /// A raid started (picked or detected from the exfil list): clicks must reach the game, so the overlay locks.
    /// Only on the start: unlocking it mid-raid stays the player's choice (a time update doesn't relock it).
    /// </summary>
    void LockForRaid()
    {
        if (MapPicker.Visibility == Visibility.Visible) _lockedBeforePicker = true;  // locks when the picker closes
        else if (!ClickThrough) SetClickThrough(true);
    }

    public bool ClickThrough { get; private set; }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        Win32.MakeOverlay(_hwnd);
        ApplyCaptureExclusion();
        FitHeight();
        ApplyPosition();

        _hotkeys = new HotkeyManager(_hwnd);
        RegisterHotkeys();
        _vm.ProfileChanged += OnProfileChanged;
        Loc.Changed += UpdateLockText;

        SetClickThrough(false);
        _topmostTimer.Start();
        Dispatcher.BeginInvoke(Peek);  // after the start-up (the handle is made before the window is shown)
    }

    protected override void OnClosed(EventArgs e)
    {
        _topmostTimer.Stop();
        _autoHideTimer.Stop();
        _hotkeys?.Dispose();
        _flea?.Close();
        _map?.Close();
        base.OnClosed(e);
    }


    /// <summary>
    /// At the saved position (fractions of the free width / height, resolution independent). Fractions keep
    /// the overlay on screen when its size changes; at 1 it stays flush against the right edge.
    /// </summary>
    void ApplyPosition()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Left + Edge + Math.Clamp(_vm.OverlayPositionX, 0, 1) * FreeWidth(area);
        Top = area.Top + Math.Clamp(_vm.OverlayPosition, 0, 1) * FreeHeight(area);
    }

    /// <summary>Gap kept from the left / right screen edges.</summary>
    const double Edge = 6;

    double FreeWidth(Rect area) => Math.Max(0, area.Width - ActualWidth - 2 * Edge);
    double FreeHeight(Rect area) => Math.Max(0, area.Height - ActualHeight);

    /// <summary>Keeps the scaled overlay inside the screen height.</summary>
    void FitHeight()
    {
        var area = SystemParameters.WorkArea;
        Scroller.MaxHeight = Math.Max(120, (area.Height - 40) / _vm.OverlayScale - 150);
    }

    // header drag: anywhere on the screen, kept inside the work area
    void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        Header.CaptureMouse();
        e.Handled = true;
    }

    void Header_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStart is not { } start || e.LeftButton != MouseButtonState.Pressed) return;
        var area = SystemParameters.WorkArea;
        var mouse = e.GetPosition(this);
        var freeX = FreeWidth(area);
        var freeY = FreeHeight(area);
        var left = Math.Clamp(Left + mouse.X - start.X, area.Left + Edge, area.Left + Edge + freeX);
        var top = Math.Clamp(Top + mouse.Y - start.Y, area.Top, area.Top + freeY);
        Left = left;
        Top = top;
        _vm.OverlayPositionX = freeX <= 0 ? 1 : (left - area.Left - Edge) / freeX;
        _vm.OverlayPosition = freeY <= 0 ? 0 : (top - area.Top) / freeY;
    }

    void Header_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _dragStart = null;
        Header.ReleaseMouseCapture();
    }

    void Root_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        _vm.OverlayScale += e.Delta > 0 ? 0.1 : -0.1;
        e.Handled = true;
    }


    void RegisterHotkeys()
    {
        _hotkeys!.Dispose();  // drop the previous profile's keys; the window hook stays
        var failed = new List<string>();
        if (!_hotkeys.Register(_vm.Settings.ClickThroughHotkey, () => SetClickThrough(!ClickThrough)))
            failed.Add(_vm.Settings.ClickThroughHotkey);
        if (!_hotkeys.Register(_vm.Settings.ToggleOverlayHotkey, ToggleVisible))
            failed.Add(_vm.Settings.ToggleOverlayHotkey);
        if (!_hotkeys.Register(_vm.Settings.FleaSearchHotkey, () => (_flea ??= new FleaSearchWindow(_vm)).Toggle()))
            failed.Add(_vm.Settings.FleaSearchHotkey);
        if (!_hotkeys.Register(_vm.Settings.RaidHotkey, ToggleMapPicker))
            failed.Add(_vm.Settings.RaidHotkey);
        if (!_hotkeys.Register(_vm.Settings.ScanHotkey, () => _ = _vm.Scanner.ReadOnceAsync()))
            failed.Add(_vm.Settings.ScanHotkey);
        if (failed.Count > 0) _vm.Toast = Loc.F("t.hotkeyFail", string.Join(", ", failed));
    }

    void OnProfileChanged()
    {
        RegisterHotkeys();
        ApplyCaptureExclusion();
        FitHeight();
        ApplyPosition();
        SetClickThrough(false);
        if (_vm.Settings.OverlayVisible) Show();
        else Hide();
        Peek();
    }

    public void SetClickThrough(bool on)
    {
        ClickThrough = on;
        Win32.SetClickThrough(_hwnd, on);
        _vm.OverlayClickThrough = on;
        Controls.Visibility = on ? Visibility.Collapsed : Visibility.Visible;
        UpdateLockText();
    }

    void UpdateLockText() =>
        LockText.Text = Loc.F(ClickThrough ? "ov.locked" : "ov.unlocked", _vm.Settings.ClickThroughHotkey);

    public void ToggleVisible()
    {
        if (_vm.OverlayAutoHide)
        {
            // the hotkey shows it like "O" does; hiding it stays at hand
            if (IsVisible)
            {
                _autoHideTimer.Stop();
                Hide();
            }
            else Peek();
            return;
        }
        if (IsVisible) Hide();
        else Show();
        _vm.Settings.OverlayVisible = IsVisible;
        _vm.SaveSettings();
    }

    void Hide_Click(object sender, RoutedEventArgs e) => ToggleVisible();

    /// <summary>The map window (overlay and main window buttons).</summary>
    void ToggleMap() => (_map ??= new MapWindow(_vm)).Toggle();

    // raid hotkey: a map list in the overlay; clicks must reach it, so it unlocks until a map is picked
    bool _lockedBeforePicker;

    void ToggleMapPicker()
    {
        if (MapPicker.Visibility == Visibility.Visible)
        {
            CloseMapPicker();
            return;
        }
        if (!IsVisible) ToggleVisible();
        _lockedBeforePicker = ClickThrough;
        SetClickThrough(false);
        MapPicker.Visibility = Visibility.Visible;
    }

    void CloseMapPicker()
    {
        MapPicker.Visibility = Visibility.Collapsed;
        SetClickThrough(_lockedBeforePicker);
    }

    void PickMap_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string map }) _vm.StartRaidCommand.Execute(map);
        CloseMapPicker();
    }
}
