using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.36: Nâng phẩm — upgrade an equipped item along the tier
/// chain for Bạc only; keeps enhance/refine/gems, tops up lines and
/// sockets for the new quality.
/// </summary>
public partial class TestPhase236 : Node
{
    private int _fails;

    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("tester236");

        // Chain.
        Check(ForgeService.UpgradeChain.Count == 6, "6 tiers in upgrade chain");
        Check(ForgeService.NextSet("tan_thu") == "chu_tuoc", "Tan Thu -> Chu Tuoc");
        Check(ForgeService.NextSet("ky_lan") == null, "Ky Lan is max");
        Check(ForgeService.NextSet("nope") == null, "unknown set has no next");
        Check(ForgeService.UpgradeSilverCost("bach_ho") == 70000, "Bach Ho upgrade costs 70000 silver");

        // Setup: +5 Tân Thủ weapon with a gem, equipped.
        gm.Save.BagEquipment.Clear();
        gm.Save.BagEquipment.Add(new BagEquipmentItem
        {
            SlotId = "weapon", Level = 5, SetId = "tan_thu", Tier = 30, Quality = "Lam",
            RefineLines = new List<RefineLine>
            {
                new() { StatId = "crit", Value = 0.03 },
                new() { StatId = "dodge", Value = 0.03 },
                new() { StatId = "speed", Value = 5 },
            },
            Sockets = new List<EquipmentSocket> { new() { Unlocked = true, GemId = "atk_1" } },
        });
        EquipmentService.EquipFromBag(gm.Save, "custom_hero", 0);

        // Gates.
        var rng = new System.Random(2);
        Check(ForgeService.TryUpgradeTier(gm.Save, "custom_hero", "weapon", rng) == ForgeService.UpgradeResult.LowLevel,
            "needs main level 50");
        gm.Save.Heroes["custom_hero"].Level = 50;
        gm.Save.Silver = 0;
        Check(ForgeService.TryUpgradeTier(gm.Save, "custom_hero", "weapon", rng) == ForgeService.UpgradeResult.NoSilver,
            "needs silver");
        gm.Save.Silver = 100000;
        Check(ForgeService.TryUpgradeTier(gm.Save, "custom_hero", "armor", rng) == ForgeService.UpgradeResult.EmptySlot,
            "empty slot fails");

        // Success: Tân Thủ -> Chu Tước.
        int silver0 = gm.Save.Silver;
        Check(ForgeService.TryUpgradeTier(gm.Save, "custom_hero", "weapon", rng) == ForgeService.UpgradeResult.Success,
            "upgrade succeeds");
        var eq = gm.Save.HeroEquipment["custom_hero"]["weapon"];
        Check(eq.SetId == "chu_tuoc" && eq.Tier == 50 && eq.Quality == "Tim", "item becomes Chu Tuoc");
        Check(eq.Level == 5, "enhance level kept");
        Check(eq.RefineLines.Count == 4, "lines topped up 3 -> 4 for Tim");
        Check(eq.Sockets.Count == 2 && eq.Sockets[0].GemId == "atk_1" && eq.Sockets[0].Unlocked && !eq.Sockets[1].Unlocked,
            "sockets grown, gem kept, new socket locked");
        Check(gm.Save.Silver == silver0 - 50000, "costs 50000 silver");

        // Full chain to max.
        gm.Save.Heroes["custom_hero"].Level = 100;
        gm.Save.Silver = 10000000;
        foreach (var s in new[] { "bach_ho", "thanh_long", "huyen_vu", "ky_lan" })
            Check(ForgeService.TryUpgradeTier(gm.Save, "custom_hero", "weapon", rng) == ForgeService.UpgradeResult.Success,
                "upgrade to " + s);
        Check(gm.Save.HeroEquipment["custom_hero"]["weapon"].SetId == "ky_lan", "reaches Ky Lan");
        Check(ForgeService.TryUpgradeTier(gm.Save, "custom_hero", "weapon", rng) == ForgeService.UpgradeResult.NoNext,
            "Ky Lan cannot upgrade");

        // UI: Nâng cấp tab.
        gm.CreateSeededSave("tester236b");
        int w = gm.Save.BagEquipment.FindIndex(b => b.SlotId == "weapon");
        EquipmentService.EquipFromBag(gm.Save, "custom_hero", w);
        gm.Save.Heroes["custom_hero"].Level = 50;
        gm.Save.Silver = 100000;
        var screenScene = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroScreen.tscn");
        var screen = screenScene.Instantiate<HeroScreen>();
        AddChild(screen);
        var tabRow = screen.GetNode<HBoxContainer>("RightPanel/TabRow");
        Check(tabRow.GetChildren().OfType<Button>().Count() == 6, "6 system tabs");
        tabRow.GetChildren().OfType<Button>().First(b => b.Text == "Nâng cấp").EmitSignal(BaseButton.SignalName.Pressed);
        var content = screen.GetNode<Control>("RightPanel/TabContent");
        var upBtn = content.FindChild("UpgradeButton", true, false) as Button;
        Check(upBtn != null && !upBtn.Disabled, "upgrade button enabled");
        upBtn.EmitSignal(BaseButton.SignalName.Pressed);
        Check(gm.Save.HeroEquipment["custom_hero"]["weapon"].SetId == "chu_tuoc", "UI upgrade works");
        var status = content.FindChild("UpgradeStatus", true, false) as Label;
        Check(status != null && status.Text.Contains("thành công"), "UI upgrade reports success");

        if (_fails == 0) GD.Print("TEST PHASE 2.36: ALL PASS");
        else GD.Print($"TEST PHASE 2.36: {_fails} FAILURES");
        GetTree().Quit();
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }
}
