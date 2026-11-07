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

    void Level_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox box) CommitLevel(box);
    }

    void Level_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box) CommitLevel(box);
    }

    void Level_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = !e.Text.All(char.IsAsciiDigit);

    void Level_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetData(DataFormats.UnicodeText) is not string text || !text.Trim().All(char.IsAsciiDigit))
            e.CancelCommand();
    }

    /// <summary>Anything above 99 (even 548787 or past int range) becomes 99; empty or 0 becomes 1.</summary>
    static void CommitLevel(TextBox box)
    {
        var digits = box.Text.Trim();
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit))
        {
            // nothing usable typed: show the current level again
            box.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
            return;
        }
        var level = digits.TrimStart('0').Length > 2
            ? MainViewModel.MaxPlayerLevel
            : Math.Clamp(int.Parse(digits), MainViewModel.MinPlayerLevel, MainViewModel.MaxPlayerLevel);
        box.Text = level.ToString();
        box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
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
