using TarkovQuestTracker.Core.Models;

namespace TarkovQuestTracker.Core.Services;

/// <summary>A quest not done yet that asks for the item (hand over, plant, or bring into the raid).</summary>
public sealed record ItemQuestUse(Quest Quest, ItemNeed Need, bool Active, bool Available);

public enum SellPlace { None, Flea, Trader }

/// <summary>What to do with one item: keep it for quests, or where it sells best.</summary>
public sealed record ItemAdvice(FleaItem Item, IReadOnlyList<ItemQuestUse> Quests, SellPlace Sell, int? SellPrice)
{
    public bool Keep => Quests.Count > 0;
}

public static class ItemAdvisor
{
    /// <summary>
    /// A trader paying at least this share of the flea price wins: no listing fee, no waiting for a buyer.
    /// </summary>
    public const double TraderShare = 0.85;

    public static ItemAdvice Advise(FleaItem item, IEnumerable<Quest> quests, Progress progress)
    {
        var (sell, price) = BestSale(item);
        return new ItemAdvice(item, QuestUses(item.Id, quests, progress), sell, price);
    }

    /// <summary>Quests not done yet that need the item: active ones first, then the ones open now, then by level.</summary>
    public static IReadOnlyList<ItemQuestUse> QuestUses(string itemId, IEnumerable<Quest> quests, Progress progress)
    {
        var uses = new List<ItemQuestUse>();
        foreach (var q in quests)
        {
            if (progress.Completed.Contains(q.Id) || q.IsPastEvent) continue;
            // the same item can be both handed over and planted: the biggest amount stands for the quest
            var need = q.ItemNeeds.Concat(q.RaidNeedItems)
                .Where(n => n.ItemId == itemId || n.AlternativeIds.Contains(itemId))
                .OrderByDescending(n => n.Count)
                .FirstOrDefault();
            if (need == null) continue;
            uses.Add(new ItemQuestUse(q, need, progress.Active.Contains(q.Id), q.Prerequisites.All(progress.Completed.Contains)));
        }
        return uses
            .OrderByDescending(u => u.Active)
            .ThenByDescending(u => u.Available)
            .ThenBy(u => u.Quest.MinLevel ?? 0)
            .ThenBy(u => u.Quest.DisplayName)
            .ToList();
    }

    /// <summary>Flea or the best trader (see <see cref="TraderShare"/>); none when neither buys it.</summary>
    public static (SellPlace Place, int? Price) BestSale(FleaItem item)
    {
        var flea = item.FleaBanned ? null : item.FleaPrice;
        var trader = item.BestTraderPrice;
        if (flea is { } f && (trader is not { } t || t < f * TraderShare)) return (SellPlace.Flea, f);
        return trader is { } p ? (SellPlace.Trader, p) : (SellPlace.None, null);
    }
}
