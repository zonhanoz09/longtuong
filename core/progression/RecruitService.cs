using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>Recruit (shard gacha). Rates are public and shown in the UI.
/// One draw costs 100 Gold and grants 5 shards of a single hero.</summary>
public static class RecruitService
{
    // Weights sum to 100.
    public static readonly List<(string HeroId, int Weight)> Rates = new()
    {
        ("quan_vu", 20), ("truong_phi", 20), ("trieu_van", 20),
        ("dieu_thuyen", 15), ("gia_cat_luong", 15), ("lu_bo", 10),
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
        heroId = "";
        if (save.Gold < ProgressionService.RecruitCostGold) return false;
        save.Gold -= ProgressionService.RecruitCostGold;
        heroId = DrawHero(rng);
        ProgressionService.AddShards(save, heroId, ProgressionService.RecruitShards);
        return true;
    }

    public static string RatesText() => string.Join(", ",
        Rates.Select(r => $"{HeroDatabase.Get(r.HeroId).Name} {r.Weight}%"));
}
