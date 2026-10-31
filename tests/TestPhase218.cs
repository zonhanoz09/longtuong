using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

// Phase 2.18: middle detail column aligned to the sketch (portrait row
// flush to both column edges, 56x56 icons with 6px gaps = 180px column
// height, "Kinh nghiệm" title above a full-width EXP bar, stats in a
// 4-column label/value grid, skills row + dismiss button bottom-right),
// plus DismissHero: removing a hero from the owned roster.
[GlobalClass]
public partial class TestPhase218 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private async void RunChecks()
    {
        // --- DismissHero rules, on plain saves (no scene needed) ---
        var s = SeedService.CreateSeededSave("test218");
        // Seeded (2.22): only custom owned, team = [custom]. Give two more
        // heroes so dismiss has somewhere to fall back to.
        s.OwnedHeroIds.Add("quan_vu");
        s.OwnedHeroIds.Add("luu_bi");
        s.TeamIds = new List<string> { "custom_hero", "quan_vu" };
        EquipmentService.EquipFromBag(s, "quan_vu", 0);
        int bagBefore = s.BagEquipment.Count;
        Check(ProgressionService.DismissHero(s, "quan_vu"), "dismissing an owned, non-permanent hero succeeds");
        Check(!s.OwnedHeroIds.Contains("quan_vu"), "dismissed hero leaves the owned list");
        Check(s.TeamIds.Count == 1 && s.TeamIds[0] == "custom_hero", "team falls back to the remaining hero");
        Check(s.BagEquipment.Count == bagBefore + 1, "equipped item returns to the bag on dismiss (not lost)");
        Check(s.HeroEquipment["quan_vu"].Values.All(eq => eq.IsEmpty), "dismissed hero's slots are all empty");
        // 2.22: Luu Bi is a normal hero now, so he CAN be dismissed.
        Check(ProgressionService.DismissHero(s, "luu_bi"), "Luu Bi can be dismissed now (normal hero, 2.22)");

        // Blocked cases.
        Check(!ProgressionService.DismissHero(s, "custom_hero"), "custom hero cannot be dismissed (the only permanent hero)");
        Check(!ProgressionService.DismissHero(s, "truong_phi"), "locked hero cannot be dismissed");
        // The roster can never become empty: EnsureDefaults always keeps the
        // custom hero owned, and DismissHero also refuses the last owned hero
        // as defence-in-depth.
        Check(s.OwnedHeroIds.Count == 1 && s.OwnedHeroIds.Contains("custom_hero"),
            "after dismiss, only the permanent custom hero remains");

        // --- Scene layout: portrait row, stats grid, skills row, dismiss ---
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("test218");
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads");
        if (hubScene == null) { Finish(); return; }
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        hub.GetNode<Button>("FunctionBar/HeroListButtonIcon/HeroListButton").EmitSignal(Button.SignalName.Pressed);
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        Check(heroList != null, "popup widget is the hero list scene");
        if (heroList == null) { hub.QueueFree(); Finish(); return; }
        var detail = heroList.GetNode<VBoxContainer>("DetailBox");
        var row = heroList.GetNode<HBoxContainer>("DetailBox/PortraitRow");
        var left = heroList.GetNode<VBoxContainer>("DetailBox/PortraitRow/LeftEquippedBox");
        var right = heroList.GetNode<VBoxContainer>("DetailBox/PortraitRow/RightEquippedBox");
        var portrait = heroList.GetNode<ColorRect>("DetailBox/PortraitRow/DetailPortrait");
        Check(left.Position.X == 0, "left icon column is flush with the detail column's left edge");
        Check(portrait.SizeFlagsHorizontal == (Control.SizeFlags.Fill | Control.SizeFlags.Expand),
            "portrait expands to fill the row, pushing the right column flush right");
        Check(left.GetChildren().OfType<Button>().All(b => b.CustomMinimumSize == new Vector2(56, 56))
            && right.GetChildren().OfType<Button>().All(b => b.CustomMinimumSize == new Vector2(56, 56)),
            "equipped icons are 56x56 on both sides");
        Check(row.Size.Y == 180, "portrait row is 180px tall (3x56 + 2x6 gaps)");
        var expTitle = heroList.GetNode<Label>("DetailBox/DetailExpTitleLabel");
        Check(expTitle.Text == "Kinh nghiệm" && expTitle.HorizontalAlignment == HorizontalAlignment.Center,
            "Kinh nghiem title is centred above the EXP bar");
        var grid = heroList.GetNode<GridContainer>("DetailBox/DetailStatsGrid");
        Check(grid.Columns == 4, "stats grid has 4 columns (label, value, label, value)");
        // Container child positions are only set on the next frame's sort,
        // so assert the minimum sizes that drive the layout instead: label
        // columns 130, value columns 80, so the right pair starts at
        // 130+80+6 = 216px — straight columns, no guessing from Position.
        var atkLabel = heroList.GetNode<Label>("DetailBox/DetailStatsGrid/StatAtkLabel");
        var hpLabel = heroList.GetNode<Label>("DetailBox/DetailStatsGrid/StatHpLabel");
        var atkValue = heroList.GetNode<Label>("DetailBox/DetailStatsGrid/StatAtkValue");
        Check(grid.GetChildren().OfType<Label>().Count() == 16
            && atkLabel.CustomMinimumSize.X == 130 && hpLabel.CustomMinimumSize.X == 130
            && atkValue.CustomMinimumSize.X == 80,
            "stats grid has 16 cells, label columns 130 and value columns 80 (right pair starts at 216)");
        Check(heroList.GetNode<ColorRect>("DetailBox/StatsDivider") != null, "divider line sits between stats and skills");
        var skillsValue = heroList.GetNode<Label>("DetailBox/SkillsRow/SkillsValueLabel");
        var skillsLabelNode = heroList.GetNode<Label>("DetailBox/SkillsRow/SkillsLabel");
        Check(skillsLabelNode.Text == "Kỹ năng" && skillsLabelNode.CustomMinimumSize.X == 200
            && !string.IsNullOrEmpty(skillsValue.Text),
            "skills row: Ky nang label is 200 wide, so the ult name starts at x=200+ (sketch proportion)");
        var dismiss = heroList.GetNode<Button>("DetailBox/DismissRow/DismissButton");
        Check(dismiss.Text == "Sa thải", "dismiss button is labelled Sa thai");
        Check(dismiss.Disabled, "dismiss is disabled for the custom hero (never-locked)");

        // Dismiss from the UI: give the account Quan Vu, reopen so the new
        // tab appears (2.19: tabs are built from the owned list only), then
        // switch to him and press Sa thải.
        var save = gm.Save;
        save.OwnedHeroIds.Add("quan_vu");
        save.TeamIds = new List<string> { "custom_hero", "quan_vu" };
        gm.SelectedTeamIds = new List<string>(save.TeamIds);
        hub.ClosePopup();
        hub.GetNode<Button>("FunctionBar/HeroListButtonIcon/HeroListButton").EmitSignal(Button.SignalName.Pressed);
        var heroList2 = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        var tabBtns = heroList2.GetNode<VBoxContainer>("TabButtons").GetChildren().OfType<Button>().ToList();
        var dismiss2 = heroList2.GetNode<Button>("DetailBox/DismissRow/DismissButton");
        Check(tabBtns.Any(b => b.Text.Contains("Quan Vũ")), "newly owned hero appears as a tab after reopen");
        var quanTab = tabBtns.First(b => b.Text.Contains("Quan Vũ"));
        quanTab.EmitSignal(Button.SignalName.Pressed);
        // Tab press rebuilds via CallDeferred, so wait two frames for it.
        await ToSignal(GetTree(), "process_frame");
        await ToSignal(GetTree(), "process_frame");
        Check(!dismiss2.Disabled, "dismiss is enabled for an owned, non-permanent hero");
        dismiss2.EmitSignal(Button.SignalName.Pressed);
        // Dismiss rebuilds via CallDeferred, so wait for the tab list to refresh.
        await ToSignal(GetTree(), "process_frame");
        await ToSignal(GetTree(), "process_frame");
        Check(!gm.Save.OwnedHeroIds.Contains("quan_vu"), "pressing Sa thai removes the hero from the owned list");
        Check(gm.SelectedHeroId == "custom_hero", "default hero falls back after dismiss");
        Check(gm.Save.TeamIds.Contains("custom_hero") && !gm.Save.TeamIds.Contains("quan_vu"),
            "team no longer contains the dismissed hero");
        // 2.19: the dismissed hero's tab is gone from the list entirely.
        var tabBtnsAfter = heroList2.GetNode<VBoxContainer>("TabButtons").GetChildren().OfType<Button>().ToList();
        Check(!tabBtnsAfter.Any(b => b.Text.Contains("Quan Vũ")), "dismissed hero has no tab in the popup anymore");

        hub.ClosePopup();
        hub.QueueFree();
        Finish();
    }

    private void Finish()
    {
        GD.Print(_failures == 0 ? "TEST PHASE2.18: ALL PASS" : $"TEST PHASE2.18: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }
}
