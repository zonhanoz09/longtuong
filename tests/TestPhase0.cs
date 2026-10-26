using Godot;
using System;
using System.IO;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase0 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CheckBattle();
        CheckSaveRoundTrip();
        CheckBattleReset();
        CheckScenesLoad();
        GD.Print(_failures == 0 ? "TEST PHASE0: ALL PASS" : $"TEST PHASE0: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }

    private void CheckBattle()
    {
        var b = new PlaceholderBattle();
        Check(b.PlayerHp == 100 && b.EnemyHp == 100 && !b.IsOver, "battle starts 100/100");
        b.Attack();
        Check(b.EnemyHp == 75 && b.PlayerHp == 90 && b.Turn == 1, "battle turn 1: enemy 75, player 90");
        b.Attack(); b.Attack(); b.Attack();
        Check(b.IsOver && b.PlayerWon, "battle ends in player win after 4 attacks");
        var turns = b.Turn;
        b.Attack();
        Check(b.Turn == turns, "battle does not advance after over");
    }

    private void CheckBattleReset()
    {
        var b = new PlaceholderBattle();
        b.Attack();
        b.Reset();
        Check(b.PlayerHp == 100 && b.EnemyHp == 100 && b.Turn == 0, "battle reset restores 100/100");
    }

    private void CheckScenesLoad()
    {
        var main = ResourceLoader.Load<PackedScene>("res://features/main/scenes/MainScene.tscn");
        Check(main != null, "main scene loads");
        var mainInst = main?.Instantiate<Control>();
        Check(mainInst != null && mainInst.GetNodeOrNull<Label>("StatusLabel") != null, "main scene instantiates with StatusLabel");
        mainInst?.QueueFree();
        var battle = ResourceLoader.Load<PackedScene>("res://features/battle/scenes/BattlePlaceholder.tscn");
        Check(battle != null, "battle scene loads");
        var battleInst = battle?.Instantiate<Control>();
        Check(battleInst != null && battleInst.GetNodeOrNull<Label>("HpLabel") != null, "battle scene instantiates with HpLabel");
        battleInst?.QueueFree();
    }

    private void CheckSaveRoundTrip()
    {
        var path = Path.Combine(OS.GetUserDataDir(), "test_phase0_save.json");
        try
        {
            var data = new SaveData { Gold = 1234, PlayerLevel = 5, LastStage = 3, Wins = 2 };
            SaveManager.Save(path, data);
            var loaded = SaveManager.Load(path);
            Check(loaded.Gold == 1234 && loaded.PlayerLevel == 5 && loaded.LastStage == 3 && loaded.Wins == 2, "save/load round trip preserves data");
            var missing = SaveManager.Load(Path.Combine(OS.GetUserDataDir(), "test_phase0_missing.json"));
            Check(missing.Gold == 1000 && missing.Wins == 0, "load missing file returns defaults");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
