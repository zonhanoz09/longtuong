using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

// Phase 2.17: Vo Tuong popup is fixed at 1000x560 (shrunk from 1180x660,
// Hub stays visible around it), the portrait label, the EXP/stars/shards
// text line and the unequip-all button are removed (only the EXP bar
// stays), the bag grid stays fixed at 28 cells, and old accounts gain
// the custom hero on load.
[GlobalClass]
public partial class TestPhase217 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        // Old account (pre-2.10, no custom hero, no Luu Bi) gains both on load.
        var old = new SaveData { OwnedHeroIds = new List<string> { "quan_vu", "truong_phi" } };
        ProgressionService.EnsureDefaults(old);
        Check(old.OwnedHeroIds.Contains("custom_hero"), "old account gains the custom hero on load");
        Check(old.OwnedHeroIds.Contains("luu_bi"), "old account still gains Luu Bi on load");
        Check(old.Shards["custom_hero"] == 0, "custom hero grant costs no shards");

        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("nhan");
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads");
        if (hubScene == null) { Finish(); return; }
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        hub.GetNode<Button>("FunctionBar/HeroListButton").EmitSignal(Button.SignalName.Pressed);
        Check(hub.IsPopupOpen, "Vo tuong button opens the popup");
        Check(HubScene.HeroListPopupSize == new Vector2(1000, 600), "hero list popup size is fixed 1000x600 (2.18)");
        var frame = hub.GetNode<Control>("PopupFrame");
        Check(frame.Size == new Vector2(1036, 636) && frame.Position == new Vector2(122, 42),
            "popup frame is 1036x636 centred at (122,42), Hub visible around it");
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        Check(heroList != null, "popup widget is the hero list scene");
        if (heroList == null) { hub.QueueFree(); Finish(); return; }
        Check(heroList.Position == Vector2.Zero && heroList.Size == new Vector2(1000, 600), "popup content is 1000x600 (2.18)");

        var tabs = heroList.GetNode<VBoxContainer>("TabButtons");
        Check(tabs.Position == new Vector2(16, 50) && tabs.Size.Y == 540 && tabs.Size.X >= 160 && tabs.Size.X <= 180,
            "hero tabs column at (16,50), height 540");
        var tabBtns = tabs.GetChildren().OfType<Button>().ToList();
        Check(tabBtns.Count == gm.Save.OwnedHeroIds.Count, "tabs are owned heroes only (2.19)");
        Check(!tabBtns.Any(b => b.Text.Contains("(Khóa)")) && !tabBtns.Any(b => b.Text.Contains("Quan Vũ")),
            "no locked tabs, and a locked hero (Quan Vu) has no tab at all");
        var detail = heroList.GetNode<VBoxContainer>("DetailBox");
        Check(detail.Position == new Vector2(192, 50) && detail.Size == new Vector2(440, 540),
            "detail column is (192,50) 440x540");
        var gridPanel = heroList.GetNode<VBoxContainer>("GridPanel");
        Check(gridPanel.Position == new Vector2(648, 50) && gridPanel.Size == new Vector2(336, 540),
            "bag grid column is (648,50) 336x540");

        // Removed nodes (2.17 deletions).
        Check(detail.GetNodeOrNull<Label>("DetailPortraitLabel") == null, "portrait label node is removed");
        Check(detail.GetNodeOrNull<Label>("DetailExpLabel") == null, "EXP/stars/shards text label node is removed");
        Check(detail.GetNodeOrNull<Button>("UnequipAllButton") == null, "unequip-all button node is removed");
        Check(detail.GetNode<ProgressBar>("DetailExpBar") != null, "experience bar is kept");
        Check(detail.GetChildren().Count == 10, "detail column has exactly 10 children (name, tabs, portrait row, exp title, exp bar, stats grid, divider, skills row, skills detail, dismiss row)");
        var dismissRow = detail.GetNode<HBoxContainer>("DismissRow");
        Check(dismissRow.Position.Y + dismissRow.Size.Y <= 540, "detail content fits its fixed 540px column (no overflow)");
        var accessoryTab = detail.GetNode<Button>("EquipmentTabs/AccessoryTabButton");
        Check(accessoryTab.Text == "Trang sức" && accessoryTab.Disabled, "Trang suc tab stays locked");

        // Fixed 28-cell grid, filled cells show the seed items.
        var grid = heroList.GetNode<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        var gridButtons = grid.GetChildren().OfType<Button>().ToList();
        Check(gridButtons.Count == 28, "bag grid stays fixed at 28 cells");
        Check(gridButtons.Count(b => !b.Disabled) == gm.Save.BagEquipment.Count,
            "filled grid cells match the bag item count");

        hub.ClosePopup();
        hub.QueueFree();
        Finish();
    }

    private void Finish()
    {
        GD.Print(_failures == 0 ? "TEST PHASE2.17: ALL PASS" : $"TEST PHASE2.17: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }
}
