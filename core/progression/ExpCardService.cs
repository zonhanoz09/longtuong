using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.38 (Huynh): Thẻ kinh nghiệm bán ở Tửu quán (view Chiêu mộ),
/// ăn trực tiếp cho tướng ở popup Võ tướng (click đúp ô thẻ trong lưới).
/// Giá + lượng EXP do Huynh chốt (đề xuất của Pax được duyệt).
/// </summary>
public record ExpCardDef(string Id, string Name, int Exp, int PriceGold);

public static class ExpCardService
{
    public static readonly List<ExpCardDef> Cards = new()
    {
        new ExpCardDef("exp_small", "Thẻ EXP (Tiểu)", 1000, 200),
        new ExpCardDef("exp_mid", "Thẻ EXP (Trung)", 5000, 800),
        new ExpCardDef("exp_big", "Thẻ EXP (Đại)", 20000, 3000),
    };

    public static ExpCardDef? Get(string cardId) => Cards.FirstOrDefault(c => c.Id == cardId);

    public static int Count(SaveData save, string cardId) =>
        save.ExpCards.TryGetValue(cardId, out int n) ? n : 0;

    public static int TotalCount(SaveData save) => save.ExpCards.Values.Sum();

    /// <summary>Mua 1 thẻ bằng Vàng. false nếu thẻ không tồn tại / không đủ Vàng.</summary>
    public static bool TryBuyCard(SaveData save, string cardId)
    {
        var def = Get(cardId);
        if (def == null || save.Gold < def.PriceGold) return false;
        save.Gold -= def.PriceGold;
        save.ExpCards[cardId] = Count(save, cardId) + 1;
        return true;
    }

    public enum FeedResult { Success, NoCard, MaxLevel, BadCard }

    /// <summary>
    /// Cho tướng ăn 1 thẻ: trừ thẻ, cộng EXP (tự lên cấp, max 180).
    /// </summary>
    public static (FeedResult Result, int ExpGained, int OldLevel, int NewLevel)
        TryFeedCard(SaveData save, string heroId, string cardId)
    {
        var def = Get(cardId);
        if (def == null) return (FeedResult.BadCard, 0, 0, 0);
        if (Count(save, cardId) <= 0) return (FeedResult.NoCard, 0, 0, 0);
        var p = ProgressionService.ProgressOf(save, heroId);
        if (p.Level >= ProgressionService.MaxLevel) return (FeedResult.MaxLevel, 0, p.Level, p.Level);
        int oldLevel = p.Level;
        save.ExpCards[cardId] = Count(save, cardId) - 1;
        ProgressionService.AddExp(save, heroId, def.Exp);
        return (FeedResult.Success, def.Exp, oldLevel, p.Level);
    }
}
