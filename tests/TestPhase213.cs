using Godot;
using System.Linq;

namespace LongTuong;

// Updated for Phase 2.14: same 3-zone hub, but buttons moved to the
// second sketch — Stage/Logout top-right, 5 functions on the bottom bar,
// and the duplicate Trang bi button removed (it opened the same
// HeroScreen as Cuong hoa). Zone heights changed to 306/306/108.
[GlobalClass]
public partial class TestPhase213 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("testhub");
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads");
        if (hubScene == null) { Finish(); return; }
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);

        var top = hub.GetNode<Control>("TopBar");
        Check(top.Position == new Vector2(0, 0) && top.Size == new Vector2(1280, 110), "top zone is slim 0-110 (2.56)");
        var yard = hub.GetNode<Control>("Courtyard");
        Check(yard.Position == new Vector2(0, 110) && yard.Size == new Vector2(1280, 502), "middle zone is 110-612");
        var bar = hub.GetNode<Control>("FunctionBar");
        Check(bar.Position == new Vector2(0, 612) && bar.Size == new Vector2(1280, 108), "bottom zone is 612-720");
        Check(HubScene.TopBarRect == new Rect2(0, 0, 1280, 110)
            && HubScene.CourtyardRect == new Rect2(0, 110, 1280, 502)
            && HubScene.FunctionBarRect == new Rect2(0, 612, 1280, 108), "zone constants match the scene");

        Check(hub.GetNodeOrNull<Control>("WidgetBox") == null
            && hub.GetNodeOrNull<Label>("TitleLabel") == null
            && hub.GetNodeOrNull<Label>("TopBar/AvatarPanel/AvatarHBox/AvatarVBox/AvatarStatusLabel") == null, "old WidgetBox/Title/Status layout removed (2.56 drops avatar status)");
        Check(hub.GetNodeOrNull<Panel>("TopBar/AvatarPanel") != null, "avatar panel in top-left");
        var nameLabel = hub.GetNode<Label>("TopBar/AvatarPanel/AvatarHBox/AvatarVBox/AvatarNameLabel");
        Check(nameLabel.Text.Contains("testhub"), "avatar shows the username");
        var heroLabel = hub.GetNode<Label>("TopBar/AvatarPanel/AvatarHBox/AvatarVBox/AvatarHeroLabel");
        Check(heroLabel.Text.Contains("testhub"), "avatar hero is the owned default (custom hero = username)");
        var topBtns = hub.GetNode<Control>("TopBar").GetChildren().OfType<Button>().ToList();
        Check(topBtns.Count == 3, "top-right holds 3 actions (Hoat Dong, Vao ai, Dang xuat)");
        var functionBtns = hub.GetNode<Control>("FunctionBar").GetChildren().OfType<Button>().ToList();
        Check(functionBtns.Count == 7, "bottom bar holds 7 function buttons (2.31 adds Cây Tiền)");
        Check(topBtns.Concat(functionBtns).Select(b => b.Name).Distinct().Count() == 10, "all 10 buttons present (3 + 7)");
        Check(hub.GetNodeOrNull<Button>("TopBar/FeatureIcons/EquipmentButton") == null
            && hub.GetNodeOrNull<Button>("FunctionBar/EquipmentButton") == null, "duplicate Trang bi button removed");

        Check(hub.CharacterPosition == HubScene.CharacterStart, "character starts at the courtyard centre");
        Check(HubScene.WalkArea.HasPoint(hub.CharacterPosition), "character start is inside the walk area");
        var target = new Vector2(900, 140);
        var before = hub.CharacterPosition.DistanceTo(target);
        hub.MoveCharacterTo(target);
        Check(hub.IsCharacterMoving, "character is moving after MoveCharacterTo");
        hub.StepCharacter(1.0f);
        var after = hub.CharacterPosition.DistanceTo(target);
        Check(after < before && hub.CharacterPosition.X > HubScene.CharacterStart.X, "character moves toward the target");
        Check(HubScene.WalkArea.HasPoint(hub.CharacterPosition), "character stays inside the walk area while moving");
        hub.StepCharacter(10.0f);
        Check(!hub.IsCharacterMoving && hub.CharacterPosition == target, "character arrives and stops at the target");

        var click = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = new Vector2(300, 200) };
        yard.EmitSignal(Control.SignalName.GuiInput, click);
        Check(hub.IsCharacterMoving, "click on the courtyard starts movement");

        hub.OpenPopup("res://features/recruit/scenes/RecruitScene.tscn", HubScene.RecruitPopupSize);
        Check(hub.IsPopupOpen, "popup still opens from the 3-zone hub");
        Check(hub.ClosePopup(), "popup closes and hub refreshes");

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
        GD.Print(_failures == 0 ? "TEST PHASE2.13: ALL PASS" : $"TEST PHASE2.13: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }
}
