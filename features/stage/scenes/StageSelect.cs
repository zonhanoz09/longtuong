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
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        UiTheme.AddCurrencyBar(this, gm);
        UiTheme.StyleButton(GetNode<Button>("StartButton"), true);
        UiTheme.StyleButton(GetNode<Button>("BackButton"));
        GetNode<Button>("BackButton").Text = "← Quay lại Hub";
        _selected.AddRange(gm.SelectedTeamIds.Where(id => gm.Save.OwnedHeroIds.Contains(id)));
        if (_selected.Count == 0 && gm.Save.OwnedHeroIds.Count > 0) _selected.Add(gm.Save.OwnedHeroIds[0]);
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
            bool owned = GetNode<GameManager>("/root/GameManager").Save.OwnedHeroIds.Contains(hero.Id);
            var btn = new Button
            {
                Text = $"{hero.Name} ({HeroDatabase.RoleName(hero.Role)}) — HP {hero.Hp} / ATK {hero.Atk} / Tốc {hero.Speed} — {hero.UltName}{(owned ? "" : " (KHÓA — cần 10 mảnh)")}",
                ToggleMode = true,
                ButtonPressed = _selected.Contains(hero.Id),
                Disabled = !owned,
            };
            UiTheme.StyleButton(btn);
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
        // Title/label info is updated below; team size is 1-3 since Phase 2.2.
        var stage = HeroDatabase.GetStage(_selectedStage);
        if (_infoLabel != null)
            _infoLabel.Text = $"Đội ({_selected.Count}/3): {string.Join(", ", names)} | Ải {_selectedStage}: {stage.Name}{(note != null ? $" | {note}" : "")}";
    }

    private void OnStartPressed()
    {
        if (_selected.Count < 1 || _selected.Count > 3) { RefreshInfo("Chọn từ 1 đến 3 tướng!"); return; }
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
