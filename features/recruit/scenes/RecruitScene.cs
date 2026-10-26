using Godot;

namespace LongTuong;

[GlobalClass]
public partial class RecruitScene : Control
{
    private Label? _statusLabel;
    private Label? _goldLabel;

    public override void _Ready()
    {
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
        if (RecruitService.TryRecruit(gm.Save, new System.Random(), out var heroId))
        {
            if (_statusLabel != null) _statusLabel.Text = $"Chúc mừng! Nhận 5 mảnh {HeroDatabase.Get(heroId).Name}.";
            gm.SaveGame();
        }
        else if (_statusLabel != null) _statusLabel.Text = "Không đủ Vàng (cần 100)! Thắng ải để kiếm thêm.";
        Refresh();
    }

    private void OnBackPressed() => GetTree().ChangeSceneToFile("res://features/hub/scenes/HubScene.tscn");
}
