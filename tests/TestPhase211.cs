using Godot;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase211 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        // Seed: default hero is owned (custom hero, the starter).
        gm.CreateSeededSave("test_default");
        Check(gm.SelectedHeroId == "custom_hero" && gm.Save.OwnedHeroIds.Contains(gm.SelectedHeroId), "seeded default hero is owned");
        // Simulate the old bug: default left at a locked hero after login.
        gm.SelectedHeroId = "quan_vu";
        Check(!gm.Save.OwnedHeroIds.Contains("quan_vu"), "quan_vu is locked for this new account (test setup)");
        gm.LoadGame();
        Check(gm.Save.OwnedHeroIds.Contains(gm.SelectedHeroId), "after LoadGame, default hero is owned again");
        Check(gm.SelectedHeroId == "custom_hero", "after LoadGame, default hero is the team leader (custom hero)");
        // Viewing a locked tab must not make it the global default.
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads for default-hero test");
        if (hubScene != null)
        {
            var hub = hubScene.Instantiate<HubScene>();
            AddChild(hub);
            hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
            var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
            var tabBtns = heroList.GetNode<VBoxContainer>("TabButtons").GetChildren().OfType<Button>().ToList();
            var lockedTab = tabBtns.First(b => b.Text.Contains("(Khóa)"));
            lockedTab.EmitSignal(Button.SignalName.Pressed);
            Check(gm.Save.OwnedHeroIds.Contains(gm.SelectedHeroId), "pressing a locked tab does not make it the global default");
            hub.ClosePopup();
            hub.QueueFree();
        }
        gm.Logout();
        Check(gm.SelectedHeroId == "", "logout clears the default hero (no account)");
        GD.Print(_failures == 0 ? "TEST PHASE2.11: ALL PASS" : $"TEST PHASE2.11: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }
}
