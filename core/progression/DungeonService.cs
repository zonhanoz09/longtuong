using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.45: dungeon rules. Gates (all must hold): the Phó Bản feature is
/// unlocked (chapter 2 + Lv30), the main hero reaches the dungeon's level,
/// and Khó needs Thường to have been cleared once. A run walks stages 1..4
/// in order; winning the last stage resets the run to stage 1 (so the
/// dungeon can be farmed again). Losing keeps the current stage and costs
/// nothing. No daily limit (Huynh 1 = b).
/// </summary>
public static class DungeonService
{
    public const int SilverPerWin = 2000;

    public static string Key(DungeonDef d, DungeonDifficulty diff) => $"{d.Id}:{(diff == DungeonDifficulty.Hard ? "hard" : "normal")}";
    public static string StageKey(DungeonDef d, DungeonDifficulty diff, int index) => $"{Key(d, diff)}:{index}";

    public static bool IsCleared(SaveData save, DungeonDef d, DungeonDifficulty diff)
    {
        ProgressionService.EnsureDefaults(save);
        return save.DungeonProgress.TryGetValue(Key(d, diff) + ":done", out var v) && v > 0;
    }

    /// Index (0..3) of the stage the next battle fights.
    public static int NextStageIndex(SaveData save, DungeonDef d, DungeonDifficulty diff)
    {
        ProgressionService.EnsureDefaults(save);
        return save.DungeonProgress.TryGetValue(Key(d, diff), out var v) ? Math.Clamp(v, 0, DungeonDatabase.StageCount - 1) : 0;
    }

    /// "" when the dungeon can be entered, else the reason (UI text).
    public static string LockReason(SaveData save, DungeonDef d, DungeonDifficulty diff)
    {
        if (!FeatureUnlockService.IsUnlocked(save, Feature.Dungeon)) return FeatureUnlockService.LockReason(save, Feature.Dungeon);
        if (TavernService.PlayerLevel(save) < d.MinLevel) return $"Cần cấp chủ tướng Lv{d.MinLevel}";
        if (diff == DungeonDifficulty.Hard && !IsCleared(save, d, DungeonDifficulty.Normal)) return "Cần qua độ khó Thường trước";
        return "";
    }

    public static bool CanEnter(SaveData save, DungeonDef d, DungeonDifficulty diff) => LockReason(save, d, diff) == "";

    /// The generated battle stage. Ids are far above the campaign's 1..70.
    public static StageDef StageFor(DungeonDef d, DungeonDifficulty diff, int index)
    {
        var def = d.Stages[index];
        double scale = (d.BaseScale + 0.1 * index) * (diff == DungeonDifficulty.Hard ? DungeonDatabase.HardMultiplier : 1.0);
        int dungeonIdx = DungeonDatabase.All.IndexOf(d);
        return new StageDef
        {
            Id = 10000 + dungeonIdx * 100 + (diff == DungeonDifficulty.Hard ? 10 : 0) + index,
            Chapter = 0,
            Name = $"{d.Name} — {def.Name}",
            EnemyIds = new List<string>(def.EnemyIds),
            Scale = Math.Round(scale, 3),
            BossId = def.BossId,
        };
    }

    /// Fragments dropped by a win: 1 per stage, 3 on the boss stage; Khó doubles.
    public static int FragmentsFor(int index, DungeonDifficulty diff) =>
        (index == DungeonDatabase.StageCount - 1 ? 3 : 1) * (diff == DungeonDifficulty.Hard ? 2 : 1);

    public static BattleReward Roll(DungeonDef d, DungeonDifficulty diff, int index)
    {
        var reward = new BattleReward { Silver = SilverPerWin, ExpPerHero = 100 + 5 * d.MinLevel };
        int n = FragmentsFor(index, diff);
        reward.Drops.Add(new RewardDrop
        {
            Kind = StageRewardService.MaterialKind, ItemId = ForgeService.FragmentId(d.FragmentSetId),
            Name = ForgeService.FragmentName(d.FragmentSetId), Count = n,
        });
        return reward;
    }

    /// Settle a won dungeon battle. `modeKey` = "<dungeon>:<diff>:<stage index>".
    public static BattleReward? Apply(SaveData save, string modeKey, IEnumerable<string> teamIds)
    {
        var parts = modeKey.Split(':');
        if (parts.Length != 3 || !int.TryParse(parts[2], out int index)) return null;
        var d = DungeonDatabase.All.FirstOrDefault(x => x.Id == parts[0]);
        if (d == null) return null;
        var diff = parts[1] == "hard" ? DungeonDifficulty.Hard : DungeonDifficulty.Normal;
        var reward = Roll(d, diff, index);
        StageRewardService.Apply(save, teamIds, reward);
        string key = Key(d, diff);
        if (index >= DungeonDatabase.StageCount - 1)
        {
            save.DungeonProgress[key] = 0;           // run over: start again from stage 1
            save.DungeonProgress[key + ":done"] = 1; // Khó can now be entered
        }
        else save.DungeonProgress[key] = Math.Max(NextStageIndex(save, d, diff), index + 1);
        return reward;
    }
}
