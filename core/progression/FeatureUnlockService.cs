using System.Collections.Generic;

namespace LongTuong;

public enum Feature { Dungeon, Duel, Trial, Party }

/// <summary>
/// Phase 2.42: campaign-gated PvE modes. A mode opens only when BOTH
/// conditions hold (Huynh 2 = a): its campaign chapter is cleared AND the
/// main hero (custom_hero = player level) reaches the level. Pax setup.
/// Features that existed before 2.42 stay always open.
/// </summary>
public static class FeatureUnlockService
{
    public record Requirement(string Name, int Chapter, int Level);

    public static readonly Dictionary<Feature, Requirement> Requirements = new()
    {
        [Feature.Dungeon] = new("Phó Bản", 2, 30),
        [Feature.Duel] = new("Ải Vượt Tướng", 3, 20),
        [Feature.Trial] = new("Thí Luyện Tháp", 4, 25),
        [Feature.Party] = new("Chiến Dịch Tổ Đội", 5, 40),
    };

    public static bool IsUnlocked(SaveData save, Feature feature)
    {
        var req = Requirements[feature];
        return HeroDatabase.ChapterCleared(save, req.Chapter) && TavernService.PlayerLevel(save) >= req.Level;
    }

    /// "" when unlocked, else e.g. "Qua chương 3 và đạt Lv20".
    public static string LockReason(SaveData save, Feature feature)
    {
        if (IsUnlocked(save, feature)) return "";
        var req = Requirements[feature];
        return $"Qua chương {req.Chapter} và đạt Lv{req.Level}";
    }
}
