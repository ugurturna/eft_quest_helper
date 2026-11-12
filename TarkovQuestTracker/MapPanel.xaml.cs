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
    static readonly Brush AiPmcBrush = Frozen(Color.FromRgb(0xE0, 0x6B, 0xD8));
    static readonly Brush BossBrush = Frozen(Color.FromRgb(0xFF, 0x3B, 0x3B));
    static readonly Brush LockBrush = Frozen(Color.FromRgb(0xD8, 0xC0, 0x7A));
    static readonly Brush SearchBrush = Frozen(Color.FromRgb(0xFF, 0x5C, 0xC8));
    static readonly Brush MineFill = Frozen(Color.FromArgb(0x48, 0xFF, 0x30, 0x30));
    static readonly Brush MineStroke = Frozen(Color.FromArgb(0xC0, 0xFF, 0x40, 0x40));
    static readonly Brush SniperFill = Frozen(Color.FromArgb(0x30, 0xFF, 0x9A, 0x2E));
    static readonly Brush SniperStroke = Frozen(Color.FromArgb(0xA0, 0xFF, 0x9A, 0x2E));
    /// <summary>Container groups (<see cref="MapContainerGroups"/>), by group key.</summary>
    static readonly Dictionary<string, Brush> ContainerBrushes = new()
    {
        ["safe"] = Frozen(Color.FromRgb(0xFF, 0xC1, 0x07)),
        ["pc"] = Frozen(Color.FromRgb(0x4F, 0xC3, 0xF7)),
        ["weapon"] = Frozen(Color.FromRgb(0x8B, 0xC3, 0x4A)),
        ["medical"] = Frozen(Color.FromRgb(0xF0, 0x62, 0x92)),
        ["bags"] = Frozen(Color.FromRgb(0xBC, 0xAA, 0xA4)),
        ["cash"] = Frozen(Color.FromRgb(0xFF, 0xEB, 0x3B)),
        ["tools"] = Frozen(Color.FromRgb(0x90, 0xA4, 0xAE)),
        ["caches"] = Frozen(Color.FromRgb(0xFF, 0x98, 0x00)),
        ["bodies"] = Frozen(Color.FromRgb(0x9E, 0x9E, 0x9E)),
        [MapContainerGroups.Other] = Frozen(Color.FromRgb(0xCE, 0x93, 0xD8)),
    };
    /// <summary>Points on another floor than the one shown.</summary>
    const double OtherFloorOpacity = 0.3;
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
        if (e.PropertyName == nameof(MainViewModel.MapSearchItem)) ShowSearchItem();
        if (e.PropertyName == nameof(MainViewModel.MapNames)) OnMapDataReloaded();
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

    /// <summary>Newer map data came (app start): same map and view, the floors and pictures from the new data.</summary>
    void OnMapDataReloaded()
    {
        if (!IsVisible)
        {
            _shownMap = null;  // reloaded when the panel shows
            return;
        }
        if (_shownMap != _vm?.MapName) return;
        FillFloors();
        _ = LoadPicturesAsync(fit: false);
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
        DrawLayers(map);
        UpdateSearchCount(map);
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

    // optional layers (In-Game GPS settings): hazards under everything, then containers, spawns, the searched item, locks, bosses

    void DrawLayers(MapGeometry map)
    {
        _layerLabels.Clear();
        DrawLayerPoints(map);
        foreach (var label in _layerLabels) Markers.Children.Add(label);
        _layerLabels.Clear();
    }

    void DrawLayerPoints(MapGeometry map)
    {
        var vm = _vm!;
        var lang = Loc.Lang;
        // labels only where there is room: not on the mini map, and zoomed in
        var labels = !IsMini && _scale >= _fitScale * 2.5;
        if (vm.MapShowHazards)
            foreach (var h in map.Hazards)
            {
                var mines = h.Type == "minefield";
                AddArea(map, h.Outline, mines ? MineFill : SniperFill, mines ? MineStroke : SniperStroke,
                        Loc.T(mines ? "s.map.minefield" : "s.map.sniper"));
            }
        if (vm.MapShowContainers)
            foreach (var c in map.Containers)
                if (vm.ShowsContainer(c.Type))
                    AddMark(map, c.Position, ContainerBrushes[MapContainerGroups.Of(c.Type)], 6, Mark.Square, map.Names.Of(c.Type, lang));
        foreach (var s in map.Spawns)
        {
            var (show, brush, key) = s.Kind switch
            {
                "pmc" => (vm.MapShowPmcSpawns, PmcBrush, "s.map.spawn.pmc"),
                "aipmc" => (vm.MapShowPmcSpawns, AiPmcBrush, "s.map.spawn.aipmc"),
                _ => (vm.MapShowScavSpawns, ScavBrush, "s.map.spawn.scav"),
            };
            if (show) AddMark(map, s.Position, brush, 8, Mark.Ring, Loc.T(key));
        }
        if (vm.MapSearchIndex(map) is var item and >= 0)
        {
            var name = vm.MapSearchItem!.Name;
            foreach (var spot in map.Loose)
                if (spot.Items.Contains(item))
                    AddMark(map, spot.Position, SearchBrush, 12, Mark.Diamond, $"{name}\n{Loc.T("s.map.search.loose")}", labels ? name : null);
        }
        if (vm.MapShowLocks)
            foreach (var l in map.Locks)
            {
                var tip = $"\U0001F511 {map.Names.Of(l.Key, lang)}\n{Loc.T("s.map.lock." + l.Type)}"
                          + (l.Power ? $" · {Loc.T("s.map.lock.power")}" : "");
                AddMark(map, l.Position, LockBrush, 8, Mark.Square, tip, labels ? "\U0001F511 " + Short(map, l.Key, lang) : null);
            }
        if (vm.MapShowBosses)
        {
            // bosses sharing a spot (Knight and the Rogues on Lighthouse) get one marker with every name
            var spots = map.Bosses
                .SelectMany(b => b.Positions.Select((p, i) => (Boss: b, Position: p, First: i == 0)))
                .GroupBy(x => (x.Position[0], x.Position[1], x.Position[2]));
            foreach (var spot in spots)
            {
                var bosses = spot.Select(x => x.Boss).Distinct().ToList();
                var tip = string.Join("\n", bosses.Select(b => Loc.F("s.map.boss.tip", map.Names.Of(b.Name, lang),
                    (b.Chance * 100).ToString("0", CultureInfo.InvariantCulture), b.Escorts)));
                // named once: a boss has several spots close together
                var label = IsMini || (!labels && !spot.Any(x => x.First)) ? null
                    : "\u2620 " + string.Join(", ", bosses.Select(b => map.Names.Of(b.Name, lang)));
                AddMark(map, spot.First().Position, BossBrush, 10, Mark.Diamond, tip, label);
            }
        }
    }

    // item search box: the picked item's loose loot spots are marked (on the mini map too)

    bool _settingSearchText;

    void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchHint.Visibility = SearchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        SearchClear.Visibility = SearchBox.Text.Length == 0 && _vm?.MapSearchItem == null ? Visibility.Collapsed : Visibility.Visible;
        if (_settingSearchText || _vm == null) return;
        var found = _vm.FindMapItems(SearchBox.Text);
        SearchList.ItemsSource = found;
        SearchPopup.IsOpen = found.Count > 0;
    }

    void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!SearchPopup.IsOpen || SearchList.Items.Count == 0) return;
        if (e.Key == Key.Down)
        {
            SearchList.SelectedIndex = 0;
            (SearchList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem)?.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            PickSearchItem(SearchList.Items[0] as MapItemChoice);
            e.Handled = true;
        }
    }

    void SearchList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        PickSearchItem(SearchList.SelectedItem as MapItemChoice);
        e.Handled = true;
    }

    void SearchList_MouseUp(object sender, MouseButtonEventArgs e) => PickSearchItem(SearchList.SelectedItem as MapItemChoice);

    void PickSearchItem(MapItemChoice? item)
    {
        if (item == null || _vm == null) return;
        SearchPopup.IsOpen = false;
        _vm.MapSearchItem = item;
        Keyboard.Focus(Viewport);
    }

    void SearchClear_Click(object sender, RoutedEventArgs e)
    {
        if (_vm != null) _vm.MapSearchItem = null;
        ShowSearchItem();
    }

    /// <summary>The box shows the picked item (it may have been picked on another map panel).</summary>
    void ShowSearchItem()
    {
        _settingSearchText = true;
        SearchBox.Text = _vm?.MapSearchItem?.Name ?? "";
        _settingSearchText = false;
        SearchPopup.IsOpen = false;
    }

    void UpdateSearchCount(MapGeometry map)
    {
        var item = _vm!.MapSearchIndex(map);
        SearchCount.Visibility = item >= 0 ? Visibility.Visible : Visibility.Collapsed;
        if (item >= 0) SearchCount.Text = Loc.F("s.map.search.count", map.Loose.Count(s => s.Items.Contains(item)));
    }

    static string Short(MapGeometry map, string id, string lang) =>
        map.Names.Table.ContainsKey(id + " short") ? map.Names.Of(id + " short", lang) : map.Names.Of(id, lang);

    enum Mark { Square, Ring, Diamond }

    /// <summary>A small screen-sized marker; dimmed when it is on another floor than the one shown.</summary>
    void AddMark(MapGeometry map, double[] position, Brush brush, double size, Mark shape, string tip, string? label = null)
    {
        var at = Screen(MapProjection.Project(map, position));
        if (at.X < -20 || at.Y < -20 || at.X > Viewport.ActualWidth + 20 || at.Y > Viewport.ActualHeight + 20) return;
        if (IsMini) size *= 0.8;
        Shape dot = shape switch
        {
            Mark.Ring => new Ellipse { Width = size, Height = size, Stroke = brush, StrokeThickness = 2, Fill = Brushes.Transparent },
            Mark.Diamond => new Rectangle { Width = size, Height = size, Fill = brush, Stroke = Brushes.Black, StrokeThickness = 1,
                                            RenderTransform = new RotateTransform(45, size / 2, size / 2) },
            _ => new Rectangle { Width = size, Height = size, Fill = brush, Stroke = Brushes.Black, StrokeThickness = 0.8 },
        };
        var opacity = OnShownFloor(map, position) ? 1 : OtherFloorOpacity;
        dot.ToolTip = tip;
        dot.Opacity = opacity;
        Canvas.SetLeft(dot, at.X - size / 2);
        Canvas.SetTop(dot, at.Y - size / 2);
        Markers.Children.Add(dot);
        if (label == null) return;
        var text = new Border
        {
            Background = LabelBack, CornerRadius = new CornerRadius(2), Padding = new Thickness(3, 0, 3, 1), IsHitTestVisible = false,
            Opacity = opacity, Child = new TextBlock { Text = label, FontSize = IsMini ? 9.5 : 10.5, Foreground = brush },
        };
        Canvas.SetLeft(text, at.X + size / 2 + 2);
        Canvas.SetTop(text, at.Y - 8);
        _layerLabels.Add(text);
    }

    /// <summary>The layers' labels, drawn after all their points so no point covers one.</summary>
    readonly List<UIElement> _layerLabels = [];

    /// <summary>Whether a point is on the floor drawn now (a map without floors has only the ground).</summary>
    bool OnShownFloor(MapGeometry map, double[] p) =>
        map.Layers.Count == 0 || MapProjection.LayerFor(map, p[0], p[1], p[2])?.Name == _shownLayer?.Name;

    void AddArea(MapGeometry map, List<double[]> outline, Brush fill, Brush stroke, string tip)
    {
        if (outline.Count < 3) return;
        var area = new Polygon { Fill = fill, Stroke = stroke, StrokeThickness = 1.2, ToolTip = tip };
        foreach (var p in outline) area.Points.Add(Screen(MapProjection.Project(map, p[0], p[1])));
        Markers.Children.Add(area);
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
