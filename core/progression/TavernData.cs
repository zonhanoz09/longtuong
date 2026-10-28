using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

public enum TavernTier { Lam, Tim, Cam, Do }
public enum TavernGrade { Normal, High }

/// <summary>
/// Phase 2.23: tavern data (static tables, like HeroDatabase).
/// - Tier per pool hero (custom hero has no tier: it is never recruited).
/// - Recruit cost in souls per tier: Lam 100, Tim 500, Cam 1500.
///   Do (red) generals come from events only, never from the tavern.
/// - Quality factors (temporary setup by Pax, 2.23): higher tier means
///   higher base stats AND faster per-level growth.
/// - Drink prices: Normal 100 Gold, High 300 Gold.
/// - Draw weights per grade (sum 100): Normal 70/25/5, High 60/30/10.
///   Cam generals only enter the draw pool once the custom hero (the
///   player level) reaches level 70; below that the Cam weight is 0.
/// </summary>
public static class TavernData
{
    public static readonly Dictionary<string, TavernTier> TierOfHero = new()
    {
        ["luu_bi"] = TavernTier.Lam,
        ["truong_phi"] = TavernTier.Lam,
        ["quan_vu"] = TavernTier.Tim,
        ["trieu_van"] = TavernTier.Tim,
        ["dieu_thuyen"] = TavernTier.Cam,
        ["gia_cat_luong"] = TavernTier.Cam,
        ["lu_bo"] = TavernTier.Cam,
    };

    public static TavernTier? TierOf(string heroId) =>
        TierOfHero.TryGetValue(heroId, out var t) ? t : null;

    public static List<string> PoolHeroIds => TierOfHero.Keys.ToList();

    public static List<string> HeroesOfTier(TavernTier tier) =>
        TierOfHero.Where(kv => kv.Value == tier).Select(kv => kv.Key).ToList();

    public static string TierName(TavernTier tier) => tier switch
    {
        TavernTier.Lam => "Lam",
        TavernTier.Tim => "Tím",
        TavernTier.Cam => "Cam",
        TavernTier.Do => "Đỏ",
        _ => "?",
    };

    public static int RecruitCost(TavernTier tier) => tier switch
    {
        TavernTier.Lam => 100,
        TavernTier.Tim => 500,
        TavernTier.Cam => 1500,
        _ => -1,
    };

    public static double StatFactor(TavernTier tier) => tier switch
    {
        TavernTier.Lam => 1.0,
        TavernTier.Tim => 1.15,
        TavernTier.Cam => 1.35,
        TavernTier.Do => 1.6,
        _ => 1.0,
    };

    public static double GrowthFactor(TavernTier tier) => StatFactor(tier);

    public static int DrinkPrice(TavernGrade grade) => grade == TavernGrade.High ? 300 : 100;

    public static string GradeName(TavernGrade grade) => grade == TavernGrade.High ? "Cao cấp" : "Thường";

    public static (int Lam, int Tim, int Cam) DrawWeights(TavernGrade grade) =>
        grade == TavernGrade.High ? (60, 30, 10) : (70, 25, 5);

    public const int CamGateLevel = 70;
    public const int SoulRollMin = 1;
    public const int SoulRollMax = 5;
}
