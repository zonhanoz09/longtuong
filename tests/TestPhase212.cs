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
            && detail.GetNodeOrNull<ProgressBar>("DetailExpBar") != null
            && detail.GetNodeOrNull<GridContainer>("DetailStatsGrid") != null
            && detail.GetNodeOrNull<HBoxContainer>("SkillsRow") != null
            && detail.GetNodeOrNull<Label>("SkillsDetailLabel") != null
            && detail.GetNodeOrNull<HBoxContainer>("DismissRow") != null
            && detail.GetNodeOrNull<Button>("DismissRow/DismissButton") != null
            && detail.GetNodeOrNull<Label>("DetailPortraitLabel") == null
            && detail.GetNodeOrNull<Label>("DetailExpLabel") == null
            && detail.GetNodeOrNull<Button>("UnequipAllButton") == null, "detail structure (tabs, portrait flanked by 3+3 equipped slots, exp bar, stats grid, skills row, dismiss button — 2.17 removed portrait label, EXP text line and unequip-all button) is in the scene");
        var nameLabel = detail.GetNode<Label>("DetailNameLabel");
        Check(nameLabel.Text.Contains("testscene"), "detail name label shows the current tab hero (custom hero = username)");
        var tabBtns = heroList.GetNode<VBoxContainer>("TabButtons").GetChildren().OfType<Button>().ToList();
        Check(tabBtns.Count == gm.Save.OwnedHeroIds.Count && tabBtns.All(b => b.CustomMinimumSize == new Vector2(160, 56) && !b.Text.Contains("(Khóa)")), "tab buttons are one per owned hero at template size 160x56, no locked tabs (2.19)");
        var grid = heroList.GetNode<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        var gridButtons = grid.GetChildren().OfType<Button>().ToList();
        Check(gridButtons.Count == 28 && gridButtons.All(b => b.CustomMinimumSize == new Vector2(64, 64)), "28 grid cells at template size 64x64");
        // Behavior preserved: double-click equips, double-clicking the
        // equipped slot unequips it back (2.17 removed the unequip-all button).
        int bagBefore = gm.Save.BagEquipment.Count;
        gridButtons[0].EmitSignal(Control.SignalName.GuiInput, new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, DoubleClick = true });
        Check(gm.Save.BagEquipment.Count == bagBefore - 1, "double-click grid item still equips (pilot keeps behavior)");
        var leftSlots = detail.GetNode<VBoxContainer>("PortraitRow/LeftEquippedBox").GetChildren().OfType<Button>().ToList();
        leftSlots[0].EmitSignal(Control.SignalName.GuiInput, new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, DoubleClick = true });
        Check(gm.Save.HeroEquipment["custom_hero"]["weapon"].IsEmpty && gm.Save.BagEquipment.Count == bagBefore, "double-click equipped weapon slot unequips it back to the bag");
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
