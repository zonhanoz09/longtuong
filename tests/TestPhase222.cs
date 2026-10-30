using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.22 tests: hero shards removed completely from the game.
/// - New accounts own ONLY the custom hero (Luu Bi is recruit-only now).
/// - No shard state, no shard unlock, no star-up button, no shard rewards.
/// - Phase 2.23: recruit moved into the tavern (this test's old
///   "recruit disabled" checks were replaced by tavern basics).
/// - Old saves carrying a "Shards" JSON field still load fine (field ignored).
/// - Luu Bi can be dismissed (normal hero now); custom hero cannot.
/// - Battle win rewards are Gold/EXP only (no shards, no gems since 2.26).
/// </summary>
public partial class TestPhase222 : Node
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
        // --- New account: only the custom hero ---
        var save = SeedService.CreateSeededSave("tester222");
        Check(save.OwnedHeroIds.Count == 1 && save.OwnedHeroIds[0] == "custom_hero",
            "new account owns ONLY the custom hero");
        Check(!save.OwnedHeroIds.Contains("luu_bi"), "new account does NOT own Luu Bi");
        Check(save.TeamIds.Count == 1 && save.TeamIds[0] == "custom_hero",
            "new account team is just the custom hero");
        // Old accounts do not gain Luu Bi on load anymore.
        var old = new SaveData { OwnedHeroIds = new List<string> { "quan_vu" } };
        ProgressionService.EnsureDefaults(old);
        Check(old.OwnedHeroIds.Contains("custom_hero"), "old account gains the custom hero on load");
        Check(!old.OwnedHeroIds.Contains("luu_bi"), "old account does NOT gain Luu Bi on load");

        // --- Phase 2.23: recruit lives in the tavern now ---
        var s = SeedService.CreateSeededSave("tester222");
        s.Silver = 50000;
        Check(!TavernService.StartDrink(s, TavernGrade.Normal, new Random(7), out _) && s.Silver == 50000,
            "tavern drink fails without charging when silver is short (2.25: drinks cost silver)");
        ProgressionService.EnsureDefaults(s);
        Check(s.TierSouls.Count == 3 && s.TierSouls.Values.All(v => v == 0),
            "TierSouls initialized to 0 for all 3 tiers (2.24)");

        // --- Dismiss: Luu Bi yes, custom no ---
        var s2 = SeedService.CreateSeededSave("tester222");
        s2.OwnedHeroIds.Add("luu_bi");
        s2.OwnedHeroIds.Add("quan_vu");
        Check(ProgressionService.DismissHero(s2, "luu_bi"), "Luu Bi can be dismissed (normal hero now)");
        Check(!s2.OwnedHeroIds.Contains("luu_bi"), "dismissed Luu Bi leaves the owned list");
        Check(!ProgressionService.DismissHero(s2, "custom_hero"), "custom hero still cannot be dismissed");
        Check(ProgressionService.DismissHero(s2, "quan_vu"), "second dismiss succeeds");
        Check(!ProgressionService.DismissHero(s2, "custom_hero"), "last owned hero (custom) still cannot be dismissed");

        // --- Battle win: no shard reward, EXP still granted ---
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("tester222");
        gm.Save.OwnedHeroIds.Add("quan_vu");
        gm.SelectedTeamIds = new List<string> { "custom_hero", "quan_vu" };
        gm.CurrentStageId = 1;
        int goldBefore = gm.Save.Gold;
        int expBefore = gm.Save.Heroes["custom_hero"].Exp;
        gm.ApplyBattleResult(true, 3, 150);
        Check(gm.Save.Gold == goldBefore + 150, "battle win still grants gold");
        Check(gm.Save.Heroes["custom_hero"].Exp > expBefore || gm.Save.Heroes["custom_hero"].Level > 1,
            "battle win still grants EXP");
        Check(!gm.Save.OwnedHeroIds.Contains("luu_bi"), "battle win does NOT auto-unlock any locked hero");

        // --- Old save JSON with a Shards field still loads (field ignored) ---
        var path = Path.Combine(GameManager.DataDir, "test_phase222_legacy.json");
        try
        {
            File.WriteAllText(path, "{\"Gold\": 1234, \"Gems\": 56, \"Shards\": {\"quan_vu\": 99, \"luu_bi\": 10}, \"OwnedHeroIds\": [\"custom_hero\"], \"TeamIds\": [\"custom_hero\"]}");
            var loaded = SaveManager.Load(path);
            ProgressionService.EnsureDefaults(loaded);
            Check(loaded.Gold == 1234, "legacy save with Shards/Gems fields loads its other fields (old fields ignored)");
            Check(loaded.OwnedHeroIds.Count == 1 && loaded.OwnedHeroIds[0] == "custom_hero",
                "legacy save keeps its owned list (no shard unlock, no Luu Bi grant)");
        }
        finally { if (File.Exists(path)) File.Delete(path); }

        // --- Scenes: no star-up button, recruit buttons disabled ---
        var heroScene = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroScreen.tscn");
        Check(heroScene != null, "hero screen scene loads");
        if (heroScene != null)
        {
            var hero = heroScene.Instantiate<HeroScreen>();
            AddChild(hero);
            Check(hero.GetNodeOrNull<Button>("StarUpButton") == null, "star-up button is gone from the hero screen");
            Check(hero.GetNodeOrNull<Button>("RecruitButton") == null, "recruit button moved out of the equipment screen (hub Chiêu mộ button)");
            Check(hero.GetNodeOrNull<Label>("RecruitLabel") == null, "recruit label moved out of the equipment screen");
            Check(hero.GetNodeOrNull<Label>("StarLabel") == null && hero.GetNodeOrNull<Button>("UnlockStarButton") == null,
                "star chart moved out of the equipment screen (own hub button + popup)");
            hero.QueueFree();
        }
        var recruitScene = ResourceLoader.Load<PackedScene>("res://features/recruit/scenes/RecruitScene.tscn");
        Check(recruitScene != null, "recruit scene loads");
        if (recruitScene != null)
        {
            var rec = recruitScene.Instantiate<RecruitScene>();
            AddChild(rec);
            Check(rec.GetNodeOrNull<Button>("DrawButton") == null, "old draw button is gone from the tavern");
            Check(rec.GetNode<Button>("NormalButton") != null && rec.GetNode<Button>("HighButton") != null, "tavern has Normal/High drink buttons");
            Check(rec.GetNode<Button>("RockButton").Disabled && rec.GetNode<Button>("ScissorsButton").Disabled && rec.GetNode<Button>("PaperButton").Disabled, "RPS buttons disabled before any drink");
            Check(!rec.GetNode<Label>("RecruitStatusLabel").Text.Contains("làm lại"), "tavern status no longer notes a rework");
            rec.QueueFree();
        }

        GD.Print(_failed == 0 ? "TEST PHASE2.22: ALL PASS" : $"TEST PHASE2.22: {_failed} FAILURES");
        GetTree().Quit(_failed == 0 ? 0 : 1);
    }
}
