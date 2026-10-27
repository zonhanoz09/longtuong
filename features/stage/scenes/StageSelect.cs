using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.20: stage select no longer picks a team of its own — the battle
/// team is exactly the formation set in Bày trận ("bày như thế nào thì vào
/// trận như vậy"). This popup only picks the stage and shows the formation
/// team (with each hero's position) as read-only info. To change the team,
/// close this popup and open Bày trận.
/// </summary>
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
        ProgressionService.EnsureDefaults(gm.Save);
        FormationService.EnsureFormation(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        UiTheme.StyleButton(GetNode<Button>("StartButton"), true);
        // The battle team is the formation team (main hero always included).
        _selected.AddRange(FormationService.FormationToTeam(gm.Save.Formation)
            .Where(id => gm.Save.OwnedHeroIds.Contains(id)));
        if (!_selected.Contains(FormationService.MainHeroId) && gm.Save.OwnedHeroIds.Contains(FormationService.MainHeroId))
            _selected.Insert(0, FormationService.MainHeroId);
        gm.SelectedTeamIds = new List<string>(_selected);
        _selectedStage = System.Math.Min(gm.CurrentStageId, 10);
        _infoLabel = GetNode<Label>("InfoLabel");
        BuildTeamLabels();
        BuildStageButtons();
        RefreshInfo();
    }

    private void BuildTeamLabels()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var box = GetNode<VBoxContainer>("HeroBox");
        foreach (var heroId in _selected)
        {
            var hero = HeroDatabase.Get(heroId);
            var pos = FormationService.PositionOfHero(gm.Save, heroId);
            var label = new Label
            {
                Text = $"{hero.Name} — {FormationService.PositionName(pos)}" +
                       (FormationService.IsMain(heroId) ? " (Main)" : ""),
            };
            UiTheme.StyleBody(label, 16);
            box.AddChild(label);
        }
        if (_selected.Count == 0)
        {
            var label = new Label { Text = "Chưa có đội hình — hãy vào Bày trận trước." };
            UiTheme.StyleBody(label, 16);
            box.AddChild(label);
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
            _infoLabel.Text = $"Đội hình từ Bày trận ({_selected.Count}/5): {string.Join(", ", names)} | Ải {_selectedStage}: {stage.Name}{(note != null ? $" | {note}" : "")}";
    }

    private void OnStartPressed()
    {
        if (_selected.Count < 1 || _selected.Count > FormationService.MaxTeamSize)
        {
            RefreshInfo("Chưa có đội hình hợp lệ — hãy vào Bày trận trước!");
            return;
        }
        var gm = GetNode<GameManager>("/root/GameManager");
        int seed = (int)(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 1000000) + _selectedStage * 7919;
        gm.StartBattle(_selectedStage, new List<string>(_selected), seed);
        gm.SaveGame();
        GetTree().ChangeSceneToFile("res://features/battle/scenes/BattleScene.tscn");
    }

}
