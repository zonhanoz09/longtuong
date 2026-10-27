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
    // Phase 2.5: true = slot is empty (no item equipped). Defaults to false so
    // old saves (no such field in JSON) keep their equipment equipped.
    public bool IsEmpty { get; set; } = false;
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
    public const int RefineCostGold = 100;

    public static readonly List<EquipmentSlotDef> Slots = new()
    {
        new EquipmentSlotDef { Id = "weapon", Name = "Vũ khí", BaseAtk = 20, AtkPerLevel = 5 },
        new EquipmentSlotDef { Id = "armor", Name = "Giáp", BaseHp = 150, HpPerLevel = 30, BaseDef = 10, DefPerLevel = 2 },
        new EquipmentSlotDef { Id = "helmet", Name = "Mũ", BaseHp = 80, HpPerLevel = 15, BaseDef = 8, DefPerLevel = 1 },
        new EquipmentSlotDef { Id = "boots", Name = "Giày", BaseSpeed = 8, SpeedPerLevel = 1, BaseDef = 5, DefPerLevel = 1 },
        new EquipmentSlotDef { Id = "cloak", Name = "Choàng", BaseHp = 120, HpPerLevel = 25, BaseDef = 6, DefPerLevel = 1 },
        new EquipmentSlotDef { Id = "bracer", Name = "Uyển", BaseAtk = 12, AtkPerLevel = 3, BaseSpeed = 4, SpeedPerLevel = 1 },
    };

    // Refine bonus lines (1 per item, cycled): Crit, Dodge, Speed, ATK%, HP%.
    public static readonly string[] RefineNames =
        { "Bạo kích +3%", "Né tránh +3%", "Tốc độ +5", "Tấn công +10%", "HP +10%" };

    public static int ExpToNext(int level) => 100 + 50 * (level - 1);

    public static int EnhanceCost(int currentLevel) => 100 * (currentLevel + 1);

    public static void EnsureDefaults(SaveData save)
    {
        save.Heroes ??= new Dictionary<string, HeroProgress>();
        save.HeroEquipment ??= new Dictionary<string, Dictionary<string, EquipmentState>>();
        save.StageStars ??= new Dictionary<string, int>();
        save.OwnedHeroIds ??= new List<string>();
        save.BagEquipment ??= new List<BagEquipmentItem>();
        // Legacy saves (pre-2.2) have no OwnedHeroIds: they keep all heroes.
        if (save.OwnedHeroIds.Count == 0)
            save.OwnedHeroIds = HeroDatabase.Heroes.Select(h => h.Id).ToList();
        // Phase 2.22: the custom hero is the ONLY permanent hero — old
        // accounts missing it gain it on load. Every other hero (including
        // Luu Bi) is unlocked via recruit (a later feature), never granted
        // here. Its display name still comes from the username
        // (SetCustomHeroName in GameManager.LoadGame / seeded saves), so
        // this only grants ownership, never a name.
        if (!save.OwnedHeroIds.Contains("custom_hero"))
            save.OwnedHeroIds.Add("custom_hero");
        foreach (var hero in HeroDatabase.Heroes)
        {
            if (!save.Heroes.ContainsKey(hero.Id)) save.Heroes[hero.Id] = new HeroProgress();
            if (!save.HeroEquipment.ContainsKey(hero.Id))
            {
                var dict = new Dictionary<string, EquipmentState>();
                foreach (var slot in Slots) dict[slot.Id] = new EquipmentState { IsEmpty = true };
                save.HeroEquipment[hero.Id] = dict;
            }
            else foreach (var slot in Slots)
                if (!save.HeroEquipment[hero.Id].ContainsKey(slot.Id))
                    save.HeroEquipment[hero.Id][slot.Id] = new EquipmentState { IsEmpty = true };
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

    public static bool TryEnhance(SaveData save, string heroId, string slotId)
    {
        EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        int cost = EnhanceCost(eq.Level);
        if (eq.IsEmpty || eq.Level >= MaxEnhanceLevel || save.Gold < cost) return false;
        save.Gold -= cost;
        eq.Level++;
        return true;
    }

    public static bool TryRefine(SaveData save, string heroId, string slotId)
    {
        EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        if (eq.IsEmpty || save.Gold < RefineCostGold) return false;
        save.Gold -= RefineCostGold;
        eq.RefineIndex = (eq.RefineIndex + 1) % RefineNames.Length;
        return true;
    }

    public static int StarChartPoints(SaveData save) => save.StageStars.Values.Sum();

    /// <summary>Phase 2.18: dismiss a hero = remove it from the owned roster
    /// only. Equipped items return to the bag (never lost); level/stars are
    /// kept, so re-recruiting the hero later restores them. Blocked for the
    /// only permanent hero (custom: EnsureDefaults would restore it on the
    /// next load, so dismiss would be a no-op), for heroes not owned, and
    /// for the last owned hero (an empty roster cannot battle). Phase 2.22:
    /// Luu Bi is a normal hero now, so he CAN be dismissed.</summary>
    public static bool DismissHero(SaveData save, string heroId)
    {
        EnsureDefaults(save);
        save.TeamIds ??= new List<string>();
        if (!save.OwnedHeroIds.Contains(heroId)) return false;
        if (heroId == "custom_hero") return false;
        if (save.OwnedHeroIds.Count <= 1) return false;
        foreach (var slot in Slots)
        {
            var eq = save.HeroEquipment[heroId][slot.Id];
            if (!eq.IsEmpty)
            {
                save.BagEquipment.Add(new BagEquipmentItem { SlotId = slot.Id, Level = eq.Level, RefineIndex = eq.RefineIndex });
                eq.IsEmpty = true;
            }
        }
        save.OwnedHeroIds.Remove(heroId);
        save.TeamIds.Remove(heroId);
        // Phase 2.20: a dismissed hero also leaves the formation.
        if (save.Formation != null)
        {
            var slots = save.Formation.Where(kv => kv.Value == heroId).Select(kv => kv.Key).ToList();
            foreach (var slot in slots) save.Formation.Remove(slot);
            if (save.Formation.Count > 0) save.TeamIds = FormationService.FormationToTeam(save.Formation);
        }
        if (save.TeamIds.Count == 0 && save.OwnedHeroIds.Count > 0)
            save.TeamIds.Add(save.OwnedHeroIds[0]);
        return true;
    }

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
            if (eq.IsEmpty) continue;
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
