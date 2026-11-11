using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using TarkovQuestTracker.ViewModels;

namespace TarkovQuestTracker;

public partial class MainWindow : Window
{
    readonly MainViewModel _vm;
    readonly OverlayWindow _overlay;

    public MainWindow(MainViewModel vm, OverlayWindow overlay)
    {
        _vm = vm;
        _overlay = overlay;
        DataContext = vm;
        InitializeComponent();
        // at high DPI the logical screen can be smaller than the default size
        var area = SystemParameters.WorkArea;
        Width = Math.Min(Width, area.Width * 0.92);
        Height = Math.Min(Height, area.Height * 0.92);
        MinWidth = Math.Min(MinWidth, Width);
        MinHeight = Math.Min(MinHeight, Height);
        Loaded += (_, _) => SearchBox.Focus();
        PreviewMouseWheel += ZoomWithWheel;
        vm.MapAppRequested += ShowMapTab;
        vm.TicketPageRequested += () => TicketTab.IsSelected = true;
    }

    /// <summary>The map buttons in the GPS "in app" mode: this window comes up on its map page.</summary>
    void ShowMapTab()
    {
        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        MapTab.IsSelected = true;
        Activate();
    }

    void OpenMapTab_Click(object sender, RoutedEventArgs e) => MapTab.IsSelected = true;

    GridLength _detailWidth;

    /// <summary>The story and map pages are wide: the quest detail panel on the right steps aside for them.</summary>
    void Pages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != Pages) return;  // lists inside the pages raise this too
        var wide = StoryTab.IsSelected || MapTab.IsSelected;
        if (wide == (DetailPanel.Visibility != Visibility.Visible)) return;
        DetailPanel.Visibility = DetailSplitter.Visibility = wide ? Visibility.Collapsed : Visibility.Visible;
        if (wide)
        {
            _detailWidth = DetailColumn.Width;  // as the splitter left it
            (DetailColumn.MinWidth, DetailColumn.Width, SplitterColumn.Width) = (0, new GridLength(0), new GridLength(0));
        }
        else (DetailColumn.MinWidth, DetailColumn.Width, SplitterColumn.Width) = (320, _detailWidth, new GridLength(5));
    }

    /// <summary>Another GPS mode hides the map page: back to the GPS page rather than an empty one.</summary>
    void MapTab_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (MapTab.Visibility != Visibility.Visible && MapTab.IsSelected) GpsTab.IsSelected = true;
    }

    /// <summary>Ctrl + mouse wheel: the app size (same as the Settings slider).</summary>
    void ZoomWithWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        _vm.UiScale += e.Delta > 0 ? 0.05 : -0.05;
        e.Handled = true;
    }

    // list rebuilds clear the selection; only react to real picks so the detail panel stays put
    void QuestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is QuestRow row) _vm.SelectedQuest = row.Quest;
    }

    // Paged loading: appending rows re-anchors the scroll position and fires more ScrollChanged events,
    // so pages are rate-limited and a request inside the cooldown is re-checked when it ends.
    static readonly TimeSpan PageCooldown = TimeSpan.FromMilliseconds(350);
    DateTime _nextPageAllowed;
    ScrollViewer? _resultsScroll;
    DispatcherTimer? _pageRecheck;

    void QuestList_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _resultsScroll = e.OriginalSource as ScrollViewer;
        if (e.VerticalChange > 0) TryLoadNextPage();
    }

    void TryLoadNextPage()
    {
        if (_resultsScroll is not { } sv) return;
        var remaining = sv.ExtentHeight - (sv.VerticalOffset + sv.ViewportHeight);
        if (remaining > sv.ViewportHeight * 0.5) return;
        if (DateTime.UtcNow < _nextPageAllowed)
        {
            _pageRecheck ??= CreatePageRecheck();
            _pageRecheck.Start();
            return;
        }
        _nextPageAllowed = DateTime.UtcNow + PageCooldown;
        _vm.LoadMoreResults();
    }

    DispatcherTimer CreatePageRecheck()
    {
        var timer = new DispatcherTimer { Interval = PageCooldown };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            TryLoadNextPage();
        };
        return timer;
    }

    void ToggleOverlay_Click(object sender, RoutedEventArgs e) => _overlay.ToggleVisible();

    void OpenTicketGuide_Click(object sender, RoutedEventArgs e) => TicketTab.IsSelected = true;

    // keep the newest script output in view
    void UpdateLog_TextChanged(object sender, TextChangedEventArgs e) => ((TextBox)sender).ScrollToEnd();


    void NewProfile_Click(object sender, RoutedEventArgs e) =>
        AskName(Loc.T("dlg.newProfile.title"), Loc.T("dlg.newProfile.label"), "", _vm.CreateProfile);

    void RenameProfile_Click(object sender, RoutedEventArgs e) =>
        AskName(Loc.T("dlg.rename.title"), Loc.T("dlg.rename.label"), _vm.CurrentProfile, _vm.RenameProfile);

    void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, Loc.F("dlg.delete.msg", _vm.CurrentProfile), Loc.T("dlg.delete.title"),
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            _vm.DeleteCurrentProfile();
    }

    /// <summary>Small modal text prompt; <paramref name="apply"/> returns an error message or null when done.</summary>
    void AskName(string title, string label, string initial, Func<string, string?> apply)
    {
        var box = new TextBox { Text = initial, Margin = new Thickness(0, 6, 0, 0), MinWidth = 280 };
        var error = new TextBlock { Foreground = (System.Windows.Media.Brush)FindResource("WarnBrush"), Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap };
        var ok = new Button { Content = Loc.T("dlg.ok"), IsDefault = true, MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = Loc.T("dlg.cancel"), IsCancel = true, MinWidth = 80 };
        var dialog = new Window
        {
            Title = title, Owner = this, SizeToContent = SizeToContent.WidthAndHeight, ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false,
            Background = (System.Windows.Media.Brush)FindResource("PanelBrush"),
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Children =
                {
                    new TextBlock { Text = label, Foreground = (System.Windows.Media.Brush)FindResource("TextBrush") },
                    box, error,
                    new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                                     Margin = new Thickness(0, 12, 0, 0), Children = { ok, cancel } },
                },
            },
        };
        ok.Click += (_, _) =>
        {
            if (apply(box.Text) is { } message) error.Text = message;
            else dialog.DialogResult = true;
        };
        dialog.Loaded += (_, _) => { box.Focus(); box.SelectAll(); };
        dialog.ShowDialog();
    }


    /// <summary>The banner's "review" opens the Read screen page, where the read quests are confirmed.</summary>
    void ReviewImport_Click(object sender, RoutedEventArgs e) => ScanTab.IsSelected = true;

    void Export_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { FileName = $"tarkov_progress_{_vm.ModeText.ToLowerInvariant()}.json", Filter = "JSON|*.json" };
        if (dlg.ShowDialog(this) == true) _vm.ExportProgress(dlg.FileName);
    }

    void Import_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON|*.json" };
        if (dlg.ShowDialog(this) != true) return;
        if (MessageBox.Show(this, Loc.F("dlg.import.msg", _vm.ModeText), Loc.T("dlg.import.title"),
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            _vm.ImportProgress(dlg.FileName);
    }
}
