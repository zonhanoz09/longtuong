using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

public class PartyTeam
{
    public string Label { get; set; } = "";
    public bool IsBot { get; set; }
    public List<HeroDef> Heroes { get; set; } = new();
    public List<FormationPosition> Positions { get; set; } = new();
}

public class PartyRun
{
    public string Label { get; set; } = "";
    public int BossHpBefore { get; set; }
    public int BossHpAfter { get; set; }
    public int Damage => BossHpBefore - BossHpAfter;
    public int Turns { get; set; }
}

public class PartyResult
{
    public bool Won { get; set; }
    public int BossMaxHp { get; set; }
    public List<PartyRun> Runs { get; set; } = new();
}

/// <summary>
/// Phase 2.50: Chiến Dịch Tổ Đội (single-player version, Huynh 1 = a):
/// the player's formation team + 2 bot teams sized to 92–108% of the
/// player's lực chiến take turns against ONE boss whose HP carries over;
/// each team gets MaxTurnsPerTeam turns. Bosses open one after another.
/// No daily limit. Pax setup: boss HP x8; a win pays 5.000 Bạc +
/// 100 Chiến tích + 5 fragments of the boss's set; EXP only for the
/// player's team.
/// </summary>
public static class PartyService
{
    public record PartyBoss(string Id, double Scale, string FragmentSetId);

    // Phase 2.52 balance: Scale 5.6/9.3/8.1/10.0 (was 3.0/4.5/6.0/7.5) so the
    // bosses fall near Lv40/55/70/90 in the 2.52 simulation. Lữ Bố needs a
    // higher Scale than his place in the order suggests: his Vật/Sách phòng
    // (20/15) are the lowest of the four, so he melts at equal Scale.
    public static readonly List<PartyBoss> Bosses = new()
    {
        new("truong_giac", 5.6, "chu_tuoc"),
        new("lu_bo", 9.3, "bach_ho"),
        new("vien_thieu", 8.1, "thanh_long"),
        new("tao_thao", 10.0, "huyen_vu"),
    };

    public const int Teams = 3;
    public const int MaxTurnsPerTeam = 30;
    public const double BossHpMultiplier = 8.0;
    public const int SilverPerWin = 5000, MeritPerWin = 100, FragmentsPerWin = 5, ExpPerWin = 300;
    public const double BotPowerMin = 0.92, BotPowerMax = 1.08;

    // Bot heroes stand on the same slots enemies use (Tiền giữa first).
    private static readonly string[] BotSlots = HeroDatabase.DefaultEnemySlots;

    public static PartyBoss GetBoss(string id) => Bosses.First(b => b.Id == id);

    public static bool IsBossOpen(SaveData save, string bossId)
    {
        ProgressionService.EnsureDefaults(save);
        int i = Bosses.FindIndex(b => b.Id == bossId);
        if (i < 0 || !FeatureUnlockService.IsUnlocked(save, Feature.Party)) return false;
        return i == 0 || save.PartyCleared.TryGetValue(Bosses[i - 1].Id, out var n) && n > 0;
    }

    public static string LockReason(SaveData save, string bossId)
    {
        if (!FeatureUnlockService.IsUnlocked(save, Feature.Party)) return FeatureUnlockService.LockReason(save, Feature.Party);
        if (IsBossOpen(save, bossId)) return "";
        int i = Bosses.FindIndex(b => b.Id == bossId);
        return i > 0 ? $"Cần đánh bại {HeroDatabase.GetCombatant(Bosses[i - 1].Id).Name} trước" : "Không có boss này";
    }

    public static StageDef BossStage(string bossId)
    {
        var boss = GetBoss(bossId);
        return new StageDef
        {
            Id = 40000 + Bosses.IndexOf(boss),
            Chapter = 0,
            Name = $"Tổ Đội — {HeroDatabase.GetCombatant(bossId).Name}",
            EnemyIds = new List<string> { bossId },
            Scale = boss.Scale,
            EnemyHpMultiplier = BossHpMultiplier,
        };
    }

    public static PartyTeam PlayerTeam(SaveData save)
    {
        FormationService.EnsureFormation(save);
        var ids = FormationService.FormationToTeam(save.Formation).Where(id => save.OwnedHeroIds.Contains(id)).ToList();
        return new PartyTeam
        {
            Label = "Đội 1/3 (Bạn)",
            Heroes = ids.Select(id => ProgressionService.GetEffectiveHero(save, id)).ToList(),
            Positions = ids.Select(id => FormationService.PositionOfHero(save, id)).ToList(),
        };
    }

    /// Bot team `index` (1 or 2): 5 random playable heroes (never the custom
    /// hero), scaled so the team's power is 92–108% of the player's team.
    public static PartyTeam BuildBotTeam(SaveData save, int index, int seed)
    {
        var rng = new Random(unchecked(seed * 7 + index * 131));
        var pool = HeroDatabase.Heroes.Where(h => h.Id != "custom_hero").ToList();
        var picks = pool.OrderBy(_ => rng.Next()).Take(5).ToList();
        double target = PowerService.TeamPower(PlayerTeam(save).Heroes) * (BotPowerMin + rng.NextDouble() * (BotPowerMax - BotPowerMin));
        // Power(WithScale(f)) = f × A + B per hero (Speed is not scaled).
        double a = picks.Sum(h => (h.Hp * 0.2 + h.Atk + h.Def + h.MagicDef) * (1 + h.Crit + h.Dodge));
        double b = picks.Sum(h => h.Speed * 0.5 * (1 + h.Crit + h.Dodge));
        double f = Math.Max(0.1, (target - b) / a);
        return new PartyTeam
        {
            Label = $"Đội {index + 1}/3 (Bot)",
            IsBot = true,
            Heroes = picks.Select(h => h.WithScale(f)).ToList(),
            Positions = picks.Select((_, i) => FormationService.PositionOfSlot(BotSlots[i])).ToList(),
        };
    }

    public static PartyTeam TeamFor(SaveData save, int index, int seed) =>
        index == 0 ? PlayerTeam(save) : BuildBotTeam(save, index, seed);

    public static int SeedFor(int seed, int index) => unchecked(seed + index * 101);

    public static BattleEngine EngineFor(PartyTeam team, string bossId, int seed, int index, int bossHp) =>
        new(team.Heroes, team.Positions, BossStage(bossId), SeedFor(seed, index), MaxTurnsPerTeam, new[] { bossHp });

    /// Pure simulation of the 3 runs (same engines the battle screen drives).
    public static PartyResult Simulate(SaveData save, string bossId, int seed)
    {
        var first = new BattleEngine(new List<HeroDef>(), BossStage(bossId), 0);
        int bossHp = first.EnemyFighters[0].MaxHp;
        var result = new PartyResult { BossMaxHp = bossHp };
        for (int i = 0; i < Teams && bossHp > 0; i++)
        {
            var team = TeamFor(save, i, seed);
            var engine = EngineFor(team, bossId, seed, i, bossHp);
            engine.AutoRun();
            int after = engine.EnemyFighters[0].Hp;
            result.Runs.Add(new PartyRun { Label = team.Label, BossHpBefore = bossHp, BossHpAfter = after, Turns = engine.TurnCount });
            bossHp = after;
        }
        result.Won = bossHp <= 0;
        return result;
    }

    public static BattleReward Roll(string bossId)
    {
        var boss = GetBoss(bossId);
        var reward = new BattleReward { Silver = SilverPerWin, ExpPerHero = ExpPerWin, Merit = MeritPerWin };
        reward.Drops.Add(new RewardDrop
        {
            Kind = StageRewardService.MaterialKind, ItemId = ForgeService.FragmentId(boss.FragmentSetId),
            Name = ForgeService.FragmentName(boss.FragmentSetId), Count = FragmentsPerWin,
        });
        return reward;
    }

    /// Settle a won Tổ Đội battle (modeKey = boss id).
    public static BattleReward? Apply(SaveData save, string bossId, IEnumerable<string> playerTeamIds)
    {
        if (Bosses.All(b => b.Id != bossId)) return null;
        var reward = Roll(bossId);
        StageRewardService.Apply(save, playerTeamIds, reward);
        save.PartyCleared[bossId] = (save.PartyCleared.TryGetValue(bossId, out var n) ? n : 0) + 1;
        return reward;
    }
}
