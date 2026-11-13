using TarkovQuestTracker.Core.Services;

namespace TarkovQuestTracker.ViewModels;

/// <summary>Item check: "keep or sell?" for the item under the mouse in game (hotkey, or by itself).</summary>
public sealed partial class MainViewModel
{
    ItemTextMatcher? _itemMatcher;
    IReadOnlyList<FleaItem>? _itemMatcherSource;

    public ItemChecker ItemCheck { get; }

    ItemChecker CreateItemChecker()
    {
        var checker = new ItemChecker(ItemMatcherAsync);
        checker.Message += text => Toast = text;
        return checker;
    }

    /// <summary>Opens the card by itself when the mouse rests on an item in game.</summary>
    public bool ItemCheckAuto
    {
        get => Settings.ItemCheckAuto;
        set
        {
            if (Settings.ItemCheckAuto == value) return;
            Settings.ItemCheckAuto = value;
            ItemCheck.Auto = value;
            SaveSettings();
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Names of the selected mode's items (the flea files), built again when new prices land.
    /// Old prices still answer now; newer ones are downloaded in the background for the next read.
    /// </summary>
    async Task<ItemTextMatcher?> ItemMatcherAsync()
    {
        var (mode, lang) = (Settings.Mode, Settings.Language);
        if (Flea.IsStale(mode, lang) && !Flea.IsRefreshing(mode)) _ = RefreshPricesQuietlyAsync(mode, lang);
        IReadOnlyList<FleaItem>? items;
        try
        {
            items = await Flea.LoadAsync(mode, lang);
        }
        catch (Exception e) when (FleaMarket.IsDownloadError(e))
        {
            items = null;
        }
        if (items == null) return null;
        if (!ReferenceEquals(items, _itemMatcherSource))
        {
            _itemMatcher = await Task.Run(() => new ItemTextMatcher(items));
            _itemMatcherSource = items;
        }
        return _itemMatcher;
    }

    async Task RefreshPricesQuietlyAsync(Core.Models.GameMode mode, string lang)
    {
        try { await Flea.RefreshAsync(mode, lang); }
        catch (Exception e) when (FleaMarket.IsDownloadError(e)) { }
    }

    /// <summary>Quests of the current mode that still need the item, and where it sells best.</summary>
    public ItemAdvice? Advise(FleaItem item) => _repo == null ? null : ItemAdvisor.Advise(item, _repo.Quests, Progress);
}
