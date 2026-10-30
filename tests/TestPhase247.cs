using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.47 — Ải Vượt Tướng: main hero 1v1, 100 floors, full HP each
/// floor, floor kept forever, first-clear Bạc + Chiến tích only.
/// </summary>
public partial class TestPhase247 : Node
{
    private int _fails;

    public override void _Ready()
    {
        CheckRules();
        CheckApply();
        CheckUiAndBattle();

        if (_fails == 0) GD.Print("TEST PHASE 2.47: ALL PASS");
        else GD.Print($"TEST PHASE 2.47: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private static SaveData Account(string name, int level = 20)
    {
        var s = SeedService.CreateSeededSave(name);
        s.LastStage = 20;                       // chapter 3 cleared
        s.Heroes["custom_hero"].Level = level;
        return s;
    }

    private void CheckRules()
    {
        Check(DuelTowerService.MaxFloor == 100, "100 floors");
        var st = DuelTowerService.StageForFloor(1);
        Check(st.EnemyIds.Count == 1 && st.EnemyIds[0] == DuelTowerService.OpponentFor(1), "floor 1 is one enemy");
        Check(System.Math.Abs(DuelTowerService.ScaleFor(1) - 0.93) < 1e-9 && System.Math.Abs(DuelTowerService.ScaleFor(100) - 13.8) < 1e-9, "Scale 0.8 + 0.13 x floor (2.52)");
        Check(Enumerable.Range(1, 100).All(f => DuelTowerService.OpponentFor(f) != "custom_hero"), "the custom hero is never an opponent");
        Check(DuelTowerService.OpponentFor(1) != DuelTowerService.OpponentFor(2), "opponents rotate");
        var r1 = DuelTowerService.RewardForFloor(1);
        var r10 = DuelTowerService.RewardForFloor(10);
        Check(r1.Silver == 500 && r1.Merit == 12 && r10.Silver == 5000 && r10.Merit == 30, "reward 500 x n Bạc + (10 + 2n) Chiến tích");
        var e = new BattleEngine(new List<string> { "custom_hero" }, st, 1);
        Check(e.PlayerFighters.Count == 1 && e.EnemyFighters.Count == 1, "battle is 1v1");
        var fresh = SeedService.CreateSeededSave("t247a");
        Check(DuelTowerService.LockReason(fresh) == "Qua chương 3 và đạt Lv20", "locked for a new account");
        Check(DuelTowerService.LockReason(Account("t247b")) == "", "chapter 3 + Lv20: open");
    }

    private void CheckApply()
    {
        var s = Account("t247c");
        int silver = s.Silver, merit = s.Merit;
        var r = DuelTowerService.Apply(s, "1");
        Check(r != null && s.DuelFloor == 1 && s.Silver == silver + 500 && s.Merit == merit + 12, "win floor 1: +500 Bạc, +12 Chiến tích, floor 1");
        Check(DuelTowerService.Apply(s, "1") == null && s.DuelFloor == 1 && s.Silver == silver + 500, "floor 1 again pays nothing");
        Check(DuelTowerService.Apply(s, "5") == null && s.DuelFloor == 1, "cannot skip floors");
        Check(DuelTowerService.NextFloor(s) == 2, "next floor = 2");
        s.DuelFloor = 100;
        Check(DuelTowerService.IsFinished(s) && DuelTowerService.LockReason(s) == "Đã vượt hết 100 tầng!" && DuelTowerService.Apply(s, "101") == null, "after floor 100: finished");
        // Kept across save/load, and old saves start at 0.
        s.DuelFloor = 7;
        s.Merit = 33;
        var path = ProjectSettings.GlobalizePath("user://t247.json");
        SaveManager.Save(path, s);
        var loaded = SaveManager.Load(path);
        Check(loaded.DuelFloor == 7 && loaded.Merit == 33, "floor + Chiến tích persist");
        System.IO.File.Delete(path);
        Check(new SaveData().DuelFloor == 0 && new SaveData().Merit == 0, "new/old saves start at floor 0, 0 Chiến tích");
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
        gm.CreateSeededSave("t247d");
        gm.Save.LastStage = 20;
        gm.Save.Heroes["custom_hero"].Level = 60;
        gm.Save.OwnedHeroIds.Add("quan_vu");
        FormationService.EnsureFormation(gm.Save);
        FormationService.TryPlace(gm.Save, "quan_vu", "tien_mid", null, out _);
        var ui = ResourceLoader.Load<PackedScene>("res://features/duel/scenes/DuelTowerScene.tscn").Instantiate<DuelTowerScene>();
        AddChild(ui);
        var btn = ui.FindChild("ChallengeButton", true, false) as Button;
        Check(btn != null && !btn.Disabled && btn.Text == "Khiêu chiến tầng 1", "button: Khiêu chiến tầng 1");
        Check((ui.FindChild("RewardLabel", true, false) as Label)?.Text == "Thưởng lần đầu: +500 Bạc, +12 Chiến tích", "reward line for floor 1");
        Check(ui.PrepareBattle(), "PrepareBattle starts the floor");
        Check(gm.CurrentMode == BattleMode.Duel && gm.CurrentModeKey == "1" && gm.SelectedTeamIds.SequenceEqual(new[] { "custom_hero" }),
            "only the main hero fights, even with others in the formation");
        ui.QueueFree();

        var scene = ResourceLoader.Load<PackedScene>("res://features/battle/scenes/BattleScene.tscn").Instantiate<BattleScene>();
        AddChild(scene);
        Check(scene.GetNode<GridContainer>("PlayerGrid").GetChildren().OfType<BattleHeroCell>().Count() == 1
            && scene.GetNode<GridContainer>("EnemyGrid").GetChildren().OfType<BattleHeroCell>().Count() == 1, "battle screen shows 1 vs 1");
        int silver = gm.Save.Silver;
        Check(RunToEnd(scene), "duel finishes");
        Check(gm.LastBattleWon, "Lv60 main hero beats floor 1");
        Check(gm.Save.DuelFloor == 1 && gm.Save.Silver == silver + 500 && gm.Save.Merit == 12, "floor 1 reward applied");
        Check(gm.Save.LastStage == 20, "campaign untouched");
        var rewardLabel = scene.FindChildren("*", "Label", true, false).OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("Thưởng:"));
        Check(rewardLabel != null && rewardLabel.Text.Contains("+12 Chiến tích"), "result popup shows Chiến tích");
        scene.QueueFree();

        var ui2 = ResourceLoader.Load<PackedScene>("res://features/duel/scenes/DuelTowerScene.tscn").Instantiate<DuelTowerScene>();
        AddChild(ui2);
        Check((ui2.FindChild("ChallengeButton", true, false) as Button)?.Text == "Khiêu chiến tầng 2", "button moves on to Tầng 2");
        Check((ui2.FindChild("FloorLabel", true, false) as Label)?.Text.StartsWith("Đã vượt: 1/100") == true, "floor label 1/100");
        ui2.QueueFree();

        // A loss changes nothing.
        gm.StartModeBattle(BattleMode.Duel, "2", DuelTowerService.StageForFloor(2), new List<string> { "custom_hero" }, 3);
        int s2 = gm.Save.Silver, m2 = gm.Save.Merit;
        Check(gm.ApplyBattleResult(false, 0) == null && gm.Save.DuelFloor == 1 && gm.Save.Silver == s2 && gm.Save.Merit == m2, "loss: floor and currencies unchanged");
    }
}
