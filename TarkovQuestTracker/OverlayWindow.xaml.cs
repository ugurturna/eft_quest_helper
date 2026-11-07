using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using TarkovQuestTracker.Interop;
using TarkovQuestTracker.ViewModels;

namespace TarkovQuestTracker;

/// <summary>Always-on-top tracker docked to the right edge of the screen; moves only up and down.</summary>
public partial class OverlayWindow : Window
{
    readonly MainViewModel _vm;
    readonly DispatcherTimer _topmostTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    HotkeyManager? _hotkeys;
    FleaSearchWindow? _flea;
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
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.OverlayScale)) FitHeight();
            if (e.PropertyName == nameof(MainViewModel.OverlayPosition) && _dragStart == null) ApplyPosition();
        };
    }

    public bool ClickThrough { get; private set; }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        Win32.MakeOverlay(_hwnd);
        FitHeight();
        ApplyPosition();

        _hotkeys = new HotkeyManager(_hwnd);
        RegisterHotkeys();
        _vm.ProfileChanged += OnProfileChanged;
        Loc.Changed += UpdateLockText;

        SetClickThrough(false);
        _topmostTimer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _topmostTimer.Stop();
        _hotkeys?.Dispose();
        _flea?.Close();
        base.OnClosed(e);
    }


    /// <summary>Right edge, at the saved vertical position (fraction of the free height).</summary>
    void ApplyPosition()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - ActualWidth - 6;
        Top = area.Top + Math.Clamp(_vm.OverlayPosition, 0, 1) * FreeHeight(area);
    }

    double FreeHeight(Rect area) => Math.Max(0, area.Height - ActualHeight);

    /// <summary>Keeps the scaled overlay inside the screen height.</summary>
    void FitHeight()
    {
        var area = SystemParameters.WorkArea;
        Scroller.MaxHeight = Math.Max(120, (area.Height - 40) / _vm.OverlayScale - 150);
    }

    // header drag: vertical only
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
        var top = Math.Clamp(Top + e.GetPosition(this).Y - start.Y, area.Top, area.Top + FreeHeight(area));
        Top = top;
        var free = FreeHeight(area);
        _vm.OverlayPosition = free <= 0 ? 0 : (top - area.Top) / free;
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
        if (failed.Count > 0) _vm.Toast = Loc.F("t.hotkeyFail", string.Join(", ", failed));
    }

    void OnProfileChanged()
    {
        RegisterHotkeys();
        FitHeight();
        ApplyPosition();
        SetClickThrough(false);
        if (_vm.Settings.OverlayVisible) Show();
        else Hide();
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
        if (IsVisible) Hide();
        else Show();
        _vm.Settings.OverlayVisible = IsVisible;
        _vm.SaveSettings();
    }

    void Hide_Click(object sender, RoutedEventArgs e) => ToggleVisible();
}
