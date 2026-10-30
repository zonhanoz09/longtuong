using System;
using System.Collections.Generic;

namespace LongTuong;

/// <summary>
/// Phase 2.49: Thí Luyện Tháp — 60 floors fought with the Bày trận team;
/// only gems drop (Huynh 3 = a: no chân khí yet). Floor kept forever, only
/// the next floor can be fought, so each floor pays once. Pax setup:
/// 3 enemies on floors 1–20, 4 on 21–40, 5 on 41–60; Scale = 1.0 + 0.22 ×
/// floor (2.52 balance, was 0.1 — floor 60 fell at ~Lv60; now ~Lv100);
/// every 10th floor has a boss. Reward = 1 gem of level
/// 1 + (floor − 1) / 10 (3 gems on floors divisible by 10); gem types are
/// rolled from the battle seed.
/// </summary>
public static class TrialTowerService
{
    public const int MaxFloor = 60;

    public static int NextFloor(SaveData save) => Math.Min(save.TrialFloor + 1, MaxFloor);
    public static bool IsFinished(SaveData save) => save.TrialFloor >= MaxFloor;

    public static int EnemyCount(int floor) => floor <= 20 ? 3 : floor <= 40 ? 4 : 5;
    public static double ScaleFor(int floor) => Math.Round(1.0 + 0.22 * floor, 2);
    public static int GemLevel(int floor) => 1 + (floor - 1) / 10;
    public static int GemCount(int floor) => floor % 10 == 0 ? 3 : 1;

    public static StageDef StageForFloor(int floor)
    {
        var soldiers = EnemyDatabase.Soldiers;
        var bosses = EnemyDatabase.Bosses;
        string soldier = soldiers[(floor - 1) % soldiers.Count].Id;
        var enemies = new List<string> { soldier };
        string? boss = floor % 10 == 0 ? bosses[(floor / 10 - 1) * 4 % bosses.Count].Id : null;
        // A named general on every floor (rotating), the boss on every 10th.
        enemies.Add(bosses[(floor * 3) % bosses.Count].Id);
        if (boss != null && !enemies.Contains(boss)) enemies.Add(boss);
        while (enemies.Count < EnemyCount(floor)) enemies.Add(soldier);
        return new StageDef
        {
            Id = 30000 + floor,
            Chapter = 0,
            Name = $"Thí Luyện Tháp — Tầng {floor}",
            EnemyIds = enemies.GetRange(0, EnemyCount(floor)),
            Scale = ScaleFor(floor),
            BossId = boss,
        };
    }

    public static BattleReward RewardForFloor(int floor, Random rng)
    {
        var reward = new BattleReward { Silver = 0, ExpPerHero = 0 };
        for (int i = 0; i < GemCount(floor); i++)
        {
            var def = GemService.Defs[rng.Next(GemService.Defs.Count)];
            var id = GemService.MakeGemId(def.Type, GemLevel(floor));
            reward.Drops.Add(new RewardDrop { Kind = StageRewardService.GemKind, ItemId = id, Name = GemService.DescribeGem(id) });
        }
        return reward;
    }

    public static string LockReason(SaveData save)
    {
        if (!FeatureUnlockService.IsUnlocked(save, Feature.Trial)) return FeatureUnlockService.LockReason(save, Feature.Trial);
        if (IsFinished(save)) return $"Đã vượt hết {MaxFloor} tầng!";
        return "";
    }

    public static string Key(int floor) => floor.ToString();

    public static BattleReward? Apply(SaveData save, string modeKey, Random rng)
    {
        if (!int.TryParse(modeKey, out int floor) || floor != save.TrialFloor + 1 || floor > MaxFloor) return null;
        var reward = RewardForFloor(floor, rng);
        StageRewardService.Apply(save, Array.Empty<string>(), reward);
        save.TrialFloor = floor;
        return reward;
    }
}
