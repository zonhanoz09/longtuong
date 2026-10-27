using Godot;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase212 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        // Scene templates for the data-driven rows/cells load.
        Check(ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroTabButton.tscn") != null, "hero tab button template loads");
        Check(ResourceLoader.Load<PackedScene>("res://features/hero/scenes/EquipmentCell.tscn") != null, "equipment cell template loads");
        gm.CreateSeededSave("testscene");
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads for scene-first pilot test");
        if (hubScene == null) { Finish(); return; }
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        Check(heroList != null, "hero list popup opens");
        if (heroList == null) { hub.QueueFree(); Finish(); return; }
        // Static detail structure now lives in the scene, not in code.
        var detail = heroList.GetNodeOrNull<VBoxContainer>("DetailBox");
        Check(detail != null
            && detail.GetNodeOrNull<Label>("DetailNameLabel") != null
            && detail.GetNodeOrNull<HBoxContainer>("EquipmentTabs") != null
            && detail.GetNodeOrNull<HBoxContainer>("PortraitRow") != null
            && detail.GetNodeOrNull<ColorRect>("PortraitRow/DetailPortrait") != null
            && detail.GetNodeOrNull<VBoxContainer>("PortraitRow/LeftEquippedBox") != null
            && detail.GetNodeOrNull<VBoxContainer>("PortraitRow/RightEquippedBox") != null
            && detail.GetNodeOrNull<Label>("DetailPortraitLabel") != null
            && detail.GetNodeOrNull<Label>("DetailExpLabel") != null
            && detail.GetNodeOrNull<ProgressBar>("DetailExpBar") != null
            && detail.GetNodeOrNull<Label>("DetailStatsLabel") != null
            && detail.GetNodeOrNull<Label>("DetailSkillsLabel") != null
            && detail.GetNodeOrNull<Button>("UnequipAllButton") != null, "detail structure (labels, tabs, portrait flanked by 3+3 equipped slots, exp bar, unequip button) is in the scene");
        var nameLabel = detail.GetNode<Label>("DetailNameLabel");
        Check(nameLabel.Text.Contains("testscene"), "detail name label shows the current tab hero (custom hero = username)");
        var tabBtns = heroList.GetNode<VBoxContainer>("TabButtons").GetChildren().OfType<Button>().ToList();
        Check(tabBtns.Count == 8 && tabBtns.All(b => b.CustomMinimumSize == new Vector2(160, 56)), "8 tab buttons at template size 160x56");
        var grid = heroList.GetNode<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        var gridButtons = grid.GetChildren().OfType<Button>().ToList();
        Check(gridButtons.Count == 28 && gridButtons.All(b => b.CustomMinimumSize == new Vector2(64, 64)), "28 grid cells at template size 64x64");
        // Behavior preserved: double-click equips, static unequip-all button clears.
        int bagBefore = gm.Save.BagEquipment.Count;
        gridButtons[0].EmitSignal(Control.SignalName.GuiInput, new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, DoubleClick = true });
        Check(gm.Save.BagEquipment.Count == bagBefore - 1, "double-click grid item still equips (pilot keeps behavior)");
        var unequipBtn = detail.GetNode<Button>("UnequipAllButton");
        unequipBtn.EmitSignal(Button.SignalName.Pressed);
        Check(gm.Save.HeroEquipment["custom_hero"].Values.All(eq => eq.IsEmpty) && gm.Save.BagEquipment.Count == bagBefore, "static unequip-all button empties all slots back to the bag");
        hub.ClosePopup();
        hub.QueueFree();
        Finish();
    }

    private void Finish()
    {
        GD.Print(_failures == 0 ? "TEST PHASE2.12: ALL PASS" : $"TEST PHASE2.12: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }
}
