using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Seed data for NEW accounts (Phase 2.2, test seed — not final economy balance).
/// Phase 2.10: every new account starts with the custom hero, named after the
/// username (basic stats, no skills yet, not in the recruit pool).
/// </summary>
public static class SeedService
{
    public const int SeededGold = 100000;
    // Phase 2.25: starter Bạc (Pax setup — Huynh adjusts freely).
    // Enough for ~6 Normal drinks (150.000 each) so a new account can
    // use the tavern right away; Bạc income sources (money tree, tax,
    // bosses, dungeons) arrive with later phases.
    public const int SeededSilver = 1000000;
    // Phase 2.32: starter refine stones (Pax setup — Huynh adjusts freely).
    public const int SeededRefineStones = 10;

    public const string DefaultStarterHeroId = "custom_hero";

    public static string StarterHeroId(string username) => DefaultStarterHeroId;

    public static SaveData CreateSeededSave(string username)
    {
        var save = new SaveData();
        ProgressionService.EnsureDefaults(save);
        HeroDatabase.SetCustomHeroName(username);
        var starter = StarterHeroId(username);
        // Phase 2.22: a new account owns ONLY the custom hero. Every other
        // hero (including Luu Bi) is unlocked via recruit (a later feature).
        // Team stays [custom hero] (the default).
        save.OwnedHeroIds = new List<string> { starter };
        save.TeamIds = new List<string> { starter };
        // Phase 2.5: starter starts with NO equipped items; the bag holds 6
        // basic items (one per slot) so equip -> unequip -> re-equip can be tested.
        save.BagEquipment = ProgressionService.Slots
            .Select(s => new BagEquipmentItem
            {
                SlotId = s.Id, Level = 0, RefineIndex = 0,
                Tier = 30, Quality = "Lam", SetId = "tan_thu",
                // Phase 2.30: seed items are Tân Thủ (Lam = 3 lines), fixed lines.
                RefineLines = new List<RefineLine>
                {
                    new() { StatId = "crit", Value = 0.03 },
                    new() { StatId = "dodge", Value = 0.03 },
                    new() { StatId = "speed", Value = 5 },
                },
            })
            .ToList();
        save.Gold = SeededGold;
        save.RefineStones = SeededRefineStones;
        // Phase 2.33: starter gems (Pax setup — Huynh adjusts freely).
        save.GemInventory = new Dictionary<string, int>
        {
            [GemService.MakeGemId("speed", 1)] = 2,
            [GemService.MakeGemId("atk", 1)] = 2,
            [GemService.MakeGemId("hp", 1)] = 2,
        };
        return save;
    }
}
