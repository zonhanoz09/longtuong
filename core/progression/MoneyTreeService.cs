using System;

namespace LongTuong;

/// <summary>
/// Phase 2.31a: Cây Rung Tiền — the minimal Bạc income source, built first
/// so the 2.31b enhance rework (Bạc costs) has an economy to live in
/// (Huynh's decision 2a). 10 free shakes per day, reset by local date;
/// each shake grants 5.000 + 1.000 × main-hero level Bạc (Pax's numbers —
/// Huynh adjusts freely).
/// </summary>
public static class MoneyTreeService
{
    public const int FreeShakesPerDay = 10;

    public static int ShakeReward(SaveData save)
    {
        ProgressionService.EnsureDefaults(save);
        int mainLevel = ProgressionService.ProgressOf(save, "custom_hero").Level;
        return 5000 + 1000 * mainLevel;
    }

    public static int ShakesLeft(SaveData save)
    {
        ProgressionService.EnsureDefaults(save);
        ResetIfNewDay(save);
        return Math.Max(0, FreeShakesPerDay - save.MoneyTreeUsed);
    }

    public static bool TryShake(SaveData save)
    {
        ProgressionService.EnsureDefaults(save);
        ResetIfNewDay(save);
        if (save.MoneyTreeUsed >= FreeShakesPerDay) return false;
        save.Silver += ShakeReward(save);
        save.MoneyTreeUsed++;
        return true;
    }

    private static void ResetIfNewDay(SaveData save)
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        if (save.MoneyTreeDate != today)
        {
            save.MoneyTreeDate = today;
            save.MoneyTreeUsed = 0;
        }
    }
}
