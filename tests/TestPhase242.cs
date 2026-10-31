using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.42 — feature unlocks (chapter AND level) + Hub "Hoạt Động"
/// popup with 4 mode cards.
/// </summary>
public partial class TestPhase242 : Node
{
    private int _fails;

    public override void _Ready()
    {
        CheckRules();
        CheckHubAndCards();

        if (_fails == 0) GD.Print("TEST PHASE 2.42: ALL PASS");
        else GD.Print($"TEST PHASE 2.42: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private static void SetProgress(SaveData s, int lastStage, int level)
    {
        s.LastStage = lastStage;
        s.Heroes["custom_hero"].Level = level;
    }

    private void CheckRules()
    {
        var r = FeatureUnlockService.Requirements;
        Check(r[Feature.Dungeon] is { Chapter: 2, Level: 30 } && r[Feature.Duel] is { Chapter: 3, Level: 20 }
            && r[Feature.Trial] is { Chapter: 4, Level: 25 } && r[Feature.Party] is { Chapter: 5, Level: 40 }, "requirement table");
        var s = SeedService.CreateSeededSave("t242a");
        Check(System.Enum.GetValues<Feature>().All(f => !FeatureUnlockService.IsUnlocked(s, f)), "new account: every mode locked");
        // Chapter 3 = ải 15–20.
        SetProgress(s, 20, 19);
        Check(!FeatureUnlockService.IsUnlocked(s, Feature.Duel), "chapter cleared but level too low: locked");
        SetProgress(s, 19, 99);
        Check(!FeatureUnlockService.IsUnlocked(s, Feature.Duel), "level ok but chapter not cleared: locked");
        SetProgress(s, 20, 20);
        Check(FeatureUnlockService.IsUnlocked(s, Feature.Duel), "chapter 3 + Lv20 unlocks Vượt Tướng");
        Check(FeatureUnlockService.LockReason(s, Feature.Duel) == "", "no lock reason when open");
        Check(FeatureUnlockService.LockReason(s, Feature.Trial) == "Qua chương 4 và đạt Lv25", "lock reason text");
        SetProgress(s, 14, 30);
        Check(FeatureUnlockService.IsUnlocked(s, Feature.Dungeon), "chapter 2 (ải 14) + Lv30 unlocks Phó Bản");
        SetProgress(s, 38, 40);
        Check(System.Enum.GetValues<Feature>().All(f => FeatureUnlockService.IsUnlocked(s, f)), "chapter 5 + Lv40 unlocks everything");
    }

    private void CheckHubAndCards()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("t242b");
        var hub = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn").Instantiate<HubScene>();
        AddChild(hub);
        var btn = hub.GetNodeOrNull<Button>("TopBar/ActivityButton");
        Check(btn != null && btn.Text == "Hoạt Động", "hub has the Hoạt Động button");
        Check(btn != null && !btn.GetGlobalRect().Intersects(hub.GetNode<Button>("TopBar/StageButton").GetGlobalRect()), "button does not overlap Vào ải");
        Check(hub.GetNode<Control>("FunctionBar").GetChildren().OfType<Button>().Count() == 7, "bottom bar unchanged (7 buttons)");
        var sizes = new[] { HubScene.HeroListPopupSize, HubScene.BagPopupSize, HubScene.HeroPopupSize, HubScene.FormationPopupSize,
            HubScene.StagePopupSize, HubScene.RecruitPopupSize, HubScene.StarChartPopupSize, HubScene.MoneyTreePopupSize, HubScene.ActivityPopupSize };
        Check(sizes.Distinct().Count() == sizes.Length, "every popup size is different (incl. Hoạt Động)");
        btn!.EmitSignal(BaseButton.SignalName.Pressed);
        var content = hub.GetNode<Control>("PopupFrame/PopupContent");
        var activity = content.GetChildCount() == 1 ? content.GetChild(0) as ActivityScene : null;
        Check(hub.IsPopupOpen && activity != null && activity.Size == HubScene.ActivityPopupSize, "Hoạt Động opens a 960x560 popup");
        var cards = activity?.GetNode<GridContainer>("CardGrid").GetChildren().OfType<Button>().ToList() ?? new();
        Check(cards.Count == 4 && cards.All(c => c.Disabled), "new account: 4 cards, all locked");
        Check(cards.Count == 4 && cards[1].Text.Contains("KHÓA") && cards[1].TooltipText == "Qua chương 3 và đạt Lv20", "locked card shows the condition");
        hub.ClosePopup();

        gm.Save.LastStage = 20;
        gm.Save.Heroes["custom_hero"].Level = 25;
        btn.EmitSignal(BaseButton.SignalName.Pressed);
        activity = content.GetChild(0) as ActivityScene;
        var duel = activity?.GetNode<Button>("CardGrid/Card_Duel");
        Check(duel != null && !duel.Disabled, "ải 20 + Lv25: Vượt Tướng card opens");
        Check(activity?.GetNode<Button>("CardGrid/Card_Trial").Disabled == true, "Thí Luyện still locked (needs chapter 4)");
        hub.ClosePopup();
        hub.QueueFree();
    }
}
