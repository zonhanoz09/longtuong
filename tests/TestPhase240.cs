using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.40 — enemy-only soldiers/bosses (EnemyDatabase), resolved in
/// battle by HeroDatabase.GetCombatant; stage boss gets Scale x BossScale.
/// </summary>
public partial class TestPhase240 : Node
{
    private int _fails;

    public override void _Ready()
    {
        CheckData();
        CheckBattle();

        if (_fails == 0) GD.Print("TEST PHASE 2.40: ALL PASS");
        else GD.Print($"TEST PHASE 2.40: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private void CheckData()
    {
        var heroIds = HeroDatabase.Heroes.Select(h => h.Id).ToHashSet();
        var enemyIds = EnemyDatabase.All.Select(e => e.Id).ToList();
        Check(EnemyDatabase.Soldiers.Count == 8, "8 soldier types");
        Check(EnemyDatabase.Bosses.Count >= 27, $"at least 27 bosses (got {EnemyDatabase.Bosses.Count})");
        Check(enemyIds.Distinct().Count() == enemyIds.Count, "enemy ids unique");
        Check(enemyIds.All(id => !heroIds.Contains(id)), "enemy ids never collide with hero ids");
        Check(enemyIds.All(id => TavernData.TierOf(id) == null), "enemies are not in the tavern pool");
        Check(EnemyDatabase.All.All(e => e.Hp > 0 && e.Atk > 0 && e.Def > 0 && e.MagicDef > 0 && e.Speed > 0
            && e.NormalName.Length > 0 && e.UltName.Length > 0 && e.PassiveName.Length > 0), "every enemy has stats + skills");
        foreach (var name in new[] { "Trương Lương", "Trương Bảo", "Trương Giác", "Vu Cát", "Hoa Hùng", "Lý Giác", "Quách Dĩ",
                     "Đổng Trác", "Nhan Lương", "Văn Xú", "Viên Thiệu", "Hạ Hầu Đôn", "Hứa Chử", "Tào Nhân", "Tào Tháo",
                     "Nghiêm Nhan", "Trương Nhiệm", "Pháp Chính", "Mạnh Hoạch", "Tả Hiền Vương", "Vu Phu La", "Thiền Vu",
                     "Lục Tốn", "Lữ Mông" })
            Check(EnemyDatabase.Bosses.Any(b => b.Name == name), $"boss {name} exists");
        Check(HeroDatabase.GetCombatant("quan_vu") == HeroDatabase.Get("quan_vu"), "GetCombatant finds playable heroes first");
        Check(HeroDatabase.GetCombatant("truong_giac").Name == "Trương Giác", "GetCombatant finds bosses");
        bool threw = false;
        try { HeroDatabase.GetCombatant("khong_co"); } catch (System.ArgumentException) { threw = true; }
        Check(threw, "unknown combatant id throws");
        var save = SeedService.CreateSeededSave("t240");
        Check(!save.Heroes.Keys.Any(k => enemyIds.Contains(k)), "enemies never enter the save");
    }

    private static StageDef DemoStage() => new()
    {
        Id = 999, Name = "Demo Khăn Vàng", Scale = 1.0, BossId = "truong_giac",
        EnemyIds = new() { "hc_binh", "truong_giac", "hc_binh" },
    };

    private void CheckBattle()
    {
        var stage = DemoStage();
        var e = new BattleEngine(new List<string> { "quan_vu", "truong_phi", "gia_cat_luong" }, stage, 77);
        var boss = e.EnemyFighters[1];
        var giac = EnemyDatabase.Find("truong_giac")!;
        Check(boss.Name == "Trương Giác" && boss.MaxHp == (int)System.Math.Round(giac.Hp * 1.0 * 1.3), $"boss HP = base x Scale x BossScale ({boss.MaxHp})");
        Check(e.EnemyFighters[0].MaxHp == EnemyDatabase.Find("hc_binh")!.Hp, "non-boss enemies only get Scale");
        e.AutoRun();
        Check(e.IsOver, "battle with a boss finishes");
        Check(e.Log.Any(l => l.Contains("Trương Giác")), "log mentions the boss");
        var e2 = new BattleEngine(new List<string> { "quan_vu", "truong_phi", "gia_cat_luong" }, DemoStage(), 77);
        e2.AutoRun();
        Check(e2.PlayerWon == e.PlayerWon && e2.TurnCount == e.TurnCount && e2.Log.SequenceEqual(e.Log), "same seed => same battle");
        GD.Print($"demo: Trương Giác stage won={e.PlayerWon} turns={e.TurnCount}");
        // A stage with no BossId behaves exactly as before (Scale only).
        var plain = new StageDef { Id = 998, Name = "x", Scale = 1.5, EnemyIds = new() { "lu_bo" } };
        Check(new BattleEngine(new List<string> { "quan_vu" }, plain, 1).EnemyFighters[0].MaxHp == HeroDatabase.Get("lu_bo").WithScale(1.5).Hp,
            "stage without boss keeps Scale only");
    }
}
