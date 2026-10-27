using Godot;
using System.Linq;

namespace LongTuong;

// Phase 2.16: Vo Tuong popup layout from Huynh's sketch — title centred,
// hero tabs left, detail middle (Trang bi / locked Trang suc tabs, portrait
// flanked by 3+3 equipped slots, stats grid, skills), bag grid right.
// Behaviour (double-click equip/unequip, tooltip cells) is unchanged.
[GlobalClass]
public partial class TestPhase216 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("testvo");
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads");
        if (hubScene == null) { Finish(); return; }
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        hub.GetNode<Button>("FunctionBar/HeroListButton").EmitSignal(Button.SignalName.Pressed);
        Check(hub.IsPopupOpen, "Vo tuong button opens the popup");
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        Check(heroList != null, "popup widget is the hero list scene");
        if (heroList == null) { hub.QueueFree(); Finish(); return; }

        Check(heroList.Position == Vector2.Zero && heroList.Size == new Vector2(1000, 600), "popup content is 1000x600 (2.18)");
        Check(heroList.Scale == Vector2.One, "popup root is not scaled (editor scale artifact fixed)");
        var title = heroList.GetNode<Label>("TitleLabel");
        Check(title.Text == "Vo Tuong" && title.HorizontalAlignment == HorizontalAlignment.Center, "title is centred Vo Tuong");

        var tabs = heroList.GetNode<VBoxContainer>("TabButtons");
        // Tabs width is content-driven (longest hero name + "(Khóa)"), so
        // only the position, height and a sane width range are asserted.
        Check(tabs.Position == new Vector2(16, 50) && tabs.Size.Y == 540 && tabs.Size.X >= 160 && tabs.Size.X <= 180, "hero tabs column at left (16,50), height 540, width 160-180");
        var detail = heroList.GetNode<VBoxContainer>("DetailBox");
        Check(detail.Position == new Vector2(192, 50) && detail.Size == new Vector2(440, 540), "detail column in the middle (192,50) 440x540");
        var gridPanel = heroList.GetNode<VBoxContainer>("GridPanel");
        Check(gridPanel.Position == new Vector2(648, 50) && gridPanel.Size == new Vector2(336, 540), "bag grid column at right (648,50) 336x540");

        var equipTab = heroList.GetNode<Button>("DetailBox/EquipmentTabs/EquipmentTabButton");
        var accessoryTab = heroList.GetNode<Button>("DetailBox/EquipmentTabs/AccessoryTabButton");
        Check(equipTab.Text == "Trang bị" && !equipTab.Disabled, "Trang bi tab is active");
        Check(accessoryTab.Text == "Trang sức" && accessoryTab.Disabled, "Trang suc tab is locked (no separate jewelry system)");

        var row = heroList.GetNode<HBoxContainer>("DetailBox/PortraitRow");
        var left = heroList.GetNode<VBoxContainer>("DetailBox/PortraitRow/LeftEquippedBox").GetChildren().OfType<Button>().ToList();
        var right = heroList.GetNode<VBoxContainer>("DetailBox/PortraitRow/RightEquippedBox").GetChildren().OfType<Button>().ToList();
        var portrait = heroList.GetNode<ColorRect>("DetailBox/PortraitRow/DetailPortrait");
        Check(left.Count == 3 && right.Count == 3, "equipped slots are 3 left + 3 right");
        Check(left.Concat(right).All(b => b.CustomMinimumSize == new Vector2(56, 56)), "equipped cells are 56x56 (2.18)");
        Check(row.GetChildren().IndexOf(portrait) == 1, "portrait sits between the left and right slot columns");
        Check(portrait.CustomMinimumSize == new Vector2(260, 180), "portrait is 260x180 (matches the 3x56+2x6 icon columns)");

        var statsGrid = heroList.GetNode<GridContainer>("DetailBox/DetailStatsGrid");
        Check(statsGrid.Columns == 4 && statsGrid.GetChildCount() == 16, "stats grid is 4 columns x 4 rows (label, value, label, value)");
        Check(heroList.GetNode<Label>("DetailBox/DetailStatsGrid/StatAtkLabel").Text == "Sức:"
            && heroList.GetNode<Label>("DetailBox/DetailStatsGrid/StatHpLabel").Text == "Sinh mệnh:"
            && heroList.GetNode<Label>("DetailBox/DetailStatsGrid/StatRoleLabel").Text == "Vị trí trong quân:", "stats grid labels are Suc / Sinh menh / Vi tri");
        var skillsLabel = heroList.GetNode<Label>("DetailBox/SkillsRow/SkillsLabel");
        var skillsValue = heroList.GetNode<Label>("DetailBox/SkillsRow/SkillsValueLabel");
        Check(skillsLabel.Text == "Kỹ năng" && !string.IsNullOrEmpty(skillsValue.Text), "skills row shows Ky nang + the ult name");

        var grid = heroList.GetNode<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        var gridButtons = grid.GetChildren().OfType<Button>().ToList();
        Check(gridButtons.Count == 28, "bag grid still has 28 cells");
        int bagBefore = gm.Save.BagEquipment.Count;
        gridButtons[0].EmitSignal(Control.SignalName.GuiInput, new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, DoubleClick = true });
        Check(gm.Save.BagEquipment.Count == bagBefore - 1, "double-click grid item still equips");
        // Seed bag order follows Slots, so grid[0] is a weapon: it equipped
        // into left[0] (the weapon slot). Double-clicking it unequips back.
        left[0].EmitSignal(Control.SignalName.GuiInput, new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, DoubleClick = true });
        Check(gm.Save.BagEquipment.Count == bagBefore, "double-click left equipped slot unequips it back to bag");

        hub.ClosePopup();
        hub.QueueFree();
        Finish();
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }

    private void Finish()
    {
        GD.Print(_failures == 0 ? "TEST PHASE2.16: ALL PASS" : $"TEST PHASE2.16: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }
}
