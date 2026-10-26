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

        // Hero list (Phase 2.5): 3 columns, no tabs anymore.
        hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        Check(heroList != null && heroList.GetNodeOrNull<TabContainer>("HeroTabs") == null, "hero list no longer uses tabs");
        var left = heroList?.GetNodeOrNull<VBoxContainer>("HeroListBox");
        var detail = heroList?.GetNodeOrNull<VBoxContainer>("DetailBox");
        var right = heroList?.GetNodeOrNull<VBoxContainer>("RightBox");
        Check(left != null && detail != null && right != null, "hero list has left list + center detail + right columns");
        if (left != null)
            Check(left.GetChildren().OfType<Button>().Count() == HeroDatabase.Heroes.Count, "hero list left column has one button per hero");
        if (detail != null)
        {
            var equippedBox = detail.GetChildren().OfType<HBoxContainer>().FirstOrDefault();
            Check(equippedBox != null && equippedBox.GetChildren().OfType<Button>().Count() == 4, "hero detail has 4 equipped slot controls");
            var labels = string.Join("\n", detail.GetChildren().OfType<Label>().Select(l => l.Text));
            Check(labels.Contains("Tuyệt kỹ"), "hero detail shows skills");
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
