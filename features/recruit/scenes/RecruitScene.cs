using Godot;

namespace LongTuong;

[GlobalClass]
public partial class RecruitScene : Control
{
    private Label? _statusLabel;
    private Label? _goldLabel;

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm0 = GetNode<GameManager>("/root/GameManager");
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm0);
        var drawBtn = GetNode<Button>("DrawButton");
        UiTheme.StyleButton(drawBtn, true);
        drawBtn.Disabled = true; // Phase 2.22: recruit pending rework.
        _statusLabel = GetNode<Label>("StatusLabel");
        _goldLabel = GetNode<Label>("GoldLabel");
        if (_statusLabel != null) _statusLabel.Text = "Chiêu mộ đang được làm lại, sẽ ra mắt ở bản sau.";
        GetNode<Label>("RatesLabel").Text = $"Tỉ lệ công khai (cho bản chiêu mộ mới): {RecruitService.RatesText()}.";
        Refresh();
    }

    private void Refresh()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (_goldLabel != null) _goldLabel.Text = $"Vàng: {gm.Save.Gold} | Kim Nguyên Bảo: {gm.Save.Gems}";
    }

    private void OnDrawPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var ownedBefore = new System.Collections.Generic.HashSet<string>(gm.Save.OwnedHeroIds);
        if (RecruitService.TryRecruit(gm.Save, new System.Random(), out var heroId))
        {
            string unlock = !ownedBefore.Contains(heroId) && gm.Save.OwnedHeroIds.Contains(heroId) ? $" Đã mở khóa tướng {HeroDatabase.Get(heroId).Name}!" : "";
            if (_statusLabel != null) _statusLabel.Text = $"Chúc mừng! Nhận tướng {HeroDatabase.Get(heroId).Name}.{unlock}";
            gm.SaveGame();
        }
        else if (_statusLabel != null) _statusLabel.Text = "Chiêu mộ đang được làm lại, sẽ ra mắt ở bản sau.";
        Refresh();
    }

}
