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
/// Takes the keyboard while open; Esc (or a click on the game) hides it and gives focus back to the game.
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
    /// <summary>Window that had focus before we opened (the game), to return to on Esc.</summary>
    IntPtr _previous;
    bool _hiding;
    string? _shownMap;
    MapLayer? _shownLayer;
    int _loadVersion;
    bool _fillingFloors;
    /// <summary>Screen pixels per map unit and where map (0, 0) is on screen.</summary>
    double _scale = 1, _fitScale = 1;
    Vector _offset;
    Point? _dragStart;
    bool _dragged;
    /// <summary>The quest marker under the mouse when the button went down; a click (no drag) on it opens its card.</summary>
    QuestMarker? _pressed;
    /// <summary>The quest marker whose card is open.</summary>
    QuestMarker? _card;

    /// <summary>One drawn quest marker: which objective of which quest, and which of its spots.</summary>
    sealed record QuestMarker(string QuestId, string Text, double[] Position);

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
        Show();
        Activate();
        Win32.TakeForeground(_hwnd);
        Win32.BringToTop(_hwnd);
        Keyboard.Focus(Viewport);
        _topmostTimer.Start();
        if (_shownMap != _vm.MapName) LoadMap();
        else UpdateStatus();
    }

    void Dismiss(bool returnToGame)
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
        if (MapBox.IsDropDownOpen || FloorBox.IsDropDownOpen) return;
        // and on an open quest card only the card
        if (_card != null) HideCard();
        else Dismiss(returnToGame: true);
        e.Handled = true;
    }

    void Close_Click(object sender, RoutedEventArgs e) => Dismiss(returnToGame: true);

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
        if (e.PropertyName == nameof(MainViewModel.MapShowInCapture)) ApplyCaptureExclusion();
        if (e.PropertyName is nameof(MainViewModel.MapPositionX) or nameof(MainViewModel.MapPositionY) && !_dragging) ApplyPosition();
        if (e.PropertyName is nameof(MainViewModel.MapTracking) or nameof(MainViewModel.ScreenshotsFolder)) UpdateStatus();
    }

    void OnLocChanged()
    {
        FillFloors();
        Redraw();
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

    // map and floor

    void LoadMap()
    {
        _shownMap = _vm.MapName;
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
        }
        _pressed = null;
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
        if (Map is not { } map || Pictures.Children.Count == 0)
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
        DrawTrail(map);
        DrawPlayer(map);
        UpdateCard(map, spots);
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
        const double size = 10;
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

    // quest card: a click on a quest marker shows the quest's name and objectives next to it

    void ShowCard(QuestMarker marker)
    {
        _card = marker;
        Redraw();
    }

    void HideCard()
    {
        _card = null;
        QuestCard.Visibility = Visibility.Collapsed;
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

    void CardWiki_Click(object sender, RoutedEventArgs e) => _vm.OpenWikiCommand.Execute(QuestCard.Tag as Quest);

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
