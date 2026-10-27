using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.20 tests: 7-slot formation (Hậu 3, Trung 3, Tiền 1), main hero
/// (custom_hero) always deployed and only in the Trung column, max 5 heroes,
/// drag/drop/swap/remove rules, old-save migration, and battle position
/// effects (Tiền DEF+20% + tanking, Trung ATK+20%, Hậu 20% first-strike).
/// </summary>
public partial class TestPhase220 : Node
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
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("Tester220");
        var save = gm.Save;

        // --- Formation shape ---
        Check(FormationService.Slots.Count == 7, "2.20: 7 slots");
        Check(FormationService.Slots.Count(s => s.Position == FormationPosition.Rear) == 3, "2.20: 3 Hau slots");
        Check(FormationService.Slots.Count(s => s.Position == FormationPosition.Middle) == 3, "2.20: 3 Trung slots");
        Check(FormationService.Slots.Count(s => s.Position == FormationPosition.Front) == 1, "2.20: 1 Tien slot");
        Check(FormationService.GridOrder.Count == 9 && FormationService.GridOrder.Count(c => c == null) == 2,
            "2.20: grid 9 cells, 2 placeholders (no Tien top/bottom)");

        // --- Seeded save: main at center ---
        Check(save.Formation.TryGetValue("trung_mid", out var main) && main == "custom_hero",
            "2.20: seeded save main at center slot");
        Check(save.TeamIds.Contains("custom_hero"), "2.20: seeded team contains main");

        // --- Migration from an old save (TeamIds, no Formation) ---
        // Note: a seeded save owns only [custom], so the old team must
        // be granted ownership first, like a real old account would have.
        var old = SeedService.CreateSeededSave("Old220");
        old.OwnedHeroIds = new List<string> { "custom_hero", "luu_bi", "quan_vu", "truong_phi", "gia_cat_luong" };
        old.Formation = new Dictionary<string, string>();
        old.TeamIds = new List<string> { "quan_vu", "truong_phi", "gia_cat_luong" };
        FormationService.EnsureFormation(old);
        Check(old.Formation["trung_mid"] == "custom_hero", "2.20: migration forces main to center");
        Check(old.Formation.Count == 4, "2.20: migration keeps 3 old heroes + main");
        Check(old.TeamIds.Count == 4 && old.TeamIds.Contains("custom_hero"), "2.20: migration team derived from formation");

        // --- Place / swap / remove rules ---
        var s2 = SeedService.CreateSeededSave("Place220");
        s2.OwnedHeroIds.AddRange(new[] { "quan_vu", "truong_phi", "trieu_van", "dieu_thuyen", "gia_cat_luong", "lu_bo" });
        FormationService.EnsureFormation(s2); // main at center only
        Check(FormationService.TryPlace(s2, "quan_vu", "hau_top", null, out _), "2.20: list -> empty slot");
        Check(s2.Formation.TryGetValue("hau_top", out var placed) && placed == "quan_vu", "2.20: placed hero in slot");
        Check(!FormationService.TryPlace(s2, "custom_hero", "hau_mid", null, out var errMain) && errMain.Contains("Trung"),
            "2.20: main cannot go to Hau");
        Check(FormationService.TryPlace(s2, "custom_hero", "trung_bot", "trung_mid", out _),
            "2.20: main can move within Trung column");
        Check(s2.Formation["trung_bot"] == "custom_hero" && !s2.Formation.ContainsKey("trung_mid"),
            "2.20: main moved, old slot freed");
        // Slot -> slot swap, and list -> filled slot swap (occupant back to list).
        Check(FormationService.TryPlace(s2, "truong_phi", "hau_mid", null, out _), "2.20: place second hero");
        Check(FormationService.TryPlace(s2, "quan_vu", "hau_mid", "hau_top", out _)
            && s2.Formation.TryGetValue("hau_mid", out var sw1) && sw1 == "quan_vu"
            && s2.Formation.TryGetValue("hau_top", out var sw2) && sw2 == "truong_phi",
            "2.20: slot -> slot swap");
        Check(FormationService.TryPlace(s2, "trieu_van", "hau_mid", null, out _)
            && s2.Formation["hau_mid"] == "trieu_van" && !s2.Formation.ContainsValue("quan_vu"),
            "2.20: list -> filled slot swaps occupant back to list");
        // Main cannot be swapped out of Trung by a list hero onto its slot.
        Check(!FormationService.TryPlace(s2, "dieu_thuyen", "trung_bot", null, out _),
            "2.20: list hero cannot displace main from its slot");
        // Count so far: hau_top=truong_phi, hau_mid=trieu_van, trung_bot=main (3).
        Check(FormationService.TryPlace(s2, "quan_vu", "hau_bot", null, out _), "2.20: refill to 4");
        Check(FormationService.TryPlace(s2, "dieu_thuyen", "tien_mid", null, out _), "2.20: fill to 5 (Tien slot)");
        Check(s2.Formation.Count == 5, "2.20: team at max 5");
        Check(!FormationService.TryPlace(s2, "gia_cat_luong", "trung_top", null, out var errMax) && errMax.Contains("5"),
            "2.20: 6th hero rejected");
        // Remove: normal hero ok, main blocked, then hero reappears in list logic.
        Check(FormationService.TryRemove(s2, "trieu_van", out _), "2.20: remove non-main hero");
        Check(!FormationService.TryRemove(s2, "custom_hero", out var errRm) && errRm.Contains("Main"),
            "2.20: main cannot be removed");
        Check(s2.Formation.Count == 4 && !s2.Formation.ContainsValue("trieu_van"), "2.20: removed hero gone");

        // --- Popup structure (scene-first): 7 slot buttons, list = owned - deployed ---
        // Grant lu_bo so the popup drop below has an owned, undeployed hero.
        if (!gm.Save.OwnedHeroIds.Contains("lu_bo")) gm.Save.OwnedHeroIds.Add("lu_bo");
        var popup = ResourceLoader.Load<PackedScene>("res://features/formation/scenes/FormationScene.tscn").Instantiate<FormationScene>();
        AddChild(popup);
        var grid = popup.GetNode<GridContainer>("SlotGrid");
        Check(grid.GetChildCount() == 9, "2.20: popup grid has 9 cells (7 slots + 2 placeholders)");
        Check(popup.GetNode<Label>("CountLabel").Text.Contains("/5"), "2.20: popup count label is /5");
        var slotButtons = grid.GetChildren().OfType<FormationSlotButton>().ToList();
        Check(slotButtons.Count == 7, "2.20: popup has 7 slot buttons");
        // Drag handler wiring: drop a list hero onto a slot via _DropData.
        var hauTop = slotButtons.First(b => b.SlotId == "hau_top");
        hauTop._DropData(Vector2.Zero, "lu_bo|");
        Check(gm.Save.Formation.ContainsValue("lu_bo"), "2.20: _DropData list -> slot places hero");
        // Click-to-remove is covered by TryRemove above; main slot button shows main.
        var center = slotButtons.First(b => b.SlotId == "trung_mid");
        Check(center.HeroId == "custom_hero" || gm.Save.Formation.Values.Contains("custom_hero"),
            "2.20: main still deployed after popup interactions");
        popup.QueueFree();

        // --- Battle position effects ---
        var stage = HeroDatabase.GetStage(1);
        var trung = HeroDatabase.Get("quan_vu");
        var fTrung = new Fighter(trung, true) { Position = FormationPosition.Middle };
        Check(fTrung.EffectiveAtk == (int)System.Math.Round(trung.Atk * 1.20), "2.20: Trung ATK +20%");
        var fTien = new Fighter(HeroDatabase.Get("truong_phi"), true) { Position = FormationPosition.Front };
        Check(fTien.EffectiveDef == (int)System.Math.Round(HeroDatabase.Get("truong_phi").Def * 1.20), "2.20: Tien DEF +20%");
        var fNone = new Fighter(trung, true);
        Check(fNone.EffectiveAtk == trung.Atk && fNone.EffectiveDef == trung.Def, "2.20: no position, no bonus");
        // Damage uses effective stats: Trung attacker hits harder than base.
        int dmgBase = BattleEngine.CalculateDamage(fNone, fNone, 1.0, false, false, 1.0);
        int dmgTrung = BattleEngine.CalculateDamage(fTrung, fNone, 1.0, false, false, 1.0);
        Check(dmgTrung > dmgBase, "2.20: Trung bonus increases damage");
        int dmgVsTien = BattleEngine.CalculateDamage(fNone, fTien, 1.0, false, false, 1.0);
        int dmgVsBase = BattleEngine.CalculateDamage(fNone, new Fighter(HeroDatabase.Get("truong_phi"), true), 1.0, false, false, 1.0);
        Check(dmgVsTien < dmgVsBase, "2.20: Tien bonus reduces damage taken");
        // Tanking: enemy single-target attacks hit the Tien hero first, even
        // when another player hero has lower HP.
        var playerDefs = new List<HeroDef>
        {
            HeroDatabase.Get("quan_vu"),   // will be the low-HP hero
            HeroDatabase.Get("truong_phi"), // Tien
        };
        var positions = new List<FormationPosition> { FormationPosition.Middle, FormationPosition.Front };
        var eng = new BattleEngine(playerDefs, positions, stage, 12345);
        var low = eng.PlayerFighters[0];
        low.Hp = 10; // lowest HP, but NOT the front hero
        var enemy = eng.EnemyFighters[0];
        // Force the enemy to act: run engine turns until an enemy acts, then
        // check the log target. Simpler: drive one enemy action via AutoRun is
        // noisy; instead verify PickTarget indirectly by dealing damage through
        // a fresh engine where the enemy is first in order is complex — so we
        // verify via a full auto battle: the Tien hero must take the first
        // enemy single-target hit (its HP drops before the low hero dies).
        // Deterministic check: create an engine with only the Tien hero + a
        // low hero, let the enemy act first by giving enemies huge speed via
        // a custom stage is not possible, so use AutoRun and assert the Tien
        // hero lost HP while the low hero was still alive at some point is
        // flaky. Instead, assert the engine's first enemy action targets Tien
        // by inspecting the log after running turns until the first enemy hit.
        var eng2 = new BattleEngine(playerDefs, positions, stage, 777);
        eng2.PlayerFighters[0].Hp = 10;
        int guard = 0;
        string firstEnemyLog = "";
        while (guard++ < 50 && string.IsNullOrEmpty(firstEnemyLog))
        {
            var cur = eng2.Current;
            if (cur == null) break;
            if (!cur.IsPlayer)
            {
                int before = eng2.Log.Count;
                eng2.ActCurrentNormal();
                for (int i = before; i < eng2.Log.Count; i++)
                    if (eng2.Log[i].Contains("gây") || eng2.Log[i].Contains("NÉ")) { firstEnemyLog = eng2.Log[i]; break; }
            }
            else eng2.ActCurrentNormal();
            if (eng2.IsOver) break;
        }
        Check(firstEnemyLog.Contains("Trương Phi"), "2.20: enemy single-target hits Tien hero first");
        // Rear first-strike: a slow rear hero should sometimes act first.
        // Use a slow rear hero (Truong Phi speed is mid; use a custom low-speed
        // check by counting first-actor across seeds with a slow rear hero).
        var slowRearDefs = new List<HeroDef> { HeroDatabase.Get("truong_phi") };
        var slowRearPos = new List<FormationPosition> { FormationPosition.Rear };
        int rearFirst = 0, trials = 200;
        for (int seed = 0; seed < trials; seed++)
        {
            var e = new BattleEngine(slowRearDefs, slowRearPos, stage, seed);
            if (e.Current != null && e.Current.IsPlayer && e.Current.Def.Id == "truong_phi") rearFirst++;
        }
        // Without the proc, Truong Phi (speed 55 after scale? base 55) never
        // outspeeds stage-1 enemies (speeds 60-70+), so first-actor rate should
        // be near 20%, not 0% and not 100%.
        Check(rearFirst > 0 && rearFirst < trials, "2.20: rear hero sometimes acts first (proc exists)");
        Check(rearFirst >= trials * 0.10 && rearFirst <= trials * 0.35,
            $"2.20: rear first-strike rate near 20% (got {rearFirst}/{trials})");
        // No duplicate turns: within the first full round (5 alive fighters),
        // every actor appears exactly once, even with rear first-movers.
        var eng3 = new BattleEngine(playerDefs, new List<FormationPosition> { FormationPosition.Rear, FormationPosition.Rear }, stage, 42);
        var seen = new System.Collections.Generic.HashSet<Fighter>();
        int roundActions = 0;
        while (roundActions < 5 && eng3.Current != null && !eng3.IsOver)
        {
            seen.Add(eng3.Current);
            roundActions++;
            eng3.ActCurrentNormal();
        }
        Check(roundActions == 5 && seen.Count == 5, "2.20: first round has 5 distinct actors, no duplicates");

        GD.Print($"TEST PHASE2.20: {(_failed == 0 ? "ALL PASS" : $"{_failed} FAIL")} ({_passed} passed, {_failed} failed)");
        GetTree().Quit(_failed == 0 ? 0 : 1);
    }
}
