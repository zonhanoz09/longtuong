using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

public class HeroProgress
{
    public int Level { get; set; } = 1;
    public int Exp { get; set; } = 0;
    // Phase 2.23: hero stars were removed from the game entirely
    // (no Stars field, no star scaling, no "Sao X/5" display).
}

public class EquipmentState
{
    public int Level { get; set; } = 0;
    public int RefineIndex { get; set; } = 0;
    // Phase 2.5: true = slot is empty (no item equipped). Defaults to false so
    // old saves (no such field in JSON) keep their equipment equipped.
    public bool IsEmpty { get; set; } = false;
    // Phase 2.30: equipment sets (Huynh's spec). Old saves get the Tân Thủ
    // set via migration in EnsureDefaults.
    public int Tier { get; set; } = 30;
    public string Quality { get; set; } = "Lam";
    public string SetId { get; set; } = "tan_thu";
    public List<EquipmentSocket> Sockets { get; set; } = new();
    public List<RefineLine> RefineLines { get; set; } = new();
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
/// - EXP to next level: 100 + 50*(level-1). Max level 180 (raised from 60
///   in 2.23 so the tavern's Cam gate at custom-hero level 70 is reachable).
/// - Quality (2.23, TavernData): higher tier => higher base stats and faster
///   per-level growth. Stat scale: base * qualityStat * (1 + 0.04 * qualityGrowth
///   * (level-1)). Only HP/ATK/DEF scale (Speed/Crit/Dodge stay at base, as
///   with the old level/star scaling). Heroes without a tier (custom hero)
///   use factors 1.0/1.0.
/// - Equipment: 6 slots; enhance costs 100*2^level Bạc (2.31b), max follows
///   the main hero's level, success 100% to +7 then 90/80/70%.
/// - Refine: lines per item (3 Lam / 4 Tím / 5 Cam / 6 Đỏ); the old single
///   option cycles the first line until 2.32. Cost 100 Gold.
/// - Sets (2.30): 6 level-tier sets with 2/4/6-piece bonuses.
/// - Star chart: 12 nodes, next node unlocks when total stage stars >= (unlocked+1)*2.
///   Each node: +3% HP and +3% ATK for the whole team. (The star chart is
///   NOT hero stars: hero stars were removed in 2.23, the chart stays.)
/// Order in GetEffectiveHero: scaled base -> + equipment flat -> refine -> star-chart %.
/// </summary>
public static class ProgressionService
{
    public const int MaxLevel = 180;
    public const int MaxEnhanceLevel = 10;
    public const int StarChartNodes = 12;

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

    // Phase 2.31b: enhance costs Bạc (Huynh's currency spec + decision 2a).
    // Cost grows geometrically: 100 x 2^level. Max enhance follows the main
    // hero's level; high levels have a success roll (fail = Bạc lost).
    public enum EnhanceResult { Success, Failed_Empty, Failed_Maxed, Failed_NoSilver, Failed_Roll }

    public static int EnhanceCostSilver(int currentLevel) => 100 * (1 << currentLevel);

    public static int MaxEnhanceForHero(SaveData save)
    {
        EnsureDefaults(save);
        return Math.Min(MaxEnhanceLevel, ProgressOf(save, "custom_hero").Level);
    }

    public static double EnhanceSuccessRate(int targetLevel) => targetLevel switch
    {
        <= 7 => 1.0,
        8 => 0.9,
        9 => 0.8,
        _ => 0.7,
    };

    // Phase 2.38: set only by EquipmentService.ProjectEquipChange.
    internal static bool SuppressEquipmentMigration;

    public static void EnsureDefaults(SaveData save)
    {
        save.Heroes ??= new Dictionary<string, HeroProgress>();
        save.HeroEquipment ??= new Dictionary<string, Dictionary<string, EquipmentState>>();
        save.StageStars ??= new Dictionary<string, int>();
        save.OwnedHeroIds ??= new List<string>();
        save.BagEquipment ??= new List<BagEquipmentItem>();
        // Phase 2.25: one-time starter Bạc for saves created before 2.25
        // (new saves already get it via SeedService; the flag prevents
        // re-granting when a player legitimately spends down to 0).
        if (!save.SilverSeeded) { save.Silver += SeedService.SeededSilver; save.SilverSeeded = true; }
        // Phase 2.24: souls pooled per tier, 0 by default (old saves load
        // fine; per-general souls from 2.23 are discarded, not migrated).
        save.TierSouls ??= new Dictionary<string, int>();
        // Phase 2.33: gem pouch for old saves.
        save.GemInventory ??= new Dictionary<string, int>();
        // Phase 2.34: forge data for old saves.
        save.Blueprints ??= new Dictionary<string, bool>();
        save.Materials ??= new Dictionary<string, int>();
        foreach (var tier in new[] { TavernTier.Lam, TavernTier.Tim, TavernTier.Cam })
            if (!save.TierSouls.ContainsKey(tier.ToString())) save.TierSouls[tier.ToString()] = 0;
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
        // Phase 2.30: migrate old equipment (no set fields) to the Tân Thủ
        // set; old RefineIndex becomes the first refine line.
        // Phase 2.38 (B11): skipped while a stat projection has a
        // hypothetical item swapped in (it would get random "legacy" lines).
        if (SuppressEquipmentMigration) return;
        foreach (var heroEq in save.HeroEquipment.Values)
            foreach (var eq in heroEq.Values)
                MigrateEquipment(eq);
        foreach (var item in save.BagEquipment)
            MigrateBagItem(item);
    }

    private static void MigrateEquipment(EquipmentState eq)
    {
        if (string.IsNullOrEmpty(eq.SetId)) { eq.SetId = "tan_thu"; eq.Tier = 30; eq.Quality = "Lam"; }
        // Phase 2.37: Quality/Tier are authoritative from the set def
        // (auto-handles the quality remap: Chu Tước Tím->Lục, ...).
        var setDef = EquipmentSets.Get(eq.SetId);
        eq.Tier = setDef.Tier;
        eq.Quality = setDef.Quality;
        eq.Sockets ??= new List<EquipmentSocket>();
        GemService.EnsureSockets(eq);
        eq.RefineLines ??= new List<RefineLine>();
        if (!eq.IsEmpty && eq.RefineLines.Count == 0)
        {
            var (stat, val) = RefineLineForOldIndex(eq.RefineIndex);
            eq.RefineLines.Add(new RefineLine { StatId = stat, Value = val });
            var rng = new Random();
            while (eq.RefineLines.Count < EquipmentSets.LinesForSet(eq.SetId))
                eq.RefineLines.Add(RandomRefineLine(rng));
        }
    }

    private static void MigrateBagItem(BagEquipmentItem item)
    {
        if (string.IsNullOrEmpty(item.SetId)) { item.SetId = "tan_thu"; item.Tier = 30; item.Quality = "Lam"; }
        var setDef = EquipmentSets.Get(item.SetId);
        item.Tier = setDef.Tier;
        item.Quality = setDef.Quality;
        item.Sockets ??= new List<EquipmentSocket>();
        GemService.EnsureSockets(item);
        item.RefineLines ??= new List<RefineLine>();
        if (item.RefineLines.Count == 0)
        {
            var (stat, val) = RefineLineForOldIndex(item.RefineIndex);
            item.RefineLines.Add(new RefineLine { StatId = stat, Value = val });
            var rng = new Random();
            while (item.RefineLines.Count < EquipmentSets.LinesForSet(item.SetId))
                item.RefineLines.Add(RandomRefineLine(rng));
        }
    }

    /// Maps the pre-2.30 refine option index to a refine line.
    public static (string StatId, double Value) RefineLineForOldIndex(int index) => (index % 5) switch
    {
        0 => ("crit", 0.03),
        1 => ("dodge", 0.03),
        2 => ("speed", 5),
        3 => ("atk_pct", 0.10),
        _ => ("hp_pct", 0.10),
    };

    private static int OldIndexOfLine(RefineLine line) => line.StatId switch
    {
        "crit" => 0, "dodge" => 1, "speed" => 2, "atk_pct" => 3, "hp_pct" => 4, _ => 0,
    };

    /// Temporary 2.30 filler: random line from the old 5 options.
    /// Phase 2.32 replaces this with the position-based pool + lock rules.
    public static RefineLine RandomRefineLine(Random rng)
    {
        var (stat, val) = RefineLineForOldIndex(rng.Next(5));
        return new RefineLine { StatId = stat, Value = val };
    }

    // Phase 2.38 (B2): dmg_red and def_pct (rolled by the Tiền pool) now
    // apply — before they were rolled but had no effect.
    public static void ApplyRefineLines(HeroDef def, List<RefineLine> lines, ref double atkMul, ref double hpMul, ref double defMul)
    {
        if (lines == null) return;
        foreach (var line in lines)
        {
            switch (line.StatId)
            {
                case "crit": def.Crit += line.Value; break;
                case "dodge": def.Dodge += line.Value; break;
                case "speed": def.Speed += (int)line.Value; break;
                case "atk": def.Atk += (int)line.Value; break;
                case "def": def.Def += (int)line.Value; break;
                case "hp": def.Hp += (int)line.Value; break;
                case "atk_pct": atkMul *= 1 + line.Value; break;
                case "hp_pct": hpMul *= 1 + line.Value; break;
                case "def_pct": defMul *= 1 + line.Value; break;
                case "dmg_red": def.DamageReduction += line.Value; break;
            }
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

    public static EnhanceResult TryEnhance(SaveData save, string heroId, string slotId, Random rng)
    {
        EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        if (eq.IsEmpty) return EnhanceResult.Failed_Empty;
        if (eq.Level >= MaxEnhanceForHero(save)) return EnhanceResult.Failed_Maxed;
        int cost = EnhanceCostSilver(eq.Level);
        if (save.Silver < cost) return EnhanceResult.Failed_NoSilver;
        save.Silver -= cost;
        if (rng.NextDouble() < EnhanceSuccessRate(eq.Level + 1))
        {
            eq.Level++;
            return EnhanceResult.Success;
        }
        return EnhanceResult.Failed_Roll;
    }

    // Phase 2.32: full refine rework (Huynh's spec 4a) — 3-6 random lines
    // by quality, reroll unlocked lines, lock keeps a line.
    // Pool follows the hero's formation position (Tiền = tanker,
    // Trung = damage, Hậu = support); not deployed => Trung pool.
    // Only engine-supported stats are rolled; exotic stats (Kháng bạo,
    // Đỡ đòn, Trúng, Sát thương bạo, Khống chế) wait for the engine.
    // Phase 2.37 (Huynh Q5a): refine cost grows with locked lines.
    public static int RefineCostStones(SaveData save, string heroId, string slotId)
    {
        EnsureDefaults(save);
        return 1 + save.HeroEquipment[heroId][slotId].RefineLines.Count(l => l.Locked);
    }
    public static int RefineCostGold(SaveData save, string heroId, string slotId) =>
        200 * RefineCostStones(save, heroId, slotId);

    public static List<(string StatId, double Min, double Max)> RefinePoolFor(FormationPosition pos) => pos switch
    {
        FormationPosition.Front => new()
        {
            ("hp_pct", 0.08, 0.15), ("hp", 100, 300), ("dmg_red", 0.03, 0.08),
            ("dodge", 0.02, 0.05), ("def", 20, 60), ("def_pct", 0.05, 0.10),
        },
        FormationPosition.Rear => new()
        {
            ("speed", 5, 10), ("hp_pct", 0.08, 0.12), ("atk", 15, 40),
            ("dodge", 0.02, 0.05), ("crit", 0.02, 0.05),
        },
        _ => new() // Middle (Trung) + None
        {
            ("atk_pct", 0.08, 0.15), ("atk", 20, 60), ("crit", 0.03, 0.08),
            ("speed", 3, 8), ("dodge", 0.02, 0.04),
        },
    };

    public static RefineLine RollRefineLine(FormationPosition pos, Random rng)
    {
        var pool = RefinePoolFor(pos);
        var (stat, min, max) = pool[rng.Next(pool.Count)];
        double v = min + rng.NextDouble() * (max - min);
        bool pct = stat is "hp_pct" or "atk_pct" or "def_pct" or "crit" or "dodge" or "dmg_red";
        return new RefineLine { StatId = stat, Value = pct ? Math.Round(v, 3) : Math.Round(v) };
    }

    // Refine line grade by roll quality (Huynh: lines show colors by
    // grade). Ratio within the widest pool range for the stat:
    // >=0.75 Cam, >=0.5 Tím, >=0.25 Lam, else Trắng (gray).
    // Grade keys match EquipmentSets.QualityColor (no diacritics).
    public static string RefineLineGrade(RefineLine line)
    {
        double min = double.MaxValue, max = double.MinValue;
        foreach (var pos in new[] { FormationPosition.Front, FormationPosition.Middle, FormationPosition.Rear })
            foreach (var (stat, lo, hi) in RefinePoolFor(pos))
                if (stat == line.StatId) { min = Math.Min(min, lo); max = Math.Max(max, hi); }
        if (max <= min) return "Trang";
        double r = Math.Clamp((line.Value - min) / (max - min), 0, 1);
        if (r >= 0.75) return "Cam";
        if (r >= 0.5) return "Tim";
        if (r >= 0.25) return "Lam";
        return "Trang";
    }

    public static bool TryRefine(SaveData save, string heroId, string slotId, Random rng)
    {
        EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        if (eq.IsEmpty) return false;
        var pos = FormationService.PositionOfHero(save, heroId);
        // Phase 2.38 (B4): nothing to reroll (all lines locked, none
        // missing) => refuse instead of charging for nothing.
        bool missing = eq.RefineLines.Count < EquipmentSets.LinesForSet(eq.SetId);
        if (!missing && eq.RefineLines.All(l => l.Locked)) return false;
        int stoneCost = RefineCostStones(save, heroId, slotId);
        bool useStone = save.RefineStones >= stoneCost;
        if (!useStone && save.Gold < RefineCostGold(save, heroId, slotId)) return false;
        if (useStone) save.RefineStones -= stoneCost;
        else save.Gold -= RefineCostGold(save, heroId, slotId);
        // Top up to the set's line count only AFTER paying (B4: short items
        // used to receive free lines even when the refine was refused).
        while (eq.RefineLines.Count < EquipmentSets.LinesForSet(eq.SetId))
            eq.RefineLines.Add(RollRefineLine(pos, rng));
        for (int i = 0; i < eq.RefineLines.Count; i++)
            if (!eq.RefineLines[i].Locked)
                eq.RefineLines[i] = RollRefineLine(pos, rng);
        return true;
    }

    public static void ToggleRefineLock(SaveData save, string heroId, string slotId, int lineIndex)
    {
        EnsureDefaults(save);
        var lines = save.HeroEquipment[heroId][slotId].RefineLines;
        if (lineIndex >= 0 && lineIndex < lines.Count)
            lines[lineIndex].Locked = !lines[lineIndex].Locked;
    }

    public static int StarChartPoints(SaveData save) => save.StageStars.Values.Sum();

    /// <summary>Phase 2.18: dismiss a hero = remove it from the owned roster
    /// only. Equipped items return to the bag (never lost); level is kept,
    /// so re-recruiting the hero later restores it. Blocked for the
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
        // Phase 2.38 (B1): return the FULL item (set, lines, sockets and
        // socketed gems) — the old copy kept only slot/level/refine index,
        // so a Kỳ Lân item came back as Tân Thủ and its gems were lost.
        foreach (var slot in Slots)
            EquipmentService.Unequip(save, heroId, slot.Id);
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

    public static HeroDef GetScaledBase(string heroId, int level)
    {
        var baseDef = HeroDatabase.Get(heroId);
        var tier = TavernData.TierOf(heroId);
        double statFactor = tier == null ? 1.0 : TavernData.StatFactor(tier.Value);
        double growthFactor = tier == null ? 1.0 : TavernData.GrowthFactor(tier.Value);
        double levelFactor = 1 + 0.04 * growthFactor * (level - 1);
        return new HeroDef
        {
            Id = baseDef.Id, Name = baseDef.Name, Role = baseDef.Role,
            Hp = (int)Math.Round(baseDef.Hp * statFactor * levelFactor),
            Atk = (int)Math.Round(baseDef.Atk * statFactor * levelFactor),
            Def = (int)Math.Round(baseDef.Def * statFactor * levelFactor),
            Speed = baseDef.Speed, Crit = baseDef.Crit, Dodge = baseDef.Dodge,
            DamageReduction = baseDef.DamageReduction,
            Hit = baseDef.Hit, CritResist = baseDef.CritResist, Block = baseDef.Block,
            Pierce = baseDef.Pierce, CritDamage = baseDef.CritDamage,
            NormalName = baseDef.NormalName, UltName = baseDef.UltName,
            UltPower = baseDef.UltPower, UltKind = baseDef.UltKind,
            PassiveName = baseDef.PassiveName, PassiveDesc = baseDef.PassiveDesc,
        };
    }

    public static HeroDef GetEffectiveHero(SaveData save, string heroId)
    {
        var p = ProgressOf(save, heroId);
        var def = GetScaledBase(heroId, p.Level);
        double atkMul = 1.0, hpMul = 1.0, defMul = 1.0;
        foreach (var slot in Slots)
        {
            var eq = save.HeroEquipment[heroId][slot.Id];
            if (eq.IsEmpty) continue;
            def.Atk += slot.BaseAtk + slot.AtkPerLevel * eq.Level;
            def.Hp += slot.BaseHp + slot.HpPerLevel * eq.Level;
            def.Def += slot.BaseDef + slot.DefPerLevel * eq.Level;
            def.Speed += slot.BaseSpeed + slot.SpeedPerLevel * eq.Level;
            ApplyRefineLines(def, eq.RefineLines, ref atkMul, ref hpMul, ref defMul);
            GemService.ApplyGems(def, eq, ref atkMul, ref hpMul);
        }
        // Phase 2.30: set bonuses (2/4/6 pieces).
        var (setAtkMul, setHpMul) = SetBonusService.Apply(def, SetBonusService.ActiveBonuses(save, heroId));
        atkMul *= setAtkMul;
        hpMul *= setHpMul;
        def.Atk = (int)Math.Round(def.Atk * atkMul);
        def.Hp = (int)Math.Round(def.Hp * hpMul);
        def.Def = (int)Math.Round(def.Def * defMul);
        double chartMul = 1 + 0.03 * save.StarNodesUnlocked;
        def.Atk = (int)Math.Round(def.Atk * chartMul);
        def.Hp = (int)Math.Round(def.Hp * chartMul);
        return def;
    }
}
