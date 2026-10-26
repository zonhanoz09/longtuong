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
        CheckStarUp();
        CheckScaling();
        CheckEquipment();
        CheckStarChart();
        CheckRecruit();
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
        s2.Heroes["quan_vu"].Level = 60;
        ProgressionService.AddExp(s2, "quan_vu", 99999);
        Check(s2.Heroes["quan_vu"].Level == 60, "level capped at 60");
    }

    private void CheckStarUp()
    {
        var s = Fresh();
        Check(ProgressionService.StarUpCost(1) == 10 && ProgressionService.StarUpCost(4) == 40, "star-up cost 10*stars");
        s.Shards["quan_vu"] = 9;
        Check(!ProgressionService.TryStarUp(s, "quan_vu"), "star-up fails with 9/10 shards");
        s.Shards["quan_vu"] = 10;
        Check(ProgressionService.TryStarUp(s, "quan_vu") && s.Heroes["quan_vu"].Stars == 2 && s.Shards["quan_vu"] == 0, "star-up 1->2 spends 10 shards");
        s.Heroes["quan_vu"].Stars = 5;
        s.Shards["quan_vu"] = 999;
        Check(!ProgressionService.TryStarUp(s, "quan_vu"), "star-up fails at max 5 stars");
    }

    private void CheckScaling()
    {
        var baseDef = HeroDatabase.Get("quan_vu");
        var scaled = ProgressionService.GetScaledBase("quan_vu", 11, 3);
        // level factor 1+0.04*10=1.4, star factor 1+0.25*2=1.5 => 2.1x
        Check(scaled.Hp == (int)Math.Round(baseDef.Hp * 2.1) && scaled.Atk == (int)Math.Round(baseDef.Atk * 2.1), "scaled base = base * 1.4 * 1.5");
        var s = Fresh();
        var effLow = ProgressionService.GetEffectiveHero(s, "quan_vu");
        s.Heroes["quan_vu"].Level = 20;
        s.Heroes["quan_vu"].Stars = 3;
        var effHigh = ProgressionService.GetEffectiveHero(s, "quan_vu");
        Check(effHigh.Hp > effLow.Hp && effHigh.Atk > effLow.Atk, "higher level/star => higher effective stats");
    }

    private void CheckEquipment()
    {
        var s = Fresh();
        // Phase 2.5: fresh heroes start with empty slots, so equip a bag item first.
        s.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon", Level = 0, RefineIndex = 0 });
        Check(EquipmentService.EquipFromBag(s, "quan_vu", 0) && !s.HeroEquipment["quan_vu"]["weapon"].IsEmpty, "equip bag weapon into empty slot");
        int goldBefore = s.Gold;
        Check(ProgressionService.EnhanceCost(0) == 100, "enhance cost level 0 = 100");
        Check(ProgressionService.TryEnhance(s, "quan_vu", "weapon") && s.HeroEquipment["quan_vu"]["weapon"].Level == 1 && s.Gold == goldBefore - 100, "enhance weapon 0->1 costs 100");
        s.HeroEquipment["quan_vu"]["weapon"].Level = 10;
        Check(!ProgressionService.TryEnhance(s, "quan_vu", "weapon"), "enhance fails at max 10");
        var s2 = Fresh(); s2.Gold = 50;
        Check(!ProgressionService.TryEnhance(s2, "quan_vu", "weapon") && s2.Gold == 50, "enhance fails on empty slot, no deduction");
        s2.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon", Level = 0, RefineIndex = 0 });
        EquipmentService.EquipFromBag(s2, "quan_vu", 0);
        Check(!ProgressionService.TryEnhance(s2, "quan_vu", "weapon") && s2.Gold == 50, "enhance fails with insufficient gold, no deduction");
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

    private void CheckRecruit()
    {
        Check(RecruitService.Rates.Sum(r => r.Weight) == 100, "recruit rates sum to 100");
        var s = Fresh(); s.Gold = 50;
        Check(!RecruitService.TryRecruit(s, new Random(1), out _) && s.Gold == 50, "recruit fails with insufficient gold");
        var s2 = Fresh();
        Check(RecruitService.TryRecruit(s2, new Random(42), out var heroId) && s2.Gold == 100000 - 100 && s2.Shards[heroId] == 5, "recruit costs 100 and grants 5 shards");
        var s3 = Fresh();
        RecruitService.TryRecruit(s3, new Random(42), out var h1);
        var s4 = Fresh();
        RecruitService.TryRecruit(s4, new Random(42), out var h2);
        Check(h1 == h2, "same recruit seed => same hero");
    }

    private void CheckEffectiveBattle()
    {
        var s = Fresh();
        s.Heroes["quan_vu"].Level = 20;
        s.Heroes["quan_vu"].Stars = 3;
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
            s.Heroes["lu_bo"].Level = 12; s.Heroes["lu_bo"].Stars = 4; s.Shards["lu_bo"] = 33;
            s.HeroEquipment["lu_bo"]["weapon"].IsEmpty = false; s.HeroEquipment["lu_bo"]["weapon"].Level = 5; s.HeroEquipment["lu_bo"]["weapon"].RefineIndex = 2;
            s.StarNodesUnlocked = 3; s.Gems = 77;
            SaveManager.Save(path, s);
            var loaded = SaveManager.Load(path);
            ProgressionService.EnsureDefaults(loaded);
            Check(loaded.Heroes["lu_bo"].Level == 12 && loaded.Heroes["lu_bo"].Stars == 4, "save/load preserves hero level/stars");
            Check(loaded.Shards["lu_bo"] == 33 && loaded.Gems == 77 && loaded.StarNodesUnlocked == 3, "save/load preserves shards/gems/star nodes");
            Check(loaded.HeroEquipment["lu_bo"]["weapon"].Level == 5 && loaded.HeroEquipment["lu_bo"]["weapon"].RefineIndex == 2, "save/load preserves equipment");
            var empty = new SaveData();
            ProgressionService.EnsureDefaults(empty);
            Check(empty.Heroes.Count == 7 && empty.HeroEquipment["quan_vu"].Count == ProgressionService.Slots.Count, "EnsureDefaults fills 7 heroes x all equipment slots for old saves");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
