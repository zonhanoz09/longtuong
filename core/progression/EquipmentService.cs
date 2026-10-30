using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.5 equipment bag <-> hero transfer. Unequip never deletes an item:
/// it always goes to the bag; equipping into an occupied slot swaps the old
/// item back to the bag. No gold cost for equip/unequip.
/// </summary>
public static class EquipmentService
{
    // Phase 2.37: deep copy for hypothetical simulation.
    public static EquipmentState CopyState(EquipmentState eq) => new()
    {
        IsEmpty = eq.IsEmpty,
        Level = eq.Level,
        RefineIndex = eq.RefineIndex,
        Tier = eq.Tier,
        Quality = eq.Quality,
        SetId = eq.SetId,
        Sockets = eq.Sockets.Select(s => new EquipmentSocket { Unlocked = s.Unlocked, GemId = s.GemId }).ToList(),
        RefineLines = eq.RefineLines.Select(l => new RefineLine { StatId = l.StatId, Value = l.Value, Locked = l.Locked }).ToList(),
    };

    // Phase 2.37: projected stat change for the Cường hóa tab
    // ("HIỆN TẠI → DỰ KIẾN SAU NÂNG"). Temp-swaps a hypothetical
    // equipment state, diffs GetEffectiveHero, restores. Only stats
    // that actually change are returned.
    public static List<(string Name, double Before, double After)> ProjectEquipChange(
        SaveData save, string heroId, string slotId, EquipmentState hypothetical)
    {
        ProgressionService.EnsureDefaults(save);
        var before = ProgressionService.GetEffectiveHero(save, heroId);
        var dict = save.HeroEquipment[heroId];
        var original = dict[slotId];
        HeroDef after;
        // Phase 2.38 (B11): no migration on the swapped-in item (a preview
        // with no refine lines used to get random lines, so the projection
        // was wrong and changed on every refresh); always restore.
        ProgressionService.SuppressEquipmentMigration = true;
        try
        {
            dict[slotId] = hypothetical;
            after = ProgressionService.GetEffectiveHero(save, heroId);
        }
        finally
        {
            dict[slotId] = original;
            ProgressionService.SuppressEquipmentMigration = false;
        }
        var rows = new List<(string, double, double)>();
        void Add(string name, double b, double a)
        {
            if (Math.Abs(a - b) > 0.0001) rows.Add((name, b, a));
        }
        Add("HP", before.Hp, after.Hp);
        Add("Vật công", before.Atk, after.Atk);
        Add("Vật phòng", before.Def, after.Def);
        Add("Tốc độ", before.Speed, after.Speed);
        Add("Bạo kích", before.Crit, after.Crit);
        Add("Né tránh", before.Dodge, after.Dodge);
        return rows;
    }

    // Phase 2.30: equip/unequip carry the full item (set, quality, tier,
    // sockets, refine lines) — never just level/refine.
    private static BagEquipmentItem ToBagItem(string slotId, EquipmentState eq) => new()
    {
        SlotId = slotId, Level = eq.Level, RefineIndex = eq.RefineIndex,
        Tier = eq.Tier, Quality = eq.Quality, SetId = eq.SetId,
        Sockets = eq.Sockets.Select(s => new EquipmentSocket { Unlocked = s.Unlocked, GemId = s.GemId }).ToList(),
        RefineLines = eq.RefineLines.Select(l => new RefineLine { StatId = l.StatId, Value = l.Value, Locked = l.Locked }).ToList(),
    };

    private static void CopyToState(EquipmentState eq, BagEquipmentItem item)
    {
        eq.IsEmpty = false;
        eq.Level = item.Level;
        eq.RefineIndex = item.RefineIndex;
        eq.Tier = item.Tier;
        eq.Quality = item.Quality;
        eq.SetId = item.SetId;
        eq.Sockets = item.Sockets.Select(s => new EquipmentSocket { Unlocked = s.Unlocked, GemId = s.GemId }).ToList();
        eq.RefineLines = item.RefineLines.Select(l => new RefineLine { StatId = l.StatId, Value = l.Value, Locked = l.Locked }).ToList();
    }

    public static bool Unequip(SaveData save, string heroId, string slotId)
    {
        ProgressionService.EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        if (eq.IsEmpty) return false;
        save.BagEquipment.Add(ToBagItem(slotId, eq));
        eq.IsEmpty = true;
        eq.Level = 0;
        eq.RefineIndex = 0;
        eq.RefineLines.Clear();
        eq.Sockets.Clear();
        return true;
    }

    public static int UnequipAll(SaveData save, string heroId)
    {
        ProgressionService.EnsureDefaults(save);
        int count = 0;
        foreach (var slot in ProgressionService.Slots)
            if (Unequip(save, heroId, slot.Id)) count++;
        return count;
    }

    public static bool EquipFromBag(SaveData save, string heroId, int bagIndex)
    {
        ProgressionService.EnsureDefaults(save);
        if (bagIndex < 0 || bagIndex >= save.BagEquipment.Count) return false;
        var item = save.BagEquipment[bagIndex];
        var eq = save.HeroEquipment[heroId][item.SlotId];
        if (!eq.IsEmpty)
            save.BagEquipment.Add(ToBagItem(item.SlotId, eq));
        CopyToState(eq, item);
        save.BagEquipment.RemoveAt(bagIndex);
        return true;
    }

    public static List<BagEquipmentItem> BagItemsForSlot(SaveData save, string slotId)
    {
        ProgressionService.EnsureDefaults(save);
        return save.BagEquipment.Where(b => b.SlotId == slotId).ToList();
    }

    /// <summary>
    /// Full detail text for ONE equipment item, shown only on hover (Phase 2.6).
    /// Item buttons themselves show no stats — only slot name + level.
    /// </summary>
    public static string DetailText(string slotId, int level, int refineIndex, string? status)
    {
        var slot = ProgressionService.Slots.Find(s => s.Id == slotId);
        if (slot == null) return slotId;
        var lines = new System.Collections.Generic.List<string> { $"{slot.Name} — Cấp {level}/10" };
        int atk = slot.BaseAtk + slot.AtkPerLevel * level;
        int hp = slot.BaseHp + slot.HpPerLevel * level;
        int def = slot.BaseDef + slot.DefPerLevel * level;
        int speed = slot.BaseSpeed + slot.SpeedPerLevel * level;
        if (atk > 0) lines.Add($"Tấn công: {atk}");
        if (hp > 0) lines.Add($"HP: {hp}");
        if (def > 0) lines.Add($"Phòng thủ: {def}");
        if (speed > 0) lines.Add($"Tốc độ: {speed}");
        lines.Add($"Tẩy luyện: {ProgressionService.RefineNames[refineIndex % ProgressionService.RefineNames.Length]}");
        if (!string.IsNullOrEmpty(status)) lines.Add(status);
        return string.Join("\n", lines);
    }

    /// Phase 2.30: detail text with set/quality/refine lines.
    public static string DescribeRefineLine(RefineLine line)
    {
        string name = line.StatId switch
        {
            "crit" => "Bạo kích", "dodge" => "Né tránh", "speed" => "Tốc độ",
            "atk" or "atk_pct" => "Tấn công", "def" or "def_pct" => "Phòng thủ",
            "hp" or "hp_pct" => "HP", "dmg_red" => "Miễn thương",
            _ => line.StatId,
        };
        bool pct = line.StatId is "crit" or "dodge" or "atk_pct" or "hp_pct" or "def_pct" or "dmg_red";
        string val = pct ? $"+{line.Value:P1}" : $"+{line.Value:0}";
        return $"{name} {val}{(line.Locked ? " (khóa)" : "")}";
    }

    // Phase 2.35: Kế thừa (inherit) — move enhance level, refine lines and
    // socketed gems from a bag item (same slot) onto the equipped item;
    // the old item is destroyed. Target's own gems return to the pouch
    // first; moved gems fill the target's unlocked sockets, extras go to
    // the pouch. Socket unlock states stay with the target item.
    public const int InheritCostSilver = 5000;

    public static bool TryInherit(SaveData save, string heroId, string slotId, int bagIndex)
    {
        ProgressionService.EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        if (eq.IsEmpty) return false;
        if (bagIndex < 0 || bagIndex >= save.BagEquipment.Count) return false;
        var src = save.BagEquipment[bagIndex];
        if (src.SlotId != slotId) return false;
        if (save.Silver < InheritCostSilver) return false;
        save.Silver -= InheritCostSilver;
        // Target's current gems go back to the pouch.
        foreach (var sock in eq.Sockets)
            if (!string.IsNullOrEmpty(sock.GemId))
            {
                GemService.AddGems(save, sock.GemId, 1);
                sock.GemId = "";
            }
        eq.Level = src.Level;
        eq.RefineIndex = src.RefineIndex;
        // Phase 2.38 (B3): never exceed the target set's line count
        // (6-line Kỳ Lân into 3-line Tân Thủ keeps the first 3 lines).
        eq.RefineLines = src.RefineLines
            .Take(EquipmentSets.LinesForSet(eq.SetId))
            .Select(l => new RefineLine { StatId = l.StatId, Value = l.Value, Locked = l.Locked }).ToList();
        foreach (var srcSock in src.Sockets)
        {
            if (string.IsNullOrEmpty(srcSock.GemId)) continue;
            var free = eq.Sockets.FirstOrDefault(s => s.Unlocked && string.IsNullOrEmpty(s.GemId));
            if (free != null) free.GemId = srcSock.GemId;
            else GemService.AddGems(save, srcSock.GemId, 1);
        }
        save.BagEquipment.RemoveAt(bagIndex);
        return true;
    }

    public static string DetailText(BagEquipmentItem item, string? status)
    {
        var set = EquipmentSets.Get(item.SetId);
        var lines = new List<string>
        {
            $"{ProgressionService.Slots.Find(s => s.Id == item.SlotId)?.Name ?? item.SlotId} — Cấp {item.Level}/10",
            $"Bộ: {set.Name} (Lv{set.Tier} · {EquipmentSets.QualityName(item.Quality)})",
        };
        var slot = ProgressionService.Slots.Find(s => s.Id == item.SlotId);
        if (slot != null)
        {
            int atk = slot.BaseAtk + slot.AtkPerLevel * item.Level;
            int hp = slot.BaseHp + slot.HpPerLevel * item.Level;
            int def = slot.BaseDef + slot.DefPerLevel * item.Level;
            int speed = slot.BaseSpeed + slot.SpeedPerLevel * item.Level;
            if (atk > 0) lines.Add($"Tấn công: {atk}");
            if (hp > 0) lines.Add($"HP: {hp}");
            if (def > 0) lines.Add($"Phòng thủ: {def}");
            if (speed > 0) lines.Add($"Tốc độ: {speed}");
        }
        foreach (var line in item.RefineLines)
            lines.Add($"Tẩy luyện: {DescribeRefineLine(line)}");
        foreach (var b in set.Bonuses)
            lines.Add($"Set {b.Pieces} món: {SetBonusService.DescribeBonus(b)}");
        if (!string.IsNullOrEmpty(status)) lines.Add(status);
        return string.Join("\n", lines);
    }

    public static string DetailText(EquipmentState eq, string slotId, string? status) =>
        DetailText(new BagEquipmentItem
        {
            SlotId = slotId, Level = eq.Level, RefineIndex = eq.RefineIndex,
            Tier = eq.Tier, Quality = eq.Quality, SetId = eq.SetId,
            Sockets = eq.Sockets, RefineLines = eq.RefineLines,
        }, status);
}
