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
        // Phase 2.43: one new hero per campaign chapter (see UnlockChapter).
        ["ton_kien"] = TavernTier.Lam,
        ["dien_vi"] = TavernTier.Lam,
        ["gia_hu"] = TavernTier.Tim,
        ["truong_lieu"] = TavernTier.Tim,
        ["tu_thu"] = TavernTier.Tim,
        ["chu_du"] = TavernTier.Cam,
        ["chuc_dung"] = TavernTier.Cam,
        ["ma_sieu"] = TavernTier.Cam,
    };

    // Phase 2.43: a new hero enters the draw pool and can be recruited once
    // this campaign chapter is cleared. Heroes not listed are always open.
    public static readonly Dictionary<string, int> UnlockChapter = new()
    {
        ["ton_kien"] = 1, ["dien_vi"] = 2, ["gia_hu"] = 3, ["truong_lieu"] = 4,
        ["tu_thu"] = 5, ["chu_du"] = 6, ["chuc_dung"] = 7, ["ma_sieu"] = 8,
    };

    public static TavernTier? TierOf(string heroId) =>
        TierOfHero.TryGetValue(heroId, out var t) ? t : null;

    /// Every hero the tavern can ever offer (locked ones included).
    public static List<string> PoolHeroIds => TierOfHero.Keys.ToList();

    /// Phase 2.43: false while the hero's chapter is not cleared. New accounts
    /// therefore still draw from the original 7 heroes.
    public static bool IsInPool(SaveData save, string heroId) =>
        TierOfHero.ContainsKey(heroId)
        && (!UnlockChapter.TryGetValue(heroId, out var chapter) || HeroDatabase.ChapterCleared(save, chapter));

    public static List<string> HeroesOfTier(SaveData save, TavernTier tier) =>
        TierOfHero.Where(kv => kv.Value == tier && IsInPool(save, kv.Key)).Select(kv => kv.Key).ToList();

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

    // Drink prices in BẠC (Phase 2.25: was 100/300 Vàng in 2.23-2.24;
    // matches the original game's 150.000/300.000 bạc per Huynh).
    public static int DrinkPrice(TavernGrade grade) => grade == TavernGrade.High ? 300000 : 150000;
    // Phase 2.25: all-win drink (tất thắng x10) — flat price in VÀNG.
    public const int AllWinPrice = 200;
    public const int AllWinDrinks = 10;

    public static string GradeName(TavernGrade grade) => grade == TavernGrade.High ? "Cao cấp" : "Thường";

    public static (int Lam, int Tim, int Cam) DrawWeights(TavernGrade grade) =>
        grade == TavernGrade.High ? (60, 30, 10) : (70, 25, 5);

    public const int CamGateLevel = 70;
    // Phase 2.24: win soul roll is 1-3 (was 1-5 in 2.23).
    public const int SoulRollMin = 1;
    public const int SoulRollMax = 3;
}
