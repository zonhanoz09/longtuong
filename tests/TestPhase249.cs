using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.49 — Thí Luyện Tháp: 60 floors, Bày trận team, gem-only
/// first-clear rewards, floor kept forever.
/// </summary>
public partial class TestPhase249 : Node
{
    private int _fails;

    public override void _Ready()
    {
        CheckFloors();
        CheckRewards();
        CheckUiAndBattle();

        if (_fails == 0) GD.Print("TEST PHASE 2.49: ALL PASS");
        else GD.Print($"TEST PHASE 2.49: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private void CheckFloors()
    {
        Check(TrialTowerService.MaxFloor == 60, "60 floors");
        Check(TrialTowerService.StageForFloor(1).EnemyIds.Count == 3 && TrialTowerService.StageForFloor(20).EnemyIds.Count == 3, "floors 1–20: 3 enemies");
        Check(TrialTowerService.StageForFloor(21).EnemyIds.Count == 4 && TrialTowerService.StageForFloor(40).EnemyIds.Count == 4, "floors 21–40: 4 enemies");
        Check(TrialTowerService.StageForFloor(41).EnemyIds.Count == 5 && TrialTowerService.StageForFloor(60).EnemyIds.Count == 5, "floors 41–60: 5 enemies");
        Check(System.Math.Abs(TrialTowerService.StageForFloor(1).Scale - 1.22) < 1e-9 && System.Math.Abs(TrialTowerService.StageForFloor(60).Scale - 14.2) < 1e-9, "Scale 1.0 + 0.22 x floor (2.52)");
        bool bosses = Enumerable.Range(1, 60).All(f =>
        {
            var st = TrialTowerService.StageForFloor(f);
            return (f % 10 == 0) == (st.BossId != null) && (st.BossId == null || st.EnemyIds.Contains(st.BossId));
        });
        Check(bosses, "every 10th floor has a boss (and only those)");
        Check(Enumerable.Range(1, 60).All(f => TrialTowerService.StageForFloor(f).EnemyIds.All(id => id != "custom_hero")
            && new BattleEngine(new List<string> { "quan_vu" }, TrialTowerService.StageForFloor(f), 1).EnemyFighters.Count == TrialTowerService.EnemyCount(f)),
            "every floor builds a battle");
        Check(TrialTowerService.LockReason(SeedService.CreateSeededSave("t249a")) == "Qua chương 4 và đạt Lv25", "locked for a new account");
    }

    private void CheckRewards()
    {
        Check(TrialTowerService.GemLevel(1) == 1 && TrialTowerService.GemLevel(10) == 1 && TrialTowerService.GemLevel(11) == 2
            && TrialTowerService.GemLevel(60) == 6, "gem level 1 + (floor - 1) / 10");
        Check(TrialTowerService.GemCount(9) == 1 && TrialTowerService.GemCount(10) == 3, "3 gems on every 10th floor");
        var s = SeedService.CreateSeededSave("t249b");
        s.LastStage = 30;
        s.Heroes["custom_hero"].Level = 25;
        int gems0 = s.GemInventory.Values.Sum(), silver = s.Silver;
        var r1 = TrialTowerService.Apply(s, "1", new System.Random(1));
        Check(r1 != null && r1.Drops.Count == 1 && r1.Drops.All(d => d.Kind == StageRewardService.GemKind && GemService.ParseGemId(d.ItemId).Level == 1),
            "floor 1: 1 Lv1 gem");
        Check(s.TrialFloor == 1 && s.GemInventory.Values.Sum() == gems0 + 1 && s.Silver == silver, "gem lands in GemInventory, no Bạc");
        Check(TrialTowerService.Apply(s, "1", new System.Random(2)) == null && s.GemInventory.Values.Sum() == gems0 + 1, "floor 1 cannot be farmed again");
        Check(TrialTowerService.Apply(s, "3", new System.Random(2)) == null, "cannot skip floors");
        s.TrialFloor = 9;
        var r10 = TrialTowerService.Apply(s, "10", new System.Random(3));
        Check(r10 != null && r10.Drops.Count == 3 && r10.Drops.All(d => GemService.ParseGemId(d.ItemId).Level == 1) && s.TrialFloor == 10, "floor 10: 3 Lv1 gems");
        var r11 = TrialTowerService.Apply(s, "11", new System.Random(4));
        Check(r11 != null && r11.Drops.Count == 1 && GemService.ParseGemId(r11.Drops[0].ItemId).Level == 2, "floor 11: a Lv2 gem");
        var a = TrialTowerService.RewardForFloor(25, StageRewardService.DropRng(77));
        var b = TrialTowerService.RewardForFloor(25, StageRewardService.DropRng(77));
        Check(a.Drops.Select(d => d.ItemId).SequenceEqual(b.Drops.Select(d => d.ItemId)), "same seed => same gems");
        s.TrialFloor = 60;
        Check(TrialTowerService.LockReason(s) == "Đã vượt hết 60 tầng!", "finished after floor 60");
    }

    private bool RunToEnd(BattleScene scene)
    {
        for (int i = 0; i < 800; i++)
        {
            scene.StepBattle();
            if (scene.GetNodeOrNull<Control>("ResultPopup") != null) return true;
        }
        return false;
    }

    private void CheckUiAndBattle()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("t249c");
        gm.Save.LastStage = 30;
        gm.Save.OwnedHeroIds.AddRange(new[] { "quan_vu", "trieu_van", "gia_cat_luong", "lu_bo" });
        foreach (var id in gm.Save.OwnedHeroIds) gm.Save.Heroes[id].Level = 60;
        FormationService.EnsureFormation(gm.Save);
        FormationService.TryPlace(gm.Save, "quan_vu", "hau_top", null, out _);
        FormationService.TryPlace(gm.Save, "trieu_van", "tien_mid", null, out _);
        FormationService.TryPlace(gm.Save, "gia_cat_luong", "hau_mid", null, out _);
        FormationService.TryPlace(gm.Save, "lu_bo", "trung_top", null, out _);
        var ui = ResourceLoader.Load<PackedScene>("res://features/trial/scenes/TrialTowerScene.tscn").Instantiate<TrialTowerScene>();
        AddChild(ui);
        var btn = ui.GetNode<Button>("ChallengeButton");
        Check(!btn.Disabled && btn.Text == "Khiêu chiến tầng 1", "button: Khiêu chiến tầng 1");
        Check(ui.GetNode<Label>("InfoLabel").Text.Contains("1 ngọc Lv1"), "info shows the gem reward");
        Check(ui.PrepareBattle() && gm.CurrentMode == BattleMode.Trial && gm.SelectedTeamIds.Count == 5, "Trial battle with the 5-hero formation");
        ui.QueueFree();
        int gems = gm.Save.GemInventory.Values.Sum();
        var scene = ResourceLoader.Load<PackedScene>("res://features/battle/scenes/BattleScene.tscn").Instantiate<BattleScene>();
        AddChild(scene);
        Check(RunToEnd(scene) && gm.LastBattleWon, "Lv60 team clears floor 1");
        Check(gm.Save.TrialFloor == 1 && gm.Save.GemInventory.Values.Sum() == gems + 1, "floor 1 cleared: +1 gem in the pouch");
        var squares = scene.FindChild("RewardSquares", true, false) as HBoxContainer;
        Check(squares != null && squares.GetChildren().Count(c => ((Node)c).FindChild("DropLabel", true, false) != null) == 1, "popup shows the gem square");
        scene.QueueFree();
        var ui2 = ResourceLoader.Load<PackedScene>("res://features/trial/scenes/TrialTowerScene.tscn").Instantiate<TrialTowerScene>();
        AddChild(ui2);
        Check(ui2.GetNode<Button>("ChallengeButton").Text == "Khiêu chiến tầng 2" && ui2.GetNode<Label>("FloorLabel").Text.StartsWith("Đã vượt: 1/60"), "moves on to floor 2");
        ui2.QueueFree();

        // The Hoạt Động card opens this popup.
        var hub = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn").Instantiate<HubScene>();
        AddChild(hub);
        gm.Save.Heroes["custom_hero"].Level = 25;
        hub.GetNode<Button>("ActivityButtonIcon/ActivityButton").EmitSignal(BaseButton.SignalName.Pressed);
        var content = hub.GetNode<Control>("PopupFrame/PopupContent");
        (content.GetChild(0) as ActivityScene)!.GetNode<Button>("CardGrid/Card_Trial").EmitSignal(BaseButton.SignalName.Pressed);
        Check(content.GetChildren().OfType<TrialTowerScene>().FirstOrDefault()?.Size == HubScene.TrialPopupSize, "Thí Luyện card opens the 700x500 popup");
        hub.ClosePopup();
        hub.QueueFree();
    }
}
