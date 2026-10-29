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

    // Phase 2.37: Đúc Lại (recast) — full flow per Huynh: the target
    // set's blueprint must be learned (not consumed) + ore + Bạc.
    // Replaces the 2.36 dev-only Bạc cost. The Nâng cấp tab is removed;
    // this now lives in the Cường hóa tab.
    public static readonly List<string> UpgradeChain = new()
    {
        "tan_thu", "chu_tuoc", "bach_ho", "thanh_long", "huyen_vu", "ky_lan",
    };

    public static string? NextSet(string setId)
    {
        int i = UpgradeChain.IndexOf(setId);
        return i >= 0 && i + 1 < UpgradeChain.Count ? UpgradeChain[i + 1] : null;
    }

    public static int RecastOreCost(string targetSetId) =>
        EquipmentSets.Get(targetSetId).Tier / 2;
    public static int RecastSilverCost(string targetSetId) =>
        EquipmentSets.Get(targetSetId).Tier * 1000;

    public enum RecastResult { Success, NoNext, LowLevel, NoBlueprint, NoOre, NoSilver, EmptySlot }

    public static RecastResult TryRecast(SaveData save, string heroId, string slotId, Random rng)
    {
        ProgressionService.EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        if (eq.IsEmpty) return RecastResult.EmptySlot;
        string? next = NextSet(eq.SetId);
        if (next == null) return RecastResult.NoNext;
        var target = EquipmentSets.Get(next);
        int mainLevel = ProgressionService.ProgressOf(save, "custom_hero").Level;
        if (mainLevel < target.Tier) return RecastResult.LowLevel;
        if (!HasBlueprint(save, next)) return RecastResult.NoBlueprint;
        int oreNeed = RecastOreCost(next);
        if (OreCount(save) < oreNeed) return RecastResult.NoOre;
        int silverNeed = RecastSilverCost(next);
        if (save.Silver < silverNeed) return RecastResult.NoSilver;
        save.Materials[OreId] = OreCount(save) - oreNeed;
        save.Silver -= silverNeed;
        eq.SetId = next;
        eq.Tier = target.Tier;
        eq.Quality = target.Quality;
        // Keep enhance level, unlocks and socketed gems; top up refine
        // lines to the new set's count (position pool); grow sockets
        // (new ones start locked).
        var pos = FormationService.PositionOfHero(save, heroId);
        while (eq.RefineLines.Count < EquipmentSets.LinesForSet(next))
            eq.RefineLines.Add(ProgressionService.RollRefineLine(pos, rng));
        GemService.EnsureSockets(eq);
        return RecastResult.Success;
    }

    // Phase 2.37: Chế Tạo — craft the starter Tân Thủ set directly into
    // an empty slot (Huynh 7b: only the lowest tier is craftable here).
    // No blueprint, no level gate; costs ore + Bạc like Đúc Lại.
    public const string CraftSetId = "tan_thu";
    public static int CraftOreCost() => EquipmentSets.Get(CraftSetId).Tier / 2;
    public static int CraftSilverCost() => EquipmentSets.Get(CraftSetId).Tier * 1000;

    public enum CraftResult { Success, SlotOccupied, NoOre, NoSilver }

    public static CraftResult TryCraftIntoSlot(SaveData save, string heroId, string slotId, Random rng)
    {
        ProgressionService.EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        if (!eq.IsEmpty) return CraftResult.SlotOccupied;
        int oreNeed = CraftOreCost();
        if (OreCount(save) < oreNeed) return CraftResult.NoOre;
        int silverNeed = CraftSilverCost();
        if (save.Silver < silverNeed) return CraftResult.NoSilver;
        save.Materials[OreId] = OreCount(save) - oreNeed;
        save.Silver -= silverNeed;
        var set = EquipmentSets.Get(CraftSetId);
        var pos = FormationService.PositionOfHero(save, heroId);
        var fresh = new EquipmentState
        {
            IsEmpty = false,
            Level = 0,
            SetId = CraftSetId,
            Tier = set.Tier,
            Quality = set.Quality,
        };
        while (fresh.RefineLines.Count < EquipmentSets.LinesForSet(CraftSetId))
            fresh.RefineLines.Add(ProgressionService.RollRefineLine(pos, rng));
        GemService.EnsureSockets(fresh);
        save.HeroEquipment[heroId][slotId] = fresh;
        return CraftResult.Success;
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
        while (item.RefineLines.Count < EquipmentSets.LinesForSet(setId))
            item.RefineLines.Add(ProgressionService.RollRefineLine(FormationPosition.Middle, rng));
        save.BagEquipment.Add(item);
        return ForgeResult.Success;
    }
}
