using System;
using System.Collections.Generic;

namespace LongTuong;

/// <summary>
/// Phase 2.34: rèn (forge) — blueprints + Khoáng thạch (ore), both sold
/// for Vàng (dungeon/boss drops arrive later).
/// - Blueprint per set (permanent unlock once bought; Tân Thủ is the
///   starter set and needs none).
/// - Phase 2.37: Chế Tạo crafts Tân Thủ into an empty slot; Đúc Lại
///   upgrades an equipped item to the next set (blueprint + ore + Bạc,
///   main hero level >= target tier).
/// - Phase 2.38: the 2.34 "forge into the bag" flow (TryForge + the
///   Lò Rèn popup) was removed as dead code after 2.37b.
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

    // Phase 2.44: set fragments ("mảnh chế tạo") — dropped by dungeons,
    // bought with Chiến tích, and consumed by each Đúc Lại into that set
    // (Huynh 3 = b: the blueprint is still bought with Vàng). Stored in
    // Materials["frag_<setId>"]. Tân Thủ is never recast into, so it has none.
    public static string FragmentId(string setId) => "frag_" + setId;
    public static string FragmentName(string setId) => "Mảnh " + EquipmentSets.Get(setId).Name;

    // Fragments per recast INTO the set (Pax setup — Huynh tunes freely).
    public static readonly Dictionary<string, int> RecastFragmentCosts = new()
    {
        ["chu_tuoc"] = 10,
        ["bach_ho"] = 15,
        ["thanh_long"] = 20,
        ["huyen_vu"] = 25,
        ["ky_lan"] = 30,
    };

    public static int RecastFragmentCost(string targetSetId) =>
        RecastFragmentCosts.TryGetValue(targetSetId, out int n) ? n : 0;

    public static int FragmentCount(SaveData save, string setId)
    {
        ProgressionService.EnsureDefaults(save);
        return save.Materials.TryGetValue(FragmentId(setId), out int n) ? n : 0;
    }

    public static void AddFragments(SaveData save, string setId, int count)
    {
        ProgressionService.EnsureDefaults(save);
        save.Materials[FragmentId(setId)] = FragmentCount(save, setId) + count;
    }

    public static int RecastOreCost(string targetSetId) =>
        EquipmentSets.Get(targetSetId).Tier / 2;
    public static int RecastSilverCost(string targetSetId) =>
        EquipmentSets.Get(targetSetId).Tier * 1000;

    public enum RecastResult { Success, NoNext, LowLevel, NoBlueprint, NoOre, NoSilver, EmptySlot, NoFragments }

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
        int fragNeed = RecastFragmentCost(next);
        if (FragmentCount(save, next) < fragNeed) return RecastResult.NoFragments;
        int silverNeed = RecastSilverCost(next);
        if (save.Silver < silverNeed) return RecastResult.NoSilver;
        save.Materials[OreId] = OreCount(save) - oreNeed;
        save.Materials[FragmentId(next)] = FragmentCount(save, next) - fragNeed;
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
}
