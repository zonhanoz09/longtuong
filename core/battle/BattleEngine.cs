using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Pure C# 3v3 side-view battle engine. Deterministic for a given seed:
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
    {
        Stage = stage;
        Seed = seed;
        _rng = new Random(seed);
        foreach (var def in playerDefs) PlayerFighters.Add(new Fighter(def, true));
        foreach (var id in stage.EnemyIds) EnemyFighters.Add(new Fighter(HeroDatabase.Get(id).WithScale(stage.Scale), false));
        RebuildOrder();
    }

    public static int CalculateDamage(Fighter attacker, Fighter defender, double power, bool critHit, bool dodged, double variance)
    {
        if (dodged) return 0;
        double dmg = attacker.Def.Atk * power * (100.0 / (100.0 + defender.Def.Def));
        if (HeroDatabase.Counters(attacker.Def.Role, defender.Def.Role)) dmg *= 1.25;
        if (critHit) dmg *= 1.5;
        dmg *= variance;
        dmg *= 1.0 - defender.Def.DamageReduction;
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
        _order = PlayerFighters.Concat(EnemyFighters).Where(f => f.Alive)
            .OrderByDescending(f => f.Def.Speed).ThenByDescending(f => f.IsPlayer ? 1 : 0).ToList();
        _orderIndex = 0;
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
            var targets = actor.Def.UltKind == UltKind.AoEDamage ? foes
                : new List<Fighter> { foes.OrderBy(f => f.Hp).First() };
            foreach (var t in targets) Hit(actor, t, power);
            return;
        }
        var foe = EnemiesOf(actor).Where(f => f.Alive).OrderBy(f => f.Hp).FirstOrDefault();
        if (foe == null) return;
        Hit(actor, foe, power);
        actor.Rage = Math.Min(100, actor.Rage + 20);
    }

    private void Hit(Fighter actor, Fighter target, double power)
    {
        bool dodged = _rng.NextDouble() < target.Def.Dodge;
        bool crit = !dodged && _rng.NextDouble() < actor.Def.Crit;
        double variance = 0.9 + _rng.NextDouble() * 0.2;
        int dmg = CalculateDamage(actor, target, power, crit, dodged, variance);
        if (dodged)
        {
            Log.Add($"{actor.Name} đánh {target.Name} nhưng bị NÉ!");
            return;
        }
        target.Hp = Math.Max(0, target.Hp - dmg);
        if (target.Alive) target.Rage = Math.Min(100, target.Rage + 10);
        Log.Add($"{actor.Name} gây {dmg} sát thương cho {target.Name}{(crit ? " (BẠO KÍCH!)" : "")} — {target.Name} còn {target.Hp} HP.");
        if (!target.Alive) Log.Add($"{target.Name} đã gục ngã!");
    }
}
