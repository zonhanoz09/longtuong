using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.47: Ải Vượt Tướng — the main hero (custom_hero) alone vs one
/// enemy general per floor, 100 floors. Full HP every floor; the floor
/// reached is kept forever (no daily reset); only the next floor can be
/// fought, so each floor's reward (Bạc + Chiến tích) is paid once.
/// Numbers are Pax setup: Scale = 0.8 + 0.13 × floor (2.52 balance, was
/// 0.08 — floor 100 fell at ~Lv90; now ~Lv150); floor n pays
/// 500 × n Bạc + (10 + 2n) Chiến tích. Opponents cycle through the
/// enemy bosses, then the playable generals (never the custom hero).
/// </summary>
public static class DuelTowerService
{
    public const int MaxFloor = 100;
    public const string HeroId = "custom_hero";

    public static readonly List<string> Opponents =
        EnemyDatabase.Bosses.Select(b => b.Id)
            .Concat(HeroDatabase.Heroes.Where(h => h.Id != HeroId).Select(h => h.Id))
            .ToList();

    public static int NextFloor(SaveData save) => Math.Min(save.DuelFloor + 1, MaxFloor);
    public static bool IsFinished(SaveData save) => save.DuelFloor >= MaxFloor;

    public static string OpponentFor(int floor) => Opponents[(floor - 1) % Opponents.Count];
    public static double ScaleFor(int floor) => Math.Round(0.8 + 0.13 * floor, 2);

    public static StageDef StageForFloor(int floor) => new()
    {
        Id = 20000 + floor,
        Chapter = 0,
        Name = $"Ải Vượt Tướng — Tầng {floor}",
        EnemyIds = new List<string> { OpponentFor(floor) },
        Scale = ScaleFor(floor),
    };

    public static BattleReward RewardForFloor(int floor) =>
        new() { Silver = 500 * floor, ExpPerHero = 0, Merit = 10 + 2 * floor };

    /// "" when the next floor can be fought, else the reason.
    public static string LockReason(SaveData save)
    {
        if (!FeatureUnlockService.IsUnlocked(save, Feature.Duel)) return FeatureUnlockService.LockReason(save, Feature.Duel);
        if (IsFinished(save)) return "Đã vượt hết 100 tầng!";
        return "";
    }

    public static string Key(int floor) => floor.ToString();

    /// Settle a won floor. Only the next floor counts (a stale key pays nothing).
    public static BattleReward? Apply(SaveData save, string modeKey)
    {
        if (!int.TryParse(modeKey, out int floor) || floor != save.DuelFloor + 1 || floor > MaxFloor) return null;
        var reward = RewardForFloor(floor);
        StageRewardService.Apply(save, Array.Empty<string>(), reward);
        save.DuelFloor = floor;
        return reward;
    }
}
