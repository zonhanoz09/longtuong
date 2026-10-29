using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.35: Kế thừa — transfer enhance/refine/gems from a bag item
/// onto the equipped item; the old item is destroyed. Opens the Kế thừa tab.
/// </summary>
public partial class TestPhase235 : Node
{
    private int _fails;

    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("tester235");

        // Target: forged-style Chu Tước weapon equipped, fresh.
        gm.Save.BagEquipment.Clear();
        gm.Save.BagEquipment.Add(new BagEquipmentItem
        {
            SlotId = "weapon", SetId = "chu_tuoc", Tier = 50, Quality = "Luc",
            RefineLines = new List<RefineLine> { new() { StatId = "crit", Value = 0.03 } },
        });
        EquipmentService.EquipFromBag(gm.Save, "custom_hero", 0);
        GemService.TryUnlockSocket(gm.Save, "custom_hero", "weapon", 0); // open the hole for the transferred gem
        // Source: old Tân Thủ weapon, +5, 3 lines, one socketed gem.
        var src = new BagEquipmentItem
        {
            SlotId = "weapon", Level = 5, SetId = "tan_thu", Tier = 30, Quality = "Lam",
            RefineLines = new List<RefineLine>
            {
                new() { StatId = "dodge", Value = 0.03 },
                new() { StatId = "speed", Value = 5 },
                new() { StatId = "hp_pct", Value = 0.10 },
            },
            Sockets = new List<EquipmentSocket>
            {
                new() { Unlocked = true, GemId = "atk_1" },
            },
        };
        gm.Save.BagEquipment.Add(src);
        gm.Save.GemInventory["atk_1"] = 1; // the socketed gem came from the pouch

        Check(EquipmentService.InheritCostSilver == 5000, "inherit costs 5000 silver");
        int silver0 = gm.Save.Silver;
        Check(EquipmentService.TryInherit(gm.Save, "custom_hero", "weapon", 0), "inherit succeeds");
        var eq = gm.Save.HeroEquipment["custom_hero"]["weapon"];
        Check(eq.Level == 5, "enhance level transferred");
        Check(eq.RefineLines.Count == 3 && eq.RefineLines[0].StatId == "dodge", "refine lines transferred");
        Check(eq.Sockets[0].GemId == "atk_1", "socketed gem transferred");
        Check(gm.Save.BagEquipment.Count == 0, "old item destroyed");
        Check(gm.Save.Silver == silver0 - 5000, "inherit consumes silver");

        // Failures.
        Check(!EquipmentService.TryInherit(gm.Save, "custom_hero", "weapon", 5), "bad bag index fails");
        gm.Save.BagEquipment.Add(new BagEquipmentItem { SlotId = "armor" });
        Check(!EquipmentService.TryInherit(gm.Save, "custom_hero", "weapon", 0), "wrong-slot source fails");
        gm.Save.BagEquipment.Clear();
        gm.Save.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon" });
        gm.Save.Silver = 0;
        Check(!EquipmentService.TryInherit(gm.Save, "custom_hero", "weapon", 0), "no silver fails");
        gm.Save.Silver = 100000;
        Check(!EquipmentService.TryInherit(gm.Save, "custom_hero", "armor", 0), "empty target fails");

        // Target's old gems return to the pouch; overflow gems too.
        gm.Save.BagEquipment.Clear();
        gm.Save.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon" }); // fresh target
        EquipmentService.EquipFromBag(gm.Save, "custom_hero", 0);
        gm.Save.BagEquipment.RemoveAt(0); // swapped-out old item
        var tgt = gm.Save.HeroEquipment["custom_hero"]["weapon"];
        GemService.TryUnlockSocket(gm.Save, "custom_hero", "weapon", 0);
        tgt.Sockets[0].GemId = "speed_1"; // target already has a gem
        gm.Save.GemInventory.Clear();
        var src2 = new BagEquipmentItem
        {
            SlotId = "weapon",
            Sockets = new List<EquipmentSocket>
            {
                new() { Unlocked = true, GemId = "crit_1" },
                new() { Unlocked = true, GemId = "dodge_1" }, // no free socket -> pouch
            },
        };
        gm.Save.BagEquipment.Add(src2);
        EquipmentService.TryInherit(gm.Save, "custom_hero", "weapon", 0);
        Check(GemService.GemCount(gm.Save, "speed_1") == 1, "target's old gem returns to pouch");
        Check(tgt.Sockets[0].GemId == "crit_1", "source gem socketed");
        Check(GemService.GemCount(gm.Save, "dodge_1") == 1, "overflow gem goes to pouch");

        // UI: Kế thừa tab lists the source and inherits.
        gm.Save.BagEquipment.Clear();
        gm.Save.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon", Level = 2 });
        var screenScene = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroScreen.tscn");
        var screen = screenScene.Instantiate<HeroScreen>();
        AddChild(screen);
        var tabRow = screen.GetNode<HBoxContainer>("RightPanel/TabRow");
        tabRow.GetChildren().OfType<Button>().First(b => b.Text == "Kế thừa").EmitSignal(BaseButton.SignalName.Pressed);
        var content = screen.GetNode<Control>("RightPanel/TabContent");
        Check(content.FindChild("LockedNote", true, false) == null, "Kế thừa tab is open");
        var srcBtn = content.FindChild("InheritSrc_0", true, false) as Button;
        Check(srcBtn != null, "source item listed");
        srcBtn.EmitSignal(BaseButton.SignalName.Pressed);
        var inheritBtn = content.FindChild("InheritButton", true, false) as Button;
        Check(inheritBtn != null && !inheritBtn.Disabled, "inherit button enabled");
        inheritBtn.EmitSignal(BaseButton.SignalName.Pressed);
        Check(gm.Save.HeroEquipment["custom_hero"]["weapon"].Level == 2, "UI inherit transfers the level");
        Check(gm.Save.BagEquipment.Count == 0, "UI inherit destroys the source");
        var status = content.FindChild("InheritStatus", true, false) as Label;
        Check(status != null && status.Text.Contains("thành công"), "UI inherit reports success");

        if (_fails == 0) GD.Print("TEST PHASE 2.35: ALL PASS");
        else GD.Print($"TEST PHASE 2.35: {_fails} FAILURES");
        GetTree().Quit();
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }
}
