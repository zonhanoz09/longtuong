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
        CheckSaveRoundTrip();
        CheckScenesLoad();
        GD.Print(_failures == 0 ? "TEST PHASE0: ALL PASS" : $"TEST PHASE0: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }

    // Phase 2.38: the Phase 0 placeholder battle + main scene were removed
    // as dead code; this test now only covers the scaffold that is still
    // live (main scene setting, core scenes, save round trip).
    private void CheckScenesLoad()
    {
        var mainPath = (string)ProjectSettings.GetSetting("application/run/main_scene");
        Check(mainPath == "res://features/auth/scenes/AuthScene.tscn", "main scene is the auth screen");
        Check(ResourceLoader.Load<PackedScene>(mainPath) != null, "main scene loads");
        var stage = ResourceLoader.Load<PackedScene>("res://features/stage/scenes/StageSelect.tscn");
        Check(stage != null, "stage select scene loads");
        var battleScene = ResourceLoader.Load<PackedScene>("res://features/battle/scenes/BattleScene.tscn");
        Check(battleScene != null, "battle scene loads");
        var heroScene = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroScreen.tscn");
        Check(heroScene != null, "hero scene loads");
        Check(!ResourceLoader.Exists("res://features/main/scenes/MainScene.tscn"), "Phase 0 MainScene removed");
        Check(!ResourceLoader.Exists("res://features/battle/scenes/BattlePlaceholder.tscn"), "Phase 0 BattlePlaceholder removed");
    }

    private void CheckSaveRoundTrip()
    {
        var path = Path.Combine(GameManager.DataDir, "test_phase0_save.json");
        try
        {
            var data = new SaveData { Gold = 1234, PlayerLevel = 5, LastStage = 3, Wins = 2 };
            SaveManager.Save(path, data);
            var loaded = SaveManager.Load(path);
            Check(loaded.Gold == 1234 && loaded.PlayerLevel == 5 && loaded.LastStage == 3 && loaded.Wins == 2, "save/load round trip preserves data");
            var missing = SaveManager.Load(Path.Combine(GameManager.DataDir, "test_phase0_missing.json"));
            Check(missing.Gold == 1000 && missing.Wins == 0, "load missing file returns defaults");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
        }
    }
}
