using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.21 tests: battle screen "ĐẤU TRƯỜNG" — fully automatic turn by
/// turn, speed button x1 → x2 → x3 → x1, mount placeholders, formation
/// grids (player formation, temporary enemy layout), no battle log and no
/// manual attack buttons, and the win/lose result popup (5 empty reward
/// squares on win, Thoát only on loss).
/// </summary>
public partial class TestPhase221 : Node
{
    private int _passed;
    private int _failed;

    private void Check(bool cond, string name)
    {
        if (cond) { _passed++; GD.Print($"[PASS] {name}"); }
        else { _failed++; GD.PrintErr($"[FAIL] {name}"); }
    }

    // Step a battle scene until its result popup appears (or give up).
    private static bool RunToEnd(BattleScene scene)
    {
        for (int i = 0; i < 1000; i++)
        {
            scene.StepBattle();
            if (scene.GetNodeOrNull<Control>("ResultPopup") != null) return true;
        }
        return false;
    }

    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");

        // --- Win case: a strong 5-hero formation vs stage 1 ---
        gm.CreateSeededSave("Tester221");
        gm.Save.OwnedHeroIds.AddRange(new[] { "quan_vu", "truong_phi", "trieu_van", "gia_cat_luong", "lu_bo" });
        FormationService.EnsureFormation(gm.Save);
        FormationService.TryPlace(gm.Save, "quan_vu", "hau_top", null, out _);
        FormationService.TryPlace(gm.Save, "trieu_van", "tien_mid", null, out _);
        FormationService.TryPlace(gm.Save, "gia_cat_luong", "hau_mid", null, out _);
        FormationService.TryPlace(gm.Save, "lu_bo", "trung_top", null, out _);
        var team = FormationService.FormationToTeam(gm.Save.Formation);
        gm.SelectedTeamIds = new List<string>(team);
        gm.StartBattle(1, new List<string>(team), 12345);
        int goldBefore = gm.Save.Gold;

        var scene = ResourceLoader.Load<PackedScene>("res://features/battle/scenes/BattleScene.tscn").Instantiate<BattleScene>();
        AddChild(scene);

        Check(scene.GetNodeOrNull<Label>("TitleLabel")?.Text == "ĐẤU TRƯỜNG", "2.21: title is DAU TRUONG");
        Check(scene.GetNodeOrNull<PanelContainer>("PlayerMount")?.GetNode<Label>("MountLabel").Text == "Thú cưỡi",
            "2.21: player mount placeholder");
        Check(scene.GetNodeOrNull<PanelContainer>("EnemyMount")?.GetNode<Label>("MountLabel").Text == "Thú cưỡi",
            "2.21: enemy mount placeholder");
        Check(scene.GetNodeOrNull<Label>("LogLabel") == null, "2.21: battle log removed");
        Check(scene.GetNodeOrNull<Button>("NormalButton") == null, "2.21: no manual normal button");
        Check(scene.GetNodeOrNull<Button>("UltButton") == null, "2.21: no manual ult button");
        Check(scene.GetNodeOrNull<Button>("AutoButton") == null, "2.21: no auto-run button");
        var playerGrid = scene.GetNode<GridContainer>("PlayerGrid");
        var enemyGrid = scene.GetNode<GridContainer>("EnemyGrid");
        Check(playerGrid.GetChildCount() == 9, "2.21: player grid has 9 cells");
        Check(enemyGrid.GetChildCount() == 9, "2.21: enemy grid has 9 cells");
        Check(playerGrid.GetChildren().OfType<BattleHeroCell>().Count() == team.Count,
            "2.21: player cells = deployed heroes only");
        Check(enemyGrid.GetChildren().OfType<BattleHeroCell>().Count() == 3,
            "2.21: enemy grid has 3 hero cells");
        // Enemy temporary layout: first enemy in the middle-row leftmost cell
        // (Tiền, closest to the middle), i.e. grid child index 3.
        var enemyCells = enemyGrid.GetChildren().ToList();
        Check(enemyCells[3] is BattleHeroCell, "2.21: enemy first hero at Tien mid (index 3)");
        Check(enemyCells[0] is not BattleHeroCell && enemyCells[6] is not BattleHeroCell,
            "2.21: enemy grid has no Tien top/bottom cells");
        // A player cell shows name + HP + rage bars.
        var firstCell = playerGrid.GetChildren().OfType<BattleHeroCell>().First();
        Check(!string.IsNullOrEmpty(firstCell.NameLabel.Text), "2.21: hero cell shows name");
        Check(firstCell.HpBar.MaxValue > 0 && firstCell.RageBar.MaxValue == 100, "2.21: hero cell HP + rage bars");
        // Speed button cycles x1 -> x2 -> x3 -> x1.
        var speed = scene.GetNode<Button>("SpeedButton");
        Check(speed.Text.Contains("x1"), "2.21: speed starts at x1");
        speed.EmitSignal(Button.SignalName.Pressed);
        Check(speed.Text.Contains("x2"), "2.21: speed cycles to x2");
        speed.EmitSignal(Button.SignalName.Pressed);
        Check(speed.Text.Contains("x3"), "2.21: speed cycles to x3");
        speed.EmitSignal(Button.SignalName.Pressed);
        Check(speed.Text.Contains("x1"), "2.21: speed cycles back to x1");

        // Run the win battle to the end via steps (no real-time waiting).
        Check(RunToEnd(scene), "2.21: win battle reaches result popup");
        var popup = scene.GetNodeOrNull<Control>("ResultPopup");
        Check(popup != null, "2.21: result popup exists");
        if (popup != null)
        {
            // Labels live inside panel -> vbox, so search recursively.
            var allLabels = new List<Label>();
            CollectLabels(popup, allLabels);
            Check(allLabels.Any(l => l.Text.Contains("Thắng trận")), "2.21: win popup title");
            Check(allLabels.Any(l => l.Text.Contains("Sao:")), "2.21: win popup shows stars");
            Check(allLabels.Any(l => l.Text.Contains("Vàng")), "2.21: win popup shows gold reward");
            var foundSquares = FindNode<HBoxContainer>(popup, "RewardSquares");
            Check(foundSquares != null && foundSquares.GetChildCount() == 5,
                "2.21: win popup has 5 empty reward squares");
            var exitButton = FindNode<Button>(popup, "");
            Check(exitButton != null && exitButton.Text == "Thoát", "2.21: win popup has Thoat button");
        }
        Check(gm.Save.Gold > goldBefore, "2.21: win rewards applied immediately (gold increased)");
        scene.QueueFree();

        // --- Loss case: custom hero alone vs stage 1 ---
        gm.CreateSeededSave("Loser221");
        gm.StartBattle(1, new List<string> { "custom_hero" }, 999);
        var loseScene = ResourceLoader.Load<PackedScene>("res://features/battle/scenes/BattleScene.tscn").Instantiate<BattleScene>();
        AddChild(loseScene);
        Check(RunToEnd(loseScene), "2.21: loss battle reaches result popup");
        var losePopup = loseScene.GetNodeOrNull<Control>("ResultPopup");
        var loseLabels = new List<Label>();
        if (losePopup != null) CollectLabels(losePopup, loseLabels);
        Check(loseLabels.Any(l => l.Text.Contains("Thua trận")), "2.21: loss popup title");
        Check(!loseLabels.Any(l => l.Text.Contains("Phần thưởng")), "2.21: loss popup has no rewards");
        Check(FindNode<HBoxContainer>(losePopup!, "RewardSquares") == null, "2.21: loss popup has no reward squares");
        loseScene.QueueFree();

        GD.Print($"TEST PHASE2.21: {(_failed == 0 ? "ALL PASS" : $"{_failed} FAIL")} ({_passed} passed, {_failed} failed)");
        GetTree().Quit(_failed == 0 ? 0 : 1);
    }

    private static void CollectLabels(Node node, List<Label> outLabels)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Label l) outLabels.Add(l);
            CollectLabels(child, outLabels);
        }
    }

    private static T? FindNode<T>(Node? node, string name) where T : Node
    {
        if (node == null) return null;
        foreach (var child in node.GetChildren())
        {
            if (child is T t && (string.IsNullOrEmpty(name) || child.Name == name)) return t;
            var found = FindNode<T>(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
