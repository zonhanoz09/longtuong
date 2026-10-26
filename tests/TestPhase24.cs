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

        // Hero list: one tab per hero, each tab has detail + carried equipment.
        hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        var tabs = heroList?.GetNodeOrNull<TabContainer>("HeroTabs");
        Check(tabs != null, "hero list has HeroTabs TabContainer");
        if (tabs != null)
        {
            Check(tabs.GetTabCount() == HeroDatabase.Heroes.Count, $"hero tabs count = {HeroDatabase.Heroes.Count}");
            for (int i = 0; i < HeroDatabase.Heroes.Count; i++)
                Check(tabs.GetTabTitle(i).Contains(HeroDatabase.Heroes[i].Name), $"tab {i} is {HeroDatabase.Heroes[i].Name}");
            var firstPage = tabs.GetChild(0) as VBoxContainer;
            var allText = firstPage == null ? "" : string.Join("\n", firstPage.GetChildren().OfType<Label>().Select(l => l.Text));
            Check(allText.Contains("Trang bị mang theo"), "hero tab shows carried equipment header");
            foreach (var slot in ProgressionService.Slots)
                Check(allText.Contains(slot.Name), $"hero tab shows equipment slot: {slot.Name}");
            Check(allText.Contains("Chỉ số hiệu lực"), "hero tab shows effective stats");
            Check(allText.Contains("Tuyệt kỹ"), "hero tab shows skills");
        }
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
