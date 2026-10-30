using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.45 — battle mode context + dungeon logic: campaign battles are
/// unchanged, dungeon battles run through BattleScene on a generated stage,
/// drop set fragments, never touch LastStage/StageStars; Khó needs Thường.
/// </summary>
public partial class TestPhase245 : Node
{
    private int _fails;

    public override void _Ready()
    {
        CheckDatabase();
        CheckGates();
        CheckProgressAndRewards();
        CheckCampaignUnchanged();
        CheckBattleScene();

        if (_fails == 0) GD.Print("TEST PHASE 2.45: ALL PASS");
        else GD.Print($"TEST PHASE 2.45: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private static SaveData Account(string name, int level, int lastStage = 14)
    {
        var s = SeedService.CreateSeededSave(name);
        s.LastStage = lastStage;                  // chapter 2 cleared (ải 14)
        s.Heroes["custom_hero"].Level = level;
        return s;
    }

    private void CheckDatabase()
    {
        var all = DungeonDatabase.All;
        Check(all.Select(d => d.Id).SequenceEqual(new[] { "co_mo", "hung_no", "xich_bich", "xuyen_thuc", "kinh_chau" }), "5 dungeons");
        Check(all.Select(d => d.MinLevel).SequenceEqual(new[] { 30, 50, 70, 90, 100 }), "min levels 30/50/70/90/100");
        Check(all.Select(d => d.BaseScale).SequenceEqual(new[] { 4.1, 5.5, 7.4, 9.7, 12.2 }), "base scales (2.52 balance)");
        Check(all.Select(d => d.FragmentSetId).SequenceEqual(new[] { "chu_tuoc", "bach_ho", "thanh_long", "huyen_vu", "ky_lan" }),
            "each dungeon drops the fragments of the NEXT set");
        Check(all.All(d => d.Stages.Count == 4), "4 stages each");
        Check(all.All(d => d.Stages.Take(3).All(s => s.BossId == null) && d.Stages[3].BossId != null && d.Stages[3].EnemyIds.Contains(d.Stages[3].BossId!)),
            "only the last stage has a boss, and the boss is in it");
        Check(all.All(d => d.Stages.All(s => s.EnemyIds.Count is >= 3 and <= 5 && s.EnemyIds.All(id => { try { HeroDatabase.GetCombatant(id); return true; } catch { return false; } }))),
            "every dungeon enemy resolves, 3–5 per stage");
        Check(all.All(d => d.Stages.All(s => s.EnemyIds.All(id => id != "custom_hero"))), "custom hero is never an enemy");
        var kc = DungeonDatabase.Get("kinh_chau");
        Check(kc.Stages.SelectMany(s => s.EnemyIds).Distinct().Intersect(new[] { "luc_ton", "lu_mong", "quan_vu" }).Count() == 3, "Kinh Châu has Lục Tốn, Lữ Mông, Quan Vũ");
        Check(DungeonDatabase.Get("co_mo").Stages[3].BossId == "truong_giac", "Cổ Mộ final boss = Trương Giác");
        Check(DungeonDatabase.Get("xich_bich").Stages[3].BossId == "tao_thao", "Xích Bích final boss = Tào Tháo");
        // Generated stages.
        var co = DungeonDatabase.Get("co_mo");
        var n0 = DungeonService.StageFor(co, DungeonDifficulty.Normal, 0);
        var n3 = DungeonService.StageFor(co, DungeonDifficulty.Normal, 3);
        var h0 = DungeonService.StageFor(co, DungeonDifficulty.Hard, 0);
        Check(System.Math.Abs(n0.Scale - 4.1) < 1e-9 && System.Math.Abs(n3.Scale - 4.4) < 1e-9, "Scale = base + 0.1 x stage");
        Check(System.Math.Abs(h0.Scale - 4.1 * 1.5) < 1e-3, "Khó = Thường x 1.5");
        Check(n3.BossId == "truong_giac" && n0.BossId == null && n0.Id >= 10000 && n0.Id != h0.Id && n0.Id != n3.Id, "ids are unique and outside the campaign range");
        Check(new BattleEngine(new List<string> { "quan_vu" }, n3, 1).EnemyFighters.Count == 5, "a dungeon stage builds a 5-enemy battle");
    }

    private void CheckGates()
    {
        var co = DungeonDatabase.Get("co_mo");
        var hn = DungeonDatabase.Get("hung_no");
        var normal = DungeonDifficulty.Normal;
        var hard = DungeonDifficulty.Hard;
        var fresh = SeedService.CreateSeededSave("t245a");
        Check(!DungeonService.CanEnter(fresh, co, normal), "new account cannot enter");
        Check(!DungeonService.CanEnter(Account("t245b", 29), co, normal), "chapter 2 but Lv29: locked");
        Check(!DungeonService.CanEnter(Account("t245c", 30, 13), co, normal), "Lv30 but chapter 2 not cleared: locked");
        var s = Account("t245d", 30);
        Check(DungeonService.CanEnter(s, co, normal), "chapter 2 + Lv30: Cổ Mộ Thường opens");
        Check(!DungeonService.CanEnter(s, hn, normal) && DungeonService.LockReason(s, hn, normal) == "Cần cấp chủ tướng Lv50", "Hung Nô needs Lv50");
        Check(!DungeonService.CanEnter(s, co, hard) && DungeonService.LockReason(s, co, hard) == "Cần qua độ khó Thường trước", "Khó locked before Thường is cleared");
        s.DungeonProgress["co_mo:normal:done"] = 1;
        Check(DungeonService.CanEnter(s, co, hard), "Khó opens once Thường was cleared");
        s.Heroes["custom_hero"].Level = 50;
        Check(DungeonService.CanEnter(s, hn, normal), "Lv50 opens Hung Nô");
    }

    private void CheckProgressAndRewards()
    {
        var co = DungeonDatabase.Get("co_mo");
        var s = Account("t245e", 30);
        var team = new[] { "custom_hero" };
        int silver = s.Silver, exp = s.Heroes["custom_hero"].Exp;
        Check(DungeonService.NextStageIndex(s, co, DungeonDifficulty.Normal) == 0, "run starts at stage 1");
        var r = DungeonService.Apply(s, DungeonService.StageKey(co, DungeonDifficulty.Normal, 0), team);
        Check(r != null && r.Silver == 2000 && s.Silver == silver + 2000 && s.Heroes["custom_hero"].Exp == exp + r.ExpPerHero, "win: +2.000 Bạc + EXP");
        Check(ForgeService.FragmentCount(s, "chu_tuoc") == 1 && ForgeService.FragmentCount(s, "bach_ho") == 0, "stage 1: 1 fragment of Chu Tước only");
        Check(DungeonService.NextStageIndex(s, co, DungeonDifficulty.Normal) == 1, "advances to stage 2");
        DungeonService.Apply(s, DungeonService.StageKey(co, DungeonDifficulty.Normal, 1), team);
        DungeonService.Apply(s, DungeonService.StageKey(co, DungeonDifficulty.Normal, 2), team);
        Check(ForgeService.FragmentCount(s, "chu_tuoc") == 3 && DungeonService.NextStageIndex(s, co, DungeonDifficulty.Normal) == 3, "stages 1–3: 1 fragment each");
        Check(!DungeonService.IsCleared(s, co, DungeonDifficulty.Normal), "not cleared before the boss");
        DungeonService.Apply(s, DungeonService.StageKey(co, DungeonDifficulty.Normal, 3), team);
        Check(ForgeService.FragmentCount(s, "chu_tuoc") == 6, "boss stage: 3 fragments");
        Check(DungeonService.NextStageIndex(s, co, DungeonDifficulty.Normal) == 0 && DungeonService.IsCleared(s, co, DungeonDifficulty.Normal),
            "last stage won: run resets to stage 1 and Thường is marked cleared");
        Check(DungeonService.CanEnter(s, co, DungeonDifficulty.Hard), "Khó now enterable");
        // Khó doubles the fragments.
        DungeonService.Apply(s, DungeonService.StageKey(co, DungeonDifficulty.Hard, 0), team);
        DungeonService.Apply(s, DungeonService.StageKey(co, DungeonDifficulty.Hard, 3), team);
        Check(ForgeService.FragmentCount(s, "chu_tuoc") == 6 + 2 + 6, "Khó: fragments x2 (2 on a normal stage, 6 on the boss)");
        Check(DungeonService.NextStageIndex(s, co, DungeonDifficulty.Normal) == 0 && DungeonService.NextStageIndex(s, co, DungeonDifficulty.Hard) == 0,
            "Thường and Khó progress are separate");
        // Each dungeon drops the next set's fragments.
        var kc = DungeonDatabase.Get("kinh_chau");
        DungeonService.Apply(s, DungeonService.StageKey(kc, DungeonDifficulty.Normal, 0), team);
        Check(ForgeService.FragmentCount(s, "ky_lan") == 1, "Kinh Châu drops Kỳ Lân fragments");
        Check(DungeonService.Apply(s, "bad", team) == null && DungeonService.Apply(s, "nope:normal:0", team) == null, "bad keys are ignored");
        // Round-trips through the save file.
        var path = ProjectSettings.GlobalizePath("user://t245.json");
        SaveManager.Save(path, s);
        var loaded = SaveManager.Load(path);
        Check(loaded.DungeonProgress.Count == s.DungeonProgress.Count && loaded.DungeonProgress["co_mo:normal:done"] == 1, "dungeon progress persists");
        System.IO.File.Delete(path);
        // Old saves (no field) get an empty dictionary.
        var old = new SaveData { DungeonProgress = null! };
        ProgressionService.EnsureDefaults(old);
        Check(old.DungeonProgress != null && old.DungeonProgress.Count == 0, "EnsureDefaults creates DungeonProgress for old saves");
    }

    private void CheckCampaignUnchanged()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("t245f");
        gm.StartBattle(3, new List<string> { "custom_hero" }, 11);
        Check(gm.CurrentMode == BattleMode.Campaign && gm.CurrentStage == null && gm.CurrentModeKey == "", "StartBattle = Campaign");
        var reward = gm.ApplyBattleResult(true, 3);
        Check(reward != null && gm.Save.LastStage == 3 && gm.Save.StageStars["3"] == 3 && reward.Silver == 1000, "campaign result: LastStage, stars, 1.000 Bạc as before");
        Check(gm.Save.LastBattle is { Mode: "Campaign", StageId: 3 }, "campaign battle record has Mode = Campaign");
    }

    private bool RunToEnd(BattleScene scene)
    {
        for (int i = 0; i < 800; i++)
        {
            scene.StepBattle();
            if (scene.GetNodeOrNull<Control>("ResultPopup") != null) return true;
        }
        return false;
    }

    private void CheckBattleScene()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("t245g");
        gm.Save.LastStage = 14;
        gm.Save.OwnedHeroIds.AddRange(new[] { "quan_vu", "truong_phi", "trieu_van", "gia_cat_luong", "lu_bo" });
        foreach (var id in gm.Save.OwnedHeroIds) gm.Save.Heroes[id].Level = 120;
        FormationService.EnsureFormation(gm.Save);
        FormationService.TryPlace(gm.Save, "quan_vu", "hau_top", null, out _);
        FormationService.TryPlace(gm.Save, "trieu_van", "tien_mid", null, out _);
        FormationService.TryPlace(gm.Save, "gia_cat_luong", "hau_mid", null, out _);
        FormationService.TryPlace(gm.Save, "lu_bo", "trung_top", null, out _);
        var team = FormationService.FormationToTeam(gm.Save.Formation);
        var co = DungeonDatabase.Get("co_mo");
        var stage = DungeonService.StageFor(co, DungeonDifficulty.Normal, 0);
        int lastStage = gm.Save.LastStage;
        var starsBefore = new Dictionary<string, int>(gm.Save.StageStars);

        gm.StartModeBattle(BattleMode.Dungeon, DungeonService.StageKey(co, DungeonDifficulty.Normal, 0), stage, new List<string>(team), 4242);
        var scene = ResourceLoader.Load<PackedScene>("res://features/battle/scenes/BattleScene.tscn").Instantiate<BattleScene>();
        AddChild(scene);
        Check(scene.GetNode<GridContainer>("EnemyGrid").GetChildren().OfType<BattleHeroCell>().Count() == 3, "dungeon stage 1 shows its 3 enemies");
        Check(RunToEnd(scene), "dungeon battle reaches the result popup");
        Check(gm.Save.LastStage == lastStage && gm.Save.StageStars.Count == starsBefore.Count, "dungeon battle never touches LastStage / StageStars");
        Check(gm.Save.LastBattle is { Mode: "Dungeon" } rec && rec.ModeKey == "co_mo:normal:0" && rec.StageId == stage.Id, "battle record keeps Mode + ModeKey + stage id");
        if (gm.LastBattleWon)
        {
            Check(ForgeService.FragmentCount(gm.Save, "chu_tuoc") == 1 && gm.Save.Silver >= SeedService.SeededSilver + 2000, "won dungeon battle: +1 Mảnh Chu Tước, +2.000 Bạc");
            Check(scene.FindChild("RewardSquares", true, false) is HBoxContainer sq
                && sq.GetChildren().Any(c => ((Node)c).FindChild("DropLabel", true, false) is Label l && l.Text == "Mảnh Chu Tước"), "popup shows the fragment drop");
        }
        else
        {
            Check(ForgeService.FragmentCount(gm.Save, "chu_tuoc") == 0 && DungeonService.NextStageIndex(gm.Save, co, DungeonDifficulty.Normal) == 0, "lost dungeon battle: no reward, stage unchanged");
        }
        scene.QueueFree();

        // A forced win/loss through ApplyBattleResult (deterministic).
        gm.StartModeBattle(BattleMode.Dungeon, DungeonService.StageKey(co, DungeonDifficulty.Normal, 0), stage, new List<string>(team), 1);
        int frags = ForgeService.FragmentCount(gm.Save, "chu_tuoc"), silver = gm.Save.Silver;
        Check(gm.ApplyBattleResult(false, 0) == null && gm.Save.Silver == silver && ForgeService.FragmentCount(gm.Save, "chu_tuoc") == frags, "loss: nothing granted");
        var reward = gm.ApplyBattleResult(true, 3);
        Check(reward != null && reward.Drops.Count == 1 && reward.Drops[0].Kind == StageRewardService.MaterialKind && reward.Drops[0].Count == 1
            && ForgeService.FragmentCount(gm.Save, "chu_tuoc") == frags + 1 && gm.Save.Silver == silver + 2000, "win: 1 fragment + 2.000 Bạc");
        Check(gm.Save.LastStage == lastStage && gm.LastBattleReward == reward, "win leaves the campaign untouched and stores LastBattleReward");
        var saved = SaveManager.Load(gm.SavePath);
        Check(saved.DungeonProgress.Count > 0 && saved.LastBattle?.Mode == "Dungeon", "dungeon result is saved to disk");
    }
}
