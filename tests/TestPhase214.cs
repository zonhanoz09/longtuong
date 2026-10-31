using Godot;

namespace LongTuong;

// Phase 2.57: icon rows — top (Hoạt Động, Vào ải) + bottom (7 functions),
// 64x64 rounded-square icons with label below, like the reference.
[GlobalClass]
public partial class TestPhase214 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("testhub214");
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads");
        if (hubScene == null) { Finish(); return; }
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);

        // Top row: 2 icons right-aligned below TopBar (y=120).
        var activity = hub.GetNode<Button>("ActivityButtonIcon/ActivityButton");
        Check(activity.GlobalPosition == new Vector2(1104, 120) && activity.Size == new Vector2(64, 64), "Hoat Dong icon at (1104,120) 64x64 (2.57)");
        var stage = hub.GetNode<Button>("StageButtonIcon/StageButton");
        Check(stage.GlobalPosition == new Vector2(1184, 120) && stage.Size == new Vector2(64, 64), "Vao ai icon at (1184,120) 64x64 (2.57)");

        // Logout: small button top-right of slim bar.
        var logout = hub.GetNode<Button>("TopBar/LogoutButton");
        Check(logout.GlobalPosition == new Vector2(1170, 37) && logout.Size == new Vector2(94, 36), "Dang xuat small button at (1170,37) 94x36 (2.57)");
        Check(stage.Text.Contains("Ải") && logout.Text.Contains("Đăng xuất"), "top icons/buttons labelled");

        // Bottom row: 7 icons centered, 80px pitch from x=368.
        var cases = new (string Node, float X)[]
        {
            ("StarChartButton", 368),
            ("MoneyTreeButton", 448),
            ("HeroListButton", 528),
            ("BagButton", 608),
            ("FormationButton", 688),
            ("EnhanceButton", 768),
            ("RecruitButton", 848),
        };
        foreach (var (node, x) in cases)
        {
            var btn = hub.GetNode<Button>($"FunctionBar/{node}Icon/{node}");
            Check(btn.GlobalPosition == new Vector2(x, 620) && btn.Size == new Vector2(64, 64), $"bottom icon {node} at x={x} 64x64");
            var lab = hub.GetNode<Label>($"FunctionBar/{node}Icon/Label");
            Check(lab != null && lab.Text.Length > 0, $"bottom icon {node} has label");
        }
        var first = hub.GetNode<Button>("FunctionBar/StarChartButtonIcon/StarChartButton");
        Check(first.Text.Contains("★"), "first bottom icon is Tuong Tinh");

        // Equipment access is preserved through Trang bi (same HeroScreen).
        var enhance = hub.GetNode<Button>("FunctionBar/EnhanceButtonIcon/EnhanceButton");
        enhance.EmitSignal(Button.SignalName.Pressed);
        Check(hub.IsPopupOpen, "Cuong hoa opens the equipment/enhance popup");
        Check(hub.ClosePopup(), "popup closes");
        stage.EmitSignal(Button.SignalName.Pressed);
        Check(hub.IsPopupOpen, "Vao ai opens the stage popup");
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
        GD.Print(_failures == 0 ? "TEST PHASE2.14: ALL PASS" : $"TEST PHASE2.14: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }
}
