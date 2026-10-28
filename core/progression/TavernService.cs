using System;
using System.Collections.Generic;

namespace LongTuong;

public enum RpsOutcome { Win, Draw, Lose }

public class RpsResult
{
    public int PlayerMove { get; set; }
    public int GeneralMove { get; set; }
    public RpsOutcome Outcome { get; set; }
    public int SoulsGained { get; set; }
}

/// <summary>
/// Phase 2.23: tavern (đối tửu + chiêu mộ) service. Replaces the old
/// RecruitService (gacha), deleted in this phase.
/// - StartDrink: charge the grade price, draw 3 generals by the grade's
///   tier weights (Cam weight 0 while the custom hero is below level 70).
/// - PlayRps: one rock-paper-scissors round against one general.
///   Win => +1-5 souls of that general (full roll, no shortfall);
///   Draw => a free rematch against the same general (the caller replays
///   without advancing); Lose => nothing, the general's turn ends.
/// - Recruit: spend souls to unlock a general into the owned roster.
/// Player level is the custom hero's level (no separate player EXP).
/// </summary>
public static class TavernService
{
    // RPS moves: 0 = Búa (rock), 1 = Kéo (scissors), 2 = Bao (paper).
    public static readonly string[] MoveNames = { "Búa", "Kéo", "Bao" };

    public static int PlayerLevel(SaveData save) =>
        ProgressionService.ProgressOf(save, "custom_hero").Level;

    public static bool StartDrink(SaveData save, TavernGrade grade, Random rng, out List<string> heroes)
    {
        heroes = new List<string>();
        ProgressionService.EnsureDefaults(save);
        int price = TavernData.DrinkPrice(grade);
        if (save.Gold < price) return false;
        save.Gold -= price;
        for (int i = 0; i < 3; i++) heroes.Add(DrawHero(save, grade, rng));
        return true;
    }

    public static string DrawHero(SaveData save, TavernGrade grade, Random rng)
    {
        var pool = TavernData.HeroesOfTier(DrawTier(save, grade, rng));
        return pool[rng.Next(pool.Count)];
    }

    public static TavernTier DrawTier(SaveData save, TavernGrade grade, Random rng)
    {
        var (lam, tim, cam) = TavernData.DrawWeights(grade);
        if (PlayerLevel(save) < TavernData.CamGateLevel) cam = 0;
        int total = lam + tim + cam;
        int roll = rng.Next(total);
        if (roll < lam) return TavernTier.Lam;
        if (roll < lam + tim) return TavernTier.Tim;
        return TavernTier.Cam;
    }

    public static RpsResult PlayRps(SaveData save, string heroId, int playerMove, Random rng)
    {
        ProgressionService.EnsureDefaults(save);
        int generalMove = rng.Next(3);
        var outcome = playerMove == generalMove ? RpsOutcome.Draw
            : (playerMove, generalMove) is (0, 1) or (1, 2) or (2, 0) ? RpsOutcome.Win
            : RpsOutcome.Lose;
        int souls = 0;
        if (outcome == RpsOutcome.Win)
        {
            souls = rng.Next(TavernData.SoulRollMin, TavernData.SoulRollMax + 1);
            save.GeneralSouls[heroId] += souls;
        }
        return new RpsResult { PlayerMove = playerMove, GeneralMove = generalMove, Outcome = outcome, SoulsGained = souls };
    }

    public static int SoulsOf(SaveData save, string heroId)
    {
        ProgressionService.EnsureDefaults(save);
        return save.GeneralSouls.TryGetValue(heroId, out var v) ? v : 0;
    }

    public static int RecruitCostOf(string heroId)
    {
        var tier = TavernData.TierOf(heroId);
        return tier == null ? -1 : TavernData.RecruitCost(tier.Value);
    }

    public static bool CanRecruit(SaveData save, string heroId)
    {
        ProgressionService.EnsureDefaults(save);
        if (save.OwnedHeroIds.Contains(heroId)) return false;
        int cost = RecruitCostOf(heroId);
        return cost > 0 && SoulsOf(save, heroId) >= cost;
    }

    public static bool Recruit(SaveData save, string heroId)
    {
        if (!CanRecruit(save, heroId)) return false;
        save.GeneralSouls[heroId] -= RecruitCostOf(heroId);
        save.OwnedHeroIds.Add(heroId);
        return true;
    }
}
