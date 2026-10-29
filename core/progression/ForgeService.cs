using System;
using System.Collections.Generic;

namespace LongTuong;

/// <summary>
/// Phase 2.34: rèn (forge) — the main source of set equipment (Huynh's
/// decision 3). Blueprints + forge materials are sold for Vàng in the
/// forge UI (simple version; dungeon/boss drops arrive later).
/// - Blueprint per set (permanent unlock once bought; Tân Thủ is the
///   starter set and is not forgeable).
/// - Each forge consumes Khoáng thạch (ore); ore is bought with Vàng.
/// - Forging a set requires the main hero to reach the set's tier
///   (per the spec's level milestones).
/// - Forged items land in the bag (equip via the Võ tướng popup).
/// Nâng phẩm (upgrading a low-tier item while keeping its stats) is
/// covered by the 2.35 Kế thừa flow: forge the higher-tier item, then
/// inherit the old item into it.
/// </summary>
public static class ForgeService
{
    public const string OreId = "ore";
    public const string OreName = "Khoáng thạch";
    public const int OrePriceGold = 100;

    // Blueprint prices in Vàng (Pax setup — Huynh adjusts freely).
    public static readonly Dictionary<string, int> BlueprintPrices = new()
    {
        ["chu_tuoc"] = 50000,
        ["bach_ho"] = 100000,
        ["thanh_long"] = 200000,
        ["huyen_vu"] = 400000,
        ["ky_lan"] = 800000,
    };

    public static int OrePerForge(string setId) => EquipmentSets.Get(setId).Tier / 2;

    public static bool HasBlueprint(SaveData save, string setId)
    {
        ProgressionService.EnsureDefaults(save);
        return save.Blueprints.TryGetValue(setId, out bool learned) && learned;
    }

    public static bool TryBuyBlueprint(SaveData save, string setId)
    {
        ProgressionService.EnsureDefaults(save);
        if (!BlueprintPrices.TryGetValue(setId, out int price)) return false;
        if (HasBlueprint(save, setId)) return false;
        if (save.Gold < price) return false;
        save.Gold -= price;
        save.Blueprints[setId] = true;
        return true;
    }

    public static int OreCount(SaveData save)
    {
        ProgressionService.EnsureDefaults(save);
        return save.Materials.TryGetValue(OreId, out int n) ? n : 0;
    }

    public static bool TryBuyOre(SaveData save, int count)
    {
        ProgressionService.EnsureDefaults(save);
        if (count <= 0) return false;
        int price = count * OrePriceGold;
        if (save.Gold < price) return false;
        save.Gold -= price;
        save.Materials[OreId] = OreCount(save) + count;
        return true;
    }

    public enum ForgeResult { Success, NoBlueprint, LowLevel, NoOre, BadSet }

    public static ForgeResult TryForge(SaveData save, string setId, string slotId, Random rng)
    {
        ProgressionService.EnsureDefaults(save);
        var set = EquipmentSets.Get(setId);
        if (!BlueprintPrices.ContainsKey(setId)) return ForgeResult.BadSet;
        if (!HasBlueprint(save, setId)) return ForgeResult.NoBlueprint;
        int mainLevel = ProgressionService.ProgressOf(save, "custom_hero").Level;
        if (mainLevel < set.Tier) return ForgeResult.LowLevel;
        int oreNeed = OrePerForge(setId);
        if (OreCount(save) < oreNeed) return ForgeResult.NoOre;
        save.Materials[OreId] = OreCount(save) - oreNeed;
        var item = new BagEquipmentItem
        {
            SlotId = slotId,
            Level = 0,
            RefineIndex = 0,
            Tier = set.Tier,
            Quality = set.Quality,
            SetId = setId,
        };
        // Fresh lines from the Middle pool (item is not equipped yet).
        while (item.RefineLines.Count < EquipmentSets.LinesForQuality(set.Quality))
            item.RefineLines.Add(ProgressionService.RollRefineLine(FormationPosition.Middle, rng));
        save.BagEquipment.Add(item);
        return ForgeResult.Success;
    }
}
