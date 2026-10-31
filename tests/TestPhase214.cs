using Godot;

namespace LongTuong;

// Phase 2.14: button positions measured from Huynh's second sketch.
// Top boxes (viewport): Vao ai 918-1066, Dang xuat 1101-1248, y 30-111.
// Bottom boxes (viewport): 360-510, 546-694, 744-893, 913-1062, 1096-1243,
// y 624-704. Equipment stays reachable: Cuong hoa opens HeroScreen.
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

        var stage = hub.GetNode<Button>("TopBar/StageButton");
        Check(stage.GlobalPosition == new Vector2(952, 27) && stage.Size == new Vector2(148, 56), "Vao ai at top-right (952,27) 148x56 (2.56 slim)");
        var logout = hub.GetNode<Button>("TopBar/LogoutButton");
        Check(logout.GlobalPosition == new Vector2(1116, 27) && logout.Size == new Vector2(148, 56), "Dang xuat at top-right (1116,27) 148x56 (2.56 slim)");
        Check(stage.Text.Contains("Vào ải") && logout.Text.Contains("Đăng xuất"), "top buttons labelled Vao ai / Dang xuat");

        var cases = new (string Node, float X, float W)[]
        {
            ("HeroListButton", 360, 150),
            ("BagButton", 546, 148),
            ("FormationButton", 744, 149),
            ("EnhanceButton", 913, 149),
            ("RecruitButton", 1096, 147),
        };
        foreach (var (node, x, w) in cases)
        {
            var btn = hub.GetNode<Button>($"FunctionBar/{node}");
            Check(btn.GlobalPosition == new Vector2(x, 624) && btn.Size == new Vector2(w, 80), $"bottom button {node} at x={x} width={w}");
        }
        var first = hub.GetNode<Button>("FunctionBar/HeroListButton");
        Check(first.Text.Contains("Võ"), "first bottom button is Vo tuong");

        // Equipment access is preserved through Cuong hoa (same HeroScreen).
        var enhance = hub.GetNode<Button>("FunctionBar/EnhanceButton");
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
