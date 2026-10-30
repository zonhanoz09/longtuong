using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.52 — balance pass. A 5-hero team at a given level, wearing the
/// set of that level band at +min(10, level/10) with fixed mid-roll refine
/// lines (deterministic), fights each mode's milestone content over 20
/// seeds. Prints the win-rate table and checks the Pax target ranges:
/// "at the target level it is beatable (>= 50%), well below it is not
/// (<= 30%)". Ranges are Pax setup — Huynh tunes the Scale data.
/// </summary>
public partial class TestPhase252 : Node
{
    private int _fails;
    private const int Seeds = 20;
    private static readonly string[] TeamIds = { "custom_hero", "truong_phi", "quan_vu", "gia_cat_luong", "dieu_thuyen" };

    public override void _Ready()
    {
        if (OS.GetCmdlineUserArgs().Contains("--calibrate")) { Calibrate(); GetTree().Quit(0); return; }
        CheckCampaign();
        CheckDungeons();
        CheckTowers();
        CheckParty();

        if (_fails == 0) GD.Print("TEST PHASE 2.52: ALL PASS");
        else GD.Print($"TEST PHASE 2.52: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    /// The set a player would wear at this level (Tân Thủ below 50 — it is
    /// crafted without a level gate, so also below Lv30).
    public static string SetForLevel(int level) =>
        EquipmentSets.All.Where(s => s.Tier <= level).OrderBy(s => s.Tier).LastOrDefault()?.Id ?? "tan_thu";

    /// A 5-hero save at `level`, every slot filled with the level's set.
    public static SaveData Team(int level)
    {
        var s = SeedService.CreateSeededSave("t252");
        s.OwnedHeroIds = TeamIds.ToList();
        ProgressionService.EnsureDefaults(s);
        foreach (var id in TeamIds) s.Heroes[id].Level = level;
        s.Formation = new Dictionary<string, string>
        {
            ["tien_mid"] = "truong_phi", ["trung_mid"] = "custom_hero", ["trung_top"] = "quan_vu",
            ["hau_top"] = "gia_cat_luong", ["hau_mid"] = "dieu_thuyen",
        };
        var set = EquipmentSets.Get(SetForLevel(level));
        // Fixed lowest-roll refine lines (a player who has not rerolled yet):
        // an item with no lines would get random ones from the save
        // migration (unseeded), making the sim noisy.
        var pool = ProgressionService.RefinePoolFor(FormationPosition.Middle);
        foreach (var id in TeamIds)
            foreach (var slot in ProgressionService.Slots)
                s.HeroEquipment[id][slot.Id] = new EquipmentState
                {
                    IsEmpty = false, Level = System.Math.Min(10, level / 10), SetId = set.Id, Tier = set.Tier, Quality = set.Quality,
                    RefineLines = Enumerable.Range(0, EquipmentSets.LinesForSet(set.Id))
                        .Select(i => new RefineLine { StatId = pool[i % pool.Count].StatId, Value = pool[i % pool.Count].Min })
                        .ToList(),
                };
        return s;
    }

    /// Calibration helper (not part of the checks): the largest Scale at which
    /// the team still wins >= 50%. Used once to pick the 2.52 Scale data.
    public static double Threshold(SaveData team, StageDef s, IReadOnlyList<string>? ids = null)
    {
        double lo = 0.1, hi = 40;
        for (int i = 0; i < 12; i++)
        {
            double mid = (lo + hi) / 2;
            var st = new StageDef { Id = s.Id, Name = s.Name, EnemyIds = s.EnemyIds, BossId = s.BossId, EnemyHpMultiplier = s.EnemyHpMultiplier, Scale = mid };
            if (WinRate(team, st, ids) >= 0.5) lo = mid; else hi = mid;
        }
        return lo;
    }

    private void Calibrate()
    {
        var levels = new[] { 20, 35, 45, 55, 70, 85, 100, 160 };
        GD.Print("template      | " + string.Join(" ", levels.Select(l => $"Lv{l,-4}")));
        void Row(string name, StageDef s, IReadOnlyList<string>? ids = null) =>
            GD.Print($"{name,-13} | " + string.Join(" ", levels.Select(l => $"{Threshold(Team(l), s, ids),5:0.00} ")));
        Row("campaign 70", HeroDatabase.GetStage(70));
        Row("dungeon boss", DungeonService.StageFor(DungeonDatabase.Get("xich_bich"), DungeonDifficulty.Normal, 3));
        Row("trial 25", TrialTowerService.StageForFloor(25));
        Row("trial 50", TrialTowerService.StageForFloor(50));
        Row("duel lu_bo", new StageDef { Id = 1, Name = "x", EnemyIds = new() { "lu_bo" } }, new[] { "custom_hero" });
        Row("duel boss", DuelTowerService.StageForFloor(4), new[] { "custom_hero" });
        // Tổ Đội: same 3-team loop as PartyService.Simulate, with a custom boss Scale.
        foreach (var boss in new[] { "truong_giac", "tao_thao" })
            GD.Print($"party {boss,-7} | " + string.Join(" ", levels.Select(l =>
            {
                var team = Team(l);
                double lo = 0.1, hi = 40;
                for (int i = 0; i < 12; i++)
                {
                    double mid = (lo + hi) / 2;
                    var st = new StageDef { Id = 1, Name = "x", EnemyIds = new() { boss }, Scale = mid, EnemyHpMultiplier = PartyService.BossHpMultiplier };
                    int wins = 0;
                    for (int seed = 0; seed < Seeds; seed++)
                    {
                        int hp = new BattleEngine(new List<HeroDef>(), st, 0).EnemyFighters[0].MaxHp;
                        for (int t = 0; t < PartyService.Teams && hp > 0; t++)
                        {
                            var pt = PartyService.TeamFor(team, t, 500 + seed * 13);
                            var e = new BattleEngine(pt.Heroes, pt.Positions, st, PartyService.SeedFor(500 + seed * 13, t), PartyService.MaxTurnsPerTeam, new[] { hp });
                            e.AutoRun();
                            hp = e.EnemyFighters[0].Hp;
                        }
                        if (hp <= 0) wins++;
                    }
                    if (wins >= Seeds / 2) lo = mid; else hi = mid;
                }
                return $"{lo,5:0.00} ";
            })));
    }

    public static double WinRate(SaveData s, StageDef stage, IReadOnlyList<string>? ids = null)
    {
        ids ??= TeamIds;
        var defs = ids.Select(id => ProgressionService.GetEffectiveHero(s, id)).ToList();
        var pos = ids.Select(id => FormationService.PositionOfHero(s, id)).ToList();
        int wins = 0;
        for (int seed = 0; seed < Seeds; seed++)
        {
            var e = new BattleEngine(defs, pos, stage, 1000 + seed * 37);
            e.AutoRun();
            if (e.PlayerWon) wins++;
        }
        return wins / (double)Seeds;
    }

    public static double PartyWinRate(SaveData s, string bossId)
    {
        int wins = 0;
        for (int seed = 0; seed < Seeds; seed++)
            if (PartyService.Simulate(s, bossId, 500 + seed * 13).Won) wins++;
        return wins / (double)Seeds;
    }

    private static string P(double r) => $"{r * 100:0}%";

    // One target row: content beatable at `level` (>= 50%), not at `low` (<= 30%).
    private void Target(string name, int level, int low, System.Func<SaveData, double> rate)
    {
        double hi = rate(Team(level)), lo = rate(Team(low));
        GD.Print($"{name,-28} Lv{level,-3} {P(hi),5}   Lv{low,-3} {P(lo),5}");
        Check(hi >= 0.5, $"{name}: Lv{level} wins >= 50% (got {P(hi)})");
        Check(lo <= 0.3, $"{name}: Lv{low} wins <= 30% (got {P(lo)})");
    }

    private void CheckCampaign()
    {
        GD.Print("== Campaign chapter ends (target level / 15 below) ==");
        // Chapter 1–3 (ải 1–20) keep the 2.39 Scale by decision; reported only.
        foreach (var id in new[] { 6, 14, 20 })
            GD.Print($"ải {id,-25} Lv10 {P(WinRate(Team(10), HeroDatabase.GetStage(id))),5}   Lv1  {P(WinRate(Team(1), HeroDatabase.GetStage(id))),5}");
        foreach (var (id, level) in new[] { (30, 35), (38, 45), (48, 55), (60, 70), (70, 85) })
            Target($"ải {id}", level, level - 15, s => WinRate(s, HeroDatabase.GetStage(id)));
        bool up = true;
        for (int i = 1; i < HeroDatabase.Stages.Count; i++) if (HeroDatabase.Stages[i].Scale <= HeroDatabase.Stages[i - 1].Scale) up = false;
        Check(up, "campaign Scale still strictly increases");
    }

    private void CheckDungeons()
    {
        GD.Print("== Dungeon bosses: Thường at the dungeon level / 20 below; Khó at the dungeon level ==");
        foreach (var d in DungeonDatabase.All)
        {
            var normal = DungeonService.StageFor(d, DungeonDifficulty.Normal, 3);
            var hard = DungeonService.StageFor(d, DungeonDifficulty.Hard, 3);
            Target($"{d.Name} Thường", d.MinLevel, d.MinLevel - 20, s => WinRate(s, normal));
            double hardAtMin = WinRate(Team(d.MinLevel), hard);
            GD.Print($"{d.Name + " Khó",-28} Lv{d.MinLevel,-3} {P(hardAtMin),5}");
            Check(hardAtMin <= 0.3, $"{d.Name} Khó is harder than Thường at Lv{d.MinLevel} (got {P(hardAtMin)})");
        }
    }

    // At the unlock level the first floors are beatable but the tower does
    // not fall at once (a below-unlock level would be meaningless: the mode
    // is closed there).
    private void UnlockGate(string name, int level, System.Func<SaveData, double> early, System.Func<SaveData, double> later)
    {
        var team = Team(level);
        double e = early(team), l = later(team);
        GD.Print($"{name,-28} Lv{level,-3} tầng 10 {P(e),5}   tầng 30 {P(l),5}");
        Check(e >= 0.5, $"{name}: Lv{level} clears floor 10 (got {P(e)})");
        Check(l <= 0.3, $"{name}: Lv{level} does not clear floor 30 yet (got {P(l)})");
    }

    private void CheckTowers()
    {
        GD.Print("== Thí Luyện Tháp ==");
        UnlockGate("Thí Luyện (mở Lv25)", 25, s => WinRate(s, TrialTowerService.StageForFloor(10)), s => WinRate(s, TrialTowerService.StageForFloor(30)));
        Target("Thí Luyện tầng 60", 100, 70, s => WinRate(s, TrialTowerService.StageForFloor(60)));
        GD.Print("== Ải Vượt Tướng (chủ tướng 1v1) ==");
        var solo = new[] { "custom_hero" };
        UnlockGate("Vượt Tướng (mở Lv20)", 20, s => WinRate(s, DuelTowerService.StageForFloor(10), solo), s => WinRate(s, DuelTowerService.StageForFloor(30), solo));
        Target("Vượt Tướng tầng 100", 180, 100, s => WinRate(s, DuelTowerService.StageForFloor(100), solo));
    }

    private void CheckParty()
    {
        GD.Print("== Tổ Đội (3 teams, boss HP x8) ==");
        foreach (var (boss, level) in new[] { ("truong_giac", 40), ("lu_bo", 55), ("vien_thieu", 70), ("tao_thao", 90) })
            Target($"Tổ Đội {HeroDatabase.GetCombatant(boss).Name}", level, level - 20, s => PartyWinRate(s, boss));
    }
}
