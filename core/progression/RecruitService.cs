using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>Recruit (gacha). Phase 2.22: the shard system was removed, and
/// the new recruit grant (direct hero unlock / star-up on duplicates) is a
/// later feature, so TryRecruit is temporarily disabled: it never charges
/// gold and always returns false. DrawHero + Rates stay as the (shard-free)
/// random draw core for that future rework, and rates are still shown in
/// the UI.</summary>
public static class RecruitService
{
    // Weights sum to 100.
    public static readonly List<(string HeroId, int Weight)> Rates = new()
    {
        // Phase 2.9: 7 heroes. Old 20s -> 18, old 15s -> 13, 10s stay 10.
        ("luu_bi", 10), ("quan_vu", 18), ("truong_phi", 18), ("trieu_van", 18),
        ("dieu_thuyen", 13), ("gia_cat_luong", 13), ("lu_bo", 10),
    };

    public static string DrawHero(Random rng)
    {
        int roll = rng.Next(100);
        int acc = 0;
        foreach (var (id, weight) in Rates)
        {
            acc += weight;
            if (roll < acc) return id;
        }
        return Rates[^1].HeroId;
    }

    public static bool TryRecruit(SaveData save, Random rng, out string heroId)
    {
        // Phase 2.22: recruit is disabled pending its rework (see the class
        // comment). Never charge gold while there is no grant to give.
        heroId = "";
        return false;
    }

    public static string RatesText() => string.Join(", ",
        Rates.Select(r => $"{HeroDatabase.Get(r.HeroId).Name} {r.Weight}%"));
}
