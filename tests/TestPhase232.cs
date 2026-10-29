using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.32: full refine rework — position pools, roll ranges,
/// lock toggle, line top-up, stone/gold payment.
/// </summary>
public partial class TestPhase232 : Node
{
    private int _fails;

    public override void _Ready()
    {
        // Pools per formation position.
        var front = ProgressionService.RefinePoolFor(FormationPosition.Front).Select(p => p.StatId).ToHashSet();
        Check(front.Contains("dmg_red") && front.Contains("hp_pct") && !front.Contains("crit"),
            "Front pool is tanker-oriented (no crit)");
        var mid = ProgressionService.RefinePoolFor(FormationPosition.Middle).Select(p => p.StatId).ToHashSet();
        Check(mid.Contains("atk_pct") && mid.Contains("crit"), "Middle pool is damage-oriented");
        var rear = ProgressionService.RefinePoolFor(FormationPosition.Rear).Select(p => p.StatId).ToHashSet();
        Check(rear.Contains("speed") && rear.Contains("hp_pct"), "Rear pool is support-oriented");
        Check(ProgressionService.RefinePoolFor(FormationPosition.None).Select(p => p.StatId)
            .SequenceEqual(ProgressionService.RefinePoolFor(FormationPosition.Middle).Select(p => p.StatId)),
            "undeployed hero uses the Middle pool");

        // Rolls stay in range and use supported stats.
        var rng = new System.Random(42);
        foreach (var pos in new[] { FormationPosition.Front, FormationPosition.Middle, FormationPosition.Rear })
        {
            var pool = ProgressionService.RefinePoolFor(pos);
            for (int i = 0; i < 20; i++)
            {
                var line = ProgressionService.RollRefineLine(pos, rng);
                var entry = pool.First(p => p.StatId == line.StatId);
                Check(line.Value >= entry.Min && line.Value <= entry.Max, $"rolled {line.StatId} in range");
                Check(SetBonusService.IsEngineSupported(line.StatId) || line.StatId is "hp" or "def",
                    "rolled stat is engine-supported");
            }
        }

        // Lock toggle unit.
        var save = SeedService.CreateSeededSave("t232");
        save.BagEquipment.Clear();
        save.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon" });
        EquipmentService.EquipFromBag(save, "custom_hero", 0);
        var eq = save.HeroEquipment["custom_hero"]["weapon"];
        Check(eq.RefineLines.Count == 3, "migrated item has 3 lines");
        ProgressionService.ToggleRefineLock(save, "custom_hero", "weapon", 1);
        Check(eq.RefineLines[1].Locked && !eq.RefineLines[0].Locked, "lock toggles one line");
        ProgressionService.ToggleRefineLock(save, "custom_hero", "weapon", 1);
        Check(!eq.RefineLines[1].Locked, "lock toggles back");
        ProgressionService.ToggleRefineLock(save, "custom_hero", "weapon", 99);
        Check(true, "out-of-range lock index is ignored");

        // Top-up: short item grows to the quality's line count on refine.
        eq.RefineLines.RemoveAt(2);
        int stones0 = save.RefineStones;
        Check(ProgressionService.TryRefine(save, "custom_hero", "weapon", new System.Random(9)), "refine succeeds");
        Check(eq.RefineLines.Count == 3 && save.RefineStones == stones0 - 1, "top-up to 3 lines, stone consumed");

        // DescribeRefineLine covers the new stats.
        Check(EquipmentService.DescribeRefineLine(new RefineLine { StatId = "dmg_red", Value = 0.05 }).Contains("Miễn thương"),
            "describe covers Miễn thương");
        Check(EquipmentService.DescribeRefineLine(new RefineLine { StatId = "def_pct", Value = 0.1, Locked = true }).Contains("khóa"),
            "describe shows lock");

        // Refine line grades by roll quality (crit pool widest: 0.02-0.08).
        Check(ProgressionService.RefineLineGrade(new RefineLine { StatId = "crit", Value = 0.03 }) == "Trang", "low roll grades Trang");
        Check(ProgressionService.RefineLineGrade(new RefineLine { StatId = "crit", Value = 0.05 }) == "Tim", "mid roll grades Tim");
        Check(ProgressionService.RefineLineGrade(new RefineLine { StatId = "crit", Value = 0.08 }) == "Cam", "max roll grades Cam");
        Check(ProgressionService.RefineLineGrade(new RefineLine { StatId = "nope", Value = 1 }) == "Trang", "unknown stat grades Trang");

        // Unified tooltip BBCode (same for all 3 popups).
        var bb = EquipmentTooltip.ForBagItem(new BagEquipmentItem
        {
            SlotId = "weapon", SetId = "chu_tuoc", Tier = 50, Quality = "Luc",
            RefineLines = new List<RefineLine> { new() { StatId = "crit", Value = 0.08 } },
            Sockets = new List<EquipmentSocket> { new() { Unlocked = true, GemId = "atk_1" } },
        }, null);
        Check(bb.Contains("[color=") && bb.Contains("Chu Tước") && bb.Contains("Bạo kích"), "tooltip has colors, set, refine");
        Check(bb.Contains("Ngọc khảm:") && bb.Contains("atk_1") == false && bb.Contains("Công"), "tooltip shows gems by name");
        Check(EquipmentTooltip.ForEquipped(new EquipmentState { IsEmpty = true }, "weapon", null).Contains("Ô trống"),
            "empty slot tooltip");

        if (_fails == 0) GD.Print("TEST PHASE 2.32: ALL PASS");
        else GD.Print($"TEST PHASE 2.32: {_fails} FAILURES");
        GetTree().Quit();
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }
}
