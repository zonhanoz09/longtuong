using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

// Phase 2.20: formation positions (bày trận) — the battle team is exactly the
// formation, laid out on 7 slots in 3 columns (Hậu / Trung / Tiền) as sketched:
//   Hậu: 3 slots (trên / giữa / dưới)   Trung: 3 slots (trên / giữa / dưới)
//   Tiền: 1 slot (giữa) — the front column has only the middle row.
// Grid order (row-major, used for TeamIds derivation and for the popup grid,
// where the two missing Tiền slots are empty placeholder cells):
//   hau_top, trung_top, (empty), hau_mid, trung_mid, tien_mid,
//   hau_bot, trung_bot, (empty).
public enum FormationPosition
{
    None,
    Rear,   // Hậu quân
    Middle, // Trung quân
    Front,  // Tiền quân
}

public class FormationSlotDef
{
    public string Id { get; set; } = "";
    public FormationPosition Position { get; set; } = FormationPosition.None;
    public string Name { get; set; } = "";
}

public static class FormationService
{
    public const string MainHeroId = "custom_hero"; // tướng tự tạo (tên đăng nhập)
    public const int MaxTeamSize = 5;
    public const string CenterSlotId = "trung_mid";

    public static readonly List<FormationSlotDef> Slots = new()
    {
        new FormationSlotDef { Id = "hau_top", Position = FormationPosition.Rear, Name = "Hậu trên" },
        new FormationSlotDef { Id = "trung_top", Position = FormationPosition.Middle, Name = "Trung trên" },
        new FormationSlotDef { Id = "hau_mid", Position = FormationPosition.Rear, Name = "Hậu giữa" },
        new FormationSlotDef { Id = "trung_mid", Position = FormationPosition.Middle, Name = "Trung giữa" },
        new FormationSlotDef { Id = "tien_mid", Position = FormationPosition.Front, Name = "Tiền giữa" },
        new FormationSlotDef { Id = "hau_bot", Position = FormationPosition.Rear, Name = "Hậu dưới" },
        new FormationSlotDef { Id = "trung_bot", Position = FormationPosition.Middle, Name = "Trung dưới" },
    };

    // Grid cells in row-major order; null = placeholder cell (no Tiền slot
    // on the top/bottom rows, exactly as sketched).
    public static readonly List<string?> GridOrder = new()
    {
        "hau_top", "trung_top", null,
        "hau_mid", "trung_mid", "tien_mid",
        "hau_bot", "trung_bot", null,
    };

    public static bool IsValidSlot(string slotId) => Slots.Any(s => s.Id == slotId);

    public static FormationPosition PositionOfSlot(string slotId) =>
        Slots.FirstOrDefault(s => s.Id == slotId)?.Position ?? FormationPosition.None;

    public static string PositionName(FormationPosition pos) => pos switch
    {
        FormationPosition.Rear => "Hậu quân",
        FormationPosition.Middle => "Trung quân",
        FormationPosition.Front => "Tiền quân",
        _ => "",
    };

    public static bool IsMain(string heroId) => heroId == MainHeroId;

    // Position of a hero inside a formation (None if not deployed).
    public static FormationPosition PositionOfHero(Dictionary<string, string> formation, string heroId)
    {
        foreach (var (slot, hero) in formation)
            if (hero == heroId) return PositionOfSlot(slot);
        return FormationPosition.None;
    }

    public static FormationPosition PositionOfHero(SaveData save, string heroId) =>
        save.Formation == null ? FormationPosition.None : PositionOfHero(save.Formation, heroId);

    // Team list derived from a formation, in grid order (top row first,
    // left column first within a row), skipping empty slots.
    public static List<string> FormationToTeam(Dictionary<string, string> formation)
    {
        var team = new List<string>();
        foreach (var slot in GridOrder)
        {
            if (slot == null) continue;
            if (formation.TryGetValue(slot, out var hero) && !string.IsNullOrEmpty(hero) && !team.Contains(hero))
                team.Add(hero);
        }
        // Defensive: any hero in the dict but not in grid order (should not happen).
        foreach (var hero in formation.Values)
            if (!string.IsNullOrEmpty(hero) && !team.Contains(hero)) team.Add(hero);
        return team;
    }

    // Repair a formation in place and return it: drop non-owned heroes,
    // duplicates and unknown slots, trim to MaxTeamSize, and force the main
    // hero into the Trung column (default: the center slot). If the formation
    // is empty, migrate it from the save's TeamIds (plus the main hero).
    // Returns true when the formation changed.
    public static bool EnsureFormation(SaveData save)
    {
        save.Formation ??= new Dictionary<string, string>();
        var before = new Dictionary<string, string>(save.Formation);
        var clean = new Dictionary<string, string>();
        foreach (var slot in GridOrder)
        {
            if (slot == null) continue;
            if (!save.Formation.TryGetValue(slot, out var hero) || string.IsNullOrEmpty(hero)) continue;
            if (!save.OwnedHeroIds.Contains(hero)) continue;
            if (clean.ContainsValue(hero)) continue;
            clean[slot] = hero;
        }
        // Migrate from TeamIds when the formation is empty (old saves).
        if (clean.Count == 0)
        {
            var team = (save.TeamIds ?? new List<string>())
                .Where(id => save.OwnedHeroIds.Contains(id)).Distinct().ToList();
            if (save.OwnedHeroIds.Contains(MainHeroId)) clean[CenterSlotId] = MainHeroId;
            var restSlots = GridOrder.Where(s => s != null && s != CenterSlotId).ToList();
            int idx = 0;
            foreach (var id in team)
            {
                if (id == MainHeroId) continue;
                if (clean.Count >= MaxTeamSize) break;
                if (idx >= restSlots.Count) break;
                clean[restSlots[idx]!] = id;
                idx++;
            }
        }
        // Trim to MaxTeamSize, keeping the main hero if present.
        if (clean.Count > MaxTeamSize)
        {
            var keep = new Dictionary<string, string>();
            if (clean.TryGetValue(CenterSlotId, out var main) && main == MainHeroId) keep[CenterSlotId] = main;
            foreach (var slot in GridOrder)
            {
                if (slot == null || keep.Count >= MaxTeamSize) break;
                if (clean.TryGetValue(slot, out var hero) && !keep.ContainsValue(hero)) keep[slot] = hero;
            }
            clean = keep;
        }
        // Force the main hero into the Trung column.
        if (save.OwnedHeroIds.Contains(MainHeroId))
        {
            string? mainSlot = null;
            foreach (var (slot, hero) in clean) if (hero == MainHeroId) mainSlot = slot;
            if (mainSlot == null)
            {
                if (clean.TryGetValue(CenterSlotId, out var occupant))
                {
                    // Move the occupant to the first empty slot (7 slots, max 5
                    // heroes, so one always exists unless the dict is corrupt).
                    var empty = GridOrder.FirstOrDefault(s => s != null && s != CenterSlotId && !clean.ContainsKey(s!));
                    if (empty != null) { clean[empty] = occupant; }
                    clean.Remove(CenterSlotId);
                }
                clean[CenterSlotId] = MainHeroId;
            }
            else if (PositionOfSlot(mainSlot) != FormationPosition.Middle)
            {
                // Main ended up outside Trung (corrupt save): move it to center.
                clean.Remove(mainSlot);
                if (clean.TryGetValue(CenterSlotId, out var occupant))
                {
                    var empty = GridOrder.FirstOrDefault(s => s != null && s != CenterSlotId && !clean.ContainsKey(s!));
                    if (empty != null) clean[empty] = occupant;
                    clean.Remove(CenterSlotId);
                }
                clean[CenterSlotId] = MainHeroId;
            }
        }
        save.Formation = clean;
        save.TeamIds = FormationToTeam(clean);
        return !DictionariesEqual(before, clean);
    }

    private static bool DictionariesEqual(Dictionary<string, string> a, Dictionary<string, string> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var (k, v) in a) if (!b.TryGetValue(k, out var bv) || bv != v) return false;
        return true;
    }

    // Pure validation for a drag/drop (no mutation). fromSlot is null when the
    // hero is dragged from the owned list, otherwise the slot it came from.
    public static bool CanPlace(SaveData save, string heroId, string slotId, string? fromSlot, out string error)
    {
        error = "";
        if (!IsValidSlot(slotId)) { error = "Ô không hợp lệ."; return false; }
        if (!save.OwnedHeroIds.Contains(heroId)) { error = "Tướng chưa sở hữu."; return false; }
        var form = save.Formation ?? new Dictionary<string, string>();
        if (fromSlot == slotId) return true; // dropping back onto its own slot: no-op
        if (IsMain(heroId) && PositionOfSlot(slotId) != FormationPosition.Middle)
        {
            error = "Main chỉ được ở cột Trung quân.";
            return false;
        }
        string? currentSlot = null;
        foreach (var (slot, hero) in form) if (hero == heroId) currentSlot = slot;
        if (fromSlot == null)
        {
            if (currentSlot != null) { error = "Tướng đã trong đội hình."; return false; }
            if (form.TryGetValue(slotId, out var occupant))
            {
                // Swap: the occupant goes back to the list — impossible for main.
                if (IsMain(occupant)) { error = "Không thể thay Main ở ô này (Main luôn ở cột Trung)."; return false; }
                return true; // count unchanged
            }
            if (form.Count >= MaxTeamSize) { error = "Đã đủ 5 tướng."; return false; }
            return true;
        }
        // From another slot: the hero must currently sit in fromSlot.
        if (currentSlot != fromSlot) { error = "Tướng không ở ô kéo."; return false; }
        if (form.TryGetValue(slotId, out var targetOccupant))
        {
            // Swap: the occupant moves to fromSlot — check its own main rule.
            if (IsMain(targetOccupant) && PositionOfSlot(fromSlot) != FormationPosition.Middle)
            {
                error = "Không thể đổi Main sang ô ngoài cột Trung.";
                return false;
            }
            return true;
        }
        return true;
    }

    // Apply a validated drag/drop. Returns false with an error when rejected.
    public static bool TryPlace(SaveData save, string heroId, string slotId, string? fromSlot, out string error)
    {
        if (!CanPlace(save, heroId, slotId, fromSlot, out error)) return false;
        save.Formation ??= new Dictionary<string, string>();
        if (fromSlot == slotId) return true;
        bool occupied = save.Formation.TryGetValue(slotId, out var occupant);
        if (occupied && fromSlot != null) save.Formation[fromSlot] = occupant; // swap
        else if (fromSlot != null) save.Formation.Remove(fromSlot); // move: origin freed
        // fromSlot == null + occupied: the occupant is overwritten below, i.e.
        // it goes back to the list.
        save.Formation[slotId] = heroId;
        save.TeamIds = FormationToTeam(save.Formation);
        return true;
    }

    // Remove a hero from the formation (click a slot, or drag back to the
    // list). The main hero can never be removed.
    public static bool TryRemove(SaveData save, string heroId, out string error)
    {
        error = "";
        if (IsMain(heroId)) { error = "Main luôn xuất trận, không thể gỡ."; return false; }
        save.Formation ??= new Dictionary<string, string>();
        string? slot = null;
        foreach (var (s, h) in save.Formation) if (h == heroId) slot = s;
        if (slot == null) { error = "Tướng không trong đội hình."; return false; }
        save.Formation.Remove(slot);
        save.TeamIds = FormationToTeam(save.Formation);
        return true;
    }
}
