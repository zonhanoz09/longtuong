using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

// Phase 2.19: the Vo Tuong popup shows ONLY owned (recruited) heroes.
// A dismissed hero disappears from the tab list entirely — it returns
// only by re-unlocking with shards, not by staying visible as locked.
[GlobalClass]
public partial class TestPhase219 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private async void RunChecks()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("test219");
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads");
        if (hubScene == null) { Finish(); return; }
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        hub.GetNode<Button>("FunctionBar/HeroListButton").EmitSignal(Button.SignalName.Pressed);
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        Check(heroList != null, "popup widget is the hero list scene");
        if (heroList == null) { hub.QueueFree(); Finish(); return; }

        var tabBtns = heroList.GetNode<VBoxContainer>("TabButtons").GetChildren().OfType<Button>().ToList();
        Check(tabBtns.Count == gm.Save.OwnedHeroIds.Count, "tab count equals owned count (seeded: 2)");
        Check(tabBtns.All(b => !b.Text.Contains("(Khóa)")), "no locked tab suffix anywhere");
        Check(!tabBtns.Any(b => b.Text.Contains("Quan Vũ")) && !tabBtns.Any(b => b.Text.Contains("Triệu Vân")),
            "locked heroes (Quan Vu, Trieu Van) have no tabs");
        Check(tabBtns.Any(b => b.Text.Contains("test219")), "custom hero tab shows the username");
        Check(tabBtns.Any(b => b.Text.Contains("Lưu Bị")), "Luu Bi tab is present (never locked)");

        // Recruit a hero (add to owned), reopen: his tab appears.
        gm.Save.OwnedHeroIds.Add("quan_vu");
        gm.Save.TeamIds = new List<string> { "custom_hero", "quan_vu" };
        gm.SelectedTeamIds = new List<string>(gm.Save.TeamIds);
        hub.ClosePopup();
        hub.GetNode<Button>("FunctionBar/HeroListButton").EmitSignal(Button.SignalName.Pressed);
        var heroList2 = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        var tabBtns2 = heroList2.GetNode<VBoxContainer>("TabButtons").GetChildren().OfType<Button>().ToList();
        Check(tabBtns2.Count == 3 && tabBtns2.Any(b => b.Text.Contains("Quan Vũ")),
            "newly owned hero appears as a tab after reopen");

        // Dismiss him from the UI, reopen: his tab is gone entirely.
        var quanTab = tabBtns2.First(b => b.Text.Contains("Quan Vũ"));
        quanTab.EmitSignal(Button.SignalName.Pressed);
        await ToSignal(GetTree(), "process_frame");
        await ToSignal(GetTree(), "process_frame");
        var dismiss = heroList2.GetNode<Button>("DetailBox/DismissRow/DismissButton");
        Check(!dismiss.Disabled, "dismiss is enabled for the newly owned hero");
        dismiss.EmitSignal(Button.SignalName.Pressed);
        Check(!gm.Save.OwnedHeroIds.Contains("quan_vu"), "dismiss removes the hero from the owned list");
        hub.ClosePopup();
        hub.GetNode<Button>("FunctionBar/HeroListButton").EmitSignal(Button.SignalName.Pressed);
        var heroList3 = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        var tabBtns3 = heroList3.GetNode<VBoxContainer>("TabButtons").GetChildren().OfType<Button>().ToList();
        Check(tabBtns3.Count == 2 && !tabBtns3.Any(b => b.Text.Contains("Quan Vũ")),
            "dismissed hero has no tab after reopen (not shown as locked either)");

        hub.ClosePopup();
        hub.QueueFree();
        Finish();
    }

    private void Finish()
    {
        GD.Print(_failures == 0 ? "TEST PHASE2.19: ALL PASS" : $"TEST PHASE2.19: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }
}
