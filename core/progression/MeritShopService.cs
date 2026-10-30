using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.48: Cửa hàng Chiến tích — spend Chiến tích (Ải Vượt Tướng /
/// Tổ Đội) on Khoáng thạch, Đá tẩy luyện and set fragments. Prices are
/// Pax setup (Huynh tunes freely), no stock limit.
/// </summary>
public static class MeritShopService
{
    public record ShopItem(string Id, string Name, int Price);

    public static readonly List<ShopItem> Items = new()
    {
        new(ForgeService.OreId, ForgeService.OreName, 20),
        new("refine_stone", "Đá tẩy luyện", 30),
        new(ForgeService.FragmentId("chu_tuoc"), ForgeService.FragmentName("chu_tuoc"), 50),
        new(ForgeService.FragmentId("bach_ho"), ForgeService.FragmentName("bach_ho"), 80),
        new(ForgeService.FragmentId("thanh_long"), ForgeService.FragmentName("thanh_long"), 120),
        new(ForgeService.FragmentId("huyen_vu"), ForgeService.FragmentName("huyen_vu"), 180),
        new(ForgeService.FragmentId("ky_lan"), ForgeService.FragmentName("ky_lan"), 250),
    };

    public static ShopItem? Get(string id) => Items.FirstOrDefault(i => i.Id == id);

    /// How many of this item the player has (for the shop rows).
    public static int Owned(SaveData save, string id)
    {
        ProgressionService.EnsureDefaults(save);
        if (id == "refine_stone") return save.RefineStones;
        return save.Materials.TryGetValue(id, out var n) ? n : 0;
    }

    public static bool TryBuy(SaveData save, string id, int count)
    {
        ProgressionService.EnsureDefaults(save);
        var item = Get(id);
        if (item == null || count <= 0) return false;
        int cost = item.Price * count;
        if (save.Merit < cost) return false;
        save.Merit -= cost;
        if (id == "refine_stone") save.RefineStones += count;
        else save.Materials[id] = Owned(save, id) + count;
        return true;
    }
}
