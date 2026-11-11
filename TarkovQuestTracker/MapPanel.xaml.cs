using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;
using TarkovQuestTracker.ViewModels;

namespace TarkovQuestTracker;

/// <summary>Where a <see cref="MapPanel"/> is shown.</summary>
public enum MapPanelKind
{
    /// <summary>The map window over the game: header with its own close button, dragged by the top bar.</summary>
    Window,
    /// <summary>The app's map page (In-Game GPS → Map), always on, for a second monitor.</summary>
    App,
    /// <summary>The round mini map in a screen corner: no header, always on the player, at a set zoom, maybe turned with the player.</summary>
    Mini,
}

/// <summary>
/// The raid's map with the player where the last in-game screenshot was taken, the path of this raid's screenshots,
/// exfils, the BTR and the active quests' objective spots. Wheel zooms, dragging pans (and stops following the player),
/// the floor follows the player's height. The map window, the app's map page and the mini map all use it.
/// </summary>
public partial class MapPanel : UserControl
{
    static readonly Brush PmcBrush = Frozen(Color.FromRgb(0x6B, 0xD3, 0x6B));
    static readonly Brush ScavBrush = Frozen(Color.FromRgb(0xE8, 0xB3, 0x3A));
    static readonly Brush SharedBrush = Frozen(Color.FromRgb(0x5A, 0xB0, 0xF0));
    static readonly Brush TransitBrush = Frozen(Color.FromRgb(0xC7, 0x7D, 0xFF));
    static readonly Brush QuestBrush = Frozen(Color.FromRgb(0xFF, 0xD5, 0x4A));
    static readonly Brush PlayerBrush = Frozen(Color.FromRgb(0xFF, 0x45, 0x45));
    static readonly Brush TrailBrush = Frozen(Color.FromArgb(0xB0, 0xFF, 0x70, 0x70));
    static readonly Brush LabelBack = Frozen(Color.FromArgb(0xB0, 0x10, 0x12, 0x14));
    static readonly Brush BtrBrush = Frozen(Color.FromRgb(0x7F, 0xD1, 0xFF));
    static readonly Brush BtrRouteBrush = Frozen(Color.FromArgb(0x70, 0x7F, 0xD1, 0xFF));
    /// <summary>TARKOV_TRACKER_BTR_EDIT=1: a click on the map copies the game [x, z] there (to draw BTR stops and roads in data/btr.json).</summary>
    static readonly bool BtrEdit = Environment.GetEnvironmentVariable("TARKOV_TRACKER_BTR_EDIT") == "1";
    static readonly Geometry Arrow = Geometry.Parse("M 0,-12 L 8,9 L 0,4 L -8,9 Z");

    MainViewModel? _vm;
    MapPanelKind _kind = MapPanelKind.Window;
    string? _shownMap;
    MapLayer? _shownLayer;
    int _loadVersion;
    bool _fillingFloors;
    /// <summary>Screen pixels per map unit and where map (0, 0) is on screen; the mini map may also turn the view (degrees).</summary>
    double _scale = 1, _fitScale = 1, _rotation;
    Vector _offset;
    Matrix _view = Matrix.Identity;
    Point? _dragStart;
    bool _dragged;
    /// <summary>The quest marker under the mouse when the button went down; a click (no drag) on it opens its card.</summary>
    QuestMarker? _pressed;
    /// <summary>The quest marker whose card is open.</summary>
    QuestMarker? _card;

    /// <summary>One drawn quest marker: which objective of which quest, and which of its spots.</summary>
    sealed record QuestMarker(string QuestId, string Text, double[] Position);

    /// <summary>The view was turned (mini map following the player's heading): the mini map's N moves with it.</summary>
    public event Action? RotationChanged;

    public MapPanel()
    {
        InitializeComponent();
        DataContextChanged += (_, e) => Attach(e.NewValue as MainViewModel);
        IsVisibleChanged += (_, _) => { if (IsVisible) Refresh(); };
    }

    static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public MapPanelKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            var mini = value == MapPanelKind.Mini;
            var window = value == MapPanelKind.Window;
            Header.Visibility = Footer.Visibility = mini ? Visibility.Collapsed : Visibility.Visible;
            WindowButtons.Visibility = OpacitySlider.Visibility = window ? Visibility.Visible : Visibility.Collapsed;
            GripDots.Text = window ? "⠿ " : "";
            Grip.Cursor = window ? Cursors.SizeAll : null;
            if (window) Grip.SetResourceReference(ToolTipProperty, "s.map.drag");
            else Grip.ClearValue(ToolTipProperty);
            // the mini map is looked at, never clicked: the game gets the mouse
            IsHitTestVisible = !mini;
            Viewport.Margin = mini ? new Thickness(0) : new Thickness(1, 0, 1, 0);
        }
    }

    bool IsMini => _kind == MapPanelKind.Mini;

    /// <summary>Degrees the mini map is turned (0 = north up).</summary>
    public double Rotation => _rotation;

    MapGeometry? Map => _vm?.MapName is { } name && _vm.MapData.TryGetValue(name, out var map) ? map : null;

    void Attach(MainViewModel? vm)
    {
        Detach();
        _vm = vm;
        if (vm == null) return;
        vm.MapChanged += OnMapChanged;
        vm.BtrChanged += OnBtrChanged;
        vm.PropertyChanged += OnVmChanged;
        Loc.Changed += OnLocChanged;
    }

    /// <summary>Stops listening to the view model (its window is closing).</summary>
    public void Detach()
    {
        if (_vm == null) return;
        _vm.MapChanged -= OnMapChanged;
        _vm.BtrChanged -= OnBtrChanged;
        _vm.PropertyChanged -= OnVmChanged;
        Loc.Changed -= OnLocChanged;
        _vm = null;
    }

    /// <summary>The panel came into view: the map, the player and the status as they are now (nothing is drawn while hidden).</summary>
    public void Refresh()
    {
        if (_vm == null) return;
        if (_shownMap != _vm.MapName) LoadMap();
        else OnMapChanged();
    }

    void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.MapName) && IsVisible) LoadMap();
        if (e.PropertyName is nameof(MainViewModel.MapTracking) or nameof(MainViewModel.ScreenshotsFolder)) UpdateStatus();
        if (IsMini && e.PropertyName is nameof(MainViewModel.MiniMapZoom) or nameof(MainViewModel.MiniMapRotate) && IsVisible)
        {
            CenterOnPlayer();
            ApplyView();
        }
    }

    void OnLocChanged()
    {
        FillFloors();
        Redraw();
    }

    // map and floor

    void LoadMap()
    {
        _shownMap = _vm?.MapName;
        HideCard();
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
        return Map is { } map && _vm?.PlayerPosition is { } p ? MapProjection.LayerFor(map, p.X, p.Y, p.Z) : null;
    }

    async Task LoadPicturesAsync(bool fit)
    {
        var version = ++_loadVersion;
        var map = Map;
        Pictures.Children.Clear();
        if (map == null || _vm == null)
        {
            _shownLayer = null;
            // the mini map has no map list: the raid's map comes from the raid hotkey or the double O
            Message.Text = Loc.T(IsMini ? "s.map.pickMapMini" : "s.map.pickMap");
            Redraw();
            UpdateStatus();
            return;
        }
        var layer = WantedLayer();
        _shownLayer = layer;
        // made once for the whole app; only a map not made yet shows the "preparing" line
        var loading = MapPictures.LoadAsync(map, layer, _vm.MapCacheFolder);
        if (!loading.IsCompleted) Message.Text = Loc.T("s.map.loading");
        IReadOnlyList<MapPicture> pictures;
        try
        {
            pictures = await loading;
        }
        catch (Exception ex) when (MapPictures.IsDownloadError(ex))
        {
            if (version == _loadVersion) Message.Text = Loc.F("s.map.error", ex.Message);
            return;
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
            if (Following) CenterOnPlayer();
        }
        ApplyView();
        UpdateStatus();
    }

    void OnMapChanged()
    {
        if (!IsVisible || _vm == null) return;
        if (_shownMap != _vm.MapName)
        {
            LoadMap();
            return;
        }
        if (AutoFloor && !Equals(WantedLayer(), _shownLayer)) _ = LoadPicturesAsync(fit: false);
        if (Following) CenterOnPlayer();
        ApplyView();
        UpdateStatus();
    }

    /// <summary>The BTR moved on (every raid clock second) or its settings changed: only the markers are drawn again.</summary>
    void OnBtrChanged()
    {
        if (IsVisible) Redraw();
    }

    // view: zoom and pan

    /// <summary>The mini map always stays on the player.</summary>
    bool Following => IsMini || Follow.IsChecked == true;

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
        _rotation = 0;
    }

    void CenterOnPlayer()
    {
        if (Map is not { } map || _vm?.PlayerPosition is not { } p) return;
        if (IsMini)
        {
            // the set zoom, turned so the player looks up when asked to
            _scale = _fitScale * _vm.MiniMapZoom;
            var rotation = _vm.MiniMapRotate ? -MapProjection.Heading(map, p.Yaw) : 0;
            if (rotation != _rotation)
            {
                _rotation = rotation;
                RotationChanged?.Invoke();
            }
        }
        else if (_scale < _fitScale * 2) _scale = _fitScale * 2;  // close enough to see the street, not the whole map
        var at = MapProjection.Project(map, p.X, p.Z);
        _offset = new Vector(Viewport.ActualWidth / 2 - at.X * _scale, Viewport.ActualHeight / 2 - at.Y * _scale);
    }

    void Follow_Checked(object sender, RoutedEventArgs e)
    {
        // IsChecked="True" in the XAML raises this while InitializeComponent is still building the panel (no View yet)
        if (!IsLoaded) return;
        CenterOnPlayer();
        ApplyView();
    }

    void ApplyView()
    {
        _view = new Matrix(_scale, 0, 0, _scale, _offset.X, _offset.Y);
        if (_rotation != 0) _view.RotateAt(_rotation, Viewport.ActualWidth / 2, Viewport.ActualHeight / 2);
        View.Matrix = _view;
        Redraw();
    }

    Point Screen(MapPoint p) => _view.Transform(new Point(p.X, p.Y));

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
        _pressed = (e.OriginalSource as FrameworkElement)?.Tag as QuestMarker;
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
        // a click (not a drag): on a quest marker opens its card, anywhere else closes it
        if (!_dragged)
        {
            if (_pressed != null) ShowCard(_pressed);
            else HideCard();
            if (BtrEdit && _pressed == null) CopySpot(e.GetPosition(Viewport));
        }
        _pressed = null;
    }

    /// <summary>BTR data editing: the clicked spot's game [x, z] to the clipboard and the status line.</summary>
    void CopySpot(Point screen)
    {
        if (Map is not { } map) return;
        var (x, z) = MapProjection.Unproject(map, new MapPoint((screen.X - _offset.X) / _scale, (screen.Y - _offset.Y) / _scale));
        var text = string.Create(CultureInfo.InvariantCulture, $"[{x:0}, {z:0}]");
        try { Clipboard.SetText(text); } catch (System.Runtime.InteropServices.ExternalException) { }  // clipboard busy: the status line still shows it
        Status.Text = "BTR edit: " + text;
    }

    void Viewport_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsMini) Viewport.Clip = new EllipseGeometry(new Rect(e.NewSize));
        if ((e.PreviousSize.Width <= 0 || IsMini) && Map is { } map)
        {
            Fit(map);
            if (Following) CenterOnPlayer();
        }
        else _offset += new Vector((e.NewSize.Width - e.PreviousSize.Width) / 2, (e.NewSize.Height - e.PreviousSize.Height) / 2);
        ApplyView();
    }

    // markers (screen sized: they don't grow with the zoom)

    void Redraw()
    {
        Markers.Children.Clear();
        if (Map is not { } map || Pictures.Children.Count == 0 || _vm == null)
        {
            UpdateCard(null, []);
            return;
        }
        if (_vm.MapShowExtracts)
        {
            // tarkov.dev lists an exfil both PMCs and Scavs use once per side: one point, shared
            foreach (var g in map.Extracts.GroupBy(e => e.Name))
            {
                var e = g.First();
                var faction = g.Select(x => x.Faction).Distinct().Count() > 1 ? "shared" : e.Faction;
                AddPoint(map, e.Position, FactionBrush(faction), e.Name, $"{e.Name} · {Loc.T("s.map.faction." + faction)}", diamond: false);
            }
            foreach (var t in map.Transits)
                AddPoint(map, t.Position, TransitBrush, t.Name, t.Name, diamond: false);
        }
        var spots = _vm.MapShowQuests ? _vm.QuestSpots(map) : [];
        foreach (var spot in spots)
            foreach (var position in spot.Positions)
                AddPoint(map, position, QuestBrush, null, $"{spot.Quest.Quest.DisplayName}\n{spot.Text}", diamond: true,
                         new QuestMarker(spot.Quest.Quest.Id, spot.Text, position));
        DrawBtr(map);
        DrawTrail(map);
        DrawPlayer(map);
        if (IsMini) DrawQuestPointers(map, spots);
        UpdateCard(map, spots);
    }

    static readonly Geometry Pointer = Geometry.Parse("M 0,-7 L 6,5 L -6,5 Z");

    /// <summary>
    /// Mini map: a quest spot outside the circle gets a pointer on the ring, the way to go, with how far it is
    /// (the objective's nearest spot; game units are metres).
    /// </summary>
    void DrawQuestPointers(MapGeometry map, IReadOnlyList<MapQuestSpot> spots)
    {
        if (_vm!.PlayerPosition is not { } me) return;
        var center = new Point(Viewport.ActualWidth / 2, Viewport.ActualHeight / 2);
        var radius = Math.Min(Viewport.ActualWidth, Viewport.ActualHeight) / 2;
        if (radius < 40) return;
        foreach (var spot in spots)
        {
            var nearest = spot.Positions.MinBy(p => Distance(me, p));
            if (nearest == null) continue;
            var along = Screen(MapProjection.Project(map, nearest)) - center;
            if (along.Length <= radius - 8) continue;  // on the mini map already
            along.Normalize();
            var tip = center + along * (radius - 8);
            var angle = Math.Atan2(along.X, -along.Y) * 180 / Math.PI;
            var arrow = new Path
            {
                Data = Pointer, Fill = QuestBrush, Stroke = Brushes.Black, StrokeThickness = 1,
                RenderTransform = new RotateTransform(angle), IsHitTestVisible = false,
            };
            Canvas.SetLeft(arrow, tip.X);
            Canvas.SetTop(arrow, tip.Y);
            Markers.Children.Add(arrow);
            var label = new Border
            {
                Background = LabelBack, CornerRadius = new CornerRadius(2), Padding = new Thickness(3, 0, 3, 1), IsHitTestVisible = false,
                Child = new TextBlock { Text = $"{Distance(me, nearest):0} m", FontSize = 9.5, Foreground = QuestBrush },
            };
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var at = center + along * (radius - 24);
            Canvas.SetLeft(label, at.X - label.DesiredSize.Width / 2);
            Canvas.SetTop(label, at.Y - label.DesiredSize.Height / 2);
            Markers.Children.Add(label);
        }
    }

    static double Distance(ScreenshotPosition me, double[] spot) => Math.Sqrt(Math.Pow(spot[0] - me.X, 2) + Math.Pow(spot[2] - me.Z, 2));

    /// <summary>
    /// The raid's BTR on its map: the routes it may still be on (faint), each stop with the soonest ETA, and where it should
    /// be now (one marker per possible route, named when there are several).
    /// </summary>
    void DrawBtr(MapGeometry map)
    {
        if (_vm is not { BtrShowOnMap: true, Btr: { } btr } || btr.Map.Name != map.Name) return;
        var timeLeft = _vm.BtrTimeLeft;
        foreach (var route in btr.Candidates)
        {
            var line = new Polyline { Stroke = BtrRouteBrush, StrokeThickness = 3, IsHitTestVisible = false };
            foreach (var p in route.Track) line.Points.Add(Screen(MapProjection.Project(map, p.X, p.Z)));
            Markers.Children.Add(line);
        }
        var etas = timeLeft is { } t ? btr.Etas(t).ToDictionary(e => e.StopId) : [];
        foreach (var (id, stop) in btr.Map.Stops)
        {
            var eta = etas.GetValueOrDefault(id);
            var when = eta == null ? "" : eta.IsHere ? Loc.T("btr.here") : eta.Seconds is { } s ? MainViewModel.Clock(s) : "—";
            var label = when.Length > 0 ? $"{stop.Name} · {when}" : stop.Name;
            AddPoint(map, [stop.Position[0], 0, stop.Position[1]], BtrBrush, label, $"BTR · {label}", diamond: true);
        }
        if (timeLeft is not { } now) return;
        var several = btr.Candidates.Count > 1;
        foreach (var route in btr.Candidates)
        {
            var fix = route.PositionAt(now);
            if (fix.State == BtrState.Done) continue;
            var at = Screen(MapProjection.Project(map, fix.X, fix.Z));
            var marker = new Border
            {
                Background = BtrBrush, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 0, 4, 1), Opacity = several || fix.State == BtrState.NotSpawned ? 0.7 : 1,
                Child = new TextBlock { Text = several ? $"BTR {route.Route.Id}" : "BTR", FontSize = 10.5, FontWeight = FontWeights.Bold, Foreground = Brushes.Black },
                ToolTip = _vm.BtrStatus,
            };
            marker.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(marker, at.X - marker.DesiredSize.Width / 2);
            Canvas.SetTop(marker, at.Y - marker.DesiredSize.Height / 2);
            Markers.Children.Add(marker);
        }
    }

    static Brush FactionBrush(string faction) => faction switch
    {
        "pmc" => PmcBrush,
        "scav" => ScavBrush,
        _ => SharedBrush,
    };

    void AddPoint(MapGeometry map, double[] position, Brush fill, string? label, string tip, bool diamond, QuestMarker? quest = null)
    {
        var at = Screen(MapProjection.Project(map, position));
        if (at.X < -50 || at.Y < -50 || at.X > Viewport.ActualWidth + 50 || at.Y > Viewport.ActualHeight + 50) return;
        var size = IsMini ? 8.0 : 10.0;
        Shape dot = diamond
            ? new Rectangle { Width = size, Height = size, RenderTransform = new RotateTransform(45, size / 2, size / 2) }
            : new Ellipse { Width = size, Height = size };
        dot.Fill = fill;
        dot.Stroke = Brushes.Black;
        dot.StrokeThickness = 1;
        dot.ToolTip = tip;
        dot.Tag = quest;
        dot.Cursor = Cursors.Hand;
        Canvas.SetLeft(dot, at.X - size / 2);
        Canvas.SetTop(dot, at.Y - size / 2);
        Markers.Children.Add(dot);
        if (label == null) return;
        var text = new Border
        {
            Background = LabelBack, CornerRadius = new CornerRadius(2), Padding = new Thickness(3, 0, 3, 1), IsHitTestVisible = false,
            Child = new TextBlock { Text = label, FontSize = IsMini ? 9.5 : 10.5, Foreground = fill },
        };
        Canvas.SetLeft(text, at.X + size / 2 + 2);
        Canvas.SetTop(text, at.Y - 8);
        Markers.Children.Add(text);
    }

    void DrawTrail(MapGeometry map)
    {
        var trail = _vm!.PlayerTrail;
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
        if (_vm!.PlayerPosition is not { } p) return;
        var at = Screen(MapProjection.Project(map, p.X, p.Z));
        var ring = new Ellipse { Width = 30, Height = 30, Stroke = PlayerBrush, StrokeThickness = 1.5, Opacity = 0.6, IsHitTestVisible = false };
        Canvas.SetLeft(ring, at.X - 15);
        Canvas.SetTop(ring, at.Y - 15);
        Markers.Children.Add(ring);
        var arrow = new Path
        {
            Data = Arrow, Fill = PlayerBrush, Stroke = Brushes.White, StrokeThickness = 1.5,
            // a turned view turns the arrow with it (turned to the player's heading: straight up)
            RenderTransform = new RotateTransform(MapProjection.Heading(map, p.Yaw) + _rotation),
            ToolTip = Loc.T("s.map.you"),
        };
        Canvas.SetLeft(arrow, at.X);
        Canvas.SetTop(arrow, at.Y);
        Markers.Children.Add(arrow);
    }

    // quest card: a click on a quest marker shows the quest's name and objectives next to it

    void ShowCard(QuestMarker marker)
    {
        _card = marker;
        Redraw();
    }

    /// <summary>Closes the open quest card; false when none was open.</summary>
    public bool HideCard()
    {
        if (_card == null && QuestCard.Visibility != Visibility.Visible) return false;
        _card = null;
        QuestCard.Visibility = Visibility.Collapsed;
        return true;
    }

    /// <summary>Fills and places the open card; closes it when its spot is gone (objective done, quests hidden, other map).</summary>
    void UpdateCard(MapGeometry? map, IReadOnlyList<MapQuestSpot> spots)
    {
        if (_card is not { } marker) return;
        var spot = spots.FirstOrDefault(s => s.Quest.Quest.Id == marker.QuestId && s.Text == marker.Text);
        if (map == null || spot == null)
        {
            HideCard();
            return;
        }
        var quest = spot.Quest.Quest;
        QuestCard.Tag = quest;
        CardTitle.Text = quest.DisplayName;
        CardMeta.Text = string.Join(" · ", new[] { quest.Trader, quest.LevelText, quest.LoyaltyText, spot.Quest.Progress }
                                               .Where(s => !string.IsNullOrEmpty(s)));
        CardBody.Children.Clear();
        AddCardLine("◆ " + spot.Text + (spot.Objective is { IsCounter: true } o ? $" ({o.CounterText})" : ""),
                    QuestBrush, bold: true);
        foreach (var detail in spot.Objective?.Objective.DisplayDetails ?? [])
            AddCardLine("• " + detail, (Brush)FindResource("TextBrush"), indent: 10);
        var others = spot.Quest.Objectives.Where(o => o != spot.Objective).ToList();
        if (others.Count > 0)
        {
            AddCardLine(Loc.T("s.map.card.others"), (Brush)FindResource("MutedBrush"), top: 6);
            foreach (var other in others)
                AddCardLine((other.IsDone ? "✓ " : "○ ") + other.Text + (other.IsCounter ? $" ({other.CounterText})" : ""),
                            (Brush)FindResource(other.IsDone ? "DoneBrush" : "TextBrush"));
        }

        QuestCard.Visibility = Visibility.Visible;
        QuestCard.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var (w, h) = (QuestCard.DesiredSize.Width, QuestCard.DesiredSize.Height);
        var at = Screen(MapProjection.Project(map, marker.Position));
        // right of the marker, or left of it near the right edge; always inside the map
        var left = at.X + 14 + w <= Viewport.ActualWidth ? at.X + 14 : at.X - 14 - w;
        Canvas.SetLeft(QuestCard, Math.Max(4, Math.Min(left, Viewport.ActualWidth - w - 4)));
        Canvas.SetTop(QuestCard, Math.Max(4, Math.Min(at.Y - 14, Viewport.ActualHeight - h - 4)));
    }

    void AddCardLine(string text, Brush brush, bool bold = false, double indent = 0, double top = 2) =>
        CardBody.Children.Add(new TextBlock
        {
            Text = text, Foreground = brush, FontSize = 11.5, TextWrapping = TextWrapping.Wrap,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Margin = new Thickness(indent, top, 0, 0),
        });

    // clicks on the card stay on the card (no drag of the map under it, no closing it)
    void QuestCard_MouseDown(object sender, MouseButtonEventArgs e) => e.Handled = true;

    void CardClose_Click(object sender, RoutedEventArgs e) => HideCard();

    void CardWiki_Click(object sender, RoutedEventArgs e) => _vm?.OpenWikiCommand.Execute(QuestCard.Tag as Quest);

    void UpdateStatus()
    {
        if (_vm == null) return;
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
