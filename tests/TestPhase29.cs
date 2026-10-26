using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase29 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        Check(HeroDatabase.Heroes.Count == 8, "8 heroes in roster");
        var luuBi = HeroDatabase.Get("luu_bi");
        Check(luuBi.Name == "Lưu Bị" && luuBi.Role == HeroRole.Vanguard, "Luu Bi is a Vanguard hero");
        Check(luuBi.UltKind == UltKind.SingleDamage && luuBi.UltPower == 1.8, "Luu Bi ult is single damage x1.8");
        Check(luuBi.Hp == 620 && luuBi.Atk == 72 && luuBi.Def == 42, "Luu Bi balanced base stats");
        // Default starter for new accounts, regardless of username.
        Check(SeedService.StarterHeroId("bat_ky_ten_nao") == "custom_hero", "starter is the custom hero, not Luu Bi anymore");
        var save = SeedService.CreateSeededSave("tai_khoan_moi");
        Check(save.OwnedHeroIds.Count == 2 && save.OwnedHeroIds[0] == "custom_hero" && save.OwnedHeroIds.Contains("luu_bi"), "new account owns the custom hero + Luu Bi");
        Check(save.TeamIds.Count == 1 && save.TeamIds[0] == "custom_hero", "new account team is just the custom hero");
        // Old accounts keep their heroes; legacy migration now yields 7.
        var legacy = new SaveData { OwnedHeroIds = new List<string>() };
        ProgressionService.EnsureDefaults(legacy);
        Check(legacy.OwnedHeroIds.Count == 8 && legacy.OwnedHeroIds.Contains("luu_bi"), "legacy save keeps all 8 heroes");
        // Recruit rates cover all 7 heroes and sum to 100.
        Check(RecruitService.Rates.Sum(r => r.Weight) == 100, "recruit rates sum to 100");
        Check(RecruitService.Rates.Count == 7 && RecruitService.Rates.Any(r => r.HeroId == "luu_bi") && !RecruitService.Rates.Any(r => r.HeroId == "custom_hero"), "recruit rates include Luu Bi, exclude the custom hero");
        // Luu Bi can fight: 1-hero team vs stage 1 finishes via the generic engine.
        var eff = ProgressionService.GetEffectiveHero(save, "luu_bi"); // Luu Bi progress exists for all heroes even when not owned
        var engine = new BattleEngine(new List<HeroDef> { eff }, HeroDatabase.GetStage(1), 123);
        engine.AutoRun(true);
        Check(engine.IsOver, "Luu Bi solo battle finishes with auto-run");
        GD.Print(_failures == 0 ? "TEST PHASE2.9: ALL PASS" : $"TEST PHASE2.9: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }
}
