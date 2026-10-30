using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.43 — 8 new tavern heroes (one per campaign chapter), locked in
/// the tavern until their chapter is cleared; legacy saves still own only
/// the 8 original heroes.
/// </summary>
public partial class TestPhase243 : Node
{
    private int _fails;

    private static readonly (string Id, string Name, HeroRole Role, TavernTier Tier, int Chapter)[] NewHeroes =
    {
        ("ton_kien", "Tôn Kiên", HeroRole.Warrior, TavernTier.Lam, 1),
        ("dien_vi", "Điển Vi", HeroRole.Vanguard, TavernTier.Lam, 2),
        ("gia_hu", "Giả Hủ", HeroRole.Strategist, TavernTier.Tim, 3),
        ("truong_lieu", "Trương Liêu", HeroRole.Warrior, TavernTier.Tim, 4),
        ("tu_thu", "Từ Thứ", HeroRole.Support, TavernTier.Tim, 5),
        ("chu_du", "Chu Du", HeroRole.Strategist, TavernTier.Cam, 6),
        ("chuc_dung", "Chúc Dung", HeroRole.Warrior, TavernTier.Cam, 7),
        ("ma_sieu", "Mã Siêu", HeroRole.Warrior, TavernTier.Cam, 8),
    };

    public override void _Ready()
    {
        CheckHeroes();
        CheckPoolGating();
        CheckLegacySaves();
        CheckRecruit();
        CheckRecruitUi();

        if (_fails == 0) GD.Print("TEST PHASE 2.43: ALL PASS");
        else GD.Print($"TEST PHASE 2.43: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private static SaveData Account(string name, int lastStage = 0)
    {
        var s = SeedService.CreateSeededSave(name);
        s.LastStage = lastStage;
        return s;
    }

    private void CheckHeroes()
    {
        Check(HeroDatabase.Heroes.Count == 16, "16 heroes");
        foreach (var (id, name, role, tier, chapter) in NewHeroes)
        {
            var h = HeroDatabase.Get(id);
            Check(h.Name == name && h.Role == role, $"{name}: name + role");
            Check(TavernData.TierOf(id) == tier, $"{name}: tavern tier {tier}");
            Check(TavernData.UnlockChapter[id] == chapter, $"{name}: unlocks with chapter {chapter}");
            Check(h.Hp > 0 && h.Atk > 0 && h.Def > 0 && h.MagicDef > 0 && h.Speed > 0 && h.NormalName.Length > 0 && h.UltName.Length > 0
                && h.PassiveName.Length > 0 && h.PassiveDesc.Length > 0, $"{name}: stats + skills");
        }
        Check(HeroDatabase.Get("chu_du").UltKind == UltKind.AoEDamage && HeroDatabase.Get("gia_hu").UltKind == UltKind.AoEDamage, "Chu Du / Giả Hủ: AoE ult");
        Check(HeroDatabase.Get("tu_thu").UltKind == UltKind.Heal, "Từ Thứ: heal ult");
        Check(HeroDatabase.Heroes.Select(h => h.Id).Distinct().Count() == 16, "hero ids unique");
        Check(HeroDatabase.Heroes.All(h => EnemyDatabase.Find(h.Id) == null), "no hero id collides with an enemy id");
        Check(AssetIcons.PortraitChar("ma_sieu") == "Siêu", "portrait char works for new heroes");
        // New heroes can fight.
        var e = new BattleEngine(new List<string> { "ma_sieu", "chu_du", "tu_thu" }, HeroDatabase.GetStage(1), 5);
        e.AutoRun();
        Check(e.IsOver, "a team of new heroes finishes a battle");
    }

    private void CheckPoolGating()
    {
        var s = Account("t243a");
        var legacyPool = new[] { "luu_bi", "truong_phi", "quan_vu", "trieu_van", "dieu_thuyen", "gia_cat_luong", "lu_bo" };
        Check(NewHeroes.All(n => !TavernData.IsInPool(s, n.Id)), "new account: no new hero in the pool");
        Check(legacyPool.All(id => TavernData.IsInPool(s, id)), "new account: the original 7 are in the pool");
        var all3 = new[] { TavernTier.Lam, TavernTier.Tim, TavernTier.Cam }.SelectMany(t => TavernData.HeroesOfTier(s, t)).ToList();
        Check(all3.Count == 7 && all3.All(id => legacyPool.Contains(id)), "new account tier pools are exactly the original 7");

        // Chapter N cleared => exactly the heroes of chapters 1..N are open.
        for (int ch = 1; ch <= 8; ch++)
        {
            var sc = Account("t243b", HeroDatabase.Chapters[ch - 1].LastStage);
            bool ok = NewHeroes.All(n => TavernData.IsInPool(sc, n.Id) == (n.Chapter <= ch));
            Check(ok, $"clearing chapter {ch} opens exactly heroes of chapters 1..{ch}");
        }
        var s1 = Account("t243c", HeroDatabase.Chapters[0].LastStage - 1);
        Check(!TavernData.IsInPool(s1, "ton_kien"), "one stage short of chapter 1: Tôn Kiên still locked");

        // 1000 draws never return a locked hero (after chapter 1 only Tôn Kiên is new).
        var s2 = Account("t243d", HeroDatabase.Chapters[0].LastStage);
        s2.Heroes["custom_hero"].Level = 100; // Cam gate open
        var rng = new System.Random(2024);
        int tonKien = 0;
        bool lockedDrawn = false;
        for (int i = 0; i < 1000; i++)
        {
            var id = TavernService.DrawHero(s2, i % 2 == 0 ? TavernGrade.Normal : TavernGrade.High, rng);
            if (id == "ton_kien") tonKien++;
            if (!TavernData.IsInPool(s2, id)) lockedDrawn = true;
        }
        Check(!lockedDrawn, "1000 draws never return a locked hero");
        Check(tonKien > 50, $"unlocked Tôn Kiên is actually drawn (got {tonKien}/1000)");
        var s3 = Account("t243e");
        s3.Heroes["custom_hero"].Level = 100;
        bool anyNew = false;
        for (int i = 0; i < 1000; i++)
            if (NewHeroes.Any(n => n.Id == TavernService.DrawHero(s3, TavernGrade.High, rng))) anyNew = true;
        Check(!anyNew, "new account never draws a new hero (1000 High draws at Lv100)");
    }

    private void CheckLegacySaves()
    {
        var legacy = new SaveData { OwnedHeroIds = new List<string>() };
        ProgressionService.EnsureDefaults(legacy);
        Check(legacy.OwnedHeroIds.Count == 8 && NewHeroes.All(n => !legacy.OwnedHeroIds.Contains(n.Id)), "empty legacy save owns the 8 original, not the new heroes");
        Check(NewHeroes.All(n => legacy.Heroes.ContainsKey(n.Id) && legacy.HeroEquipment.ContainsKey(n.Id)), "EnsureDefaults still fills progress + equipment for new heroes");
        var seeded = Account("t243f");
        Check(seeded.OwnedHeroIds.Count == 1 && seeded.OwnedHeroIds[0] == "custom_hero", "seeded account owns only the custom hero");
    }

    private void CheckRecruit()
    {
        var s = Account("t243g");
        s.TierSouls["Lam"] = 100;
        Check(!TavernService.CanRecruit(s, "ton_kien") && !TavernService.Recruit(s, "ton_kien") && s.TierSouls["Lam"] == 100,
            "locked hero cannot be recruited even with enough souls (no charge)");
        s.LastStage = HeroDatabase.Chapters[0].LastStage;
        Check(TavernService.CanRecruit(s, "ton_kien"), "after chapter 1 + 100 Lam souls: Tôn Kiên can be recruited");
        Check(TavernService.Recruit(s, "ton_kien") && s.OwnedHeroIds.Contains("ton_kien") && s.TierSouls["Lam"] == 0, "recruit Tôn Kiên spends 100 Lam souls");
        Check(!TavernService.CanRecruit(s, "ton_kien"), "already owned");
        Check(!TavernService.CanRecruit(s, "dien_vi"), "Điển Vi (chapter 2) still locked");
    }

    private void CheckRecruitUi()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("t243h");
        gm.Save.LastStage = HeroDatabase.Chapters[0].LastStage;
        gm.Save.TierSouls["Lam"] = 100;
        var ui = ResourceLoader.Load<PackedScene>("res://features/recruit/scenes/RecruitScene.tscn").Instantiate<RecruitScene>();
        AddChild(ui);
        var box = ui.GetNode<Control>("RecruitBox");
        // 2.54: RecruitBox cuộn được, card tướng nằm trong RecruitScroll/RecruitContent.
        var content = box.GetNode<Control>("RecruitScroll/RecruitContent");
        Button? Card(string heroName) => content.GetChildren().OfType<Button>().FirstOrDefault(b => b.GetNodeOrNull<Label>("NameLabel")?.Text == heroName);
        Check(content.GetChildren().OfType<Button>().Count(b => b.GetNodeOrNull<Label>("NameLabel") != null) == 15, "tavern grid lists all 15 heroes");
        var tk = Card("Tôn Kiên");
        var dv = Card("Điển Vi");
        Check(tk != null && !tk.Disabled && tk.GetNode<Label>("OwnedLabel").Visible == false && tk.GetNode<Label>("PriceLabel").Visible,
            "unlocked Tôn Kiên card is pressable and shows its price");
        Check(dv != null && dv.Disabled && dv.GetNode<Label>("OwnedLabel") is { Visible: true, Text: "Qua chương 2" }
            && !dv.GetNode<Label>("PriceLabel").Visible, "locked Điển Vi card is grey with 'Qua chương 2'");
        var maxX = content.GetChildren().OfType<Button>().Max(b => b.Position.X + b.Size.X);
        Check(maxX <= 980, $"cards fit the grid width (right edge {maxX})");
        tk!.EmitSignal(BaseButton.SignalName.Pressed);
        Check(gm.Save.OwnedHeroIds.Contains("ton_kien"), "pressing Tôn Kiên recruits him");
        Check(Card("Tôn Kiên")!.GetNode<Label>("OwnedLabel") is { Visible: true, Text: "đã chiêu mộ" }, "owned card shows 'đã chiêu mộ'");
        ui.QueueFree();
    }
}
