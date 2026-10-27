using Godot;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase26 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private async void RunChecks()
    {
        // Service level: detail text carries stats, item cells must not.
        var detail = EquipmentService.DetailText("weapon", 3, 0, "Trạng thái: Trong túi");
        Check(detail.Contains("Tấn công:") && detail.Contains("Tẩy luyện:"), "detail text contains item stats and refine");
        Check(detail.Contains("Trạng thái: Trong túi"), "detail text contains status");

        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("testhero");
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads for 2.7 tooltip test");
        if (hubScene == null) { GetTree().Quit(1); return; }
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        Check(heroList != null, "hero list popup opens");
        if (heroList == null) { hub.QueueFree(); GetTree().Quit(1); return; }
        var grid = heroList.GetNode<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        var gridButtons = grid.GetChildren().OfType<Button>().ToList();
        var tooltipPanel = heroList.GetNode<PanelContainer>("TooltipPanel");
        var tooltip = heroList.GetNode<Label>("TooltipPanel/TooltipLabel");
        Check(gridButtons.All(b => b.Text == "" && b.CustomMinimumSize == new Vector2(64, 64)), "grid cells are icon-only 64x64");
        Check(tooltipPanel.Visible == false, "tooltip is hidden initially");
        // Wait a frame so GridContainer layout has run before checking positions.
        await ToSignal(GetTree(), "process_frame");
        var cellRect = gridButtons[0].GetGlobalRect();
        cellRect.Position -= heroList.GetGlobalRect().Position;
        gridButtons[0].EmitSignal(Control.SignalName.MouseEntered);
        Check(tooltipPanel.Visible == true, "hover on grid cell shows tooltip");
        Check(tooltip.Text.Contains("Tẩy luyện:") && tooltip.Text.Contains("Trong túi"), "hover tooltip shows detail + stats");
        bool beside = tooltipPanel.Position.X >= cellRect.Position.X + cellRect.Size.X + 8
            || tooltipPanel.Position.X + 280 <= cellRect.Position.X - 8;
        Check(beside, "tooltip sits beside the cell (right or flipped left)");
        Check(tooltipPanel.Position.X >= 0 && tooltipPanel.Position.X + 280 <= 1000
            && tooltipPanel.Position.Y >= 0 && tooltipPanel.Position.Y + tooltipPanel.Size.Y <= 560, "tooltip is clamped inside the popup (1000x560 in 2.17)");
        gridButtons[0].EmitSignal(Control.SignalName.MouseExited);
        Check(tooltipPanel.Visible == false, "mouse exit hides tooltip");
        // Hover on an empty cell shows no tooltip (empty cells have no handlers).
        gridButtons[6].EmitSignal(Control.SignalName.MouseEntered);
        Check(tooltipPanel.Visible == false, "hover on empty cell shows no tooltip");
        hub.ClosePopup();
        hub.QueueFree();
        GD.Print(_failures == 0 ? "TEST PHASE2.6: ALL PASS" : $"TEST PHASE2.6: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }
}
