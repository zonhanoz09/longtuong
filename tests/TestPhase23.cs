using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase23 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads");
        if (hubScene == null) { Finish(); return; }
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        Check(!hub.IsPopupOpen, "hub starts with no popup open");
        var cases = new (string Path, Vector2 Size)[]
        {
            ("res://features/recruit/scenes/RecruitScene.tscn", HubScene.RecruitPopupSize),
            ("res://features/formation/scenes/FormationScene.tscn", HubScene.FormationPopupSize),
            ("res://features/bag/scenes/BagScene.tscn", HubScene.BagPopupSize),
            ("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize),
            ("res://features/hero/scenes/HeroScreen.tscn", HubScene.HeroPopupSize),
            ("res://features/stage/scenes/StageSelect.tscn", HubScene.StagePopupSize),
        };
        foreach (var (path, size) in cases)
        {
            hub.OpenPopup(path, size);
            var content = hub.GetNode<Control>("PopupFrame/PopupContent");
            var frame = hub.GetNode<Panel>("PopupFrame");
            Check(hub.IsPopupOpen, $"popup opens: {path}");
            Check(content.GetChildCount() == 1, $"popup holds exactly 1 widget: {path}");
            var widget = content.GetChild(0) as Control;
            Check(widget != null && widget.Size == size, $"widget sized to popup ({size.X}x{size.Y}): {path}");
            Check(frame.Size == size + new Vector2(36, 36), $"frame wraps content + margin: {path}");
            Check(hub.GetNode<ColorRect>("Dim").Visible, $"dim visible while popup open: {path}");
            Check(hub.ClosePopup(), $"ClosePopup reports closed: {path}");
            Check(!hub.IsPopupOpen && !hub.GetNode<ColorRect>("Dim").Visible, $"popup closed and dim hidden: {path}");
        }
        // Back button inside a popup closes it (does not reload Hub).
        hub.OpenPopup("res://features/recruit/scenes/RecruitScene.tscn", HubScene.RecruitPopupSize);
        var recruit = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        var back = recruit?.GetNodeOrNull<Button>("BackButton");
        Check(back != null, "recruit popup has BackButton");
        back?.EmitSignal(Button.SignalName.Pressed);
        Check(!hub.IsPopupOpen, "BackButton inside popup closes popup instead of scene change");
        // Sizes really differ per widget.
        var distinct = cases.Select(c => c.Size).Distinct().Count();
        Check(distinct == cases.Length, "every widget popup size is different");
        Check(HubScene.RecruitPopupSize.X < HubScene.StagePopupSize.X, "recruit popup smaller than stage popup");
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
        GD.Print(_failures == 0 ? "TEST PHASE2.3: ALL PASS" : $"TEST PHASE2.3: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }
}
