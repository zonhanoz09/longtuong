using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.41 — campaign: 8 story chapters, own stage counts (70 stages),
/// global stage ids, Scale/enemy-count rules kept from 2.39, bosses.
/// </summary>
public partial class TestPhase241 : Node
{
    private int _fails;

    public override void _Ready()
    {
        CheckData();
        CheckOldSaves();
        CheckStageSelectUi();

        if (_fails == 0) GD.Print("TEST PHASE 2.41: ALL PASS");
        else GD.Print($"TEST PHASE 2.41: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    // Scale of ải 1–20 before 2.41 (2.39 table).
    private static readonly double[] OldScales =
        { 0.85, 0.94, 1.03, 1.12, 1.21, 1.30, 1.39, 1.48, 1.57, 1.66, 1.75, 1.84, 1.93, 2.02, 2.11, 2.20, 2.29, 2.38, 2.47, 2.56 };

    private void CheckData()
    {
        var chapters = HeroDatabase.Chapters;
        Check(chapters.Count == 8, "8 chapters");
        Check(chapters.Select(c => c.LastStage - c.FirstStage + 1).SequenceEqual(new[] { 6, 8, 6, 10, 8, 10, 12, 10 }), "stage counts 6/8/6/10/8/10/12/10");
        Check(chapters.Select(c => c.Name).SequenceEqual(new[] { "Loạn Hoàng Cân", "Thảo Phạt Đổng Trác", "Trường An & Lạc Dương",
            "Quan Độ & Ô Sào", "Tân Dã & Kinh Châu", "Đại Chiến Xích Bích", "Bình Định Tây Xuyên & Thất Cầm Mạnh Hoạch", "Hán Trung Tranh Bá" }),
            "chapter names follow the story");
        Check(HeroDatabase.Stages.Count == 70 && HeroDatabase.Stages.Select(s => s.Id).SequenceEqual(Enumerable.Range(1, 70)), "70 stages with contiguous ids");
        bool contiguous = chapters[0].FirstStage == 1 && chapters.Zip(chapters.Skip(1)).All(p => p.Second.FirstStage == p.First.LastStage + 1);
        Check(contiguous && chapters[^1].LastStage == 70, "chapter ranges are contiguous");
        Check(HeroDatabase.Stages.All(s => s.Id >= chapters[s.Chapter - 1].FirstStage && s.Id <= chapters[s.Chapter - 1].LastStage), "stage.Chapter matches its range");
        Check(Enumerable.Range(0, 20).All(i => System.Math.Abs(HeroDatabase.Stages[i].Scale - OldScales[i]) < 1e-9), "ải 1–20 keep their old Scale");
        bool up = true;
        for (int i = 1; i < 70; i++) if (HeroDatabase.Stages[i].Scale <= HeroDatabase.Stages[i - 1].Scale) up = false;
        Check(up, "Scale strictly increases");
        Check(HeroDatabase.Stages.All(s => s.EnemyIds.Count == (s.Id <= 10 ? 3 : s.Id <= 15 ? 4 : 5)), "enemy count 3/4/5 by stage id");
        Check(HeroDatabase.Stages.All(s => s.EnemyIds.All(id => id != "custom_hero")), "custom hero never an enemy");
        Check(HeroDatabase.Stages.All(s => s.EnemyIds.All(id => { try { HeroDatabase.GetCombatant(id); return true; } catch { return false; } })), "every enemy id resolves");
        Check(HeroDatabase.Stages.All(s => s.BossId == null || s.EnemyIds.Contains(s.BossId)), "boss stands in its stage");
        Check(chapters.All(c => HeroDatabase.Stages.Any(s => s.Chapter == c.Number && s.BossId != null)), "every chapter has a boss");
        Check(HeroDatabase.GetStage(70).BossId == "tao_thao", "ải 70 boss is Tào Tháo");
        Check(HeroDatabase.GetStage(14).BossId == "lu_bo" && HeroDatabase.GetStage(20).BossId == "dong_trac"
            && HeroDatabase.GetStage(30).BossId == "vien_thieu" && HeroDatabase.GetStage(48).BossId == "tao_thao", "milestone bosses (Lữ Bố 14, Đổng Trác 20, Viên Thiệu 30, Tào Tháo 48)");
        Check(HeroDatabase.Stages.Any(s => s.Chapter == 7 && s.EnemyIds.Contains("manh_hoach") && s.EnemyIds.Contains("gia_cat_luong")), "chapter 7 has Gia Cát Lượng + Mạnh Hoạch");
        Check(HeroDatabase.GetStage(0).Id == 1 && HeroDatabase.GetStage(999).Id == 70, "GetStage clamps out-of-range ids");
        // Every stage battle builds (enemy ids resolve, slots exist).
        bool builds = HeroDatabase.Stages.All(s => new BattleEngine(new List<string> { "quan_vu" }, s, 1).EnemyFighters.Count == s.EnemyIds.Count);
        Check(builds, "every stage builds a battle");
    }

    private void CheckOldSaves()
    {
        var s = SeedService.CreateSeededSave("t241a");
        s.LastStage = 20;
        Check(HeroDatabase.ChapterCleared(s, 3) && !HeroDatabase.ChapterCleared(s, 4), "old save at ải 20 has cleared chapter 3");
        Check(HeroDatabase.GetStage(s.LastStage + 1).Chapter == 4, "next stage after 20 is chapter 4 (Quan Độ)");
        s.LastStage = 5;
        Check(!HeroDatabase.ChapterCleared(s, 1), "chapter 1 not cleared at ải 5");
        s.LastStage = 999;
        ProgressionService.EnsureDefaults(s);
        Check(s.LastStage == 70, "EnsureDefaults clamps LastStage to 70");
    }

    private void CheckStageSelectUi()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("t241b");
        gm.Save.LastStage = 20;
        gm.CurrentStageId = 21;
        var ui = ResourceLoader.Load<PackedScene>("res://features/stage/scenes/StageSelect.tscn").Instantiate<StageSelect>();
        AddChild(ui);
        var box = ui.GetNode<VBoxContainer>("StageBox");
        var tabs = box.FindChild("ChapterTabs", true, false) as GridContainer;
        Check(tabs != null && tabs.GetChildCount() == 8, "8 chapter tabs");
        Check(box.FindChild("Chapter_4", true, false) is Button { Disabled: false } && box.FindChild("Chapter_5", true, false) is Button { Disabled: true },
            "old save at ải 20 opens chapter 4, chapter 5 locked");
        Check(box.FindChild("Stage_21", true, false) is Button { ButtonPressed: true, Disabled: false }, "opens on ải 21 (Quan Độ)");
        Check(box.GetChildren().OfType<Button>().Count() == 10, "chapter 4 lists 10 stages");
        Check((box.FindChild("ChapterHeader", true, false) as Label)?.Text.Contains("Quan Độ") == true, "header shows the chapter name");
        gm.Save.LastStage = 70;
        ui.SelectChapter(7);
        Check(box.GetChildren().OfType<Button>().Count() == 12, "chapter 7 lists 12 stages");
        ui.QueueFree();
    }
}
