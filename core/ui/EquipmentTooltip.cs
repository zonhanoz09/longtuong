using System.Collections.Generic;
using System.Text;
using Godot;

namespace LongTuong;

/// <summary>
/// The single unified equipment hover tooltip, used identically by the
/// Võ tướng popup, the Túi popup and the Trang bị popup (Huynh's
/// consistency request). Rendered as BBCode in a RichTextLabel:
/// item/set name in the equipment quality color, refine lines in
/// their roll-grade color, then gems and set bonuses.
/// </summary>
public static class EquipmentTooltip
{
    public static string ColorHex(Color c) =>
        $"{(int)(c.R * 255):X2}{(int)(c.G * 255):X2}{(int)(c.B * 255):X2}";

    public static string DisplayName(string setId, string slotId) =>
        EquipmentSets.DisplayName(setId, slotId);

    public static string Bbcode(
        string slotId, int level, string setId, string quality,
        List<RefineLine> lines, List<EquipmentSocket> sockets, string? status)
    {
        var set = EquipmentSets.Get(setId);
        var slot = ProgressionService.Slots.Find(s => s.Id == slotId);
        string qHex = ColorHex(EquipmentSets.QualityColor(quality));
        var sb = new StringBuilder();
        // Phase 2.37 (Huynh): item name = "{Set} {Slot}".
        sb.AppendLine($"[color=#{qHex}][b]{DisplayName(setId, slotId)} +{level}[/b][/color]");
        sb.AppendLine($"Lv{set.Tier} · {EquipmentSets.QualityName(quality)}");
        if (lines.Count > 0)
        {
            sb.AppendLine("Tẩy luyện:");
            foreach (var line in lines)
            {
                string gHex = ColorHex(EquipmentSets.QualityColor(ProgressionService.RefineLineGrade(line)));
                sb.AppendLine($"[color=#{gHex}]  {EquipmentService.DescribeRefineLine(line)}[/color]");
            }
        }
        if (sockets.Count > 0)
        {
            sb.AppendLine("Ngọc khảm:");
            for (int i = 0; i < sockets.Count; i++)
            {
                var s = sockets[i];
                string desc = !s.Unlocked ? "(chưa mở)"
                    : string.IsNullOrEmpty(s.GemId) ? "(trống)"
                    : GemService.DescribeGem(s.GemId);
                sb.AppendLine($"  Lỗ {i + 1}: {desc}");
            }
        }
        foreach (var b in set.Bonuses)
            sb.AppendLine($"[color=#{qHex}]Set {b.Pieces} món: {SetBonusService.DescribeBonus(b)}[/color]");
        if (!string.IsNullOrEmpty(status)) sb.AppendLine(status);
        return sb.ToString();
    }

    public static string ForEquipped(EquipmentState eq, string slotId, string? status) =>
        eq.IsEmpty ? "Ô trống — chưa mang trang bị."
        : Bbcode(slotId, eq.Level, eq.SetId, eq.Quality, eq.RefineLines, eq.Sockets, status);

    public static string ForBagItem(BagEquipmentItem item, string? status) =>
        Bbcode(item.SlotId, item.Level, item.SetId, item.Quality, item.RefineLines, item.Sockets, status);
}
