using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase1 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CheckData();
        CheckStages();
        CheckDeterminism();
        CheckDamageMath();
        CheckRageAndUltimates();
        CheckStarsAndRewards();
        CheckSaveTeamAndStars();
        GD.Print(_failures == 0 ? "TEST PHASE1: ALL PASS" : $"TEST PHASE1: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }

    private void CheckData()
    {
        Check(HeroDatabase.Heroes.Count == 16, "16 heroes (2.43: 8 original + 8 tavern)");
        Check(HeroDatabase.Heroes.Select(h => h.Id).Distinct().Count() == 16, "hero ids unique");
        Check(HeroDatabase.Heroes.All(h => h.NormalName.Length > 0 && h.UltName.Length > 0 && h.PassiveName.Length > 0 && h.PassiveDesc.Length > 0), "every hero has normal + ult + passive");
        Check(HeroDatabase.Heroes.All(h => h.Hp > 0 && h.Atk > 0 && h.Speed > 0), "hero stats positive");
    }

    private void CheckStages()
    {
        // Phase 2.41: 8 story chapters, 70 stages; chapter 1 keeps 3 enemies.
        Check(HeroDatabase.Stages.Count == 70, "70 stages (8 chapters)");
        Check(HeroDatabase.Stages.Where(s => s.Chapter == 1).All(s => s.EnemyIds.Count == 3), "every chapter 1 stage has 3 enemies");
        bool scaleUp = true;
        for (int i = 1; i < HeroDatabase.Stages.Count; i++)
            if (HeroDatabase.Stages[i].Scale <= HeroDatabase.Stages[i - 1].Scale) scaleUp = false;
        Check(scaleUp, "stage scale strictly increases");
        Check(StageRewardService.SilverPerWin == 1000, "2.39: every win rewards 1000 silver");
    }

    private BattleEngine NewEngine(int seed) => new(
        new List<string> { "quan_vu", "truong_phi", "gia_cat_luong" },
        HeroDatabase.GetStage(1), seed);

    private void CheckDeterminism()
    {
        var a = NewEngine(12345); a.AutoRun(true);
        var b = NewEngine(12345); b.AutoRun(true);
        Check(a.PlayerWon == b.PlayerWon && a.TurnCount == b.TurnCount, "same seed => same winner and turns");
        Check(a.PlayerFighters.Sum(f => f.Hp) == b.PlayerFighters.Sum(f => f.Hp)
            && a.EnemyFighters.Sum(f => f.Hp) == b.EnemyFighters.Sum(f => f.Hp), "same seed => same final HP");
        Check(a.IsOver && b.IsOver, "auto run finishes battle");
    }

    private Fighter MakeFighter(string id, bool isPlayer)
    {
        var def = HeroDatabase.Get(id);
        return new Fighter(new HeroDef { Id = def.Id, Name = def.Name, Role = def.Role, Hp = def.Hp, Atk = def.Atk, Def = def.Def, Speed = def.Speed, Crit = def.Crit, Dodge = def.Dodge, DamageReduction = def.DamageReduction, NormalName = def.NormalName, UltName = def.UltName, UltPower = def.UltPower, UltKind = def.UltKind, PassiveName = def.PassiveName, PassiveDesc = def.PassiveDesc }, isPlayer);
    }

    private void CheckDamageMath()
    {
        var warrior = MakeFighter("quan_vu", true);
        warrior.Def.Atk = 100; warrior.Def.Crit = 0; warrior.Def.Dodge = 0;
        var strategist = MakeFighter("gia_cat_luong", false);
        strategist.Def.Def = 0; strategist.Def.Dodge = 0; strategist.Def.DamageReduction = 0;
        var vanguard = MakeFighter("truong_phi", false);
        vanguard.Def.Def = 0; vanguard.Def.Dodge = 0; vanguard.Def.DamageReduction = 0;
        int counterDmg = BattleEngine.CalculateDamage(warrior, strategist, 1.0, false, false, 1.0);
        int neutralDmg = BattleEngine.CalculateDamage(warrior, vanguard, 1.0, false, false, 1.0);
        Check(counterDmg == 125 && neutralDmg == 100, "counter 1.25x: warrior vs strategist 125, vs vanguard 100");
        Check(BattleEngine.CalculateDamage(warrior, strategist, 1.0, false, true, 1.0) == 0, "dodge => 0 damage");
        Check(BattleEngine.CalculateDamage(warrior, strategist, 1.0, true, false, 1.0) == 188, "crit 1.5x on counter (125*1.5=188 rounded)");
        Check(!HeroDatabase.Counters(HeroRole.Vanguard, HeroRole.Warrior) && HeroDatabase.Counters(HeroRole.Support, HeroRole.Warrior), "counter table: vanguard counters none, support counters warrior");
    }

    private void CheckRageAndUltimates()
    {
        var e = NewEngine(777);
        var gcl = e.PlayerFighters.First(f => f.Def.Id == "gia_cat_luong");
        gcl.Rage = 99;
        Check(!e.UseUltimate(gcl) && gcl.Rage == 99, "ult blocked below 100 rage");
        var enemyHpBefore = e.EnemyFighters.Select(f => f.Hp).ToList();
        gcl.Rage = 100;
        Check(e.UseUltimate(gcl) && gcl.Rage == 0, "ult fires at 100 rage and resets rage");
        // AoE targets all enemies, but a single dodge can leave one HP unchanged —
        // so assert total enemy HP drops (not every single target, dodge is legal).
        Check(e.EnemyFighters.Sum(f => f.Hp) < enemyHpBefore.Sum(), "Gia Cat Luong AoE ult reduces total enemy HP");

        var e2 = NewEngine(778);
        var dt = e2.PlayerFighters.First(f => f.Def.Id == "gia_cat_luong");
        // Heal test needs Dieu Thuyen in team: build a custom engine via team with DT.
        var e3 = new BattleEngine(new List<string> { "dieu_thuyen", "truong_phi", "quan_vu" }, HeroDatabase.GetStage(1), 779);
        var healer = e3.PlayerFighters.First(f => f.Def.Id == "dieu_thuyen");
        var wounded = e3.PlayerFighters.First(f => f.Def.Id == "truong_phi");
        wounded.Hp = 100;
        int before = wounded.Hp;
        healer.Rage = 100;
        Check(e3.UseUltimate(healer), "heal ult fires");
        Check(wounded.Hp > before, "heal ult restores lowest ally HP");
        _ = dt; _ = e2;
    }

    private void CheckStarsAndRewards()
    {
        Check(BattleEngine.StarsFor(false, 0.9, 5) == 0, "lose => 0 stars");
        Check(BattleEngine.StarsFor(true, 0.8, 10) == 3, "win high HP fast => 3 stars");
        Check(BattleEngine.StarsFor(true, 0.5, 10) == 2, "win mid HP => 2 stars");
        Check(BattleEngine.StarsFor(true, 0.1, 30) == 1, "win low HP slow => 1 star");
    }

    private void CheckSaveTeamAndStars()
    {
        var path = Path.Combine(GameManager.DataDir, "test_phase1_save.json");
        try
        {
            var data = new SaveData { Gold = 500, TeamIds = new List<string> { "lu_bo", "trieu_van", "dieu_thuyen" }, StageStars = new Dictionary<string, int> { ["1"] = 3, ["2"] = 2 } };
            SaveManager.Save(path, data);
            var loaded = SaveManager.Load(path);
            Check(loaded.TeamIds.Count == 3 && loaded.TeamIds[0] == "lu_bo", "save/load preserves team");
            Check(loaded.StageStars["1"] == 3 && loaded.StageStars["2"] == 2, "save/load preserves stage stars");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
