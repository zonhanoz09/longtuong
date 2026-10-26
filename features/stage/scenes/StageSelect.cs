using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class StageSelect : Control
{
    private readonly List<string> _selected = new();
    private int _selectedStage = 1;
    private Label? _infoLabel;

    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        _selected.AddRange(gm.SelectedTeamIds);
        _selectedStage = System.Math.Min(gm.CurrentStageId, 10);
        _infoLabel = GetNode<Label>("InfoLabel");
        BuildHeroButtons();
        BuildStageButtons();
        RefreshInfo();
    }

    private void BuildHeroButtons()
    {
        var box = GetNode<VBoxContainer>("HeroBox");
        foreach (var hero in HeroDatabase.Heroes)
        {
            var btn = new Button
            {
                Text = $"{hero.Name} ({HeroDatabase.RoleName(hero.Role)}) — HP {hero.Hp} / ATK {hero.Atk} / Tốc {hero.Speed} — {hero.UltName}",
                ToggleMode = true,
                ButtonPressed = _selected.Contains(hero.Id),
            };
            var id = hero.Id;
            btn.Pressed += () =>
            {
                if (btn.ButtonPressed)
                {
                    if (_selected.Count >= 3) { btn.ButtonPressed = false; RefreshInfo("Chỉ chọn tối đa 3 tướng!"); return; }
                    _selected.Add(id);
                }
                else _selected.Remove(id);
                RefreshInfo();
            };
            box.AddChild(btn);
        }
    }

    private void BuildStageButtons()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var box = GetNode<VBoxContainer>("StageBox");
        foreach (var stage in HeroDatabase.Stages)
        {
            bool unlocked = stage.Id <= gm.Save.LastStage + 1;
            gm.Save.StageStars.TryGetValue(stage.Id.ToString(), out var stars);
            var btn = new Button
            {
                Text = $"Ải {stage.Id}: {stage.Name} — Thưởng {stage.RewardGold} Vàng — Sao: {stars}/3{(unlocked ? "" : " (KHÓA)")}",
                Disabled = !unlocked,
                ToggleMode = true,
                ButtonPressed = stage.Id == _selectedStage,
            };
            var sid = stage.Id;
            btn.Pressed += () =>
            {
                if (!btn.ButtonPressed) { btn.ButtonPressed = true; return; }
                _selectedStage = sid;
                foreach (var child in box.GetChildren().OfType<Button>()) if (child != btn) child.ButtonPressed = false;
                RefreshInfo();
            };
            box.AddChild(btn);
        }
    }

    private void RefreshInfo(string? note = null)
    {
        var names = _selected.Select(id => HeroDatabase.Get(id).Name).ToList();
        var stage = HeroDatabase.GetStage(_selectedStage);
        if (_infoLabel != null)
            _infoLabel.Text = $"Đội ({_selected.Count}/3): {string.Join(", ", names)} | Ải {_selectedStage}: {stage.Name}{(note != null ? $" | {note}" : "")}";
    }

    private void OnStartPressed()
    {
        if (_selected.Count != 3) { RefreshInfo("Phải chọn đúng 3 tướng!"); return; }
        var gm = GetNode<GameManager>("/root/GameManager");
        int seed = (int)(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 1000000) + _selectedStage * 7919;
        gm.StartBattle(_selectedStage, new List<string>(_selected), seed);
        gm.SaveGame();
        GetTree().ChangeSceneToFile("res://features/battle/scenes/BattleScene.tscn");
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://features/hub/scenes/HubScene.tscn");
    }
}
