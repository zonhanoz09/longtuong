using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.38 — ổn định nền: bug fixes B1–B8/B11, test data isolation,
/// and the new combat formula (Huynh's spec: Né−Chính xác, Bạo−Kiên
/// cường, Đỡ−Phá kích, caps, (Công−Thủ) base damage, 5% chip, ±5%).
/// </summary>
public partial class TestPhase238 : Node
{
    private int _fails;

    public override void _Ready()
    {
        CheckIsolation();
        CheckDismissKeepsItem();
        CheckRefineStats();
        CheckInheritTrim();
        CheckRefineCharge();
        CheckFormationTrim();
        CheckMissingSave();
        CheckSaveFiles();
        CheckProjection();
        CheckDamageFormula();
        CheckChancesAndCaps();
        CheckMissGivesNoRage();
        CheckBlockSetBonus();

        if (_fails == 0) GD.Print("TEST PHASE 2.38: ALL PASS");
        else GD.Print($"TEST PHASE 2.38: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private void CheckIsolation()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        Check(GameManager.IsTestRun, "test run detected from command line");
        Check(GameManager.DataDir.EndsWith("/tests"), "test data dir is user://.../tests");
        gm.CurrentUsername = "nhan";
        Check(gm.SavePath.StartsWith(GameManager.DataDir + "/"), "test saves never touch the real save folder");
        Check(gm.AccountsPath.StartsWith(GameManager.DataDir + "/"), "test accounts file is isolated too");
        gm.CurrentUsername = "";
    }

    // B1: dismissing a hero returns the FULL item (set, lines, gems).
    private void CheckDismissKeepsItem()
    {
        var s = SeedService.CreateSeededSave("t238a");
        s.OwnedHeroIds.Add("luu_bi");
        ProgressionService.EnsureDefaults(s);
        var gemId = GemService.MakeGemId("atk", 2);
        var eq = new EquipmentState
        {
            IsEmpty = false, Level = 4, SetId = "ky_lan",
            RefineLines = Enumerable.Range(0, 6).Select(i => new RefineLine { StatId = "atk", Value = 10 + i }).ToList(),
        };
        GemService.EnsureSockets(eq);
        eq.Sockets[0].Unlocked = true;
        eq.Sockets[0].GemId = gemId;
        s.HeroEquipment["luu_bi"]["weapon"] = eq;
        int bagBefore = s.BagEquipment.Count;
        Check(ProgressionService.DismissHero(s, "luu_bi"), "B1: dismiss Luu Bi");
        Check(s.BagEquipment.Count == bagBefore + 1, "B1: one item returned to the bag");
        var back = s.BagEquipment[^1];
        Check(back.SetId == "ky_lan" && back.Level == 4, "B1: set and level kept (was reset to Tan Thu)");
        Check(back.RefineLines.Count == 6 && back.RefineLines[5].Value == 15, "B1: refine lines kept");
        Check(back.Sockets.Count == 4 && back.Sockets[0].GemId == gemId, "B1: socketed gem kept on the item");
        var slotAfter = s.HeroEquipment["luu_bi"]["weapon"];
        Check(slotAfter.IsEmpty && slotAfter.Sockets.All(k => string.IsNullOrEmpty(k.GemId)),
            "B1: hero slot cleared (no hidden gems left behind)");
    }

    // B2: dmg_red and def_pct refine lines (Tiền pool) now apply.
    private void CheckRefineStats()
    {
        var s = SeedService.CreateSeededSave("t238b");
        s.HeroEquipment["custom_hero"]["armor"] = new EquipmentState
        {
            IsEmpty = false, SetId = "tan_thu",
            RefineLines = new List<RefineLine> { new() { StatId = "hp", Value = 1 } },
        };
        var before = ProgressionService.GetEffectiveHero(s, "custom_hero");
        s.HeroEquipment["custom_hero"]["armor"].RefineLines = new List<RefineLine>
        {
            new() { StatId = "dmg_red", Value = 0.05 },
            new() { StatId = "def_pct", Value = 0.10 },
        };
        var after = ProgressionService.GetEffectiveHero(s, "custom_hero");
        Check(System.Math.Abs(after.DamageReduction - before.DamageReduction - 0.05) < 1e-9, "B2: dmg_red line adds Miễn thương");
        Check(after.Def == (int)System.Math.Round(before.Def * 1.10), "B2: def_pct line multiplies Thủ");
    }

    // B3: inheriting never exceeds the target set's line count.
    private void CheckInheritTrim()
    {
        var s = SeedService.CreateSeededSave("t238c");
        s.Silver = 100000;
        s.HeroEquipment["custom_hero"]["weapon"] = new EquipmentState
        {
            IsEmpty = false, SetId = "tan_thu",
            RefineLines = new List<RefineLine> { new() { StatId = "atk", Value = 1 } },
        };
        s.BagEquipment.Add(new BagEquipmentItem
        {
            SlotId = "weapon", SetId = "ky_lan", Level = 5,
            RefineLines = Enumerable.Range(0, 6).Select(i => new RefineLine { StatId = "atk", Value = 20 + i }).ToList(),
        });
        Check(EquipmentService.TryInherit(s, "custom_hero", "weapon", s.BagEquipment.Count - 1), "B3: inherit succeeds");
        var eq = s.HeroEquipment["custom_hero"]["weapon"];
        Check(eq.RefineLines.Count == EquipmentSets.LinesForSet("tan_thu"), "B3: lines trimmed to Tan Thu's 3");
        Check(eq.RefineLines[0].Value == 20 && eq.Level == 5, "B3: first lines + level inherited");
    }

    // B4: no charge when nothing can be rerolled; no free top-up lines.
    private void CheckRefineCharge()
    {
        var s = SeedService.CreateSeededSave("t238d");
        var rng = new System.Random(3);
        s.HeroEquipment["custom_hero"]["weapon"] = new EquipmentState
        {
            IsEmpty = false, SetId = "tan_thu",
            RefineLines = Enumerable.Range(0, 3).Select(_ => new RefineLine { StatId = "atk", Value = 30, Locked = true }).ToList(),
        };
        int stones = s.RefineStones, gold = s.Gold;
        Check(!ProgressionService.TryRefine(s, "custom_hero", "weapon", rng), "B4: all lines locked => refused");
        Check(s.RefineStones == stones && s.Gold == gold, "B4: refused refine charges nothing");
        s.HeroEquipment["custom_hero"]["weapon"].RefineLines = new List<RefineLine> { new() { StatId = "atk", Value = 30 } };
        s.RefineStones = 0; s.Gold = 0;
        Check(!ProgressionService.TryRefine(s, "custom_hero", "weapon", rng), "B4: cannot pay => refused");
        Check(s.HeroEquipment["custom_hero"]["weapon"].RefineLines.Count == 1, "B4: no free lines when refused");
        s.RefineStones = 5;
        Check(ProgressionService.TryRefine(s, "custom_hero", "weapon", rng), "B4: paid refine succeeds");
        Check(s.HeroEquipment["custom_hero"]["weapon"].RefineLines.Count == 3 && s.RefineStones == 4,
            "B4: paid refine tops up to 3 lines for 1 stone");
    }

    // B5: trimming an oversized formation keeps 5 heroes, not 3.
    private void CheckFormationTrim()
    {
        var s = SeedService.CreateSeededSave("t238e");
        s.OwnedHeroIds = new List<string> { "custom_hero", "luu_bi", "quan_vu", "truong_phi", "trieu_van", "gia_cat_luong" };
        s.Formation = new Dictionary<string, string>
        {
            ["trung_mid"] = "custom_hero", ["hau_top"] = "luu_bi", ["trung_top"] = "quan_vu",
            ["hau_mid"] = "truong_phi", ["tien_mid"] = "trieu_van", ["hau_bot"] = "gia_cat_luong",
        };
        FormationService.EnsureFormation(s);
        Check(s.Formation.Count == FormationService.MaxTeamSize, $"B5: trimmed to 5 (got {s.Formation.Count})");
        Check(s.Formation.TryGetValue("trung_mid", out var main) && main == "custom_hero", "B5: main hero kept");
    }

    // B6/B7: missing save for a logged-in account => seeded save;
    // canonical username spelling from the accounts file.
    private void CheckMissingSave()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CurrentUsername = "lost238";
        if (File.Exists(gm.SavePath)) File.Delete(gm.SavePath);
        gm.LoadGame();
        Check(gm.Save.OwnedHeroIds.Count == 1 && gm.Save.OwnedHeroIds[0] == "custom_hero",
            "B6: missing save => seeded (only the custom hero, not all 8)");
        Check(gm.Save.Silver == SeedService.SeededSilver && gm.Save.Gold == SeedService.SeededGold, "B6: seeded currencies");
        Check(File.Exists(gm.SavePath), "B6: seeded save written");
        var accounts = Path.Combine(GameManager.DataDir, "t238_accounts.json");
        Check(LocalAuth.Register(accounts, "Huynh238", "pass1234", out _), "B7: register mixed-case name");
        Check(LocalAuth.Login(accounts, "huynh238", "pass1234"), "B7: login is case-insensitive");
        Check(LocalAuth.CanonicalUsername(accounts, "HUYNH238") == "Huynh238", "B7: canonical spelling returned");
        gm.Logout();
    }

    // B8: atomic writes with .bak; corrupt files never become "empty".
    private void CheckSaveFiles()
    {
        var path = Path.Combine(GameManager.DataDir, "t238_save.json");
        SaveManager.Save(path, new SaveData { Gold = 111 });
        SaveManager.Save(path, new SaveData { Gold = 222 });
        Check(SaveManager.Load(path).Gold == 222, "B8: latest save loads");
        Check(File.Exists(path + ".bak") && !File.Exists(path + ".tmp"), "B8: previous save kept as .bak, no .tmp left");
        File.WriteAllText(path, "{ broken");
        Check(SaveManager.Load(path).Gold == 111, "B8: corrupt save falls back to .bak");
        File.WriteAllText(path + ".bak", "also broken");
        bool threw = false;
        try { SaveManager.Load(path); } catch (InvalidDataException) { threw = true; }
        Check(threw, "B8: both corrupt => InvalidDataException (no silent empty save)");
        Check(File.Exists(path + ".corrupt"), "B8: corrupt file preserved as .corrupt");

        var accounts = Path.Combine(GameManager.DataDir, "t238_accounts_bad.json");
        File.WriteAllText(accounts, "[ not json");
        threw = false;
        try { LocalAuth.Register(accounts, "newbie238", "pass1234", out _); } catch (InvalidDataException) { threw = true; }
        Check(threw, "B8: corrupt accounts file blocks register");
        Check(File.ReadAllText(accounts) == "[ not json", "B8: corrupt accounts file not overwritten");
    }

    // B11: craft preview is deterministic and leaves the slot untouched.
    private void CheckProjection()
    {
        var s = SeedService.CreateSeededSave("t238f");
        var preview = new EquipmentState { IsEmpty = false, Level = 0, SetId = "tan_thu" };
        var a = EquipmentService.ProjectEquipChange(s, "custom_hero", "weapon", preview);
        var b = EquipmentService.ProjectEquipChange(s, "custom_hero", "weapon", preview);
        Check(a.Count == b.Count && a.Zip(b).All(p => p.First == p.Second), "B11: projection is stable across refreshes");
        Check(preview.RefineLines.Count == 0, "B11: preview item gets no random refine lines");
        Check(!a.Any(r => r.Name == "Bạo kích" || r.Name == "Né tránh"), "B11: no phantom refine stats in the preview");
        Check(a.Any(r => r.Name == "Vật công" && r.After - r.Before == 20), "B11: weapon base ATK +20 shown");
        Check(s.HeroEquipment["custom_hero"]["weapon"].IsEmpty, "B11: slot restored after projection");
    }

    private static Fighter F(int atk, int def, HeroRole role = HeroRole.Vanguard) =>
        new(new HeroDef { Id = "t", Name = "t", Role = role, Hp = 1000, Atk = atk, Def = def }, true);

    private void CheckDamageFormula()
    {
        Check(BattleEngine.CalculateDamage(F(100, 0), F(100, 40), 1.0, false, false, 1.0) == 60,
            "spec: base = (Công − Thủ) × hệ số = 60");
        Check(BattleEngine.CalculateDamage(F(100, 0), F(100, 40), 2.0, false, false, 1.0) == 120,
            "spec: skill ratio 2.0 doubles (Công − Thủ)");
        Check(BattleEngine.CalculateDamage(F(100, 0), F(100, 200), 1.0, false, false, 1.0) == 5,
            "spec: Thủ >= Công => chip 5% Công");
        Check(BattleEngine.CalculateDamage(F(100, 0), F(100, 40), 1.0, false, true, 1.0) == 0, "spec: né => 0");
        Check(BattleEngine.CalculateDamage(F(100, 0), F(100, 40), 1.0, true, false, 1.0) == 90, "spec: bạo ×1.5");
        var critDmg = F(100, 0); critDmg.Def.CritDamage = 0.30;
        Check(BattleEngine.CalculateDamage(critDmg, F(100, 40), 1.0, true, false, 1.0) == 108, "spec: bạo ×(1.5 + 30%)");
        Check(BattleEngine.CalculateDamage(F(100, 0), F(100, 40), 1.0, false, false, 1.0, blocked: true) == 30, "spec: đỡ ×0.5");
        Check(BattleEngine.CalculateDamage(F(100, 0), F(100, 40), 1.0, true, false, 1.0, blocked: true) == 45, "spec: bạo + đỡ = ×0.75");
        var tank = F(100, 40); tank.Def.DamageReduction = 0.95;
        var capped = F(100, 40); capped.Def.DamageReduction = BattleEngine.DamageReductionCap;
        Check(BattleEngine.CalculateDamage(F(100, 0), tank, 1.0, false, false, 1.0)
            == BattleEngine.CalculateDamage(F(100, 0), capped, 1.0, false, false, 1.0)
            && BattleEngine.CalculateDamage(F(100, 0), tank, 1.0, false, false, 1.0) == 15, "spec: miễn thương capped at 75%");
        Check(BattleEngine.VarianceMin == 0.95 && BattleEngine.VarianceMax == 1.05, "spec: variance ±5%");
        Check(BattleEngine.CalculateDamage(F(100, 0, HeroRole.Warrior), F(100, 40, HeroRole.Strategist), 1.0, false, false, 1.0) == 75,
            "counter ×1.25 on base (Võ > Mưu)");
    }

    private void CheckChancesAndCaps()
    {
        var atk = F(100, 0); var def = F(100, 0);
        def.Def.Dodge = 0.30; atk.Def.Hit = 0.10;
        Check(System.Math.Abs(BattleEngine.DodgeChance(atk, def) - 0.20) < 1e-9, "spec: né thực = Né − Chính xác");
        atk.Def.Hit = 0.50;
        Check(BattleEngine.DodgeChance(atk, def) == 0, "spec: Né <= Chính xác => luôn trúng");
        def.Def.Dodge = 3.0; atk.Def.Hit = 0;
        Check(BattleEngine.DodgeChance(atk, def) == BattleEngine.DodgeCap && BattleEngine.DodgeCap == 0.85, "cap: né tối đa 85%");
        atk.Def.Crit = 0.40; def.Def.CritResist = 0.15;
        Check(System.Math.Abs(BattleEngine.CritChance(atk, def) - 0.25) < 1e-9, "spec: bạo thực = Bạo − Kiên cường");
        atk.Def.Crit = 2.0; def.Def.CritResist = 0;
        Check(BattleEngine.CritChance(atk, def) == 0.90, "cap: bạo tối đa 90%");
        def.Def.Block = 0.30; atk.Def.Pierce = 0.10;
        Check(System.Math.Abs(BattleEngine.BlockChance(atk, def) - 0.20) < 1e-9, "spec: đỡ thực = Đỡ − Phá kích");
        def.Def.Block = 2.0; atk.Def.Pierce = 0;
        Check(BattleEngine.BlockChance(atk, def) == 0.80, "cap: đỡ tối đa 80%");
    }

    // Spec: a dodged attack gives the attacker no rage; dodge cap holds.
    private void CheckMissGivesNoRage()
    {
        var players = new List<HeroDef> { ProgressionService.GetScaledBase("lu_bo", 1) };
        var e = new BattleEngine(players, HeroDatabase.GetStage(1), 4242);
        // Enemies: dodge 500% (capped to 85%), harmless, near-unkillable,
        // so the player gets many attacks to sample.
        foreach (var f in e.EnemyFighters) { f.Def.Dodge = 5.0; f.Def.Atk = 0; f.Hp = 1000000; }
        int attacks = 0, misses = 0; bool rageOk = true;
        for (int i = 0; i < 400 && !e.IsOver; i++)
        {
            var cur = e.Current;
            if (cur == null) break;
            if (cur.IsPlayer && cur.Rage < 100)
            {
                int rage = cur.Rage, logCount = e.Log.Count;
                e.ActCurrentNormal();
                attacks++;
                bool missed = e.Log.Skip(logCount).Any(l => l.Contains("NÉ"));
                if (missed) { misses++; if (cur.Rage != rage) rageOk = false; }
                else if (cur.Rage != System.Math.Min(100, rage + 20)) rageOk = false;
            }
            else if (cur.Rage >= 100) e.ActCurrentUltimate();
            else e.ActCurrentNormal();
        }
        Check(rageOk, "spec: miss => no rage, hit => +20 rage");
        double rate = attacks == 0 ? 0 : misses / (double)attacks;
        Check(attacks >= 30 && rate > 0.70 && rate < 0.95, $"cap: dodge 500% still lets ~15% hits land (miss rate {rate:P0} of {attacks})");
    }

    // Huyền Vũ 4 món: Đỡ đòn +10% is live (no longer "sắp mở").
    private void CheckBlockSetBonus()
    {
        var s = SeedService.CreateSeededSave("t238g");
        foreach (var slot in new[] { "weapon", "armor", "helmet", "boots" })
            s.HeroEquipment["custom_hero"][slot] = new EquipmentState
            {
                IsEmpty = false, SetId = "huyen_vu",
                RefineLines = new List<RefineLine> { new() { StatId = "hp", Value = 1 } },
            };
        var h = ProgressionService.GetEffectiveHero(s, "custom_hero");
        Check(System.Math.Abs(h.Block - 0.10) < 1e-9, "Huyen Vu 4 pieces => Block 10%");
        Check(!SetBonusService.DescribeBonus(new SetBonus { Pieces = 4, StatId = "block", Value = 0.10 }).Contains("sắp mở"),
            "block bonus no longer marked sắp mở");
    }
}
