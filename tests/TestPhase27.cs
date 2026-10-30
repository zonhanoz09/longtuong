using Godot;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase27 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private async void RunChecks()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("testhero");
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads for 2.7 test");
        if (hubScene == null) { GetTree().Quit(1); return; }
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
        Check(HubScene.HeroListPopupSize == new Vector2(1000, 600), "hero list popup is 1000x600 (2.18)");
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        var tabBtns = heroList.GetNode<VBoxContainer>("TabButtons").GetChildren().OfType<Button>().ToList();
        Check(tabBtns.Count == gm.Save.OwnedHeroIds.Count, "tabs are owned heroes only in a vertical button column (2.19)");
        var starter = gm.Save.OwnedHeroIds[0];
        Check(tabBtns.All(b => !b.Text.Contains("(Khóa)")) && !tabBtns.Any(b => b.Text.Contains("Quan Vũ")),
            "no locked tabs, and a locked hero (Quan Vu) has no tab at all");
        var grid = heroList.GetNode<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        Check(grid.Columns == 4 && grid.GetChildCount() == 28, "grid is 4 columns x 7 rows = 28 cells");
        var gridButtons = grid.GetChildren().OfType<Button>().ToList();
        Check(gridButtons.Count(b => b.Disabled == false) == 6 + ExpCardService.Cards.Count, "grid has 6 equipment + 3 EXP cards from seed");
        // Equip to the selected owned hero, then reopen: item leaves the grid.
        // (2.19 removed equipping to locked heroes: they have no tab anymore.)
        gridButtons[0].EmitSignal(Control.SignalName.GuiInput, new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, DoubleClick = true });
        Check(!gm.Save.HeroEquipment[starter]["weapon"].IsEmpty, "grid item equips to the selected owned hero");
        hub.ClosePopup();
        hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
        var heroList2 = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        var grid2 = heroList2.GetNode<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        Check(grid2.GetChildren().OfType<Button>().Count(b => b.Disabled == false) == 5 + ExpCardService.Cards.Count, "equipped item is gone from the grid after reopen");
        // Beyond 28 items: grid grows rows and scrolls (approved 2.7 option).
        for (int i = 0; i < 27; i++)
            gm.Save.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon", Level = 0, RefineIndex = 0 });
        Check(gm.Save.BagEquipment.Count == 32, "bag holds 32 items for scroll test");
        hub.ClosePopup();
        hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
        await ToSignal(GetTree(), "process_frame");
        var heroList3 = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        var grid3 = heroList3.GetNode<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        var scroll3 = heroList3.GetNode<ScrollContainer>("GridPanel/GridScroll");
        Check(grid3.GetChildCount() == 36, "grid grows to 36 cells for 32 items + 3 EXP cards");
        Check(grid3.Size.Y > scroll3.Size.Y, "grid content is taller than its scroll viewport (scrolls)");
        hub.ClosePopup();
        hub.QueueFree();
        GD.Print(_failures == 0 ? "TEST PHASE2.7: ALL PASS" : $"TEST PHASE2.7: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }
}
