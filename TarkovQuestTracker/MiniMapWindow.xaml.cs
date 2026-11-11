using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using TarkovQuestTracker.Interop;
using TarkovQuestTracker.ViewModels;

namespace TarkovQuestTracker;

/// <summary>
/// In-Game GPS "mini map" mode: a round map around the player in one of the screen's four corners. A new in-game
/// screenshot brings it up for a few seconds (only the position changes: it never takes the keyboard or the mouse).
/// </summary>
public partial class MiniMapWindow : Window
{
    /// <summary>Gap kept from the screen edges.</summary>
    const double Edge = 16;

    readonly MainViewModel _vm;
    readonly DispatcherTimer _topmostTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    readonly DispatcherTimer _hideTimer = new();
    IntPtr _hwnd;

    public MiniMapWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        _topmostTimer.Tick += (_, _) => { if (IsVisible) Win32.BringToTop(_hwnd); };
        _hideTimer.Tick += (_, _) => Dismiss();
        SizeChanged += (_, _) => ApplyPosition();
        Root.SizeChanged += (_, _) => PlaceNorth();
        Panel.RotationChanged += PlaceNorth;
        vm.PropertyChanged += OnVmChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        Win32.MakeOverlay(_hwnd);
        Win32.SetClickThrough(_hwnd, true);
        ApplyCaptureExclusion();
        ApplyPosition();
    }

    /// <summary>Up for the set time (again from the start when it is already up).</summary>
    public void Peek()
    {
        if (!IsVisible)
        {
            ApplyPosition();
            Show();
            Win32.BringToTop(_hwnd);
            _topmostTimer.Start();
        }
        _hideTimer.Stop();
        _hideTimer.Interval = TimeSpan.FromSeconds(_vm.MiniMapSeconds);
        _hideTimer.Start();
    }

    public void Dismiss()
    {
        _hideTimer.Stop();
        _topmostTimer.Stop();
        Hide();
    }

    protected override void OnClosed(EventArgs e)
    {
        _hideTimer.Stop();
        _topmostTimer.Stop();
        _vm.PropertyChanged -= OnVmChanged;
        Panel.Detach();
        base.OnClosed(e);
    }

    void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.MapShowInCapture)) ApplyCaptureExclusion();
        if (e.PropertyName == nameof(MainViewModel.MiniMapCorner)) ApplyPosition();
    }

    void ApplyCaptureExclusion()
    {
        if (_hwnd != IntPtr.Zero) Win32.ExcludeFromCapture(_hwnd, !_vm.MapShowInCapture);
    }

    /// <summary>In the picked corner of the main screen.</summary>
    void ApplyPosition()
    {
        var area = SystemParameters.WorkArea;
        var (w, h) = (ActualWidth > 0 ? ActualWidth : _vm.MiniMapSize, ActualHeight > 0 ? ActualHeight : _vm.MiniMapSize);
        var corner = _vm.MiniMapCorner;
        Left = corner is MiniMapCorners.TopRight or MiniMapCorners.BottomRight ? area.Right - Edge - w : area.Left + Edge;
        Top = corner is MiniMapCorners.BottomLeft or MiniMapCorners.BottomRight ? area.Bottom - Edge - h : area.Top + Edge;
    }

    /// <summary>The N on the ring, where north is on the (maybe turned) map.</summary>
    void PlaceNorth()
    {
        var r = Root.ActualWidth / 2;
        if (r <= 0) return;
        var angle = Panel.Rotation * Math.PI / 180;
        var ring = r - 9;
        Canvas.SetLeft(North, r + ring * Math.Sin(angle) - North.Width / 2);
        Canvas.SetTop(North, r - ring * Math.Cos(angle) - North.Height / 2);
    }
}
