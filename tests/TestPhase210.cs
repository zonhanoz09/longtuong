using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase210 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        Check(HeroDatabase.Heroes.Count == 8, "8 heroes in roster");
        Check(HeroDatabase.Heroes[0].Id == "custom_hero", "custom hero is first in the roster");
        var custom = HeroDatabase.Get("custom_hero");
        Check(custom.Role == HeroRole.Vanguard, "custom hero is Vanguard (counter-neutral)");
        Check(custom.Hp == 500 && custom.Atk == 60 && custom.Def == 30 && custom.Speed == 60, "custom hero has basic stats");
        Check(custom.UltPower == 1.0 && custom.PassiveName == "Chưa có bị động", "custom hero has no real skills yet (placeholder ult 1.0x)");
        // Name follows the username, for any new account.
        var save = SeedService.CreateSeededSave("nguoi_choi_a");
        Check(HeroDatabase.Get("custom_hero").Name == "nguoi_choi_a", "custom hero is named after the username");
        Check(SeedService.StarterHeroId("bat_ky_ai") == "custom_hero", "starter is the custom hero for any username");
        Check(save.OwnedHeroIds.Count == 2 && save.OwnedHeroIds[0] == "custom_hero" && save.OwnedHeroIds.Contains("luu_bi"), "new account owns the custom hero + Luu Bi (never locked)");
        Check(save.TeamIds.Count == 1 && save.TeamIds[0] == "custom_hero", "new account team is just the custom hero");
        // Not in the recruit pool.
        Check(!RecruitService.Rates.Any(r => r.HeroId == "custom_hero"), "custom hero is not in the recruit pool");
        Check(RecruitService.Rates.Sum(r => r.Weight) == 100 && RecruitService.Rates.Count == 7, "recruit rates still sum to 100 over 7 heroes");
        // GameManager path: seed then reload keeps the username as the hero name.
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("ten_moi");
        Check(gm.SelectedHeroId == "custom_hero" && HeroDatabase.Get("custom_hero").Name == "ten_moi", "GameManager seed names the custom hero");
        gm.LoadGame();
        Check(HeroDatabase.Get("custom_hero").Name == "ten_moi", "reload after login keeps the custom hero name");
        // Old (2.9) accounts do NOT auto-gain the custom hero.
        var old = new SaveData { OwnedHeroIds = new List<string> { "luu_bi" } };
        ProgressionService.EnsureDefaults(old);
        Check(!old.OwnedHeroIds.Contains("custom_hero"), "old accounts do not auto-gain the custom hero");
        var old2 = new SaveData { OwnedHeroIds = new List<string> { "quan_vu" } };
        ProgressionService.EnsureDefaults(old2);
        Check(old2.OwnedHeroIds.Contains("luu_bi"), "old accounts gain Luu Bi on load (never locked)");
        // The placeholder hero can still fight (plain 1.0x ult, no engine special-case).
        var eff = ProgressionService.GetEffectiveHero(save, "custom_hero");
        var engine = new BattleEngine(new List<HeroDef> { eff }, HeroDatabase.GetStage(1), 123);
        engine.AutoRun(true);
        Check(engine.IsOver, "custom hero solo battle finishes with auto-run");
        GD.Print(_failures == 0 ? "TEST PHASE2.10: ALL PASS" : $"TEST PHASE2.10: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }
}
