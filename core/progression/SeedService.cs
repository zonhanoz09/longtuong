using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Seed data for NEW accounts (Phase 2.2, test seed — not final economy balance).
/// Starter hero is deterministic from the username so the same name always gets
/// the same starter: index = (sum of char codes + 31 * length) % hero count.
/// </summary>
public static class SeedService
{
    public const int SeededGold = 100000;
    public const int SeededGems = 10000;
    public const int UnlockShards = 10;

    public static string StarterHeroId(string username)
    {
        if (string.IsNullOrEmpty(username)) return HeroDatabase.Heroes[0].Id;
        int sum = username.Sum(c => (int)c) + 31 * username.Length;
        return HeroDatabase.Heroes[((sum % HeroDatabase.Heroes.Count) + HeroDatabase.Heroes.Count) % HeroDatabase.Heroes.Count].Id;
    }

    public static SaveData CreateSeededSave(string username)
    {
        var save = new SaveData();
        ProgressionService.EnsureDefaults(save);
        var starter = StarterHeroId(username);
        save.OwnedHeroIds = new List<string> { starter };
        save.TeamIds = new List<string> { starter };
        save.Gold = SeededGold;
        save.Gems = SeededGems;
        return save;
    }
}
