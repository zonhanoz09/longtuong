using Godot;

namespace LongTuong;

[GlobalClass]
public partial class MainScene : Control
{
    private Label? _statusLabel;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("StatusLabel");
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (_statusLabel != null)
            _statusLabel.Text = $"longtuong Phase 0 — v{GameManager.Version} | Vang: {gm.Save.Gold} | Thang: {gm.Save.Wins}";
    }

    private void OnBattlePressed()
    {
        GetTree().ChangeSceneToFile("res://features/battle/scenes/BattlePlaceholder.tscn");
    }

    private void OnSavePressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.SaveGame();
        RefreshStatus();
    }

    private void OnLoadPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.LoadGame();
        RefreshStatus();
    }
}
