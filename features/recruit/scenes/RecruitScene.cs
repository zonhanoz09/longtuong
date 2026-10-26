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
        UiTheme.StyleButton(GetNode<Button>("DrawButton"), true);
        _statusLabel = GetNode<Label>("StatusLabel");
        _goldLabel = GetNode<Label>("GoldLabel");
        GetNode<Label>("RatesLabel").Text = $"Tỉ lệ công khai (1 lượt = 5 mảnh 1 tướng): {RecruitService.RatesText()}.";
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
            if (_statusLabel != null) _statusLabel.Text = $"Chúc mừng! Nhận 5 mảnh {HeroDatabase.Get(heroId).Name}.{unlock}";
            gm.SaveGame();
        }
        else if (_statusLabel != null) _statusLabel.Text = "Không đủ Vàng (cần 100)! Thắng ải để kiếm thêm.";
        Refresh();
    }

}
