using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.30: equipment sets by level tier (Huynh's spec 2026-10-29).
/// StatId values: hp, hp_pct, atk, atk_pct, def, def_pct, speed, crit,
/// dodge, dmg_red (Miễn thương), starting_rage, cc_resist, block.
/// The last three are DATA-ONLY until the battle engine grows to support
/// them (see SetBonusService.IsEngineSupported).
/// Set bonus numbers for Tân Thủ are Pax's setup (spec gives no numbers);
/// the rest follow Huynh's table exactly.
/// </summary>
public class SetBonus
{
    public int Pieces { get; set; } = 2;      // 2 / 4 / 6
    public string StatId { get; set; } = "";
    public double Value { get; set; }
}

public class EquipmentSetDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Tier { get; set; } = 30;       // 30 / 50 / 70 / 80 / 90 / 100
    public string Quality { get; set; } = "Lam"; // Lam / Tim / Cam / Do
    public List<SetBonus> Bonuses { get; set; } = new();
}

/// Phase 2.30: gem socket (content in 2.33 — khảm ngọc).
public class EquipmentSocket
{
    public bool Unlocked { get; set; } = false;
    public string GemId { get; set; } = "";
}

/// Phase 2.30: one refine line (full reroll + lock UI in 2.32).
public class RefineLine
{
    public string StatId { get; set; } = "crit";
    public double Value { get; set; } = 0.03;
    public bool Locked { get; set; } = false;
}

public static class EquipmentSets
{
    public static readonly List<EquipmentSetDef> All = new()
    {
        new EquipmentSetDef { Id = "tan_thu", Name = "Tân Thủ", Tier = 30, Quality = "Lam",
            Bonuses = new() { new() { Pieces = 2, StatId = "hp", Value = 200 }, new() { Pieces = 4, StatId = "atk", Value = 20 } } },
        new EquipmentSetDef { Id = "chu_tuoc", Name = "Chu Tước", Tier = 50, Quality = "Tim",
            Bonuses = new() { new() { Pieces = 2, StatId = "hp_pct", Value = 0.10 }, new() { Pieces = 4, StatId = "atk_pct", Value = 0.10 }, new() { Pieces = 6, StatId = "speed", Value = 10 } } },
        new EquipmentSetDef { Id = "bach_ho", Name = "Bạch Hổ", Tier = 70, Quality = "Cam",
            Bonuses = new() { new() { Pieces = 2, StatId = "hp_pct", Value = 0.15 }, new() { Pieces = 4, StatId = "crit", Value = 0.08 }, new() { Pieces = 6, StatId = "speed", Value = 10 }, new() { Pieces = 6, StatId = "dmg_red", Value = 0.05 } } },
        new EquipmentSetDef { Id = "thanh_long", Name = "Thanh Long", Tier = 80, Quality = "Cam",
            Bonuses = new() { new() { Pieces = 2, StatId = "hp_pct", Value = 0.20 }, new() { Pieces = 4, StatId = "atk_pct", Value = 0.15 }, new() { Pieces = 4, StatId = "crit", Value = 0.05 }, new() { Pieces = 6, StatId = "speed", Value = 15 }, new() { Pieces = 6, StatId = "cc_resist", Value = 0.15 } } },
        new EquipmentSetDef { Id = "huyen_vu", Name = "Huyền Vũ", Tier = 90, Quality = "Cam",
            Bonuses = new() { new() { Pieces = 2, StatId = "hp_pct", Value = 0.25 }, new() { Pieces = 4, StatId = "def_pct", Value = 0.15 }, new() { Pieces = 4, StatId = "block", Value = 0.10 }, new() { Pieces = 6, StatId = "speed", Value = 15 }, new() { Pieces = 6, StatId = "dmg_red", Value = 0.08 } } },
        new EquipmentSetDef { Id = "ky_lan", Name = "Kỳ Lân", Tier = 100, Quality = "Do",
            Bonuses = new() { new() { Pieces = 2, StatId = "hp_pct", Value = 0.10 }, new() { Pieces = 2, StatId = "atk_pct", Value = 0.10 }, new() { Pieces = 2, StatId = "def_pct", Value = 0.10 }, new() { Pieces = 4, StatId = "speed", Value = 10 }, new() { Pieces = 4, StatId = "crit", Value = 0.05 }, new() { Pieces = 6, StatId = "starting_rage", Value = 25 } } },
    };

    public static EquipmentSetDef Get(string setId) =>
        All.Find(s => s.Id == setId) ?? All[0];

    public static string QualityName(string q) => q switch
    {
        "Lam" => "Lam", "Tim" => "Tím", "Cam" => "Cam", "Do" => "Đỏ", _ => q,
    };

    public static Color QualityColor(string q) => q switch
    {
        "Lam" => new Color(0.15f, 0.55f, 1f),
        "Tim" => new Color(0.85f, 0.25f, 0.95f),
        "Cam" => new Color(1f, 0.6f, 0.1f),
        "Do" => new Color(1f, 0.25f, 0.25f),
        _ => new Color(0.7f, 0.7f, 0.7f),
    };

    public static int LinesForQuality(string q) => q switch
    {
        "Lam" => 3, "Tim" => 4, "Cam" => 5, "Do" => 6, _ => 3,
    };
}

public static class SetBonusService
{
    /// Stats the current engine can apply. starting_rage / cc_resist /
    /// block are kept as data until the battle engine supports them.
    public static bool IsEngineSupported(string statId) => statId is
        "hp" or "hp_pct" or "atk" or "atk_pct" or "def" or "def_pct" or
        "speed" or "crit" or "dodge" or "dmg_red";

    public static Dictionary<string, int> CountSets(SaveData save, string heroId)
    {
        ProgressionService.EnsureDefaults(save);
        var counts = new Dictionary<string, int>();
        foreach (var slot in ProgressionService.Slots)
        {
            var eq = save.HeroEquipment[heroId][slot.Id];
            if (eq.IsEmpty) continue;
            counts[eq.SetId] = counts.TryGetValue(eq.SetId, out var n) ? n + 1 : 1;
        }
        return counts;
    }

    public static List<(EquipmentSetDef Set, SetBonus Bonus)> ActiveBonuses(SaveData save, string heroId)
    {
        var result = new List<(EquipmentSetDef, SetBonus)>();
        foreach (var (setId, n) in CountSets(save, heroId))
        {
            var set = EquipmentSets.Get(setId);
            foreach (var b in set.Bonuses)
                if (n >= b.Pieces) result.Add((set, b));
        }
        return result;
    }

    /// Applies engine-supported bonuses. Returns the % multipliers so the
    /// caller can fold them into its own mul order.
    public static (double AtkMul, double HpMul) Apply(HeroDef def, List<(EquipmentSetDef Set, SetBonus Bonus)> bonuses)
    {
        double atkMul = 1, hpMul = 1;
        foreach (var (_, b) in bonuses)
        {
            if (!IsEngineSupported(b.StatId)) continue;
            switch (b.StatId)
            {
                case "hp": def.Hp += (int)b.Value; break;
                case "hp_pct": hpMul *= 1 + b.Value; break;
                case "atk": def.Atk += (int)b.Value; break;
                case "atk_pct": atkMul *= 1 + b.Value; break;
                case "def": def.Def += (int)b.Value; break;
                case "def_pct": def.Def = (int)Math.Round(def.Def * (1 + b.Value)); break;
                case "speed": def.Speed += (int)b.Value; break;
                case "crit": def.Crit += b.Value; break;
                case "dodge": def.Dodge += b.Value; break;
                case "dmg_red": def.DamageReduction += b.Value; break;
            }
        }
        return (atkMul, hpMul);
    }

    public static string DescribeBonus(SetBonus b)
    {
        string stat = b.StatId switch
        {
            "hp" or "hp_pct" => "HP", "atk" or "atk_pct" => "Công",
            "def" or "def_pct" => "Thủ", "speed" => "Tốc độ",
            "crit" => "Bạo kích", "dodge" => "Né tránh",
            "dmg_red" => "Miễn thương", "starting_rage" => "Nộ khởi đầu",
            "cc_resist" => "Kháng khống chế", "block" => "Đỡ đòn",
            _ => b.StatId,
        };
        bool pct = b.StatId is "hp_pct" or "atk_pct" or "def_pct" or "crit"
            or "dodge" or "dmg_red" or "cc_resist" or "block";
        string val = pct ? $"+{b.Value:P0}" : $"+{b.Value:0}";
        string tag = IsEngineSupported(b.StatId) ? "" : " (sắp mở)";
        return $"{val} {stat} ({b.Pieces} món){tag}";
    }

    public static List<string> DescribeForHero(SaveData save, string heroId)
    {
        var lines = new List<string>();
        foreach (var (setId, n) in CountSets(save, heroId).OrderByDescending(kv => kv.Value))
        {
            var set = EquipmentSets.Get(setId);
            var actives = set.Bonuses.Where(b => n >= b.Pieces).ToList();
            string bonusText = actives.Count == 0 ? "chưa kích hoạt"
                : string.Join(", ", actives.Select(DescribeBonus));
            lines.Add($"{set.Name} ({n}/6): {bonusText}");
        }
        return lines;
    }
}
