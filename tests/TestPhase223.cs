using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.23 tests: tavern (đối tửu + chiêu mộ), MaxLevel 180,
/// hero stars removed, quality factors.
/// - TavernData: tiers, recruit costs, drink prices, draw weights.
/// - Player level = custom hero level; Cam enters the draw pool at 70.
/// - StartDrink: grade prices, insufficient gold charges nothing, 3 heroes.
/// - DrawTier matches the weight tables exactly (replica Random).
/// - PlayRps matches a replica exactly over 100 seeds (moves, outcomes,
///   souls); win/draw/lose all occur; win souls always 1-3 (2.24).
/// - Phase 2.24: souls pooled per tier (TierSouls); per-general souls
///   from 2.23 are discarded, not migrated.
/// - Recruit: thresholds, exact deduction, owned/custom blocks.
/// - Quality scaling: exact Cam/Lam scaled values at level 11.
/// - Old saves with per-general souls load with tier pools at 0 (discarded).
/// - Tavern popup nodes + recruit button enablement; hero screen has
///   no hero-star display.
/// </summary>
public partial class TestPhase223 : Node
{
    private int _passed;
    private int _failed;

    private void Check(bool cond, string name)
    {
        if (cond) { _passed++; GD.Print($"[PASS] {name}"); }
        else { _failed++; GD.PrintErr($"[FAIL] {name}"); }
    }

    public override void _Ready()
    {
        // --- TavernData tables ---
        Check(TavernData.TierOf("luu_bi") == TavernTier.Lam && TavernData.TierOf("truong_phi") == TavernTier.Lam, "Lam tier: Luu Bi, Truong Phi");
        Check(TavernData.TierOf("quan_vu") == TavernTier.Tim && TavernData.TierOf("trieu_van") == TavernTier.Tim, "Tim tier: Quan Vu, Trieu Van");
        Check(TavernData.TierOf("dieu_thuyen") == TavernTier.Cam && TavernData.TierOf("gia_cat_luong") == TavernTier.Cam && TavernData.TierOf("lu_bo") == TavernTier.Cam, "Cam tier: Dieu Thuyen, Gia Cat Luong, Lu Bo");
        Check(TavernData.TierOf("custom_hero") == null, "custom hero has no tier");
        Check(TavernData.RecruitCost(TavernTier.Lam) == 100 && TavernData.RecruitCost(TavernTier.Tim) == 500 && TavernData.RecruitCost(TavernTier.Cam) == 1500, "recruit costs 100/500/1500");
        Check(TavernData.DrinkPrice(TavernGrade.Normal) == 150000 && TavernData.DrinkPrice(TavernGrade.High) == 300000, "drink prices 150000/300000 silver (2.25)");
        Check(TavernData.StatFactor(TavernTier.Lam) < TavernData.StatFactor(TavernTier.Tim) && TavernData.StatFactor(TavernTier.Tim) < TavernData.StatFactor(TavernTier.Cam) && TavernData.StatFactor(TavernTier.Cam) < TavernData.StatFactor(TavernTier.Do), "quality factors increase with tier");
        Check(TavernData.PoolHeroIds.Count == 7 && !TavernData.PoolHeroIds.Contains("custom_hero"), "tavern pool is 7 heroes, no custom hero");

        // --- Player level = custom hero level; MaxLevel 180 ---
        var s = SeedService.CreateSeededSave("tester223");
        Check(TavernService.PlayerLevel(s) == 1, "player level starts at custom hero level 1");
        s.Heroes["custom_hero"].Level = 70;
        Check(TavernService.PlayerLevel(s) == 70, "player level tracks custom hero level");
        var sMax = SeedService.CreateSeededSave("tester223");
        sMax.Heroes["custom_hero"].Level = 180;
        ProgressionService.AddExp(sMax, "custom_hero", 99999);
        Check(sMax.Heroes["custom_hero"].Level == 180, "level capped at 180");
        Check(ProgressionService.MaxLevel == 180, "MaxLevel is 180");

        // --- StartDrink: prices, insufficient gold, exactly 3 heroes ---
        var s2 = SeedService.CreateSeededSave("tester223");
        s2.Silver = 100000;
        Check(!TavernService.StartDrink(s2, TavernGrade.Normal, new Random(1), out _) && s2.Silver == 100000, "normal drink fails at 100000 silver, no charge");
        s2.Silver = 200000;
        Check(!TavernService.StartDrink(s2, TavernGrade.High, new Random(1), out _) && s2.Silver == 200000, "high drink fails at 200000 silver, no charge");
        s2.Silver = 1000000;
        Check(TavernService.StartDrink(s2, TavernGrade.Normal, new Random(2), out var heroes) && s2.Silver == 850000 && heroes.Count == 3, "normal drink charges 150000 silver and draws 3 heroes");
        Check(heroes.All(h => TavernData.PoolHeroIds.Contains(h)), "drawn heroes are all from the pool");
        Check(TavernService.StartDrink(s2, TavernGrade.High, new Random(3), out var heroes2) && s2.Silver == 550000 && heroes2.Count == 3, "high drink charges 300000 silver and draws 3 heroes");
        Check(s2.Gold == SeedService.SeededGold, "drinks do not touch gold (2.25: drinks cost silver)");

        // --- DrawTier mapping is exact for fixed seeds (replica Random) ---
        var sLow = SeedService.CreateSeededSave("tester223"); // custom level 1 < 70
        var rng = new Random(12345);
        var replica = new Random(12345);
        bool mappingOk = true;
        for (int i = 0; i < 200; i++)
        {
            int roll = replica.Next(95); // Normal 70/25, Cam 0 below 70
            var expected = roll < 70 ? TavernTier.Lam : TavernTier.Tim;
            if (TavernService.DrawTier(sLow, TavernGrade.Normal, rng) != expected) { mappingOk = false; break; }
        }
        Check(mappingOk, "DrawTier Normal below 70 matches 70/25 weights exactly (200 draws)");
        var rngH = new Random(777);
        var replicaH = new Random(777);
        bool mappingHOk = true;
        for (int i = 0; i < 200; i++)
        {
            int roll = replicaH.Next(90); // High 60/30, Cam 0 below 70
            var expected = roll < 60 ? TavernTier.Lam : TavernTier.Tim;
            if (TavernService.DrawTier(sLow, TavernGrade.High, rngH) != expected) { mappingHOk = false; break; }
        }
        Check(mappingHOk, "DrawTier High below 70 matches 60/30 weights exactly (200 draws)");

        // --- Cam gate: zero Cam below 70, ~10% Cam at 70 on High ---
        bool anyCamLow = false;
        var rngLow = new Random(99);
        for (int i = 0; i < 2000; i++)
            if (TavernService.DrawTier(sLow, TavernGrade.High, rngLow) == TavernTier.Cam) { anyCamLow = true; break; }
        Check(!anyCamLow, "no Cam tier drawn below level 70 (2000 draws)");
        var s70 = SeedService.CreateSeededSave("tester223");
        s70.Heroes["custom_hero"].Level = 70;
        int camCount = 0;
        var rng70 = new Random(99);
        for (int i = 0; i < 2000; i++)
            if (TavernService.DrawTier(s70, TavernGrade.High, rng70) == TavernTier.Cam) camCount++;
        Check(camCount > 100 && camCount < 300, $"High Cam rate near 10% at level 70 (got {camCount}/2000)");

        // --- PlayRps: exact replica across seeds ---
        bool rpsOk = true;
        for (int seed = 0; seed < 100; seed++)
        {
            var sR = SeedService.CreateSeededSave("tester223");
            var r = new Random(seed);
            var rep = new Random(seed);
            int genMove = rep.Next(3);
            int playerMove = seed % 3;
            var expectedOutcome = playerMove == genMove ? RpsOutcome.Draw
                : (playerMove, genMove) is (0, 1) or (1, 2) or (2, 0) ? RpsOutcome.Win : RpsOutcome.Lose;
            int expectedSouls = expectedOutcome == RpsOutcome.Win ? rep.Next(1, 4) : 0;
            int before = TavernService.TierSoulsOf(sR, TavernTier.Lam);
            var result = TavernService.PlayRps(sR, "luu_bi", playerMove, r);
            if (result.GeneralMove != genMove || result.Outcome != expectedOutcome || result.SoulsGained != expectedSouls
                || TavernService.TierSoulsOf(sR, TavernTier.Lam) != before + expectedSouls) { rpsOk = false; break; }
        }
        Check(rpsOk, "PlayRps matches replica exactly over 100 seeds (moves, outcomes, souls)");
        var outcomes = new HashSet<RpsOutcome>();
        for (int seed = 0; seed < 100; seed++)
        {
            var sR = SeedService.CreateSeededSave("tester223");
            outcomes.Add(TavernService.PlayRps(sR, "luu_bi", seed % 3, new Random(seed)).Outcome);
        }
        Check(outcomes.Count == 3, "win, draw and lose all occur across seeds");
        bool rangeOk = true;
        for (int seed = 0; seed < 500; seed++)
        {
            var sR = SeedService.CreateSeededSave("tester223");
            var res = TavernService.PlayRps(sR, "luu_bi", seed % 3, new Random(seed));
            if (res.Outcome == RpsOutcome.Win && (res.SoulsGained < 1 || res.SoulsGained > 3)) { rangeOk = false; break; }
        }
        Check(rangeOk, "win soul roll always within 1-3 (2.24: was 1-5)");

        // --- Recruit: thresholds, exact deduction, blocks ---
        var s3 = SeedService.CreateSeededSave("tester223");
        Check(!TavernService.CanRecruit(s3, "luu_bi"), "cannot recruit Luu Bi with 0 souls");
        s3.TierSouls["Lam"] = 99;
        Check(!TavernService.Recruit(s3, "luu_bi") && !s3.OwnedHeroIds.Contains("luu_bi"), "99 Lam souls cannot recruit Lam (needs 100)");
        s3.TierSouls["Lam"] = 100;
        Check(TavernService.CanRecruit(s3, "luu_bi"), "100 Lam souls can recruit Lam");
        Check(TavernService.Recruit(s3, "luu_bi") && s3.OwnedHeroIds.Contains("luu_bi") && s3.TierSouls["Lam"] == 0, "recruit deducts exactly 100 Lam souls and unlocks");
        Check(!TavernService.CanRecruit(s3, "luu_bi"), "owned hero cannot be recruited again");
        var s4 = SeedService.CreateSeededSave("tester223");
        s4.TierSouls["Cam"] = 1499;
        Check(!TavernService.Recruit(s4, "lu_bo"), "1499 Cam souls cannot recruit Cam (needs 1500)");
        s4.TierSouls["Cam"] = 1500;
        Check(TavernService.Recruit(s4, "lu_bo") && s4.OwnedHeroIds.Contains("lu_bo"), "1500 Cam souls recruit Cam");
        Check(!TavernService.CanRecruit(s4, "custom_hero"), "custom hero is never recruitable");

        // --- Quality scaling (exact) ---
        var scaledCam = ProgressionService.GetScaledBase("lu_bo", 11);
        // Cam: stat 1.35, growth 1.35 => 1.35 * (1 + 0.04*1.35*10) = 1.35*1.54 = 2.079
        Check(scaledCam.Hp == (int)Math.Round(600 * 2.079) && scaledCam.Atk == (int)Math.Round(105 * 2.079), "Cam scaled base uses 1.35 stat x 1.35 growth");
        var scaledLam = ProgressionService.GetScaledBase("luu_bi", 11);
        Check(scaledLam.Hp == (int)Math.Round(620 * 1.4), "Lam scaled base uses 1.0 factors (1.4 at level 11)");
        Check(scaledCam.Atk > scaledLam.Atk, "Cam out-damages Lam at the same level");

        // --- Old save with per-general souls (2.23 format) loads, souls discarded ---
        var path = Path.Combine(OS.GetUserDataDir(), "test_phase223_legacy.json");
        try
        {
            File.WriteAllText(path, "{\"Gold\": 4321, \"GeneralSouls\": {\"luu_bi\": 999}, \"OwnedHeroIds\": [\"custom_hero\"], \"TeamIds\": [\"custom_hero\"]}");
            var loaded = SaveManager.Load(path);
            ProgressionService.EnsureDefaults(loaded);
            Check(loaded.Gold == 4321 && loaded.TierSouls["Lam"] == 0 && loaded.TierSouls["Tim"] == 0 && loaded.TierSouls["Cam"] == 0,
                "legacy save with GeneralSouls loads with tier pools at 0 (old souls discarded)");
        }
        finally { if (File.Exists(path)) File.Delete(path); }

        // --- Tavern popup nodes + recruit button enablement ---
        var tavernScene = ResourceLoader.Load<PackedScene>("res://features/recruit/scenes/RecruitScene.tscn");
        Check(tavernScene != null, "tavern scene loads");
        if (tavernScene != null)
        {
            var gm = GetNode<GameManager>("/root/GameManager");
            gm.CreateSeededSave("tester223");
            gm.Save.TierSouls["Lam"] = 100;
            var tav = tavernScene.Instantiate<RecruitScene>();
            AddChild(tav);
            Check(tav.GetNode<Button>("NormalButton").Text.Contains("150.000"), "tavern normal button shows 150.000 silver (2.25)");
            Check(tav.GetNode<Button>("HighButton").Text.Contains("300.000"), "tavern high button shows 300.000 silver (2.25)");
            Check(tav.GetNode<Button>("RockButton").Disabled, "RPS disabled before any drink");
            var recruitBtns = tav.GetNode<Control>("RecruitBox").GetChildren().OfType<Button>().ToList();
            var luuBtn = recruitBtns.FirstOrDefault(b => b.GetChildren().OfType<Label>().Any(l => l.Name == "NameLabel" && l.Text.Contains("Lưu Bị")));
            Check(luuBtn != null && !luuBtn.Disabled, "recruit button enabled for Luu Bi at 100 souls");
            tav.QueueFree();
        }

        // --- Hero screen shows no hero stars ---
        var heroScene = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroScreen.tscn");
        Check(heroScene != null, "hero screen scene loads");
        if (heroScene != null)
        {
            var hero = heroScene.Instantiate<HeroScreen>();
            AddChild(hero);
            Check(!hero.GetNode<Label>("DetailLabel").Text.Contains("Sao"), "hero detail shows no hero stars");
            hero.QueueFree();
        }

        GD.Print(_failed == 0 ? "TEST PHASE2.23: ALL PASS" : $"TEST PHASE2.23: {_failed} FAILURES");
        GetTree().Quit(_failed == 0 ? 0 : 1);
    }
}
