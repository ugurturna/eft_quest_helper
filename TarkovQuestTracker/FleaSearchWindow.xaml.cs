using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using TarkovQuestTracker.Core.Models;
using TarkovQuestTracker.Core.Services;
using TarkovQuestTracker.Interop;
using TarkovQuestTracker.ViewModels;

namespace TarkovQuestTracker;

/// <summary>
/// Flea market price search in the middle of the screen (hotkey, default Ctrl+Shift+F).
/// Takes the keyboard while open; Esc hides it and gives focus back to the game.
/// Prices follow the selected game mode (PvP / PvE) and are searched in the local tarkov.dev files;
/// files older than <see cref="FleaMarket.MaxAge"/> are downloaded again when it opens.
/// </summary>
public partial class FleaSearchWindow : Window
{
    static readonly NumberFormatInfo RubFormat = new() { NumberGroupSeparator = " " };

    readonly MainViewModel _vm;
    IntPtr _hwnd;
    /// <summary>Window that had focus before we opened (the game), to return to on Esc.</summary>
    IntPtr _previous;
    bool _hiding;
    int _searchVersion;
    /// <summary>Why the last download failed; null after a good one.</summary>
    string? _refreshError;

    public FleaSearchWindow(MainViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        Win32.MakeToolWindow(_hwnd);
    }

    public void Toggle()
    {
        if (IsVisible) Dismiss(returnToGame: true);
        else Open();
    }

    void Open()
    {
        var foreground = Win32.GetForegroundWindow();
        _previous = foreground == _hwnd ? IntPtr.Zero : foreground;
        // the game runs full screen, so the middle of its whole monitor (not the work area)
        new WindowInteropHelper(this).EnsureHandle();
        Win32.CenterOnMonitorOf(_hwnd, _previous);
        Show();
        Win32.CenterOnMonitorOf(_hwnd, _previous);  // again with the size WPF settled on for this monitor's DPI
        Activate();
        Win32.SetForegroundWindow(_hwnd);
        Keyboard.Focus(Query);
        Query.SelectAll();  // typing replaces the last search
        var (mode, lang) = (_vm.Settings.Mode, _vm.Settings.Language);
        // old prices (or the start-up download still running): search the local files now, re-render when the new ones land
        if (_vm.Flea.IsStale(mode, lang) || _vm.Flea.IsRefreshing(mode)) _ = RefreshAsync(mode, lang);
        _ = SearchAsync();
    }

    async Task RefreshAsync(GameMode mode, string lang)
    {
        _refreshError = null;
        try
        {
            await _vm.Flea.RefreshAsync(mode, lang);
        }
        catch (Exception ex) when (FleaMarket.IsDownloadError(ex))
        {
            _refreshError = ex.Message;
        }
        if (IsVisible) _ = SearchAsync();
    }

    void Dismiss(bool returnToGame)
    {
        if (!IsVisible || _hiding) return;
        _hiding = true;
        Hide();
        _hiding = false;
        if (returnToGame && _previous != IntPtr.Zero) Win32.SetForegroundWindow(_previous);
    }

    // clicking the game (or alt-tab) closes it too
    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        Dismiss(returnToGame: false);
    }

    void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        Dismiss(returnToGame: true);
        e.Handled = true;
    }

    void Query_TextChanged(object sender, TextChangedEventArgs e) => _ = SearchAsync();

    async Task SearchAsync()
    {
        var version = ++_searchVersion;
        var mode = _vm.Settings.Mode;
        var lang = _vm.Settings.Language;
        ModeRun.Text = _vm.ModeText;
        Placeholder.Visibility = Query.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

        IReadOnlyList<FleaItem>? items;
        try
        {
            items = await _vm.Flea.LoadAsync(mode, lang);
        }
        catch (Exception ex) when (FleaMarket.IsDownloadError(ex))
        {
            items = null;
            _refreshError ??= ex.Message;
        }
        if (version != _searchVersion) return;  // a newer keystroke renders instead

        UpdateFooter(mode);
        if (items != null) ShowResults(items);
        else if (_vm.Flea.IsRefreshing(mode)) ShowMessage(Loc.F("flea.loading", _vm.ModeText));
        else ShowMessage(Loc.F("flea.error", _refreshError ?? ""));
    }

    /// <summary>Mode · time of the local prices · "updating" while a download runs.</summary>
    void UpdateFooter(GameMode mode)
    {
        var at = _vm.Flea.UpdatedAt(mode);
        var text = Loc.F("flea.source", _vm.ModeText, at is { } t ? t.ToLocalTime().ToString("HH:mm") : "—");
        if (_vm.Flea.IsRefreshing(mode)) text += " · " + Loc.T("flea.updating");
        else if (_refreshError != null && at != null) text += " · " + Loc.T("flea.updateFailed");
        Footer.Text = text;
    }

    void ShowResults(IReadOnlyList<FleaItem> items)
    {
        var text = Query.Text.Trim();
        var found = FleaMarket.Search(items, text);
        Results.ItemsSource = found.Select(i => new FleaRow(i)).ToList();
        SetStatus(SearchService.Normalize(text).Replace(" ", "").Length < FleaMarket.MinQueryLength ? Loc.T("flea.hint")
                  : found.Count == 0 ? Loc.F("flea.none", text)
                  : "");
    }

    /// <summary>Loading / error text instead of results.</summary>
    void ShowMessage(string text)
    {
        Results.ItemsSource = null;
        SetStatus(text);
    }

    void SetStatus(string text)
    {
        Status.Text = text;
        Status.Visibility = text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public static string Rub(int? value) => value is { } v ? v.ToString("N0", RubFormat) + " ₽" : "—";
}

/// <summary>One search result, formatted for display.</summary>
public sealed class FleaRow(FleaItem item)
{
    public string Name => item.Name;
    /// <summary>tarkov.dev icons are WebP, which WPF can't always decode; the same icon exists as JPEG.</summary>
    public string? Icon => item.IconLink?.Replace(".webp", ".jpg");
    public bool HasPrice => !item.FleaBanned && item.FleaPrice != null;

    public string Price => item.FleaBanned ? Loc.T("flea.banned")
                         : item.FleaPrice == null ? Loc.T("flea.noOffers")
                         : FleaSearchWindow.Rub(item.FleaPrice);

    /// <summary>Short name · best trader sell price.</summary>
    public string Sub => item.BestTraderPrice is { } p
        ? $"{item.ShortName} · {Loc.F("flea.trader", item.BestTrader, FleaSearchWindow.Rub(p))}"
        : item.ShortName;

    /// <summary>Price per inventory slot (bigger items) and the 24 h average.</summary>
    public string Details
    {
        get
        {
            if (!HasPrice) return "";
            var parts = new List<string>();
            if (item.Slots > 1) parts.Add(Loc.F("flea.perSlot", FleaSearchWindow.Rub(item.FleaPrice / item.Slots)));
            if (item.Avg24hPrice is { } avg && item.LastLowPrice != null) parts.Add(Loc.F("flea.avg", FleaSearchWindow.Rub(avg)));
            return string.Join(" · ", parts);
        }
    }

    public string Change => HasPrice && item.Change48hPercent is { } c && Math.Abs(c) >= 0.05
        ? string.Format(CultureInfo.InvariantCulture, " {0:+0.#;-0.#}%", c)
        : "";

    /// <summary>48 h price trend: true up, false down, null none.</summary>
    public bool? Up => Change.Length == 0 ? null : item.Change48hPercent > 0;
}
