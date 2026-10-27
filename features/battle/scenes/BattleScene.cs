using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class BattleScene : Control
{
    private BattleEngine? _engine;
    private readonly Dictionary<Fighter, Label> _labels = new();
    private Label? _turnLabel;
    private Label? _logLabel;
    private Label? _resultLabel;
    private Button? _normalButton;
    private Button? _ultButton;
    private Button? _autoButton;
    private bool _finished;

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        UiTheme.StyleButton(GetNode<Button>("NormalButton"));
        UiTheme.StyleButton(GetNode<Button>("UltButton"), true);
        UiTheme.StyleButton(GetNode<Button>("AutoButton"));
        UiTheme.StyleButton(GetNode<Button>("BackButton"));
        GetNode<Button>("BackButton").Text = "← Quay lại Hub";
        var stage = HeroDatabase.GetStage(gm.CurrentStageId);
        var effectiveTeam = gm.SelectedTeamIds.Select(id => ProgressionService.GetEffectiveHero(gm.Save, id)).ToList();
        // Phase 2.20: position bonuses come from the saved formation.
        var positions = gm.SelectedTeamIds.Select(id => FormationService.PositionOfHero(gm.Save, id)).ToList();
        _engine = new BattleEngine(effectiveTeam, positions, stage, gm.CurrentSeed);
        _turnLabel = GetNode<Label>("TurnLabel");
        _logLabel = GetNode<Label>("LogLabel");
        _resultLabel = GetNode<Label>("ResultLabel");
        _normalButton = GetNode<Button>("NormalButton");
        _ultButton = GetNode<Button>("UltButton");
        _autoButton = GetNode<Button>("AutoButton");
        BuildRows(GetNode<VBoxContainer>("PlayerBox"), _engine.PlayerFighters, new Color(0.2f, 0.6f, 1f));
        BuildRows(GetNode<VBoxContainer>("EnemyBox"), _engine.EnemyFighters, new Color(1f, 0.25f, 0.25f));
        Refresh();
    }

    private void BuildRows(VBoxContainer box, List<Fighter> fighters, Color color)
    {
        foreach (var f in fighters)
        {
            var row = new HBoxContainer();
            var rect = new ColorRect { Color = color, CustomMinimumSize = new Vector2(24, 24) };
            var label = new Label();
            row.AddChild(rect);
            row.AddChild(label);
            box.AddChild(row);
            _labels[f] = label;
        }
    }

    private void Refresh()
    {
        if (_engine == null) return;
        foreach (var (f, label) in _labels)
        {
            var pos = f.Position == FormationPosition.None ? "" : $" [{FormationService.PositionName(f.Position)}]";
            label.Text = $"{f.Name}{pos} — HP {f.Hp}/{f.MaxHp} — Nộ {f.Rage}/100{(f.Alive ? "" : " (GỤC)")}";
        }
        var cur = _engine.Current;
        if (_turnLabel != null)
            _turnLabel.Text = _engine.IsOver ? "Trận đã kết thúc."
                : cur == null ? "—" : $"Lượt: {cur.Name} ({(cur.IsPlayer ? "Ta" : "Địch")}) — Nộ {cur.Rage}/100";
        if (_logLabel != null)
            _logLabel.Text = string.Join("\n", _engine.Log.TakeLast(8));
        bool playerTurn = !_engine.IsOver && cur != null && cur.IsPlayer;
        if (_normalButton != null) _normalButton.Disabled = !playerTurn;
        if (_ultButton != null) _ultButton.Disabled = !playerTurn || cur == null || cur.Rage < 100;
        if (_autoButton != null) _autoButton.Disabled = _engine.IsOver;
        if (_ultButton != null && cur != null) _ultButton.Text = $"Tuyệt kỹ: {cur.Def.UltName} (cần 100 Nộ)";
    }

    private void FinishIfOver()
    {
        if (_engine == null || !_engine.IsOver || _finished) return;
        _finished = true;
        var gm = GetNode<GameManager>("/root/GameManager");
        var stage = HeroDatabase.GetStage(gm.CurrentStageId);
        int stars = _engine.Stars();
        int reward = _engine.PlayerWon ? stage.RewardGold : 0;
        gm.ApplyBattleResult(_engine.PlayerWon, stars, reward);
        if (_resultLabel != null)
            _resultLabel.Text = _engine.PlayerWon
                ? $"THẮNG! Sao: {stars}/3 — Thưởng: {reward} Vàng (seed {_engine.Seed})"
                : $"THUA! Sao: 0/3 — Thử lại với đội hình khác (seed {_engine.Seed})";
        Refresh();
    }

    private void OnNormalPressed()
    {
        if (_engine == null || !_engine.ActCurrentNormal()) return;
        // Nếu lượt tiếp theo là địch, đánh luôn lượt địch để người chơi không phải bấm hộ địch.
        while (!_engine.IsOver && _engine.Current != null && !_engine.Current.IsPlayer)
        {
            var cur = _engine.Current;
            if (cur.Rage >= 100) _engine.ActCurrentUltimate(); else _engine.ActCurrentNormal();
        }
        FinishIfOver();
        Refresh();
    }

    private void OnUltPressed()
    {
        if (_engine == null || !_engine.ActCurrentUltimate()) return;
        while (!_engine.IsOver && _engine.Current != null && !_engine.Current.IsPlayer)
        {
            var cur = _engine.Current;
            if (cur.Rage >= 100) _engine.ActCurrentUltimate(); else _engine.ActCurrentNormal();
        }
        FinishIfOver();
        Refresh();
    }

    private void OnAutoPressed()
    {
        if (_engine == null) return;
        _engine.AutoRun(true);
        FinishIfOver();
        Refresh();
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://features/hub/scenes/HubScene.tscn");
    }
}
