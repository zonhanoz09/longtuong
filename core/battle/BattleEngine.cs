using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Pure C# side-view battle engine (up to 5v5). Deterministic for a given seed:
/// same seed + same teams + same stage => same winner, turns and final HP.
/// </summary>
public class BattleEngine
{
    public List<Fighter> PlayerFighters { get; } = new();
    public List<Fighter> EnemyFighters { get; } = new();
    public List<string> Log { get; } = new();
    public int TurnCount { get; private set; }
    public int Seed { get; }
    public StageDef Stage { get; }

    private readonly Random _rng;
    private List<Fighter> _order = new();
    private int _orderIndex;

    public Fighter? Current => _orderIndex >= 0 && _orderIndex < _order.Count ? _order[_orderIndex] : null;
    public bool IsOver => PlayerFighters.All(f => !f.Alive) || EnemyFighters.All(f => !f.Alive);
    public bool PlayerWon => EnemyFighters.All(f => !f.Alive) && PlayerFighters.Any(f => f.Alive);

    public BattleEngine(List<string> playerIds, StageDef stage, int seed)
        : this(playerIds.Select(id => HeroDatabase.Get(id)).ToList(), stage, seed) { }

    public BattleEngine(List<HeroDef> playerDefs, StageDef stage, int seed)
        : this(playerDefs, null, stage, seed) { }

    // playerPositions parallels playerDefs (None when absent); enemy
    // positions come from the stage (StageDef.EnemySlot).
    public BattleEngine(List<HeroDef> playerDefs, List<FormationPosition>? playerPositions, StageDef stage, int seed)
    {
        Stage = stage;
        Seed = seed;
        _rng = new Random(seed);
        for (int i = 0; i < playerDefs.Count; i++)
        {
            var f = new Fighter(playerDefs[i], true);
            if (playerPositions != null && i < playerPositions.Count) f.Position = playerPositions[i];
            PlayerFighters.Add(f);
        }
        // Phase 2.39: enemies stand on the stage's formation slots and get the
        // same position rules as the player side.
        for (int i = 0; i < stage.EnemyIds.Count; i++)
            EnemyFighters.Add(new Fighter(HeroDatabase.Get(stage.EnemyIds[i]).WithScale(stage.Scale), false)
            {
                Position = FormationService.PositionOfSlot(stage.EnemySlot(i)),
            });
        RebuildOrder();
    }

    // Phase 2.38: combat formula per Huynh's spec (Long Tướng style).
    // Caps pick a value inside each range Huynh gave (tune freely):
    // né 80–85% -> 85% (always >= 15% hit), bạo 80–90% -> 90%,
    // đỡ đòn 80%, miễn thương thuần 75–80% -> 75%.
    public const double DodgeCap = 0.85;
    public const double CritCap = 0.90;
    public const double BlockCap = 0.80;
    public const double DamageReductionCap = 0.75;
    public const double BaseCritMultiplier = 1.5;
    public const double BlockMultiplier = 0.5;
    public const double MinDamageRatio = 0.05;   // chip damage = 5% of ATK
    public const double VarianceMin = 0.95, VarianceMax = 1.05;
    // Role counter (Võ > Mưu > Phụ > Võ) is this game's own rule, not part
    // of the spec; it multiplies the base damage.
    public const double CounterMultiplier = 1.25;

    public static double DodgeChance(Fighter attacker, Fighter defender) =>
        Math.Clamp(defender.Def.Dodge - attacker.Def.Hit, 0, DodgeCap);
    public static double CritChance(Fighter attacker, Fighter defender) =>
        Math.Clamp(attacker.Def.Crit - defender.Def.CritResist, 0, CritCap);
    public static double BlockChance(Fighter attacker, Fighter defender) =>
        Math.Clamp(defender.Def.Block - attacker.Def.Pierce, 0, BlockCap);

    // Base = max(5% ATK, (ATK - DEF) x skill ratio) x counter x (1 - DR capped)
    // Final = Base x crit (1.5 + bonus) x block (0.5) x variance [0.95, 1.05]
    public static int CalculateDamage(Fighter attacker, Fighter defender, double power, bool critHit, bool dodged, double variance, bool blocked = false)
    {
        if (dodged) return 0;
        double atk = attacker.EffectiveAtk;
        // Phase 2.39: Mưu/Phụ hit Sách phòng, others hit Vật phòng.
        double def = HeroDatabase.UsesStrategy(attacker.Def.Role) ? defender.EffectiveMagicDef : defender.EffectiveDef;
        double dmg = Math.Max(MinDamageRatio * atk, (atk - def) * power);
        if (HeroDatabase.Counters(attacker.Def.Role, defender.Def.Role)) dmg *= CounterMultiplier;
        dmg *= 1.0 - Math.Min(defender.Def.DamageReduction, DamageReductionCap);
        if (critHit) dmg *= BaseCritMultiplier + attacker.Def.CritDamage;
        if (blocked) dmg *= BlockMultiplier;
        dmg *= variance;
        return Math.Max(1, (int)Math.Round(dmg));
    }

    public static int StarsFor(bool won, double remainingHpPct, int turns)
    {
        if (!won) return 0;
        if (remainingHpPct >= 0.70 && turns <= 20) return 3;
        if (remainingHpPct >= 0.40) return 2;
        return 1;
    }

    public double PlayerRemainingHpPct()
    {
        var max = PlayerFighters.Sum(f => f.MaxHp);
        return max == 0 ? 0 : PlayerFighters.Sum(f => f.Hp) / (double)max;
    }

    public int Stars() => StarsFor(PlayerWon, PlayerRemainingHpPct(), TurnCount);

    private void RebuildOrder()
    {
        var alive = PlayerFighters.Concat(EnemyFighters).Where(f => f.Alive).ToList();
        // Phase 2.20 — Hậu quân: each alive rear hero has a 20% chance
        // (seeded RNG, so a given seed still replays deterministically) to act
        // first this round. First-movers go before everyone else, once each;
        // they do NOT get an extra action. Phase 2.39: enemy rear heroes too
        // (rolled after the player side, so stages without enemy Hậu keep
        // the old RNG sequence).
        var first = new List<Fighter>();
        foreach (var f in PlayerFighters.Concat(EnemyFighters).Where(f => f.Alive && f.Position == FormationPosition.Rear))
            if (_rng.NextDouble() < 0.20) first.Add(f);
        first = first.OrderByDescending(f => f.Def.Speed).ToList();
        var rest = alive.Where(f => !first.Contains(f))
            .OrderByDescending(f => f.Def.Speed).ThenByDescending(f => f.IsPlayer ? 1 : 0).ToList();
        _order = first.Concat(rest).ToList();
        _orderIndex = 0;
    }

    // Single-target choice. Phase 2.20 — Tiền quân: a single-target attack
    // hits an alive front (Tiền) foe first, else the lowest-HP foe.
    // Phase 2.39: applies to both sides (enemies now have positions).
    private Fighter? PickTarget(Fighter actor)
    {
        var foes = EnemiesOf(actor).Where(f => f.Alive).ToList();
        if (foes.Count == 0) return null;
        var front = foes.FirstOrDefault(f => f.Position == FormationPosition.Front);
        if (front != null) return front;
        return foes.OrderBy(f => f.Hp).First();
    }

    private void Advance()
    {
        if (IsOver) { _orderIndex = -1; return; }
        do
        {
            _orderIndex++;
            if (_orderIndex >= _order.Count) RebuildOrder();
        } while (Current != null && !Current.Alive && !IsOver);
        if (IsOver) _orderIndex = -1;
    }

    public bool ActCurrentNormal()
    {
        var actor = Current;
        if (actor == null || IsOver) return false;
        Execute(actor, false);
        if (!IsOver) Advance();
        return true;
    }

    public bool ActCurrentUltimate()
    {
        var actor = Current;
        if (actor == null || IsOver || actor.Rage < 100) return false;
        Execute(actor, true);
        if (!IsOver) Advance();
        return true;
    }

    // Dùng cho test và cho AI nội bộ: tung tuyệt kỹ của 1 tướng cụ thể mà không đổi lượt.
    public bool UseUltimate(Fighter actor)
    {
        if (actor.Rage < 100 || !actor.Alive) return false;
        Execute(actor, true);
        return true;
    }

    public void AutoRun(bool playerAutoUlt = true)
    {
        int guard = 0;
        while (!IsOver && guard++ < 500)
        {
            var actor = Current;
            if (actor == null) break;
            if (actor.Rage >= 100 && (actor.IsPlayer ? playerAutoUlt : true)) ActCurrentUltimate();
            else ActCurrentNormal();
        }
    }

    private List<Fighter> AlliesOf(Fighter f) => f.IsPlayer ? PlayerFighters : EnemyFighters;
    private List<Fighter> EnemiesOf(Fighter f) => f.IsPlayer ? EnemyFighters : PlayerFighters;

    private void Execute(Fighter actor, bool isUlt)
    {
        TurnCount++;
        double power = isUlt ? actor.Def.UltPower : 1.0;
        if (isUlt)
        {
            actor.Rage = 0;
            Log.Add($"{actor.Name} tung tuyệt kỹ {actor.Def.UltName}!");
            if (actor.Def.UltKind == UltKind.Heal)
            {
                var target = AlliesOf(actor).Where(f => f.Alive).OrderBy(f => f.Hp / (double)f.MaxHp).FirstOrDefault();
                if (target != null)
                {
                    int heal = (int)Math.Round(target.MaxHp * actor.Def.UltPower);
                    target.Hp = Math.Min(target.MaxHp, target.Hp + heal);
                    Log.Add($"{target.Name} hồi {heal} HP.");
                }
                return;
            }
            var foes = EnemiesOf(actor).Where(f => f.Alive).ToList();
            if (actor.Def.UltKind == UltKind.AoEDamage)
            {
                foreach (var t in foes) Hit(actor, t, power);
                return;
            }
            var single = PickTarget(actor);
            if (single == null) return;
            Hit(actor, single, power);
            return;
        }
        var foe = PickTarget(actor);
        if (foe == null) return;
        // Phase 2.38 (spec): a missed (dodged) attack gives no rage.
        if (Hit(actor, foe, power)) actor.Rage = Math.Min(100, actor.Rage + 20);
    }

    // Returns false when the attack was dodged.
    private bool Hit(Fighter actor, Fighter target, double power)
    {
        // Roll order per spec: dodge -> crit -> block -> variance. The block
        // roll is skipped when the chance is 0 so seeded battles without
        // any Block stat keep their old RNG sequence.
        bool dodged = _rng.NextDouble() < DodgeChance(actor, target);
        bool crit = !dodged && _rng.NextDouble() < CritChance(actor, target);
        double blockChance = BlockChance(actor, target);
        bool blocked = !dodged && blockChance > 0 && _rng.NextDouble() < blockChance;
        double variance = VarianceMin + _rng.NextDouble() * (VarianceMax - VarianceMin);
        int dmg = CalculateDamage(actor, target, power, crit, dodged, variance, blocked);
        if (dodged)
        {
            Log.Add($"{actor.Name} đánh {target.Name} nhưng bị NÉ!");
            return false;
        }
        target.Hp = Math.Max(0, target.Hp - dmg);
        if (target.Alive) target.Rage = Math.Min(100, target.Rage + 10);
        Log.Add($"{actor.Name} gây {dmg} sát thương cho {target.Name}{(crit ? " (BẠO KÍCH!)" : "")}{(blocked ? " (ĐỠ ĐÒN)" : "")} — {target.Name} còn {target.Hp} HP.");
        if (!target.Alive) Log.Add($"{target.Name} đã gục ngã!");
        return true;
    }
}
