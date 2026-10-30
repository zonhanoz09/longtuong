using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.46 — Phó Bản screen: 5 dungeons (level-locked), Thường/Khó,
/// 4 stages with bosses, fragment info; Vào trận starts a Dungeon battle;
/// the Hoạt Động card opens this screen; the result popup shows fragments.
/// </summary>
public partial class TestPhase246 : Node
{
    private int _fails;

    public override void _Ready()
    {
        CheckScreen();
        CheckActivityCard();
        CheckBattleFlow();

        if (_fails == 0) GD.Print("TEST PHASE 2.46: ALL PASS");
        else GD.Print($"TEST PHASE 2.46: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private GameManager SetupAccount(string name, int level)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave(name);
        gm.Save.LastStage = 14;
        gm.Save.Heroes["custom_hero"].Level = level;
        return gm;
    }

    private DungeonScene OpenScreen()
    {
        var ui = ResourceLoader.Load<PackedScene>("res://features/dungeon/scenes/DungeonScene.tscn").Instantiate<DungeonScene>();
        AddChild(ui);
        return ui;
    }

    private void CheckScreen()
    {
        var gm = SetupAccount("t246a", 50);
        var ui = OpenScreen();
        var list = ui.GetNode<VBoxContainer>("DungeonList").GetChildren().OfType<Button>().ToList();
        Check(list.Count == 5, "5 dungeons listed");
        Check(list.Take(2).All(b => !b.Disabled) && list.Skip(2).All(b => b.Disabled && b.Text.Contains("KHÓA")), "Lv50: Cổ Mộ + Hung Nô open, the rest locked");
        Check(list[2].TooltipText == "Cần cấp chủ tướng Lv70", "locked dungeon tooltip shows the level");
        var hard = ui.FindChild("DiffHard", true, false) as Button;
        var normal = ui.FindChild("DiffNormal", true, false) as Button;
        Check(normal != null && !normal.Disabled && hard != null && hard.Disabled, "Khó disabled before Thường is cleared");
        var info = ui.GetNode<Label>("InfoLabel").Text;
        Check(info.Contains("Cổ Mộ Hán Trung") && info.Contains("Mảnh Chu Tước") && info.Contains("BOSS Trương Giác"), "info shows dungeon, fragment set and boss");
        Check(info.Split('\n').Count(l => l.Contains("Ải ")) == 4, "info lists 4 stages");
        Check(ui.GetNode<Button>("EnterButton") is { Disabled: false } b && b.Text.Contains("1/4"), "Vào trận enabled on stage 1/4");
        ((Button)list[1]).EmitSignal(BaseButton.SignalName.Pressed);
        Check(ui.SelectedDungeonId == "hung_no" && ui.GetNode<Label>("InfoLabel").Text.Contains("Mảnh Bạch Hổ"), "selecting Hung Nô shows Bạch Hổ fragments");
        ui.QueueFree();

        gm.Save.DungeonProgress["co_mo:normal:done"] = 1;
        gm.Save.DungeonProgress["co_mo:normal"] = 2;
        var ui2 = OpenScreen();
        Check(ui2.FindChild("DiffHard", true, false) is Button { Disabled: false }, "Khó enabled after Thường is cleared");
        Check(ui2.GetNode<Button>("EnterButton").Text.Contains("3/4"), "Vào trận shows the current stage (3/4)");
        ui2.Select("co_mo", DungeonDifficulty.Hard);
        Check(ui2.SelectedDifficulty == DungeonDifficulty.Hard && ui2.GetNode<Label>("InfoLabel").Text.Contains("Khó")
            && ui2.GetNode<Label>("InfoLabel").Text.Contains("[6 mảnh]"), "Khó selected: boss gives 6 fragments");
        ui2.QueueFree();

        var gmLow = SetupAccount("t246b", 20);
        var ui3 = OpenScreen();
        Check(ui3.GetNode<VBoxContainer>("DungeonList").GetChildren().OfType<Button>().All(x => x.Disabled), "Lv20: every dungeon locked");
        Check(ui3.GetNode<Button>("EnterButton").Disabled && !ui3.PrepareBattle(), "Vào trận disabled and PrepareBattle refuses");
        ui3.QueueFree();
    }

    private void CheckActivityCard()
    {
        var gm = SetupAccount("t246c", 30);
        var hub = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn").Instantiate<HubScene>();
        AddChild(hub);
        hub.GetNode<Button>("TopBar/ActivityButton").EmitSignal(BaseButton.SignalName.Pressed);
        var content = hub.GetNode<Control>("PopupFrame/PopupContent");
        var activity = content.GetChild(0) as ActivityScene;
        var card = activity?.GetNode<Button>("CardGrid/Card_Dungeon");
        Check(card != null && !card.Disabled, "chapter 2 + Lv30: Phó Bản card open");
        card!.EmitSignal(BaseButton.SignalName.Pressed);
        var dungeon = content.GetChildren().OfType<DungeonScene>().FirstOrDefault();
        Check(hub.IsPopupOpen && content.GetChildCount() == 1 && dungeon != null && dungeon.Size == HubScene.DungeonPopupSize,
            "the card replaces Hoạt Động with the 920x600 Phó Bản popup");
        hub.ClosePopup();
        hub.QueueFree();
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

    private void CheckBattleFlow()
    {
        var gm = SetupAccount("t246d", 30);
        gm.Save.OwnedHeroIds.AddRange(new[] { "quan_vu", "trieu_van", "gia_cat_luong", "lu_bo" });
        foreach (var id in gm.Save.OwnedHeroIds) gm.Save.Heroes[id].Level = 150;
        FormationService.EnsureFormation(gm.Save);
        FormationService.TryPlace(gm.Save, "quan_vu", "hau_top", null, out _);
        FormationService.TryPlace(gm.Save, "trieu_van", "tien_mid", null, out _);
        FormationService.TryPlace(gm.Save, "gia_cat_luong", "hau_mid", null, out _);
        FormationService.TryPlace(gm.Save, "lu_bo", "trung_top", null, out _);
        var ui = OpenScreen();
        Check(ui.PrepareBattle(), "PrepareBattle starts a battle");
        Check(gm.CurrentMode == BattleMode.Dungeon && gm.CurrentModeKey == "co_mo:normal:0" && gm.CurrentStage != null
            && gm.CurrentStage.EnemyIds.SequenceEqual(DungeonDatabase.Get("co_mo").Stages[0].EnemyIds), "Mode = Dungeon on Cổ Mộ stage 1");
        Check(gm.SelectedTeamIds.Count == 5 && gm.SelectedTeamIds.Contains("custom_hero"), "the formation team goes into battle");
        ui.QueueFree();
        int frags = ForgeService.FragmentCount(gm.Save, "chu_tuoc");
        var scene = ResourceLoader.Load<PackedScene>("res://features/battle/scenes/BattleScene.tscn").Instantiate<BattleScene>();
        AddChild(scene);
        Check(RunToEnd(scene), "dungeon battle finishes");
        Check(gm.LastBattleWon, "a Lv150 team wins Cổ Mộ stage 1");
        Check(ForgeService.FragmentCount(gm.Save, "chu_tuoc") == frags + 1, "+1 Mảnh Chu Tước after the win");
        var squares = scene.FindChild("RewardSquares", true, false) as HBoxContainer;
        var dropNames = squares?.GetChildren().Select(c => ((Node)c).FindChild("DropLabel", true, false) as Label).Where(l => l != null).Select(l => l!.Text).ToList() ?? new();
        Check(dropNames.Contains("Mảnh Chu Tước"), "result popup shows the fragment square");
        Check(DungeonService.NextStageIndex(gm.Save, DungeonDatabase.Get("co_mo"), DungeonDifficulty.Normal) == 1, "the run moves on to stage 2");
        scene.QueueFree();
    }
}
