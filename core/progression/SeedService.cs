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
    public const int SeededGems = 10000;
    public const int UnlockShards = 10;

    public const string DefaultStarterHeroId = "custom_hero";

    public static string StarterHeroId(string username) => DefaultStarterHeroId;

    public static SaveData CreateSeededSave(string username)
    {
        var save = new SaveData();
        ProgressionService.EnsureDefaults(save);
        HeroDatabase.SetCustomHeroName(username);
        var starter = StarterHeroId(username);
        save.OwnedHeroIds = new List<string> { starter };
        save.TeamIds = new List<string> { starter };
        // Phase 2.5: starter starts with NO equipped items; the bag holds 6
        // basic items (one per slot) so equip -> unequip -> re-equip can be tested.
        save.BagEquipment = ProgressionService.Slots
            .Select(s => new BagEquipmentItem { SlotId = s.Id, Level = 0, RefineIndex = 0 })
            .ToList();
        save.Gold = SeededGold;
        save.Gems = SeededGems;
        return save;
    }
}
