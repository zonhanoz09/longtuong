using Godot;

namespace LongTuong;

/// <summary>
/// Phase 2.29: Tướng Tinh moved out of the Trang bị popup into its own
/// small popup, opened from the Hub's new "Tướng Tinh" button
/// (Huynh's decision 2b).
/// </summary>
[GlobalClass]
public partial class StarChartScene : Control
{
    private Label? _infoLabel;
    private Label? _statusLabel;

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        var title = GetNode<Label>("TitleLabel");
        title.Text = "Tướng Tinh";
        UiTheme.StyleTitle(title);
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        _infoLabel = GetNode<Label>("InfoLabel");
        UiTheme.StyleBody(_infoLabel);
        var unlock = GetNode<Button>("UnlockButton");
        unlock.Text = "Mở ô";
        UiTheme.StyleButton(unlock, true);
        unlock.Pressed += OnUnlockPressed;
        _statusLabel = GetNode<Label>("StatusLabel");
        UiTheme.StyleBody(_statusLabel, UiTheme.FontSmall);
        _statusLabel.AddThemeColorOverride("font_color", UiTheme.Muted);
        Refresh();
    }

    private void Refresh()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var save = gm.Save;
        int points = ProgressionService.StarChartPoints(save);
        int nextCost = (save.StarNodesUnlocked + 1) * 2;
        if (_infoLabel != null)
            _infoLabel.Text = $"Đã mở {save.StarNodesUnlocked}/12 ô (+3% HP/ATK toàn đội mỗi ô)\nĐiểm sao ải: {points} — Ô tiếp theo cần {nextCost} điểm.";
    }

    private void OnUnlockPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (_statusLabel == null) return;
        if (ProgressionService.TryUnlockStarNode(gm.Save)) { _statusLabel.Text = "Mở ô Tướng Tinh thành công!"; gm.SaveGame(); }
        else _statusLabel.Text = "Chưa đủ điểm sao ải để mở ô Tướng Tinh!";
        Refresh();
    }
}
