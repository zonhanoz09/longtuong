using System.Collections.Generic;

namespace LongTuong;

/// <summary>
/// Phase 2.45: settles a WON non-campaign battle (GameManager.ApplyBattleResult
/// routes here when CurrentMode != Campaign). Each mode owns its rules; later
/// phases add Duel / Trial / Party. Never touches LastStage or StageStars.
/// </summary>
public static class ModeRewardService
{
    public static BattleReward? Apply(SaveData save, BattleMode mode, string modeKey, IEnumerable<string> teamIds, int seed)
    {
        ProgressionService.EnsureDefaults(save);
        return mode switch
        {
            BattleMode.Dungeon => DungeonService.Apply(save, modeKey, teamIds),
            BattleMode.Duel => DuelTowerService.Apply(save, modeKey),
            BattleMode.Trial => TrialTowerService.Apply(save, modeKey, StageRewardService.DropRng(seed)),
            BattleMode.Party => PartyService.Apply(save, modeKey, teamIds),
            _ => null,
        };
    }
}
