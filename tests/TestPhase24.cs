using Godot;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase24 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        if (hubScene == null) { GD.PrintErr("FAIL: hub scene loads"); GetTree().Quit(1); return; }
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);

        // No Back button in any widget popup (X closes them since 2.4).
        var widgets = new (string Path, Vector2 Size)[]
        {
            ("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize),
            ("res://features/bag/scenes/BagScene.tscn", HubScene.BagPopupSize),
            ("res://features/hero/scenes/HeroScreen.tscn", HubScene.HeroPopupSize),
            ("res://features/formation/scenes/FormationScene.tscn", HubScene.FormationPopupSize),
            ("res://features/stage/scenes/StageSelect.tscn", HubScene.StagePopupSize),
            ("res://features/recruit/scenes/RecruitScene.tscn", HubScene.RecruitPopupSize),
        };
        foreach (var (path, size) in widgets)
        {
            hub.OpenPopup(path, size);
            var widget = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
            Check(widget != null && widget.GetNodeOrNull<Button>("BackButton") == null, $"no BackButton in widget popup: {path}");
            hub.ClosePopup();
        }

        // Hero list (Phase 2.7): vertical tabs + 4-column equipment grid.
        hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        var tabButtons = heroList?.GetNodeOrNull<VBoxContainer>("TabButtons");
        var tabBtns = tabButtons?.GetChildren().OfType<Button>().ToList() ?? new System.Collections.Generic.List<Button>();
        // TabButtons is a VBoxContainer, so its buttons stack vertically by construction.
        Check(tabButtons != null && tabBtns.Count == HeroDatabase.Heroes.Count, "hero tabs are a vertical button column, one per hero");
        var grid = heroList?.GetNodeOrNull<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        Check(grid != null && grid.Columns == 4, "equipment grid has 4 columns");
        Check(grid != null && grid.GetChildCount() == 28, "equipment grid has 28 cells (4x7) when bag is empty");
        if (grid != null)
            Check(grid.GetChildren().OfType<Button>().All(b => b.Text == ""), "grid cells are icon-only (no text)");
        var page = heroList?.GetNodeOrNull<VBoxContainer>("DetailBox");
        var equippedBox = page?.GetChildren().OfType<HBoxContainer>().FirstOrDefault();
        Check(equippedBox != null && equippedBox.GetChildren().OfType<Button>().Count() == 6, "hero tab page has 6 equipped icon slots");
        if (equippedBox != null)
            Check(equippedBox.GetChildren().OfType<Button>().All(b => b.Text == ""), "equipped slots are icon-only (no text)");
        hub.ClosePopup();
        hub.QueueFree();
        GD.Print(_failures == 0 ? "TEST PHASE2.4: ALL PASS" : $"TEST PHASE2.4: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }
}
