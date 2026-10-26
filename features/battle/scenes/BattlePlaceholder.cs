using Godot;

namespace LongTuong;

[GlobalClass]
public partial class BattlePlaceholder : Control
{
    private readonly PlaceholderBattle _battle = new();
    private Label? _hpLabel;
    private Label? _resultLabel;

    public override void _Ready()
    {
        _hpLabel = GetNode<Label>("HpLabel");
        _resultLabel = GetNode<Label>("ResultLabel");
        Refresh();
    }

    private void Refresh()
    {
        if (_hpLabel != null)
            _hpLabel.Text = $"Ta: {_battle.PlayerHp} HP | Dich: {_battle.EnemyHp} HP | Luot: {_battle.Turn}";
        if (_resultLabel != null)
            _resultLabel.Text = _battle.IsOver ? (_battle.PlayerWon ? "THANG!" : "THUA!") : "Chua phan thang bai";
    }

    private void OnAttackPressed()
    {
        _battle.Attack();
        if (_battle.IsOver && _battle.PlayerWon)
        {
            var gm = GetNode<GameManager>("/root/GameManager");
            gm.RecordWin();
            gm.SaveGame();
        }
        Refresh();
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://features/main/scenes/MainScene.tscn");
    }
}
