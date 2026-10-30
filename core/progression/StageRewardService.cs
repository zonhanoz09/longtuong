using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

public class RewardDrop
{
    public string Kind { get; set; } = "";   // "ore" / "stone" / "gem"
    public string ItemId { get; set; } = ""; // gem id for gems
    public int Count { get; set; } = 1;
    public string Name { get; set; } = "";
}

public class BattleReward
{
    public int Silver { get; set; }
    public int ExpPerHero { get; set; }
    // Phase 2.47: Chiến tích (merit) — Ải Vượt Tướng / Tổ Đội rewards.
    public int Merit { get; set; }
    public List<RewardDrop> Drops { get; set; } = new();
}

/// <summary>
/// Phase 2.39 — vòng chơi ải (Huynh's decisions 2026-10-30):
/// - Every win (any stage, replays included) gives 1.000 Bạc; Vàng is no
///   longer a battle reward.
/// - EXP per hero = 40 + 10 × stage (unchanged formula; chapter 2 stages
///   give more).
/// - Drops: three independent 10% rolls — 1 Khoáng thạch, 1 Đá tẩy luyện,
///   1 random Lv1 gem.
/// - Quét ải: a 3-star stage can be swept for the same reward without
///   fighting, SweepsPerDay times per local day (Pax's number — sweeps
///   are instant, so without a cap Bạc would be unlimited; Huynh tunes).
/// Drops are rolled from a Random derived from the battle seed, so a
/// battle replayed with the same seed gives the same drops.
/// </summary>
public static class StageRewardService
{
    public const int SilverPerWin = 1000;
    public const double DropChance = 0.10;
    public const int SweepsPerDay = 20;
    // Phase 2.44: "material" = any Materials[itemId] (set fragments).
    public const string OreKind = "ore", StoneKind = "stone", GemKind = "gem", MaterialKind = "material";

    public static int ExpPerHero(int stageId) => 40 + 10 * stageId;

    public static Random DropRng(int seed) => new(unchecked(seed * 31 + 17));

    public static BattleReward Roll(int stageId, Random rng)
    {
        var reward = new BattleReward { Silver = SilverPerWin, ExpPerHero = ExpPerHero(stageId) };
        // Fixed roll order (ore, stone, gem) keeps seeded drops reproducible.
        if (rng.NextDouble() < DropChance)
            reward.Drops.Add(new RewardDrop { Kind = OreKind, ItemId = ForgeService.OreId, Name = ForgeService.OreName });
        if (rng.NextDouble() < DropChance)
            reward.Drops.Add(new RewardDrop { Kind = StoneKind, ItemId = "refine_stone", Name = "Đá tẩy luyện" });
        if (rng.NextDouble() < DropChance)
        {
            var gem = GemService.Defs[rng.Next(GemService.Defs.Count)];
            var id = GemService.MakeGemId(gem.Type, 1);
            reward.Drops.Add(new RewardDrop { Kind = GemKind, ItemId = id, Name = GemService.DescribeGem(id) });
        }
        return reward;
    }

    public static void Apply(SaveData save, IEnumerable<string> teamIds, BattleReward reward)
    {
        ProgressionService.EnsureDefaults(save);
        save.Silver += reward.Silver;
        save.Merit += reward.Merit;
        foreach (var heroId in teamIds)
            ProgressionService.AddExp(save, heroId, reward.ExpPerHero);
        foreach (var d in reward.Drops)
        {
            switch (d.Kind)
            {
                case OreKind: save.Materials[ForgeService.OreId] = ForgeService.OreCount(save) + d.Count; break;
                case StoneKind: save.RefineStones += d.Count; break;
                case GemKind: GemService.AddGems(save, d.ItemId, d.Count); break;
                case MaterialKind:
                    save.Materials[d.ItemId] = (save.Materials.TryGetValue(d.ItemId, out var have) ? have : 0) + d.Count;
                    break;
            }
        }
    }

    public static int SweepsLeft(SaveData save)
    {
        ResetSweepIfNewDay(save);
        return Math.Max(0, SweepsPerDay - save.SweepUsed);
    }

    public static bool CanSweep(SaveData save, int stageId) =>
        save.StageStars != null && save.StageStars.TryGetValue(stageId.ToString(), out var stars) && stars >= 3;

    public enum SweepResult { Success, NotThreeStars, NoSweepsLeft, NoTeam }

    public static SweepResult TrySweep(SaveData save, List<string> teamIds, int stageId, Random rng, out BattleReward? reward)
    {
        reward = null;
        ProgressionService.EnsureDefaults(save);
        if (teamIds.Count == 0) return SweepResult.NoTeam;
        if (!CanSweep(save, stageId)) return SweepResult.NotThreeStars;
        if (SweepsLeft(save) <= 0) return SweepResult.NoSweepsLeft;
        reward = Roll(stageId, rng);
        Apply(save, teamIds, reward);
        save.SweepUsed++;
        return SweepResult.Success;
    }

    public static string Describe(BattleReward r)
    {
        var parts = new List<string> { $"+{r.Silver:N0} Bạc", $"+{r.ExpPerHero} EXP mỗi tướng" };
        if (r.Merit > 0) parts.Add($"+{r.Merit} Chiến tích");
        parts.AddRange(r.Drops.Select(d => $"+{d.Count} {d.Name}"));
        return string.Join(", ", parts);
    }

    private static void ResetSweepIfNewDay(SaveData save)
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        if (save.SweepDate != today) { save.SweepDate = today; save.SweepUsed = 0; }
    }
}
