using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.39 — vòng chơi ải: Bạc reward, 10% drops, chapter 2 (ải 11–20),
/// flatter EXP curve, Vật/Sách damage split, enemy formation positions,
/// Quét ải (3 sao, daily cap) and the saved battle record (seed).
/// </summary>
public partial class TestPhase239 : Node
{
    private int _fails;

    public override void _Ready()
    {
        CheckExpCurve();
        CheckChapters();
        CheckEnemyPositions();
        CheckStrategySplit();
        CheckRewards();
        CheckBattleResult();
        CheckSweep();
        CheckStageSelectUi();
        CheckBattlePopupDrops();

        if (_fails == 0) GD.Print("TEST PHASE 2.39: ALL PASS");
        else GD.Print($"TEST PHASE 2.39: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private void CheckExpCurve()
    {
        Check(ProgressionService.ExpToNext(1) == 100 && ProgressionService.ExpToNext(50) == 1080, "exp curve 100 + 20*(lv-1)");
        int total = Enumerable.Range(1, 49).Sum(ProgressionService.ExpToNext);
        Check(total == 28420, $"Lv1 -> Lv50 needs 28420 EXP (got {total})");
        Check(StageRewardService.ExpPerHero(20) == 240 && StageRewardService.ExpPerHero(10) == 140, "EXP per win 40 + 10*stage");
    }

    private void CheckChapters()
    {
        Check(HeroDatabase.ChapterCount == 2, "2 chapters");
        Check(HeroDatabase.GetStage(10).Chapter == 1 && HeroDatabase.GetStage(11).Chapter == 2, "stage 11 opens chapter 2");
        Check(Enumerable.Range(11, 5).All(id => HeroDatabase.GetStage(id).EnemyIds.Count == 4), "stages 11-15 have 4 enemies");
        Check(Enumerable.Range(16, 5).All(id => HeroDatabase.GetStage(id).EnemyIds.Count == 5), "stages 16-20 have 5 enemies");
        Check(HeroDatabase.GetStage(20).Scale > HeroDatabase.GetStage(10).Scale, "chapter 2 is harder");
        Check(HeroDatabase.Stages.All(s => s.EnemyIds.All(id => id != "custom_hero")), "custom hero never an enemy");
    }

    private void CheckEnemyPositions()
    {
        var e = new BattleEngine(new List<string> { "quan_vu" }, HeroDatabase.GetStage(16), 1);
        var pos = e.EnemyFighters.Select(f => f.Position).ToList();
        Check(pos.SequenceEqual(new[] { FormationPosition.Front, FormationPosition.Middle, FormationPosition.Middle,
            FormationPosition.Rear, FormationPosition.Middle }), "5 enemies on Tiền/Trung/Trung/Hậu/Trung");
        var front = e.EnemyFighters[0];
        Check(front.EffectiveDef == (int)System.Math.Round(front.Def.Def * 1.2)
            && front.EffectiveMagicDef == (int)System.Math.Round(front.Def.MagicDef * 1.2), "enemy Tiền gets +20% Vật/Sách phòng");
        var mid = e.EnemyFighters[1];
        Check(mid.EffectiveAtk == (int)System.Math.Round(mid.Def.Atk * 1.2), "enemy Trung gets +20% Công");

        // Player single-target attacks hit the enemy Tiền first, even when
        // another enemy has lower HP.
        var e2 = new BattleEngine(new List<string> { "quan_vu" }, HeroDatabase.GetStage(1), 99);
        e2.EnemyFighters[1].Hp = 1;
        string tienName = e2.EnemyFighters[0].Name;
        string firstPlayerHit = "";
        for (int i = 0; i < 30 && firstPlayerHit == "" && !e2.IsOver; i++)
        {
            var cur = e2.Current;
            if (cur == null) break;
            int before = e2.Log.Count;
            e2.ActCurrentNormal();
            if (cur.IsPlayer) firstPlayerHit = string.Join(" ", e2.Log.Skip(before));
        }
        Check(firstPlayerHit.Contains(tienName), $"player hits enemy Tiền ({tienName}) first: {firstPlayerHit}");

        // Enemy Hậu can act first (20% per round, like the player's Hậu).
        int rearFirst = 0;
        for (int seed = 0; seed < 300; seed++)
        {
            var e3 = new BattleEngine(new List<string> { "truong_phi" }, HeroDatabase.GetStage(11), seed);
            if (e3.Current != null && !e3.Current.IsPlayer && e3.Current.Position == FormationPosition.Rear) rearFirst++;
        }
        Check(rearFirst > 300 * 0.10 && rearFirst < 300 * 0.35, $"enemy Hậu first-strike near 20% (got {rearFirst}/300)");
    }

    private static Fighter F(HeroRole role, int atk, int def, int mdef) =>
        new(new HeroDef { Id = "t", Name = "t", Role = role, Hp = 1000, Atk = atk, Def = def, MagicDef = mdef }, true);

    private void CheckStrategySplit()
    {
        var target = F(HeroRole.Vanguard, 10, 30, 60);
        Check(BattleEngine.CalculateDamage(F(HeroRole.Vanguard, 100, 0, 0), target, 1, false, false, 1) == 70, "Vật attack uses Vật phòng (100-30)");
        Check(BattleEngine.CalculateDamage(F(HeroRole.Strategist, 100, 0, 0), target, 1, false, false, 1) == 40, "Mưu attack uses Sách phòng (100-60)");
        Check(BattleEngine.CalculateDamage(F(HeroRole.Support, 100, 0, 0), target, 1, false, false, 1) == 40, "Phụ attack uses Sách phòng");
        Check(BattleEngine.CalculateDamage(F(HeroRole.Warrior, 100, 0, 0), target, 1, false, false, 1) == 70, "Võ attack uses Vật phòng");
        Check(HeroDatabase.Heroes.All(h => h.MagicDef > 0), "every hero has Sách phòng");
        Check(HeroDatabase.Get("gia_cat_luong").WithScale(2.0).MagicDef == 90, "stage scale multiplies Sách phòng");
        var lv11 = ProgressionService.GetScaledBase("custom_hero", 11);
        Check(lv11.MagicDef == (int)System.Math.Round(25 * 1.4), "Sách phòng grows with level like Vật phòng");

        var s = SeedService.CreateSeededSave("t239a");
        var before = ProgressionService.GetEffectiveHero(s, "custom_hero");
        s.HeroEquipment["custom_hero"]["helmet"] = new EquipmentState
        {
            IsEmpty = false, Level = 2, SetId = "tan_thu",
            RefineLines = new List<RefineLine> { new() { StatId = "hp", Value = 1 } },
        };
        s.HeroEquipment["custom_hero"]["boots"] = new EquipmentState
        {
            IsEmpty = false, Level = 2, SetId = "tan_thu",
            RefineLines = new List<RefineLine> { new() { StatId = "hp", Value = 1 } },
        };
        var after = ProgressionService.GetEffectiveHero(s, "custom_hero");
        Check(after.MagicDef - before.MagicDef == 10, "Mũ +2 gives Sách phòng 8+2; Giày gives none");
        var proj = EquipmentService.ProjectEquipChange(s, "custom_hero", "cloak",
            new EquipmentState { IsEmpty = false, Level = 0, SetId = "tan_thu" });
        Check(proj.Any(r => r.Name == "Sách phòng" && r.After - r.Before == 6), "projection shows Choàng Sách phòng +6");
    }

    private void CheckRewards()
    {
        var a = StageRewardService.Roll(5, StageRewardService.DropRng(1234));
        var b = StageRewardService.Roll(5, StageRewardService.DropRng(1234));
        Check(a.Silver == 1000 && a.ExpPerHero == 90, "reward: 1000 silver + stage EXP");
        Check(a.Drops.Select(d => d.ItemId).SequenceEqual(b.Drops.Select(d => d.ItemId)), "same seed => same drops");
        int ore = 0, stone = 0, gem = 0, n = 3000;
        var rng = new System.Random(42);
        bool gemsLv1 = true;
        for (int i = 0; i < n; i++)
        {
            var r = StageRewardService.Roll(1, rng);
            ore += r.Drops.Count(d => d.Kind == StageRewardService.OreKind);
            stone += r.Drops.Count(d => d.Kind == StageRewardService.StoneKind);
            foreach (var d in r.Drops.Where(d => d.Kind == StageRewardService.GemKind))
            {
                gem++;
                if (GemService.ParseGemId(d.ItemId).Level != 1) gemsLv1 = false;
            }
        }
        bool Near10(int c) => c > n * 0.08 && c < n * 0.12;
        Check(Near10(ore) && Near10(stone) && Near10(gem), $"each drop ~10% (ore {ore}, stone {stone}, gem {gem} of {n})");
        Check(gemsLv1, "dropped gems are Lv1");
    }

    private void CheckBattleResult()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("t239b");
        // A seed whose drops include all three kinds, to check inventory.
        int seed = Enumerable.Range(0, 100000).First(x => StageRewardService.Roll(1, StageRewardService.DropRng(x)).Drops.Count == 3);
        gm.StartBattle(1, new List<string> { "custom_hero" }, seed);
        int gold = gm.Save.Gold, silver = gm.Save.Silver, stones = gm.Save.RefineStones;
        int ore = ForgeService.OreCount(gm.Save), gems = gm.Save.GemInventory.Values.Sum();
        var reward = gm.ApplyBattleResult(true, 3);
        Check(reward != null && reward.Drops.Count == 3, "win returns the reward with drops");
        Check(gm.Save.Silver == silver + 1000 && gm.Save.Gold == gold, "win: +1000 silver, gold unchanged");
        Check(gm.Save.RefineStones == stones + 1 && ForgeService.OreCount(gm.Save) == ore + 1
            && gm.Save.GemInventory.Values.Sum() == gems + 1, "drops land in the inventory");
        var rec = gm.Save.LastBattle;
        Check(rec != null && rec.StageId == 1 && rec.Seed == seed && rec.Won && rec.Stars == 3
            && rec.TeamIds.SequenceEqual(new[] { "custom_hero" }), "battle record saved (stage, seed, team, result)");
        var loaded = SaveManager.Load(gm.SavePath);
        Check(loaded.LastBattle?.Seed == seed, "battle record persisted to the save file");
        silver = gm.Save.Silver;
        gm.StartBattle(2, new List<string> { "custom_hero" }, 7);
        Check(gm.ApplyBattleResult(false, 0) == null && gm.Save.Silver == silver, "loss: no reward");
        Check(gm.Save.LastBattle is { StageId: 2, Seed: 7, Won: false }, "loss is recorded too");
    }

    private void CheckSweep()
    {
        var s = SeedService.CreateSeededSave("t239c");
        var team = new List<string> { "custom_hero" };
        var rng = new System.Random(5);
        Check(StageRewardService.TrySweep(s, team, 1, rng, out _) == StageRewardService.SweepResult.NotThreeStars, "sweep needs 3 stars");
        s.StageStars["1"] = 2;
        Check(StageRewardService.TrySweep(s, team, 1, rng, out _) == StageRewardService.SweepResult.NotThreeStars, "2 stars is not enough");
        s.StageStars["1"] = 3;
        int silver = s.Silver, exp = s.Heroes["custom_hero"].Exp;
        Check(StageRewardService.TrySweep(s, team, 1, rng, out var r) == StageRewardService.SweepResult.Success && r != null, "sweep 3-star stage");
        Check(s.Silver == silver + 1000 && s.Heroes["custom_hero"].Exp == exp + 50, "sweep gives the win reward");
        Check(StageRewardService.SweepsLeft(s) == StageRewardService.SweepsPerDay - 1, "sweep uses one daily sweep");
        Check(StageRewardService.TrySweep(s, new List<string>(), 1, rng, out _) == StageRewardService.SweepResult.NoTeam, "sweep needs a team");
        while (StageRewardService.SweepsLeft(s) > 0) StageRewardService.TrySweep(s, team, 1, rng, out _);
        silver = s.Silver;
        Check(StageRewardService.TrySweep(s, team, 1, rng, out _) == StageRewardService.SweepResult.NoSweepsLeft && s.Silver == silver,
            $"daily cap {StageRewardService.SweepsPerDay} sweeps, then refused");
        s.SweepDate = "2000-01-01";
        Check(StageRewardService.SweepsLeft(s) == StageRewardService.SweepsPerDay, "new day resets sweeps");
    }

    private void CheckStageSelectUi()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("t239d");
        gm.Save.LastStage = 0;
        var scene = ResourceLoader.Load<PackedScene>("res://features/stage/scenes/StageSelect.tscn");
        var ui = scene.Instantiate<StageSelect>();
        AddChild(ui);
        var box = ui.GetNode<VBoxContainer>("StageBox");
        var ch2 = box.FindChild("Chapter_2", true, false) as Button;
        Check(ch2 != null && ch2.Disabled, "chapter 2 locked before clearing stage 10");
        Check(box.GetChildren().OfType<Button>().Count() == 10 && box.FindChild("Stage_1", true, false) != null, "chapter 1 lists 10 stages");
        var sweep = ui.GetNode<Button>("SweepButton");
        Check(sweep.Disabled, "sweep disabled without 3 stars");
        Check(ui.GetNode<Label>("InfoLabel").Text.Contains("1.000 Bạc") || ui.GetNode<Label>("InfoLabel").Text.Contains("1,000 Bạc"),
            "info shows the 1000 silver reward");
        ui.QueueFree();

        gm.Save.LastStage = 12;
        gm.Save.StageStars["12"] = 3;
        gm.CurrentStageId = 12;
        var ui2 = scene.Instantiate<StageSelect>();
        AddChild(ui2);
        var box2 = ui2.GetNode<VBoxContainer>("StageBox");
        Check(!((Button)box2.FindChild("Chapter_2", true, false)!).Disabled, "chapter 2 opens after stage 10");
        Check(box2.FindChild("Stage_12", true, false) is Button { ButtonPressed: true }, "opens on the chapter of the current stage");
        var sweep2 = ui2.GetNode<Button>("SweepButton");
        Check(!sweep2.Disabled, "sweep enabled on a 3-star stage");
        int silver = gm.Save.Silver;
        sweep2.EmitSignal(BaseButton.SignalName.Pressed);
        Check(gm.Save.Silver == silver + 1000 && ui2.GetNode<Label>("InfoLabel").Text.Contains("Quét ải 12"), "sweep button grants the reward");
        ui2.SelectChapter(1);
        Check(box2.GetChildren().OfType<Button>().Any(b => b.Name == "Stage_1") && box2.FindChild("Stage_12", true, false) == null,
            "switching tab lists chapter 1 stages");
        ui2.QueueFree();
    }

    private bool RunToEnd(BattleScene scene)
    {
        for (int i = 0; i < 600; i++)
        {
            scene.StepBattle();
            if (scene.GetNodeOrNull<Control>("ResultPopup") != null) return true;
        }
        return false;
    }

    private void CheckBattlePopupDrops()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("t239e");
        gm.Save.OwnedHeroIds.AddRange(new[] { "quan_vu", "truong_phi", "trieu_van", "gia_cat_luong", "lu_bo" });
        foreach (var id in gm.Save.OwnedHeroIds) gm.Save.Heroes[id].Level = 30;
        FormationService.EnsureFormation(gm.Save);
        FormationService.TryPlace(gm.Save, "quan_vu", "hau_top", null, out _);
        FormationService.TryPlace(gm.Save, "trieu_van", "tien_mid", null, out _);
        FormationService.TryPlace(gm.Save, "gia_cat_luong", "hau_mid", null, out _);
        FormationService.TryPlace(gm.Save, "lu_bo", "trung_top", null, out _);
        var team = FormationService.FormationToTeam(gm.Save.Formation);
        gm.SelectedTeamIds = new List<string>(team);
        int seed = Enumerable.Range(0, 100000).First(x => StageRewardService.Roll(1, StageRewardService.DropRng(x)).Drops.Count >= 2);
        gm.StartBattle(1, new List<string>(team), seed);
        var scene = ResourceLoader.Load<PackedScene>("res://features/battle/scenes/BattleScene.tscn").Instantiate<BattleScene>();
        AddChild(scene);
        Check(scene.GetNode<GridContainer>("EnemyGrid").GetChildren()[3] is BattleHeroCell, "enemy Tiền drawn at Tiền giữa");
        Check(RunToEnd(scene), "battle reaches the result popup");
        var squares = scene.FindChild("RewardSquares", true, false) as HBoxContainer;
        int filled = squares?.GetChildren().Count(c => ((Node)c).FindChild("DropLabel", true, false) != null) ?? -1;
        Check(gm.LastBattleWon && squares != null && squares.GetChildCount() == 5, "win popup keeps 5 reward squares");
        Check(filled == gm.LastBattleReward?.Drops.Count && filled >= 2, $"drops shown in the reward squares ({filled})");
        scene.QueueFree();
    }
}
