using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using TarkovQuestTracker.Core.Services;
using TarkovQuestTracker.Interop;
using TarkovQuestTracker.ViewModels;

namespace TarkovQuestTracker;

/// <summary>
/// "Keep or sell?" card next to the mouse for the item <see cref="ItemChecker"/> read: the quests that still need it,
/// and where it sells best. Never takes focus or clicks; the checker hides it when the mouse moves away.
/// </summary>
public partial class ItemCardWindow : Window
{
    /// <summary>Quests listed on the card; the rest is a "+N more" line.</summary>
    const int MaxQuests = 4;
    /// <summary>Gap between the mouse and the card (physical pixels).</summary>
    const int Gap = 28;

    readonly MainViewModel _vm;
    IntPtr _hwnd;

    public ItemCardWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;  // opacity comes from the settings
        InitializeComponent();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.FleaShowInCapture)) ApplyCaptureExclusion();
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        Win32.MakeOverlay(_hwnd);
        Win32.SetClickThrough(_hwnd, true);
        ApplyCaptureExclusion();
    }

    void ApplyCaptureExclusion()
    {
        if (_hwnd != IntPtr.Zero) Win32.ExcludeFromCapture(_hwnd, !_vm.FleaShowInCapture);
    }

    static Brush Res(string key) => (Brush)Application.Current.FindResource(key);

    /// <summary>Fills the card for the item and shows it beside <paramref name="at"/> (the mouse, physical pixels).</summary>
    public void ShowFor(FleaItem item, (int X, int Y) at)
    {
        if (_vm.Advise(item) is not { } advice) return;
        Fill(advice);
        new WindowInteropHelper(this).EnsureHandle();
        if (!IsVisible) Show();
        UpdateLayout();
        Place(at);
    }

    void Fill(ItemAdvice advice)
    {
        var item = advice.Item;
        ItemName.Text = item.Name;

        Verdict.Text = advice.Keep ? Loc.F("ic.keep", advice.Quests.Count) : Loc.T("ic.noQuest");
        Verdict.Foreground = Res(advice.Keep ? "WarnBrush" : "DoneBrush");
        Quests.ItemsSource = advice.Quests.Take(MaxQuests).Select(u => new ItemCardQuest(u)).ToList();
        More.Text = advice.Quests.Count > MaxQuests ? Loc.F("ic.more", advice.Quests.Count - MaxQuests) : "";
        More.Visibility = More.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

        Sell.Text = advice.Sell switch
        {
            SellPlace.Flea => Loc.F("ic.sellFlea", FleaSearchWindow.Rub(advice.SellPrice)),
            SellPlace.Trader => Loc.F("ic.sellTrader", item.BestTrader, FleaSearchWindow.Rub(advice.SellPrice)),
            _ => Loc.T("ic.sellNone"),
        };
        // a quest item's price is only for reference
        Sell.Foreground = Res(advice.Keep || advice.Sell == SellPlace.None ? "MutedBrush" : "DoneBrush");

        var prices = new List<string>();
        if (item.FleaBanned) prices.Add(Loc.T("flea.banned"));
        else if (item.FleaPrice is { } flea)
            prices.Add("Flea " + FleaSearchWindow.Rub(flea)
                       + (item.Slots > 1 ? " · " + Loc.F("flea.perSlot", FleaSearchWindow.Rub(flea / item.Slots)) : ""));
        else prices.Add("Flea: " + Loc.T("flea.noOffers"));
        if (item.BestTraderPrice is { } trader) prices.Add(Loc.F("flea.trader", item.BestTrader, FleaSearchWindow.Rub(trader)));
        Prices.Text = string.Join("  ·  ", prices);

        var at = _vm.Flea.UpdatedAt(_vm.Settings.Mode);
        Footer.Text = Loc.F("flea.source", _vm.ModeText, at is { } t ? t.ToLocalTime().ToString("HH:mm") : "—");
    }

    /// <summary>
    /// Left of the mouse, a little lower: the game's own tooltip opens to the right of it. When the screen edge is
    /// in the way, right of the mouse below the tooltip instead. Physical pixels, so display scaling can't move it.
    /// </summary>
    void Place((int X, int Y) at)
    {
        var (w, h) = Win32.Size(_hwnd);
        var m = Win32.MonitorAt(at.X, at.Y);
        int x = at.X - Gap - w, y = at.Y + Gap / 2;
        if (x < m.Left)
        {
            x = at.X + Gap;
            y = at.Y + 3 * Gap;
        }
        x = Math.Clamp(x, m.Left, Math.Max(m.Left, m.Right - w));
        y = Math.Clamp(y, m.Top, Math.Max(m.Top, m.Bottom - h));
        Win32.MoveTo(_hwnd, x, y);
    }

    public void HideCard()
    {
        if (IsVisible) Hide();
    }
}

/// <summary>One quest line on the card.</summary>
public sealed class ItemCardQuest(ItemQuestUse use)
{
    /// <summary>◆ active · ○ can be started · · locked behind other quests.</summary>
    public string Mark => use.Active ? "◆" : use.Available ? "○" : "·";
    public Brush MarkBrush => (Brush)Application.Current.FindResource(use.Active ? "AccentBrush" : use.Available ? "TextBrush" : "MutedBrush");
    public string Name => use.Quest.DisplayName;

    public string Details
    {
        get
        {
            var parts = new List<string> { $"×{use.Need.Count}" + (use.Need.Fir ? " FiR" : "") };
            if (use.Need.AlternativeIds.Count > 0) parts.Add(Loc.F("ic.anyOf", use.Need.AlternativeIds.Count + 1));
            if (use.Active) parts.Add(Loc.T("ic.active"));
            else if (!use.Available) parts.Add(use.Quest.LevelText is { Length: > 0 } level ? $"{level} · {Loc.T("ic.locked")}" : Loc.T("ic.locked"));
            return "· " + string.Join(" · ", parts);
        }
    }
}
