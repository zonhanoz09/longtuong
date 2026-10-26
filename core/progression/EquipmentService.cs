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
    public static bool Unequip(SaveData save, string heroId, string slotId)
    {
        ProgressionService.EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        if (eq.IsEmpty) return false;
        save.BagEquipment.Add(new BagEquipmentItem { SlotId = slotId, Level = eq.Level, RefineIndex = eq.RefineIndex });
        eq.IsEmpty = true;
        eq.Level = 0;
        eq.RefineIndex = 0;
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
            save.BagEquipment.Add(new BagEquipmentItem { SlotId = item.SlotId, Level = eq.Level, RefineIndex = eq.RefineIndex });
        eq.IsEmpty = false;
        eq.Level = item.Level;
        eq.RefineIndex = item.RefineIndex;
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
}
