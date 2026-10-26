using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

public class HeroProgress
{
    public int Level { get; set; } = 1;
    public int Exp { get; set; } = 0;
    public int Stars { get; set; } = 1;
}

public class EquipmentState
{
    public int Level { get; set; } = 0;
    public int RefineIndex { get; set; } = 0;
}

public class EquipmentSlotDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int BaseAtk { get; set; }
    public int BaseHp { get; set; }
    public int BaseDef { get; set; }
    public int BaseSpeed { get; set; }
    public int AtkPerLevel { get; set; }
    public int HpPerLevel { get; set; }
    public int DefPerLevel { get; set; }
    public int SpeedPerLevel { get; set; }
}

/// <summary>
/// Phase 2 progression. All formulas are public and data-driven here so balance
/// can be tuned without touching combat code.
/// - EXP to next level: 100 + 50*(level-1). Max level 60.
/// - Stars: 1..5. Star-up cost in shards: 10 * current stars.
/// - Stat scale: base * (1 + 0.04*(level-1)) * (1 + 0.25*(stars-1)).
/// - Equipment: 4 slots, enhance max 10, cost 100*(level+1) Gold.
/// - Refine: 1 bonus line per item, cycles through 5 options, cost 100 Gold.
/// - Star chart: 12 nodes, next node unlocks when total stage stars >= (unlocked+1)*2.
///   Each node: +3% HP and +3% ATK for the whole team.
/// Order in GetEffectiveHero: scaled base -> + equipment flat -> refine -> star-chart %.
/// </summary>
public static class ProgressionService
{
    public const int MaxLevel = 60;
    public const int MaxStars = 5;
    public const int MaxEnhanceLevel = 10;
    public const int StarChartNodes = 12;
    public const int RecruitCostGold = 100;
    public const int RecruitShards = 5;
    public const int RefineCostGold = 100;

    public static readonly List<EquipmentSlotDef> Slots = new()
    {
        new EquipmentSlotDef { Id = "weapon", Name = "Vũ khí", BaseAtk = 20, AtkPerLevel = 5 },
        new EquipmentSlotDef { Id = "armor", Name = "Giáp", BaseHp = 150, HpPerLevel = 30, BaseDef = 10, DefPerLevel = 2 },
        new EquipmentSlotDef { Id = "helmet", Name = "Mũ", BaseHp = 80, HpPerLevel = 15, BaseDef = 8, DefPerLevel = 1 },
        new EquipmentSlotDef { Id = "boots", Name = "Giày", BaseSpeed = 8, SpeedPerLevel = 1, BaseDef = 5, DefPerLevel = 1 },
    };

    // Refine bonus lines (1 per item, cycled): Crit, Dodge, Speed, ATK%, HP%.
    public static readonly string[] RefineNames =
        { "Bạo kích +3%", "Né tránh +3%", "Tốc độ +5", "Tấn công +10%", "HP +10%" };

    public static int ExpToNext(int level) => 100 + 50 * (level - 1);
    public static int StarUpCost(int currentStars) => 10 * currentStars;
    public static int EnhanceCost(int currentLevel) => 100 * (currentLevel + 1);

    public static void EnsureDefaults(SaveData save)
    {
        save.Heroes ??= new Dictionary<string, HeroProgress>();
        save.Shards ??= new Dictionary<string, int>();
        save.HeroEquipment ??= new Dictionary<string, Dictionary<string, EquipmentState>>();
        save.StageStars ??= new Dictionary<string, int>();
        foreach (var hero in HeroDatabase.Heroes)
        {
            if (!save.Heroes.ContainsKey(hero.Id)) save.Heroes[hero.Id] = new HeroProgress();
            if (!save.Shards.ContainsKey(hero.Id)) save.Shards[hero.Id] = 0;
            if (!save.HeroEquipment.ContainsKey(hero.Id))
            {
                var dict = new Dictionary<string, EquipmentState>();
                foreach (var slot in Slots) dict[slot.Id] = new EquipmentState();
                save.HeroEquipment[hero.Id] = dict;
            }
            else foreach (var slot in Slots)
                if (!save.HeroEquipment[hero.Id].ContainsKey(slot.Id))
                    save.HeroEquipment[hero.Id][slot.Id] = new EquipmentState();
        }
    }

    public static HeroProgress ProgressOf(SaveData save, string heroId)
    {
        EnsureDefaults(save);
        return save.Heroes[heroId];
    }

    public static void AddExp(SaveData save, string heroId, int amount)
    {
        var p = ProgressOf(save, heroId);
        if (p.Level >= MaxLevel) return;
        p.Exp += amount;
        while (p.Level < MaxLevel && p.Exp >= ExpToNext(p.Level))
        {
            p.Exp -= ExpToNext(p.Level);
            p.Level++;
        }
        if (p.Level >= MaxLevel) p.Exp = 0;
    }

    public static void AddShards(SaveData save, string heroId, int amount)
    {
        EnsureDefaults(save);
        save.Shards[heroId] += amount;
    }

    public static bool TryStarUp(SaveData save, string heroId)
    {
        var p = ProgressOf(save, heroId);
        int cost = StarUpCost(p.Stars);
        if (p.Stars >= MaxStars || save.Shards[heroId] < cost) return false;
        save.Shards[heroId] -= cost;
        p.Stars++;
        return true;
    }

    public static bool TryEnhance(SaveData save, string heroId, string slotId)
    {
        EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        int cost = EnhanceCost(eq.Level);
        if (eq.Level >= MaxEnhanceLevel || save.Gold < cost) return false;
        save.Gold -= cost;
        eq.Level++;
        return true;
    }

    public static bool TryRefine(SaveData save, string heroId, string slotId)
    {
        EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        if (save.Gold < RefineCostGold) return false;
        save.Gold -= RefineCostGold;
        eq.RefineIndex = (eq.RefineIndex + 1) % RefineNames.Length;
        return true;
    }

    public static int StarChartPoints(SaveData save) => save.StageStars.Values.Sum();
    public static bool TryUnlockStarNode(SaveData save)
    {
        if (save.StarNodesUnlocked >= StarChartNodes) return false;
        if (StarChartPoints(save) < (save.StarNodesUnlocked + 1) * 2) return false;
        save.StarNodesUnlocked++;
        return true;
    }

    public static HeroDef GetScaledBase(string heroId, int level, int stars)
    {
        var baseDef = HeroDatabase.Get(heroId);
        double levelFactor = 1 + 0.04 * (level - 1);
        double starFactor = 1 + 0.25 * (stars - 1);
        return new HeroDef
        {
            Id = baseDef.Id, Name = baseDef.Name, Role = baseDef.Role,
            Hp = (int)Math.Round(baseDef.Hp * levelFactor * starFactor),
            Atk = (int)Math.Round(baseDef.Atk * levelFactor * starFactor),
            Def = (int)Math.Round(baseDef.Def * levelFactor * starFactor),
            Speed = baseDef.Speed, Crit = baseDef.Crit, Dodge = baseDef.Dodge,
            DamageReduction = baseDef.DamageReduction,
            NormalName = baseDef.NormalName, UltName = baseDef.UltName,
            UltPower = baseDef.UltPower, UltKind = baseDef.UltKind,
            PassiveName = baseDef.PassiveName, PassiveDesc = baseDef.PassiveDesc,
        };
    }

    public static HeroDef GetEffectiveHero(SaveData save, string heroId)
    {
        var p = ProgressOf(save, heroId);
        var def = GetScaledBase(heroId, p.Level, p.Stars);
        double atkMul = 1.0, hpMul = 1.0;
        foreach (var slot in Slots)
        {
            var eq = save.HeroEquipment[heroId][slot.Id];
            def.Atk += slot.BaseAtk + slot.AtkPerLevel * eq.Level;
            def.Hp += slot.BaseHp + slot.HpPerLevel * eq.Level;
            def.Def += slot.BaseDef + slot.DefPerLevel * eq.Level;
            def.Speed += slot.BaseSpeed + slot.SpeedPerLevel * eq.Level;
            switch (eq.RefineIndex)
            {
                case 0: def.Crit += 0.03; break;
                case 1: def.Dodge += 0.03; break;
                case 2: def.Speed += 5; break;
                case 3: atkMul *= 1.10; break;
                case 4: hpMul *= 1.10; break;
            }
        }
        def.Atk = (int)Math.Round(def.Atk * atkMul);
        def.Hp = (int)Math.Round(def.Hp * hpMul);
        double chartMul = 1 + 0.03 * save.StarNodesUnlocked;
        def.Atk = (int)Math.Round(def.Atk * chartMul);
        def.Hp = (int)Math.Round(def.Hp * chartMul);
        return def;
    }
}
