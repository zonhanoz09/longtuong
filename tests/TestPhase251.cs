using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.51 — Tổ Đội screen + the 3-team boss battle in BattleScene:
/// teams switch, the boss HP carries over, the result matches
/// PartyService.Simulate for the same seed, rewards on a win only.
/// </summary>
public partial class TestPhase251 : Node
{
    private int _fails;

    public override void _Ready()
    {
        CheckScreen();
        CheckBattle(win: true);
        CheckBattle(win: false);
        CheckActivityCard();

        if (_fails == 0) GD.Print("TEST PHASE 2.51: ALL PASS");
        else GD.Print($"TEST PHASE 2.51: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private GameManager Setup(string name, int level)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave(name);
        gm.Save.LastStage = 38;
        gm.Save.OwnedHeroIds.AddRange(new[] { "quan_vu", "trieu_van", "gia_cat_luong", "lu_bo" });
        foreach (var id in gm.Save.OwnedHeroIds) gm.Save.Heroes[id].Level = level;
        FormationService.EnsureFormation(gm.Save);
        FormationService.TryPlace(gm.Save, "quan_vu", "hau_top", null, out _);
        FormationService.TryPlace(gm.Save, "trieu_van", "tien_mid", null, out _);
        FormationService.TryPlace(gm.Save, "gia_cat_luong", "hau_mid", null, out _);
        FormationService.TryPlace(gm.Save, "lu_bo", "trung_top", null, out _);
        return gm;
    }

    private PartyScene OpenScreen()
    {
        var ui = ResourceLoader.Load<PackedScene>("res://features/party/scenes/PartyScene.tscn").Instantiate<PartyScene>();
        AddChild(ui);
        return ui;
    }

    private void CheckScreen()
    {
        var gm = Setup("t251a", 40);
        var ui = OpenScreen();
        var bosses = ui.GetNode<VBoxContainer>("BossList").GetChildren().OfType<Button>().ToList();
        Check(bosses.Count == 4 && !bosses[0].Disabled && bosses.Skip(1).All(b => b.Disabled && b.Text.Contains("KHÓA")), "4 bosses, only Trương Giác open");
        Check(bosses[1].TooltipText == "Cần đánh bại Trương Giác trước", "locked boss tooltip");
        ui.SetSeed(77);
        var info = ui.GetNode<Label>("InfoLabel").Text;
        var bot1 = PartyService.BuildBotTeam(gm.Save, 1, 77);
        Check(info.Contains("Đội 1/3 (Bạn)") && info.Contains("Đội 2/3 (Bot)") && info.Contains("Đội 3/3 (Bot)"), "preview lists the 3 teams");
        Check(info.Contains(string.Join(", ", bot1.Heroes.Select(h => h.Name))), "bot preview matches the seed");
        Check(info.Contains("5 Mảnh Chu Tước") && info.Contains("100 Chiến tích"), "reward line");
        Check(!ui.GetNode<Button>("GoButton").Disabled, "Xuất chinh enabled");
        Check(ui.PrepareBattle() && gm.CurrentMode == BattleMode.Party && gm.CurrentModeKey == "truong_giac" && gm.CurrentSeed == 77, "Xuất chinh starts a Party battle with the preview seed");
        ui.QueueFree();
        gm.Save.PartyCleared["truong_giac"] = 1;
        var ui2 = OpenScreen();
        Check(ui2.SelectedBossId == "lu_bo" && !ui2.GetNode<VBoxContainer>("BossList").GetChildren().OfType<Button>().ToList()[1].Disabled, "after beating Trương Giác, Lữ Bố opens and is selected");
        ui2.QueueFree();
    }

    private void CheckBattle(bool win)
    {
        // Lv200 teams win; Lv1 teams cannot.
        var gm = Setup(win ? "t251b" : "t251c", win ? 200 : 1);
        gm.Save.Heroes["custom_hero"].Level = win ? 200 : 40;
        int seed = 31;
        var expected = PartyService.Simulate(gm.Save, "truong_giac", seed);
        var team = FormationService.FormationToTeam(gm.Save.Formation).Where(id => gm.Save.OwnedHeroIds.Contains(id)).ToList();
        gm.StartModeBattle(BattleMode.Party, "truong_giac", PartyService.BossStage("truong_giac"), team, seed);
        int silver = gm.Save.Silver, merit = gm.Save.Merit, frags = ForgeService.FragmentCount(gm.Save, "chu_tuoc");
        var scene = ResourceLoader.Load<PackedScene>("res://features/battle/scenes/BattleScene.tscn").Instantiate<BattleScene>();
        AddChild(scene);
        var label = scene.GetNodeOrNull<Label>("PartyLabel");
        Check(label != null && label.Text == "Đội 1/3 (Bạn)", "starts with Đội 1/3 (Bạn)");
        Check(scene.GetNodeOrNull<Label>("BossHpLabel")?.Text.Contains($"/ {expected.BossMaxHp:N0}") == true, "boss HP label shows the max HP");
        var labels = new HashSet<string>();
        int switches = 0, lastIndex = 0;
        bool finished = false;
        for (int i = 0; i < 2000 && !finished; i++)
        {
            scene.StepBattle();
            if (scene.PartyTeamIndex != lastIndex) { switches++; lastIndex = scene.PartyTeamIndex; }
            if (label != null) labels.Add(label.Text);
            finished = scene.GetNodeOrNull<Control>("ResultPopup") != null;
        }
        Check(finished, "Tổ Đội battle reaches the result popup");
        var runs = scene.PartyRuns;
        Check(runs.Count == expected.Runs.Count && runs.Select(r => r.BossHpAfter).SequenceEqual(expected.Runs.Select(r => r.BossHpAfter)),
            $"battle screen matches PartyService.Simulate ({string.Join(",", runs.Select(r => r.BossHpAfter))} vs {string.Join(",", expected.Runs.Select(r => r.BossHpAfter))})");
        Check(Enumerable.Range(1, runs.Count - 1).All(i => runs[i].BossHpBefore == runs[i - 1].BossHpAfter), "boss HP carries over between teams");
        Check(switches == runs.Count - 1 && labels.Count == runs.Count, "one team switch per extra run, labels change");
        Check(gm.LastBattleWon == expected.Won && expected.Won == win, $"outcome = {(win ? "win" : "loss")}");
        Check(scene.FindChild("PartyDamageLabel", true, false) is Label d && d.Text.Split('\n').Length == runs.Count, "popup lists damage per team");
        if (win)
        {
            Check(gm.Save.Silver == silver + 5000 && gm.Save.Merit == merit + 100 && ForgeService.FragmentCount(gm.Save, "chu_tuoc") == frags + 5, "win: rewards applied");
            Check(PartyService.IsBossOpen(gm.Save, "lu_bo"), "win opens the next boss");
            Check(scene.FindChild("PartyChestLabel", true, false) != null, "win popup shows Rương chiến công");
        }
        else
        {
            Check(runs.Count == 3, "a loss uses all 3 teams");
            Check(gm.Save.Silver == silver && gm.Save.Merit == merit && ForgeService.FragmentCount(gm.Save, "chu_tuoc") == frags, "loss: no reward");
            Check(!PartyService.IsBossOpen(gm.Save, "lu_bo"), "loss keeps the next boss locked");
        }
        scene.QueueFree();
    }

    private void CheckActivityCard()
    {
        var gm = Setup("t251d", 40);
        var hub = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn").Instantiate<HubScene>();
        AddChild(hub);
        hub.GetNode<Button>("ActivityButtonIcon/ActivityButton").EmitSignal(BaseButton.SignalName.Pressed);
        var content = hub.GetNode<Control>("PopupFrame/PopupContent");
        (content.GetChild(0) as ActivityScene)!.GetNode<Button>("CardGrid/Card_Party").EmitSignal(BaseButton.SignalName.Pressed);
        Check(content.GetChildren().OfType<PartyScene>().FirstOrDefault()?.Size == HubScene.PartyPopupSize, "Tổ Đội card opens the 860x540 popup");
        hub.ClosePopup();
        hub.QueueFree();
    }
}
