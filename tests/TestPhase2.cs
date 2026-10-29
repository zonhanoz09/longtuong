using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase2 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CheckExpAndLevel();
        CheckScaling();
        CheckEquipment();
        CheckStarChart();
        // Phase 2.23: recruit lives in the tavern (TestPhase223); the old
        // gacha CheckRecruit was deleted with RecruitService.
        CheckEffectiveBattle();
        CheckSaveProgression();
        GD.Print(_failures == 0 ? "TEST PHASE2: ALL PASS" : $"TEST PHASE2: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }

    private SaveData Fresh()
    {
        var s = new SaveData { Gold = 100000 };
        ProgressionService.EnsureDefaults(s);
        return s;
    }

    private void CheckExpAndLevel()
    {
        Check(ProgressionService.ExpToNext(1) == 100 && ProgressionService.ExpToNext(2) == 150, "exp curve 100/150");
        var s = Fresh();
        ProgressionService.AddExp(s, "quan_vu", 250);
        Check(s.Heroes["quan_vu"].Level == 3 && s.Heroes["quan_vu"].Exp == 0, "250 exp: level 1 -> 3, exp 0");
        var s2 = Fresh();
        s2.Heroes["quan_vu"].Level = 180;
        ProgressionService.AddExp(s2, "quan_vu", 99999);
        Check(s2.Heroes["quan_vu"].Level == 180, "level capped at 180 (2.23: was 60)");
    }

    private void CheckScaling()
    {
        var baseDef = HeroDatabase.Get("quan_vu");
        var scaled = ProgressionService.GetScaledBase("quan_vu", 11);
        // Phase 2.23: no stars; Quan Vu is Tim (stat 1.15, growth 1.15):
        // level factor 1+0.04*1.15*10 = 1.46, total 1.15*1.46 = 1.679x.
        Check(scaled.Hp == (int)Math.Round(baseDef.Hp * 1.679) && scaled.Atk == (int)Math.Round(baseDef.Atk * 1.679), "scaled base = base * 1.15 * 1.46 (Tim quality)");
        var s = Fresh();
        var effLow = ProgressionService.GetEffectiveHero(s, "quan_vu");
        s.Heroes["quan_vu"].Level = 20;
        var effHigh = ProgressionService.GetEffectiveHero(s, "quan_vu");
        Check(effHigh.Hp > effLow.Hp && effHigh.Atk > effLow.Atk, "higher level => higher effective stats");
    }

    private void CheckEquipment()
    {
        var s = Fresh();
        // Phase 2.5: fresh heroes start with empty slots, so equip a bag item first.
        s.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon", Level = 0, RefineIndex = 0 });
        Check(EquipmentService.EquipFromBag(s, "quan_vu", 0) && !s.HeroEquipment["quan_vu"]["weapon"].IsEmpty, "equip bag weapon into empty slot");
        var rng = new System.Random(7);
        int silverBefore = s.Silver;
        Check(ProgressionService.EnhanceCostSilver(0) == 100 && ProgressionService.EnhanceCostSilver(9) == 51200, "enhance cost 100x2^level silver");
        Check(ProgressionService.MaxEnhanceForHero(s) == 1, "max enhance follows custom hero level (1)");
        Check(ProgressionService.TryEnhance(s, "quan_vu", "weapon", rng) == ProgressionService.EnhanceResult.Success
            && s.HeroEquipment["quan_vu"]["weapon"].Level == 1 && s.Silver == silverBefore - 100, "enhance weapon 0->1 costs 100 silver");
        Check(ProgressionService.TryEnhance(s, "quan_vu", "weapon", rng) == ProgressionService.EnhanceResult.Failed_Maxed, "enhance fails at hero-level cap");
        s.HeroEquipment["quan_vu"]["weapon"].Level = 10;
        Check(ProgressionService.TryEnhance(s, "quan_vu", "weapon", rng) == ProgressionService.EnhanceResult.Failed_Maxed, "enhance fails at max 10");
        var s2 = Fresh(); s2.Silver = 50;
        Check(ProgressionService.TryEnhance(s2, "quan_vu", "weapon", rng) == ProgressionService.EnhanceResult.Failed_Empty && s2.Silver == 50, "enhance fails on empty slot, no deduction");
        s2.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon", Level = 0, RefineIndex = 0 });
        EquipmentService.EquipFromBag(s2, "quan_vu", 0);
        Check(ProgressionService.TryEnhance(s2, "quan_vu", "weapon", rng) == ProgressionService.EnhanceResult.Failed_NoSilver && s2.Silver == 50, "enhance fails with insufficient silver, no deduction");
        Check(ProgressionService.EnhanceSuccessRate(7) == 1.0 && ProgressionService.EnhanceSuccessRate(8) == 0.9
            && ProgressionService.EnhanceSuccessRate(9) == 0.8 && ProgressionService.EnhanceSuccessRate(10) == 0.7, "success rates 100/90/80/70");
        int failSeed = -1;
        for (int i = 0; i < 500 && failSeed < 0; i++) if (new System.Random(i).NextDouble() >= 0.9) failSeed = i;
        Check(failSeed >= 0, "found a failing roll seed");
        var s4 = Fresh();
        s4.Heroes["custom_hero"].Level = 10;
        s4.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon", Level = 7 });
        EquipmentService.EquipFromBag(s4, "custom_hero", 0);
        int silv = s4.Silver;
        Check(ProgressionService.TryEnhance(s4, "custom_hero", "weapon", new System.Random(failSeed)) == ProgressionService.EnhanceResult.Failed_Roll
            && s4.HeroEquipment["custom_hero"]["weapon"].Level == 7 && s4.Silver == silv - ProgressionService.EnhanceCostSilver(7),
            "failed roll keeps level but consumes silver");
        var s3 = Fresh();
        s3.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon", Level = 0, RefineIndex = 0 });
        EquipmentService.EquipFromBag(s3, "quan_vu", 0);
        Check(ProgressionService.TryRefine(s3, "quan_vu", "weapon") && s3.HeroEquipment["quan_vu"]["weapon"].RefineIndex == 1, "refine cycles 0->1 and costs 100");
        for (int i = 0; i < 4; i++) ProgressionService.TryRefine(s3, "quan_vu", "weapon");
        Check(s3.HeroEquipment["quan_vu"]["weapon"].RefineIndex == 0, "refine cycles back to 0 after 5");
        Check(ProgressionService.RefineNames.Length == 5, "5 refine bonus lines");
    }

    private void CheckStarChart()
    {
        var s = Fresh();
        s.StageStars["1"] = 1;
        Check(!ProgressionService.TryUnlockStarNode(s), "star node blocked with 1 point (needs 2)");
        s.StageStars["1"] = 2;
        Check(ProgressionService.TryUnlockStarNode(s) && s.StarNodesUnlocked == 1, "star node unlocks at 2 points");
        var eff = ProgressionService.GetEffectiveHero(s, "quan_vu");
        var s0 = Fresh();
        var eff0 = ProgressionService.GetEffectiveHero(s0, "quan_vu");
        Check(eff.Atk > eff0.Atk && eff.Hp > eff0.Hp, "star node gives team-wide +3% HP/ATK");
        Check(ProgressionService.StarChartPoints(s) == 2, "star chart points = sum of stage stars");
    }

    private void CheckEffectiveBattle()
    {
        var s = Fresh();
        s.Heroes["quan_vu"].Level = 20;
        var eff = ProgressionService.GetEffectiveHero(s, "quan_vu");
        var engine = new BattleEngine(new List<HeroDef> { eff, ProgressionService.GetEffectiveHero(s, "truong_phi"), ProgressionService.GetEffectiveHero(s, "gia_cat_luong") }, HeroDatabase.GetStage(1), 99);
        var fighter = engine.PlayerFighters.First(f => f.Def.Id == "quan_vu");
        Check(fighter.MaxHp > HeroDatabase.Get("quan_vu").Hp, "effective hero in battle has higher HP than base");
        Check(fighter.MaxHp == eff.Hp, "battle fighter HP equals effective hero HP");
    }

    private void CheckSaveProgression()
    {
        var path = Path.Combine(OS.GetUserDataDir(), "test_phase2_save.json");
        try
        {
            var s = Fresh();
            s.Heroes["lu_bo"].Level = 12;
            s.HeroEquipment["lu_bo"]["weapon"].IsEmpty = false; s.HeroEquipment["lu_bo"]["weapon"].Level = 5; s.HeroEquipment["lu_bo"]["weapon"].RefineIndex = 2;
            s.StarNodesUnlocked = 3; s.Silver = 77;
            SaveManager.Save(path, s);
            var loaded = SaveManager.Load(path);
            ProgressionService.EnsureDefaults(loaded);
            Check(loaded.Heroes["lu_bo"].Level == 12, "save/load preserves hero level (2.23: no stars)");
            Check(loaded.Silver == 77 && loaded.StarNodesUnlocked == 3, "save/load preserves silver/star nodes");
            Check(loaded.HeroEquipment["lu_bo"]["weapon"].Level == 5 && loaded.HeroEquipment["lu_bo"]["weapon"].RefineIndex == 2, "save/load preserves equipment");
            var empty = new SaveData();
            ProgressionService.EnsureDefaults(empty);
            Check(empty.Heroes.Count == 8 && empty.HeroEquipment["quan_vu"].Count == ProgressionService.Slots.Count, "EnsureDefaults fills 8 heroes x all equipment slots for old saves");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
