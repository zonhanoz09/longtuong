using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.50 — multi-team boss engine support (maxTurns, enemyStartHp,
/// EnemyHpMultiplier), PowerService, PartyService (bots, boss order,
/// simulation, rewards).
/// </summary>
public partial class TestPhase250 : Node
{
    private int _fails;

    public override void _Ready()
    {
        CheckEngine();
        CheckPowerAndBots();
        CheckBossesAndSimulation();

        if (_fails == 0) GD.Print("TEST PHASE 2.50: ALL PASS");
        else GD.Print($"TEST PHASE 2.50: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private static List<HeroDef> Team() => new[] { "quan_vu", "truong_phi", "gia_cat_luong" }.Select(HeroDatabase.Get).ToList();

    private void CheckEngine()
    {
        var stage = HeroDatabase.GetStage(5);
        // Defaults keep the old behaviour exactly (same seed => same battle as the 2-arg overloads).
        var a = new BattleEngine(Team(), null, stage, 321);
        var b = new BattleEngine(Team(), stage, 321);
        a.AutoRun(); b.AutoRun();
        Check(a.Log.SequenceEqual(b.Log) && a.TurnCount == b.TurnCount && a.PlayerWon == b.PlayerWon && a.MaxTurns == 0, "no new arguments => identical battle");
        var c = new BattleEngine(Team(), null, stage, 321, 0, null);
        c.AutoRun();
        Check(c.Log.SequenceEqual(a.Log), "explicit defaults => identical battle");

        var capped = new BattleEngine(Team(), null, stage, 321, maxTurns: 5);
        capped.AutoRun();
        Check(capped.IsOver && capped.TurnCount == 5 && !capped.PlayerWon, "maxTurns stops the battle at 5 turns, not a win");
        Check(capped.EnemyFighters.Any(f => f.Alive) && capped.PlayerFighters.Any(f => f.Alive), "both sides still standing at the cap");

        var hp = new BattleEngine(Team(), null, stage, 1, 0, new[] { 50, 999999 });
        Check(hp.EnemyFighters[0].Hp == 50 && hp.EnemyFighters[1].Hp == hp.EnemyFighters[1].MaxHp && hp.EnemyFighters[2].Hp == hp.EnemyFighters[2].MaxHp,
            "enemyStartHp sets HP, clamps to max, missing entries stay full");
        var dead = new BattleEngine(Team(), null, new StageDef { Id = 1, Name = "x", EnemyIds = new() { "lu_bo" }, Scale = 1 }, 1, 0, new[] { 0 });
        Check(dead.IsOver && dead.PlayerWon, "an enemy starting at 0 HP counts as already beaten");

        var big = new StageDef { Id = 1, Name = "x", EnemyIds = new() { "truong_giac" }, Scale = 2.0, EnemyHpMultiplier = 8 };
        var e = new BattleEngine(Team(), big, 1);
        Check(e.EnemyFighters[0].MaxHp == (int)System.Math.Round(System.Math.Round(EnemyDatabase.Find("truong_giac")!.Hp * 2.0) * 8)
            && e.EnemyFighters[0].Def.Atk == (int)System.Math.Round(EnemyDatabase.Find("truong_giac")!.Atk * 2.0), "EnemyHpMultiplier scales HP only");
        Check(EnemyDatabase.Find("truong_giac")!.Hp == 640, "the shared enemy def is never mutated");
    }

    private static GameManager Setup(Node node, string name)
    {
        var gm = node.GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave(name);
        gm.Save.LastStage = 38;
        gm.Save.OwnedHeroIds.AddRange(new[] { "quan_vu", "trieu_van", "gia_cat_luong", "lu_bo" });
        foreach (var id in gm.Save.OwnedHeroIds) gm.Save.Heroes[id].Level = 40;
        FormationService.EnsureFormation(gm.Save);
        FormationService.TryPlace(gm.Save, "quan_vu", "hau_top", null, out _);
        FormationService.TryPlace(gm.Save, "trieu_van", "tien_mid", null, out _);
        FormationService.TryPlace(gm.Save, "gia_cat_luong", "hau_mid", null, out _);
        FormationService.TryPlace(gm.Save, "lu_bo", "trung_top", null, out _);
        return gm;
    }

    private void CheckPowerAndBots()
    {
        var h = HeroDatabase.Get("quan_vu");
        double expected = (h.Hp * 0.2 + h.Atk + h.Def + h.MagicDef + h.Speed * 0.5) * (1 + h.Crit + h.Dodge);
        Check(System.Math.Abs(PowerService.HeroPower(h) - expected) < 1e-9, "HeroPower formula");
        Check(PowerService.HeroPower(h.WithScale(2)) > PowerService.HeroPower(h), "stronger hero => more power");

        var gm = Setup(this, "t250a");
        var player = PartyService.PlayerTeam(gm.Save);
        Check(player.Heroes.Count == 5 && !player.IsBot, "player team = 5-hero formation");
        double pp = PowerService.TeamPower(player.Heroes);
        bool allInRange = true, noCustom = true, five = true;
        for (int seed = 0; seed < 50; seed++)
            foreach (var idx in new[] { 1, 2 })
            {
                var bot = PartyService.BuildBotTeam(gm.Save, idx, seed);
                double ratio = PowerService.TeamPower(bot.Heroes) / pp;
                if (ratio < 0.90 || ratio > 1.10) allInRange = false;
                if (bot.Heroes.Any(x => x.Id == "custom_hero")) noCustom = false;
                if (bot.Heroes.Count != 5 || bot.Heroes.Select(x => x.Id).Distinct().Count() != 5 || bot.Positions.Count != 5) five = false;
            }
        Check(allInRange, "bot power within 90–110% of the player's team (100 bots)");
        Check(noCustom, "bots never use the custom hero");
        Check(five, "bots are 5 distinct heroes with positions");
        var b1 = PartyService.BuildBotTeam(gm.Save, 1, 9);
        var b1b = PartyService.BuildBotTeam(gm.Save, 1, 9);
        var b2 = PartyService.BuildBotTeam(gm.Save, 2, 9);
        Check(b1.Heroes.Select(x => x.Id + x.Hp).SequenceEqual(b1b.Heroes.Select(x => x.Id + x.Hp)), "same seed => same bot team");
        Check(b1.IsBot && b1.Label == "Đội 2/3 (Bot)" && b2.Label == "Đội 3/3 (Bot)" && player.Label == "Đội 1/3 (Bạn)", "team labels");
    }

    private void CheckBossesAndSimulation()
    {
        Check(PartyService.Bosses.Select(b => b.Id).SequenceEqual(new[] { "truong_giac", "lu_bo", "vien_thieu", "tao_thao" }), "4 bosses in order");
        var gm = Setup(this, "t250b");
        var s = gm.Save;
        Check(PartyService.IsBossOpen(s, "truong_giac") && !PartyService.IsBossOpen(s, "lu_bo"), "only the first boss is open");
        Check(PartyService.LockReason(s, "lu_bo") == "Cần đánh bại Trương Giác trước", "lock reason names the previous boss");
        var fresh = SeedService.CreateSeededSave("t250c");
        Check(!PartyService.IsBossOpen(fresh, "truong_giac") && PartyService.LockReason(fresh, "truong_giac") == "Qua chương 5 và đạt Lv40", "feature gate applies");

        var stage = PartyService.BossStage("truong_giac");
        Check(stage.EnemyIds.SequenceEqual(new[] { "truong_giac" }) && stage.EnemyHpMultiplier == 8, "boss stage: 1 boss, HP x8");

        var r1 = PartyService.Simulate(s, "truong_giac", 2024);
        var r2 = PartyService.Simulate(s, "truong_giac", 2024);
        Check(r1.Runs.Count >= 1 && r1.Runs.Count <= 3, "1–3 team runs");
        Check(r1.Runs.Select(r => r.BossHpAfter).SequenceEqual(r2.Runs.Select(r => r.BossHpAfter)) && r1.Won == r2.Won, "same seed => same 3-team result");
        Check(r1.Runs[0].BossHpBefore == r1.BossMaxHp, "team 1 starts against a full-HP boss");
        bool carried = Enumerable.Range(1, r1.Runs.Count - 1).All(i => r1.Runs[i].BossHpBefore == r1.Runs[i - 1].BossHpAfter);
        Check(carried, "boss HP carries over between teams");
        Check(r1.Runs.All(r => r.Turns <= PartyService.MaxTurnsPerTeam), "each team gets at most 30 turns");
        Check(r1.Won == (r1.Runs[^1].BossHpAfter == 0) && (r1.Won || r1.Runs.Count == 3), "won iff the boss reached 0 HP; a loss uses all 3 teams");
        GD.Print($"demo Trương Giác: {string.Join(" | ", r1.Runs.Select(r => $"{r.Label}: {r.BossHpBefore}->{r.BossHpAfter} ({r.Turns} lượt)"))} won={r1.Won}");

        int silver = s.Silver, merit = s.Merit, exp = s.Heroes["quan_vu"].Exp;
        var reward = PartyService.Apply(s, "truong_giac", new[] { "quan_vu" });
        Check(reward != null && s.Silver == silver + 5000 && s.Merit == merit + 100 && ForgeService.FragmentCount(s, "chu_tuoc") == 5,
            "win: 5.000 Bạc + 100 Chiến tích + 5 Mảnh Chu Tước");
        Check(s.Heroes["quan_vu"].Exp > exp || s.Heroes["quan_vu"].Level > 40, "EXP for the player's team");
        Check(PartyService.IsBossOpen(s, "lu_bo") && !PartyService.IsBossOpen(s, "vien_thieu"), "beating boss 1 opens boss 2 only");
        Check(PartyService.Apply(s, "truong_giac", new[] { "quan_vu" }) != null && s.PartyCleared["truong_giac"] == 2, "bosses can be fought again (no daily limit)");
        Check(PartyService.Apply(s, "nope", new string[0]) == null, "unknown boss ignored");
        Check(PartyService.GetBoss("lu_bo").FragmentSetId == "bach_ho" && PartyService.GetBoss("tao_thao").FragmentSetId == "huyen_vu", "fragment set per boss tier");
        var old = new SaveData { PartyCleared = null! };
        ProgressionService.EnsureDefaults(old);
        Check(old.PartyCleared != null, "EnsureDefaults creates PartyCleared for old saves");
    }
}
