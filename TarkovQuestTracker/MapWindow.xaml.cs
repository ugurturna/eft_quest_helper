using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;
using TarkovQuestTracker.Interop;
using TarkovQuestTracker.ViewModels;

namespace TarkovQuestTracker;

/// <summary>
/// Map window (opens with a new in-game screenshot, or from the overlay / settings buttons): the raid's map with the player where the last in-game screenshot
/// was taken, the path of this raid's screenshots, exfils and the active quests' objective spots.
/// Wheel zooms, dragging pans (and stops following the player), the floor follows the player's height.
/// </summary>
public partial class MapWindow : Window
{
    static readonly Brush PmcBrush = Frozen(Color.FromRgb(0x6B, 0xD3, 0x6B));
    static readonly Brush ScavBrush = Frozen(Color.FromRgb(0xE8, 0xB3, 0x3A));
    static readonly Brush SharedBrush = Frozen(Color.FromRgb(0x5A, 0xB0, 0xF0));
    static readonly Brush TransitBrush = Frozen(Color.FromRgb(0xC7, 0x7D, 0xFF));
    static readonly Brush QuestBrush = Frozen(Color.FromRgb(0xFF, 0xD5, 0x4A));
    static readonly Brush PlayerBrush = Frozen(Color.FromRgb(0xFF, 0x45, 0x45));
    static readonly Brush TrailBrush = Frozen(Color.FromArgb(0xB0, 0xFF, 0x70, 0x70));
    static readonly Brush LabelBack = Frozen(Color.FromArgb(0xB0, 0x10, 0x12, 0x14));
    static readonly Geometry Arrow = Geometry.Parse("M 0,-12 L 8,9 L 0,4 L -8,9 Z");

    readonly MainViewModel _vm;
    readonly DispatcherTimer _topmostTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    /// <summary>Pictures already made, by map and floor (null = ground).</summary>
    readonly Dictionary<(string Map, string? Floor), IReadOnlyList<MapPicture>> _pictures = [];
    IntPtr _hwnd;
    string? _shownMap;
    MapLayer? _shownLayer;
    int _loadVersion;
    bool _fillingFloors;
    /// <summary>Screen pixels per map unit and where map (0, 0) is on screen.</summary>
    double _scale = 1, _fitScale = 1;
    Vector _offset;
    Point? _dragStart;
    bool _dragged;

    public MapWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        PlaceFromSettings();
        _topmostTimer.Tick += (_, _) => { if (IsVisible) Win32.BringToTop(_hwnd); };
        vm.MapChanged += OnMapChanged;
        vm.PropertyChanged += OnVmChanged;
        Loc.Changed += OnLocChanged;
    }

    static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    MapGeometry? Map => _vm.MapName is { } name && _vm.MapData.TryGetValue(name, out var map) ? map : null;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        Win32.MakeToolWindow(_hwnd);
        ApplyCaptureExclusion();
    }

    public void Toggle()
    {
        if (IsVisible)
        {
            SaveBounds();
            _topmostTimer.Stop();
            Hide();
            return;
        }
        Show();
        Win32.BringToTop(_hwnd);
        _topmostTimer.Start();
        if (_shownMap != _vm.MapName) LoadMap();
        else UpdateStatus();
    }

    /// <summary>Brings the window up if it is hidden (a new screenshot); an open one just stays.</summary>
    public void ShowMap()
    {
        if (!IsVisible) Toggle();
    }

    void Close_Click(object sender, RoutedEventArgs e) => Toggle();

    protected override void OnClosing(CancelEventArgs e)
    {
        SaveBounds();
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _topmostTimer.Stop();
        _vm.MapChanged -= OnMapChanged;
        _vm.PropertyChanged -= OnVmChanged;
        Loc.Changed -= OnLocChanged;
        base.OnClosed(e);
    }

    void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.MapName) && IsVisible) LoadMap();
        if (e.PropertyName == nameof(MainViewModel.ReadsScreen)) ApplyCaptureExclusion();
        if (e.PropertyName is nameof(MainViewModel.MapTracking) or nameof(MainViewModel.ScreenshotsFolder)) UpdateStatus();
    }

    void OnLocChanged()
    {
        FillFloors();
        Redraw();
    }

    /// <summary>Like the overlay: left out of screen pictures while the corner is read, so it never hides the exfil list.</summary>
    void ApplyCaptureExclusion()
    {
        if (_hwnd != IntPtr.Zero) Win32.ExcludeFromCapture(_hwnd, _vm.ReadsScreen);
    }

    // where it sits

    void PlaceFromSettings()
    {
        var s = _vm.Settings;
        var screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                              SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        var saved = new Rect(s.MapWindowLeft, s.MapWindowTop, Math.Max(MinWidth, s.MapWindowWidth), Math.Max(MinHeight, s.MapWindowHeight));
        if (s.MapWindowWidth > 0 && screen.IntersectsWith(saved))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            (Left, Top, Width, Height) = (saved.Left, saved.Top, saved.Width, saved.Height);
        }
        else WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    void SaveBounds()
    {
        if (!IsVisible) return;
        var s = _vm.Settings;
        (s.MapWindowLeft, s.MapWindowTop, s.MapWindowWidth, s.MapWindowHeight) = (Left, Top, ActualWidth, ActualHeight);
        _vm.SaveSettings();
    }

    void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject d && FindParent<ComboBox>(d) != null) return;
        DragMove();
    }

    static T? FindParent<T>(DependencyObject d) where T : DependencyObject
    {
        for (var x = d; x != null; x = x is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(x) : LogicalTreeHelper.GetParent(x))
            if (x is T t) return t;
        return null;
    }

    // map and floor

    void LoadMap()
    {
        _shownMap = _vm.MapName;
        FillFloors();
        _ = LoadPicturesAsync(fit: true);
    }

    /// <summary>Floor list: automatic (by the player's height), ground floor, then the map's floors.</summary>
    void FillFloors()
    {
        _fillingFloors = true;
        var selected = FloorBox.SelectedIndex;
        FloorBox.Items.Clear();
        var map = Map;
        FloorBox.Items.Add(new ComboBoxItem { Content = Loc.T("s.map.floorAuto") });
        FloorBox.Items.Add(new ComboBoxItem { Content = Loc.T("s.map.floorGround") });
        foreach (var layer in map?.Layers ?? []) FloorBox.Items.Add(new ComboBoxItem { Content = layer.Name, Tag = layer });
        FloorBox.SelectedIndex = map != null && _shownMap == map.Name && selected >= 0 && selected < FloorBox.Items.Count ? selected : 0;
        FloorBox.Visibility = map?.Layers.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        _fillingFloors = false;
    }

    void FloorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_fillingFloors || Map == null) return;
        if (!Equals(WantedLayer(), _shownLayer)) _ = LoadPicturesAsync(fit: false);
        UpdateStatus();
    }

    bool AutoFloor => FloorBox.SelectedIndex <= 0;

    /// <summary>The floor to draw: picked by hand, or (automatic) the one the player is on.</summary>
    MapLayer? WantedLayer()
    {
        if (!AutoFloor) return (FloorBox.SelectedItem as ComboBoxItem)?.Tag as MapLayer;
        return Map is { } map && _vm.PlayerPosition is { } p ? MapProjection.LayerFor(map, p.X, p.Y, p.Z) : null;
    }

    async Task LoadPicturesAsync(bool fit)
    {
        var version = ++_loadVersion;
        var map = Map;
        Pictures.Children.Clear();
        if (map == null)
        {
            _shownLayer = null;
            Message.Text = Loc.T("s.map.pickMap");
            Redraw();
            UpdateStatus();
            return;
        }
        var layer = WantedLayer();
        _shownLayer = layer;
        var key = (map.Name, layer?.Name);
        if (!_pictures.TryGetValue(key, out var pictures))
        {
            Message.Text = Loc.T("s.map.loading");
            try
            {
                pictures = await MapPictures.LoadAsync(map, layer, _vm.MapCacheFolder);
                _pictures[key] = pictures;
            }
            catch (Exception ex) when (MapPictures.IsDownloadError(ex))
            {
                if (version == _loadVersion) Message.Text = Loc.F("s.map.error", ex.Message);
                return;
            }
        }
        if (version != _loadVersion) return;  // another map / floor was asked for meanwhile
        Message.Text = "";
        foreach (var picture in pictures)
        {
            var image = new Image { Source = picture.Source, Width = picture.Rect.Width, Height = picture.Rect.Height,
                                    Opacity = picture.Opacity, Stretch = Stretch.Fill };
            Canvas.SetLeft(image, picture.Rect.Left);
            Canvas.SetTop(image, picture.Rect.Top);
            Pictures.Children.Add(image);
        }
        if (fit)
        {
            Fit(map);
            if (Follow.IsChecked == true) CenterOnPlayer();
        }
        ApplyView();
        UpdateStatus();
    }

    void OnMapChanged()
    {
        if (!IsVisible) return;
        if (_shownMap != _vm.MapName)
        {
            LoadMap();
            return;
        }
        if (AutoFloor && !Equals(WantedLayer(), _shownLayer)) _ = LoadPicturesAsync(fit: false);
        if (Follow.IsChecked == true) CenterOnPlayer();
        ApplyView();
        UpdateStatus();
    }

    // view: zoom and pan

    Rect MapRect(MapGeometry map)
    {
        var (left, top, right, bottom) = MapProjection.Rect(map, map.SvgBounds ?? map.Bounds);
        return new Rect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
    }

    void Fit(MapGeometry map)
    {
        var rect = MapRect(map);
        var (w, h) = (Math.Max(50, Viewport.ActualWidth), Math.Max(50, Viewport.ActualHeight));
        _fitScale = _scale = Math.Min(w / rect.Width, h / rect.Height) * 0.95;
        _offset = new Vector(w / 2 - (rect.Left + rect.Width / 2) * _scale, h / 2 - (rect.Top + rect.Height / 2) * _scale);
    }

    void CenterOnPlayer()
    {
        if (Map is not { } map || _vm.PlayerPosition is not { } p) return;
        if (_scale < _fitScale * 2) _scale = _fitScale * 2;  // close enough to see the street, not the whole map
        var at = MapProjection.Project(map, p.X, p.Z);
        _offset = new Vector(Viewport.ActualWidth / 2 - at.X * _scale, Viewport.ActualHeight / 2 - at.Y * _scale);
    }

    void Follow_Checked(object sender, RoutedEventArgs e)
    {
        // IsChecked="True" in the XAML raises this while InitializeComponent is still building the window (no View yet)
        if (!IsLoaded) return;
        CenterOnPlayer();
        ApplyView();
    }

    void ApplyView()
    {
        View.Matrix = new Matrix(_scale, 0, 0, _scale, _offset.X, _offset.Y);
        Redraw();
    }

    Point Screen(MapPoint p) => new(p.X * _scale + _offset.X, p.Y * _scale + _offset.Y);

    void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var mouse = e.GetPosition(Viewport);
        var at = new Point((mouse.X - _offset.X) / _scale, (mouse.Y - _offset.Y) / _scale);
        _scale = Math.Clamp(_scale * Math.Pow(1.25, e.Delta / 120.0), _fitScale * 0.5, _fitScale * 40);
        _offset = new Vector(mouse.X - at.X * _scale, mouse.Y - at.Y * _scale);
        ApplyView();
        e.Handled = true;
    }

    void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(Viewport);
        _dragged = false;
        Viewport.CaptureMouse();
    }

    void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStart is not { } start || e.LeftButton != MouseButtonState.Pressed) return;
        var now = e.GetPosition(Viewport);
        var delta = now - start;
        if (!_dragged && delta.Length < 3) return;
        if (!_dragged) Follow.IsChecked = false;  // the player looks around: stop pulling the view back
        _dragged = true;
        _offset += delta;
        _dragStart = now;
        ApplyView();
    }

    void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _dragStart = null;
        Viewport.ReleaseMouseCapture();
    }

    void Viewport_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.PreviousSize.Width <= 0 && Map is { } map)
        {
            Fit(map);
            if (Follow.IsChecked == true) CenterOnPlayer();
        }
        else _offset += new Vector((e.NewSize.Width - e.PreviousSize.Width) / 2, (e.NewSize.Height - e.PreviousSize.Height) / 2);
        ApplyView();
    }

    // markers (screen sized: they don't grow with the zoom)

    void Redraw()
    {
        Markers.Children.Clear();
        if (Map is not { } map || Pictures.Children.Count == 0) return;
        if (_vm.MapShowExtracts)
        {
            foreach (var e in map.Extracts)
                AddPoint(map, e.Position, FactionBrush(e.Faction), e.Name, $"{e.Name} · {Loc.T("s.map.faction." + e.Faction)}", diamond: false);
            foreach (var t in map.Transits)
                AddPoint(map, t.Position, TransitBrush, t.Name, t.Name, diamond: false);
        }
        if (_vm.MapShowQuests)
            foreach (var spot in _vm.QuestSpots(map))
                foreach (var position in spot.Positions)
                    AddPoint(map, position, QuestBrush, null, $"{spot.Quest}\n{spot.Objective}", diamond: true);
        DrawTrail(map);
        DrawPlayer(map);
    }

    static Brush FactionBrush(string faction) => faction switch
    {
        "pmc" => PmcBrush,
        "scav" => ScavBrush,
        _ => SharedBrush,
    };

    void AddPoint(MapGeometry map, double[] position, Brush fill, string? label, string tip, bool diamond)
    {
        var at = Screen(MapProjection.Project(map, position));
        if (at.X < -50 || at.Y < -50 || at.X > Viewport.ActualWidth + 50 || at.Y > Viewport.ActualHeight + 50) return;
        const double size = 10;
        Shape dot = diamond
            ? new Rectangle { Width = size, Height = size, RenderTransform = new RotateTransform(45, size / 2, size / 2) }
            : new Ellipse { Width = size, Height = size };
        dot.Fill = fill;
        dot.Stroke = Brushes.Black;
        dot.StrokeThickness = 1;
        dot.ToolTip = tip;
        dot.Cursor = Cursors.Hand;
        Canvas.SetLeft(dot, at.X - size / 2);
        Canvas.SetTop(dot, at.Y - size / 2);
        Markers.Children.Add(dot);
        if (label == null) return;
        var text = new Border
        {
            Background = LabelBack, CornerRadius = new CornerRadius(2), Padding = new Thickness(3, 0, 3, 1), IsHitTestVisible = false,
            Child = new TextBlock { Text = label, FontSize = 10.5, Foreground = fill },
        };
        Canvas.SetLeft(text, at.X + size / 2 + 2);
        Canvas.SetTop(text, at.Y - 8);
        Markers.Children.Add(text);
    }

    void DrawTrail(MapGeometry map)
    {
        var trail = _vm.PlayerTrail;
        if (trail.Count < 2) return;
        var line = new Polyline { Stroke = TrailBrush, StrokeThickness = 2, StrokeDashArray = [3, 2], IsHitTestVisible = false };
        foreach (var p in trail) line.Points.Add(Screen(MapProjection.Project(map, p.X, p.Z)));
        Markers.Children.Add(line);
        foreach (var p in trail.Take(trail.Count - 1))
        {
            var at = Screen(MapProjection.Project(map, p.X, p.Z));
            var dot = new Ellipse { Width = 6, Height = 6, Fill = TrailBrush,
                                    ToolTip = p.Taken.ToString("HH:mm", CultureInfo.InvariantCulture) };
            Canvas.SetLeft(dot, at.X - 3);
            Canvas.SetTop(dot, at.Y - 3);
            Markers.Children.Add(dot);
        }
    }

    void DrawPlayer(MapGeometry map)
    {
        if (_vm.PlayerPosition is not { } p) return;
        var at = Screen(MapProjection.Project(map, p.X, p.Z));
        var ring = new Ellipse { Width = 30, Height = 30, Stroke = PlayerBrush, StrokeThickness = 1.5, Opacity = 0.6, IsHitTestVisible = false };
        Canvas.SetLeft(ring, at.X - 15);
        Canvas.SetTop(ring, at.Y - 15);
        Markers.Children.Add(ring);
        var arrow = new Path
        {
            Data = Arrow, Fill = PlayerBrush, Stroke = Brushes.White, StrokeThickness = 1.5,
            RenderTransform = new RotateTransform(MapProjection.Heading(map, p.Yaw)),
            ToolTip = Loc.T("s.map.you"),
        };
        Canvas.SetLeft(arrow, at.X);
        Canvas.SetTop(arrow, at.Y);
        Markers.Children.Add(arrow);
    }

    void UpdateStatus()
    {
        var p = _vm.PlayerPosition;
        Status.Text = !_vm.MapTracking ? Loc.T("s.map.off")
            : !_vm.Screenshots.FolderExists ? Loc.F("s.map.noFolder", _vm.Screenshots.Folder)
            : p == null ? Loc.T("s.map.hint")
            : Loc.F("s.map.pos", p.Taken.ToString("HH:mm", CultureInfo.InvariantCulture),
                    p.X.ToString("0", CultureInfo.InvariantCulture), p.Z.ToString("0", CultureInfo.InvariantCulture),
                    p.Y.ToString("0.0", CultureInfo.InvariantCulture), _shownLayer?.Name ?? Loc.T("s.map.floorGround"));
        if (AutoFloor && FloorBox.Items.Count > 0 && FloorBox.Items[0] is ComboBoxItem auto)
            auto.Content = _shownLayer == null ? Loc.T("s.map.floorAuto") : $"{Loc.T("s.map.floorAuto")} · {_shownLayer.Name}";
    }
}
