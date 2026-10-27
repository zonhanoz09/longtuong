using Godot;
using System.Linq;

namespace LongTuong;

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

        // 3 zones, measured from Huynh's sketch (1280x720).
        var top = hub.GetNode<Control>("TopBar");
        Check(top.Position == new Vector2(0, 0) && top.Size == new Vector2(1280, 320), "top zone is 0-320");
        var yard = hub.GetNode<Control>("Courtyard");
        Check(yard.Position == new Vector2(0, 320) && yard.Size == new Vector2(1280, 323), "middle zone is 320-643");
        var bar = hub.GetNode<Control>("FunctionBar");
        Check(bar.Position == new Vector2(0, 643) && bar.Size == new Vector2(1280, 77), "bottom zone is 643-720");
        Check(HubScene.TopBarRect == new Rect2(0, 0, 1280, 320)
            && HubScene.CourtyardRect == new Rect2(0, 320, 1280, 323)
            && HubScene.FunctionBarRect == new Rect2(0, 643, 1280, 77), "zone constants match the scene");

        // Old single-column layout is gone; avatar + all 8 widgets remain.
        Check(hub.GetNodeOrNull<Control>("WidgetBox") == null
            && hub.GetNodeOrNull<Label>("TitleLabel") == null
            && hub.GetNodeOrNull<Label>("StatusLabel") == null, "old WidgetBox/Title/Status layout removed");
        Check(hub.GetNodeOrNull<Panel>("TopBar/AvatarPanel") != null, "avatar panel in top-left");
        var nameLabel = hub.GetNode<Label>("TopBar/AvatarPanel/AvatarBox/AvatarNameLabel");
        Check(nameLabel.Text.Contains("testhub"), "avatar shows the username");
        var heroLabel = hub.GetNode<Label>("TopBar/AvatarPanel/AvatarBox/AvatarHeroLabel");
        Check(heroLabel.Text.Contains("testhub"), "avatar hero is the owned default (custom hero = username)");
        var currency = hub.GetNode<Label>("TopBar/AvatarPanel/AvatarBox/AvatarCurrencyLabel");
        Check(currency.Text.Contains("Vàng:"), "currency moved into the avatar panel");
        var featureBtns = hub.GetNode<Control>("TopBar/FeatureIcons").GetChildren().OfType<Button>().ToList();
        Check(featureBtns.Count == 5, "top-right holds 5 feature icons");
        var functionBtns = hub.GetNode<Control>("FunctionBar").GetChildren().OfType<Button>().ToList();
        Check(functionBtns.Count == 3, "bottom bar holds 3 function icons");
        Check(featureBtns.Concat(functionBtns).Select(b => b.Name).Distinct().Count() == 8, "all 8 widgets still reachable (5 + 3)");

        // Character: starts inside the walk area, moves toward a target, arrives.
        Check(hub.CharacterPosition == HubScene.CharacterStart, "character starts at the courtyard centre");
        Check(HubScene.WalkArea.HasPoint(hub.CharacterPosition), "character start is inside the walk area");
        var target = new Vector2(900, 150);
        var before = hub.CharacterPosition.DistanceTo(target);
        hub.MoveCharacterTo(target);
        Check(hub.IsCharacterMoving, "character is moving after MoveCharacterTo");
        hub.StepCharacter(1.0f);
        var after = hub.CharacterPosition.DistanceTo(target);
        Check(after < before && hub.CharacterPosition.X > HubScene.CharacterStart.X, "character moves toward the target");
        Check(HubScene.WalkArea.HasPoint(hub.CharacterPosition), "character stays inside the walk area while moving");
        hub.StepCharacter(10.0f);
        Check(!hub.IsCharacterMoving && hub.CharacterPosition == target, "character arrives and stops at the target");

        // Click-to-move: a left click on the courtyard (local coords) sets a new target.
        var click = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = new Vector2(300, 200) };
        yard.EmitSignal(Control.SignalName.GuiInput, click);
        Check(hub.IsCharacterMoving, "click on the courtyard starts movement");

        // Popups still work from the new layout (spot-check one widget).
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
