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
        Check(save.OwnedHeroIds.Count == 1 && save.OwnedHeroIds[0] == "custom_hero", "new account owns ONLY the custom hero (2.22)");
        Check(save.TeamIds.Count == 1 && save.TeamIds[0] == "custom_hero", "new account team is just the custom hero");
        // Old accounts keep their heroes; legacy migration now yields 7.
        var legacy = new SaveData { OwnedHeroIds = new List<string>() };
        ProgressionService.EnsureDefaults(legacy);
        Check(legacy.OwnedHeroIds.Count == 8 && legacy.OwnedHeroIds.Contains("luu_bi"), "legacy save keeps all 8 heroes");
        // Tavern pool covers all 7 heroes, weights sum to 100 per grade.
        Check(TavernData.PoolHeroIds.Count == 7 && TavernData.PoolHeroIds.Contains("luu_bi") && !TavernData.PoolHeroIds.Contains("custom_hero"), "tavern pool includes Luu Bi, excludes the custom hero");
        var (ln, tn, cn) = TavernData.DrawWeights(TavernGrade.Normal);
        var (lh, th, ch) = TavernData.DrawWeights(TavernGrade.High);
        Check(ln + tn + cn == 100 && lh + th + ch == 100, "tavern draw weights sum to 100 per grade");
        Check(cn == 5 && ch == 10, "Cam draw weight 5% normal / 10% high");
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
