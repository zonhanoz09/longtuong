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

        // Hero list (Phase 2.6): no tabs, 3 fixed columns: owned equipment left,
        // hero detail center (with hero selector), stats + tooltip right.
        hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        Check(heroList != null && heroList.GetNodeOrNull<TabContainer>("HeroTabs") == null, "hero list no longer uses tabs");
        var ownedScroll = heroList?.GetNodeOrNull<ScrollContainer>("OwnedScroll");
        var ownedBox = heroList?.GetNodeOrNull<VBoxContainer>("OwnedScroll/OwnedListBox");
        var detail = heroList?.GetNodeOrNull<VBoxContainer>("DetailBox");
        var right = heroList?.GetNodeOrNull<VBoxContainer>("RightBox");
        var tooltip = heroList?.GetNodeOrNull<Label>("RightBox/TooltipPanel/TooltipLabel");
        Check(ownedScroll != null && ownedBox != null && detail != null && right != null && tooltip != null, "hero list has owned list (scroll) + detail + right stats/tooltip columns");
        if (detail != null)
        {
            var rows = detail.GetChildren().OfType<HBoxContainer>().ToList();
            Check(rows.Count >= 3 && rows[0].GetChildren().OfType<Button>().Count() + rows[1].GetChildren().OfType<Button>().Count() == HeroDatabase.Heroes.Count, "hero detail has selector rows with one button per hero");
            Check(rows.Count >= 3 && rows[2].GetChildren().OfType<Button>().Count() == 4, "hero detail has 4 equipped slot controls");
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
