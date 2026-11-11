using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using TarkovQuestTracker.Interop;
using TarkovQuestTracker.ViewModels;

namespace TarkovQuestTracker;

/// <summary>
/// Map window (In-Game GPS "overlay" mode; opens with a new in-game screenshot, or from the overlay / settings buttons):
/// the <see cref="MapPanel"/> over the game. Takes the keyboard while open; Esc (or a click on the game) hides it and
/// gives focus back to the game.
/// </summary>
public partial class MapWindow : Window
{
    readonly MainViewModel _vm;
    readonly DispatcherTimer _topmostTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    IntPtr _hwnd;
    /// <summary>Window that had focus before we opened (the game), to return to on Esc.</summary>
    IntPtr _previous;
    bool _hiding;

    public MapWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        PlaceFromSettings();
        _topmostTimer.Tick += (_, _) => { if (IsVisible) Win32.BringToTop(_hwnd); };
        Panel.Header.MouseLeftButtonDown += Header_MouseDown;
        Panel.CloseButton.Click += (_, _) => Dismiss(returnToGame: true);
        vm.PropertyChanged += OnVmChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        Win32.MakeToolWindow(_hwnd);
        ApplyCaptureExclusion();
    }

    public void Toggle()
    {
        if (IsVisible) Dismiss(returnToGame: true);
        else Open();
    }

    /// <summary>Brings the window up if it is hidden (a new screenshot); an open one just stays.</summary>
    public void ShowMap()
    {
        if (!IsVisible) Open();
    }

    /// <summary>
    /// Like the flea search: up with the keyboard, so Esc closes it at once and the game gets the keyboard back.
    /// </summary>
    void Open()
    {
        var foreground = Win32.GetForegroundWindow();
        _previous = foreground == _hwnd ? IntPtr.Zero : foreground;
        ApplyPosition();
        Show();  // the panel draws the map as it comes into view
        Activate();
        Win32.TakeForeground(_hwnd);
        Win32.BringToTop(_hwnd);
        Keyboard.Focus(Panel.Viewport);
        _topmostTimer.Start();
    }

    /// <summary>Hides the window (another GPS mode was picked, or Esc / a click on the game).</summary>
    public void Dismiss(bool returnToGame)
    {
        if (!IsVisible || _hiding) return;
        _hiding = true;
        SaveBounds();
        _topmostTimer.Stop();
        Hide();
        _hiding = false;
        if (returnToGame && _previous != IntPtr.Zero) Win32.SetForegroundWindow(_previous);
    }

    // clicking the game (or alt-tab) closes it too, like the flea search
    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        Dismiss(returnToGame: false);
    }

    void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        // Esc on an open list only closes the list
        if (Panel.MapBox.IsDropDownOpen || Panel.FloorBox.IsDropDownOpen) return;
        // and on an open quest card only the card
        if (!Panel.HideCard()) Dismiss(returnToGame: true);
        e.Handled = true;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        SaveBounds();
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _topmostTimer.Stop();
        _vm.PropertyChanged -= OnVmChanged;
        Panel.Detach();
        base.OnClosed(e);
    }

    void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.MapShowInCapture)) ApplyCaptureExclusion();
        if (e.PropertyName is nameof(MainViewModel.MapPositionX) or nameof(MainViewModel.MapPositionY) && !_dragging) ApplyPosition();
    }

    /// <summary>Shown on streams / recordings only when the player wants it (Settings); the player always sees it.</summary>
    void ApplyCaptureExclusion()
    {
        if (_hwnd != IntPtr.Zero) Win32.ExcludeFromCapture(_hwnd, !_vm.MapShowInCapture);
    }

    // where it sits: fractions of the main screen's free space (sliders on the GPS page), like the overlay;
    // dragged onto another monitor the fraction goes past 0–1 and the window stays there

    bool _dragging;

    void PlaceFromSettings()
    {
        var s = _vm.Settings;
        WindowStartupLocation = WindowStartupLocation.Manual;
        if (s.MapWindowWidth > 0) (Width, Height) = (Math.Max(MinWidth, s.MapWindowWidth), Math.Max(MinHeight, s.MapWindowHeight));
        ApplyPosition();
    }

    (double W, double H) Size => (ActualWidth > 0 ? ActualWidth : Width, ActualHeight > 0 ? ActualHeight : Height);

    void ApplyPosition()
    {
        var area = SystemParameters.WorkArea;
        var (w, h) = Size;
        var left = area.Left + _vm.Settings.MapPositionX * Math.Max(0, area.Width - w);
        var top = area.Top + _vm.Settings.MapPositionY * Math.Max(0, area.Height - h);
        var screens = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                               SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        if (!screens.IntersectsWith(new Rect(left, top, w, h)))
        {
            // its monitor is gone: back to the middle of the main screen
            left = area.Left + (area.Width - w) / 2;
            top = area.Top + (area.Height - h) / 2;
        }
        (Left, Top) = (left, top);
    }

    /// <summary>Where the window is now, as the fractions <see cref="ApplyPosition"/> reads (not clamped: another monitor).</summary>
    void SavePosition()
    {
        var area = SystemParameters.WorkArea;
        var (w, h) = Size;
        var freeX = area.Width - w;
        var freeY = area.Height - h;
        _vm.SetMapPosition(freeX > 0 ? (Left - area.Left) / freeX : 0.5, freeY > 0 ? (Top - area.Top) / freeY : 0.5, clamp: false);
    }

    void SaveBounds()
    {
        if (!IsVisible) return;
        (_vm.Settings.MapWindowWidth, _vm.Settings.MapWindowHeight) = (ActualWidth, ActualHeight);
        SavePosition();
        _vm.SaveSettings();
    }

    /// <summary>The top bar moves the window (not its lists, boxes and buttons, which take their own clicks).</summary>
    void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject d && FindParent<ComboBox>(d) != null) return;
        _dragging = true;
        try
        {
            DragMove();  // returns when the button is let go
        }
        catch (InvalidOperationException)
        {
            // the button was already up
        }
        finally
        {
            _dragging = false;
        }
        SavePosition();
    }

    static T? FindParent<T>(DependencyObject d) where T : DependencyObject
    {
        for (var x = d; x != null; x = x is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(x) : LogicalTreeHelper.GetParent(x))
            if (x is T t) return t;
        return null;
    }
}
