using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.37: Đúc Lại — recast an equipped item along the tier chain
/// with the FULL cost (blueprint learned + ore + Bạc); replaces the 2.36
/// dev-only Bạc cost. Keeps enhance/refine/gems, tops up lines and
/// sockets for the new set. The Nâng cấp tab is gone (moved into Cường hóa).
/// Quality remap: Chu Tước=Lục, Bạch Hổ=Lam, Thanh Long=Tím, ...
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
        Check(ForgeService.RecastSilverCost("bach_ho") == 70000, "Bach Ho recast costs 70000 silver");
        Check(ForgeService.RecastOreCost("bach_ho") == 35, "Bach Ho recast costs 35 ore");

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

        // Gates: level -> blueprint -> ore -> silver.
        var rng = new System.Random(2);
        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", rng) == ForgeService.RecastResult.LowLevel,
            "needs main level 50");
        gm.Save.Heroes["custom_hero"].Level = 50;
        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", rng) == ForgeService.RecastResult.NoBlueprint,
            "needs blueprint");
        gm.Save.Blueprints["chu_tuoc"] = true;
        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", rng) == ForgeService.RecastResult.NoOre,
            "needs ore");
        gm.Save.Materials[ForgeService.OreId] = 999;
        gm.Save.Silver = 0;
        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", rng) == ForgeService.RecastResult.NoSilver,
            "needs silver");
        gm.Save.Silver = 100000;
        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "armor", rng) == ForgeService.RecastResult.EmptySlot,
            "empty slot fails");

        // Success: Tân Thủ -> Chu Tước (Lục after the 2.37 remap).
        int silver0 = gm.Save.Silver, ore0 = ForgeService.OreCount(gm.Save);
        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", rng) == ForgeService.RecastResult.Success,
            "recast succeeds");
        var eq = gm.Save.HeroEquipment["custom_hero"]["weapon"];
        Check(eq.SetId == "chu_tuoc" && eq.Tier == 50 && eq.Quality == "Luc", "item becomes Chu Tuoc (Luc)");
        Check(eq.Level == 5, "enhance level kept");
        Check(eq.RefineLines.Count == 4, "lines topped up 3 -> 4 for Chu Tuoc");
        Check(eq.Sockets.Count == 2 && eq.Sockets[0].GemId == "atk_1" && eq.Sockets[0].Unlocked && !eq.Sockets[1].Unlocked,
            "sockets grown, gem kept, new socket locked");
        Check(gm.Save.Silver == silver0 - 50000 && ForgeService.OreCount(gm.Save) == ore0 - 25,
            "costs 50000 silver + 25 ore");

        // Full chain to max (blueprints + ore + silver ready).
        gm.Save.Heroes["custom_hero"].Level = 100;
        gm.Save.Silver = 10000000;
        gm.Save.Materials[ForgeService.OreId] = 99999;
        foreach (var s in new[] { "bach_ho", "thanh_long", "huyen_vu", "ky_lan" })
        {
            gm.Save.Blueprints[s] = true;
            Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", rng) == ForgeService.RecastResult.Success,
                "recast to " + s);
        }
        var eqMax = gm.Save.HeroEquipment["custom_hero"]["weapon"];
        Check(eqMax.SetId == "ky_lan" && eqMax.Quality == "Do", "reaches Ky Lan (Do)");
        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", rng) == ForgeService.RecastResult.NoNext,
            "Ky Lan cannot recast");

        // UI: Đúc Lại lives in the Cường hóa tab now (no Nâng cấp tab).
        gm.CreateSeededSave("tester236b");
        int w = gm.Save.BagEquipment.FindIndex(b => b.SlotId == "weapon");
        EquipmentService.EquipFromBag(gm.Save, "custom_hero", w);
        gm.Save.Heroes["custom_hero"].Level = 50;
        gm.Save.Silver = 100000;
        gm.Save.Blueprints["chu_tuoc"] = true;
        gm.Save.Materials[ForgeService.OreId] = 999;
        var screenScene = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroScreen.tscn");
        var screen = screenScene.Instantiate<HeroScreen>();
        AddChild(screen);
        var tabRow = screen.GetNode<HBoxContainer>("RightPanel/TabRow");
        var tabNames = tabRow.GetChildren().OfType<Button>().Select(b => b.Text).ToList();
        Check(tabNames.Count == 5 && !tabNames.Contains("Nâng cấp"), "5 tabs, Nâng cấp removed");
        var content = screen.GetNode<Control>("RightPanel/TabContent");
        var recastBtn = content.FindChild("ActionRecast", true, false) as Button;
        Check(recastBtn != null && recastBtn.Text == "Đúc lại" && !recastBtn.Disabled, "recast button enabled");
        recastBtn.EmitSignal(BaseButton.SignalName.Pressed);
        Check(gm.Save.HeroEquipment["custom_hero"]["weapon"].SetId == "chu_tuoc", "UI recast works");
        var status = content.FindChild("ActionStatus", true, false) as Label;
        Check(status != null && status.Text.Contains("thành công"), "UI recast reports success");
        var projLabels = content.FindChildren("*", "Label", true, false).OfType<Label>().Select(l => l.Text).ToList();
        Check(projLabels.Any(t => t.StartsWith("▸")), "recast projection summary shown");
        Check(projLabels.Any(t => t.Contains("Khoáng thạch")), "ore requirement shown");

        if (_fails == 0) GD.Print("TEST PHASE 2.36: ALL PASS");
        else GD.Print($"TEST PHASE 2.36: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }
}
