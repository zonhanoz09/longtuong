using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase22 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CheckSeed();
        CheckUnlock();
        CheckLegacyMigration();
        CheckOneHeroBattle();
        CheckUiTheme();
        CheckScenesLoad();
        GD.Print(_failures == 0 ? "TEST PHASE2.2: ALL PASS" : $"TEST PHASE2.2: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }

    private void CheckSeed()
    {
        var a = SeedService.StarterHeroId("huynh");
        var b = SeedService.StarterHeroId("nguoi_khac");
        Check(a == "custom_hero" && b == "custom_hero", "every new account starts with the custom hero");
        Check(HeroDatabase.Heroes.Any(h => h.Id == a), "starter hero is a valid hero");
        Check(SeedService.SeededGold == 100000 && SeedService.SeededGems == 10000, "seed gold 100000 / gems 10000");
        var save = SeedService.CreateSeededSave("huynh");
        Check(save.Gold == 100000 && save.Gems == 10000, "seeded save has test gold/gems");
        Check(save.OwnedHeroIds.Count == 2 && save.OwnedHeroIds[0] == a && save.OwnedHeroIds.Contains("luu_bi"), "seeded save owns the starter + Luu Bi (never locked)");
        Check(HeroDatabase.Get("custom_hero").Name == "huynh", "custom hero is named after the username");
        Check(save.TeamIds.Count == 1 && save.TeamIds[0] == a, "seeded save team is just the starter");
    }

    private void CheckUnlock()
    {
        var save = SeedService.CreateSeededSave("huynh");
        var starter = save.OwnedHeroIds[0];
        var locked = HeroDatabase.Heroes.First(h => !save.OwnedHeroIds.Contains(h.Id)).Id; // not Luu Bi: he is never locked
        save.Shards[locked] = 9;
        Check(ProgressionService.CheckUnlocks(save).Count == 0 && !save.OwnedHeroIds.Contains(locked), "9 shards does not unlock");
        save.Shards[locked] = 10;
        var unlocked = ProgressionService.CheckUnlocks(save);
        Check(unlocked.Contains(locked) && save.OwnedHeroIds.Contains(locked) && save.Shards[locked] == 0, "10 shards unlocks hero and spends 10 shards");
    }

    private void CheckLegacyMigration()
    {
        var save = new SaveData();
        save.OwnedHeroIds = new List<string>();
        ProgressionService.EnsureDefaults(save);
        Check(save.OwnedHeroIds.Count == 8, "legacy save with empty owned list keeps all 8 heroes");
    }

    private void CheckOneHeroBattle()
    {
        var save = SeedService.CreateSeededSave("solo");
        var eff = ProgressionService.GetEffectiveHero(save, save.OwnedHeroIds[0]);
        var engine = new BattleEngine(new List<HeroDef> { eff }, HeroDatabase.GetStage(1), 123);
        engine.AutoRun(true);
        Check(engine.IsOver, "1-hero team battle finishes with auto-run");
        Check(engine.PlayerFighters.Count == 1 && engine.EnemyFighters.Count == 3, "battle supports 1 vs 3 team sizes");
    }

    private void CheckUiTheme()
    {
        var btn = new Button();
        UiTheme.StyleButton(btn, true);
        Check(btn.CustomMinimumSize.Y >= 56, "primary button min height >= 56");
        Check(UiTheme.Gold.R > 0.8f && UiTheme.Background.R < 0.2f, "theme palette: gold accent on dark background");
        var label = new Label();
        UiTheme.StyleTitle(label);
        Check(true, "theme title styling applies without crash");
        btn.Free(); label.Free();
    }

    private void CheckScenesLoad()
    {
        string[] scenes =
        {
            "res://features/hub/scenes/HubScene.tscn",
            "res://features/auth/scenes/AuthScene.tscn",
            "res://features/hero/scenes/HeroListScene.tscn",
            "res://features/bag/scenes/BagScene.tscn",
            "res://features/formation/scenes/FormationScene.tscn",
            "res://features/recruit/scenes/RecruitScene.tscn",
            "res://features/stage/scenes/StageSelect.tscn",
            "res://features/battle/scenes/BattleScene.tscn",
        };
        foreach (var s in scenes)
            Check(ResourceLoader.Load<PackedScene>(s) != null, $"scene loads: {s}");
    }
}
