using Godot;

namespace LongTuong;

// Phase 2.15: bottom button is now "Vo tuong"; clicking it opens the
// hero-list popup (title "Vo Tuong" in HeroListScene), and ESC closes
// any open Hub popup. ESC with no popup open does nothing.
[GlobalClass]
public partial class TestPhase215 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("testesc");
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads");
        if (hubScene == null) { Finish(); return; }
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);

        var voTuong = hub.GetNode<Button>("FunctionBar/HeroListButtonIcon/HeroListButton");
        Check(voTuong.Text.Contains("Võ"), "bottom button renamed to Vo tuong");

        // ESC with no popup open: nothing happens, no crash.
        hub._UnhandledInput(new InputEventKey { Keycode = Key.Escape, Pressed = true });
        Check(!hub.IsPopupOpen, "ESC with no popup does nothing");

        // Click Vo tuong -> hero-list popup opens.
        voTuong.EmitSignal(Button.SignalName.Pressed);
        Check(hub.IsPopupOpen, "clicking Vo tuong opens a popup");
        var widget = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0);
        Check(widget.Name == "HeroListScene", "Vo tuong popup is the hero-list scene");
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0);
        Check(heroList.GetNodeOrNull<Label>("TitleLabel")?.Text == "Vo Tuong", "popup title is Vo Tuong");

        // Held-key echo must not close it; a real ESC press does.
        hub._UnhandledInput(new InputEventKey { Keycode = Key.Escape, Pressed = true, Echo = true });
        Check(hub.IsPopupOpen, "ESC echo (held key) does not close the popup");
        hub._UnhandledInput(new InputEventKey { Keycode = Key.Escape, Pressed = true });
        Check(!hub.IsPopupOpen && !hub.GetNode<ColorRect>("Dim").Visible, "ESC closes the popup and hides the dim");

        // ESC is hub-wide, not just for Vo tuong: recruit popup also closes.
        hub.OpenPopup("res://features/recruit/scenes/RecruitScene.tscn", HubScene.RecruitPopupSize);
        Check(hub.IsPopupOpen, "recruit popup opens");
        hub._UnhandledInput(new InputEventKey { Keycode = Key.Escape, Pressed = true });
        Check(!hub.IsPopupOpen, "ESC closes a non-hero popup too");

        // A non-ESC key does not close a popup.
        hub.OpenPopup("res://features/recruit/scenes/RecruitScene.tscn", HubScene.RecruitPopupSize);
        hub._UnhandledInput(new InputEventKey { Keycode = Key.Enter, Pressed = true });
        Check(hub.IsPopupOpen, "Enter does not close the popup");
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
        GD.Print(_failures == 0 ? "TEST PHASE2.15: ALL PASS" : $"TEST PHASE2.15: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }
}
