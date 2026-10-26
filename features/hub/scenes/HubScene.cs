using Godot;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class HubScene : Control
{
    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        UiTheme.AddCurrencyBar(this, gm);
        var status = GetNode<Label>("StatusLabel");
        UiTheme.StyleBody(status, 17);
        var teamNames = string.Join(", ", gm.SelectedTeamIds.Select(id => HeroDatabase.Get(id).Name));
        var starter = gm.Save.OwnedHeroIds.Count > 0 ? HeroDatabase.Get(gm.Save.OwnedHeroIds[0]).Name : "?";
        status.Text = $"Chào {gm.CurrentUsername} | Tướng đại diện: {starter} | Sở hữu: {gm.Save.OwnedHeroIds.Count}/6 tướng | Thắng: {gm.Save.Wins} | Ải cao nhất: {gm.Save.LastStage} | Đội hình: {teamNames}";
        var cards = new (string Node, string Text, bool Primary)[]
        {
            ("HeroListButton", $"Danh sách tướng sở hữu\n{gm.Save.OwnedHeroIds.Count}/6 tướng — xem cấp, sao, mảnh", false),
            ("BagButton", $"Túi đồ\n{gm.Save.Gold} Vàng — {gm.Save.Gems} KNB", false),
            ("EquipmentButton", "Trang bị\nCường hóa, tẩy luyện 4 món", false),
            ("FormationButton", $"Bày trận\nĐội hình: {teamNames}", true),
            ("StageButton", $"Vào ải\nẢi tiếp theo: {gm.Save.LastStage + 1}/10", true),
            ("EnhanceButton", "Cường hóa\nNâng cấp trang bị tướng", false),
            ("RecruitButton", "Chiêu mộ\n100 Vàng = 5 mảnh, tỉ lệ công khai", false),
            ("LogoutButton", "Đăng xuất\nVề màn hình đăng nhập", false),
        };
        foreach (var (node, text, primary) in cards)
        {
            var btn = GetNode<Button>($"WidgetBox/{node}");
            btn.Text = text;
            UiTheme.StyleButton(btn, primary);
        }
    }

    private void Go(string path) => GetTree().ChangeSceneToFile(path);
    private void OnHeroListPressed() => Go("res://features/hero/scenes/HeroListScene.tscn");
    private void OnBagPressed() => Go("res://features/bag/scenes/BagScene.tscn");
    private void OnEquipmentPressed() => Go("res://features/hero/scenes/HeroScreen.tscn");
    private void OnFormationPressed() => Go("res://features/formation/scenes/FormationScene.tscn");
    private void OnStagePressed() => Go("res://features/stage/scenes/StageSelect.tscn");
    private void OnEnhancePressed() => Go("res://features/hero/scenes/HeroScreen.tscn");
    private void OnRecruitPressed() => Go("res://features/recruit/scenes/RecruitScene.tscn");
    private void OnLogoutPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.Logout();
        Go("res://features/auth/scenes/AuthScene.tscn");
    }
}
