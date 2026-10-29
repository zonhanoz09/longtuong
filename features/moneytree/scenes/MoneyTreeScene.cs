using Godot;

namespace LongTuong;

/// <summary>
/// Phase 2.31a: Cây Rung Tiền popup — 10 free shakes/day for Bạc.
/// </summary>
[GlobalClass]
public partial class MoneyTreeScene : Control
{
    private Label? _infoLabel;
    private Label? _statusLabel;
    private Button? _shakeButton;

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        var title = GetNode<Label>("TitleLabel");
        title.Text = "Cây Rung Tiền";
        UiTheme.StyleTitle(title);
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        _infoLabel = GetNode<Label>("InfoLabel");
        UiTheme.StyleBody(_infoLabel);
        _shakeButton = GetNode<Button>("ShakeButton");
        _shakeButton.Text = "Rung cây";
        UiTheme.StyleButton(_shakeButton, true);
        _shakeButton.Pressed += OnShakePressed;
        _statusLabel = GetNode<Label>("StatusLabel");
        UiTheme.StyleBody(_statusLabel, UiTheme.FontSmall);
        _statusLabel.AddThemeColorOverride("font_color", UiTheme.Muted);
        Refresh();
    }

    private void Refresh()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (_infoLabel != null)
            _infoLabel.Text = $"Mỗi lượt rung: +{MoneyTreeService.ShakeReward(gm.Save)} Bạc\n" +
                $"Lượt còn lại hôm nay: {MoneyTreeService.ShakesLeft(gm.Save)}/{MoneyTreeService.FreeShakesPerDay}";
        if (_shakeButton != null)
            _shakeButton.Disabled = MoneyTreeService.ShakesLeft(gm.Save) <= 0;
    }

    private void OnShakePressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (_statusLabel == null) return;
        if (MoneyTreeService.TryShake(gm.Save))
        {
            _statusLabel.Text = $"+{MoneyTreeService.ShakeReward(gm.Save)} Bạc!";
            gm.SaveGame();
        }
        else _statusLabel.Text = "Hết lượt rung hôm nay, mai quay lại nhé!";
        Refresh();
    }
}
