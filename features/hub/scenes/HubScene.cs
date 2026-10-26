using Godot;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class HubScene : Control
{
    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        var status = GetNode<Label>("StatusLabel");
        var teamNames = string.Join(", ", gm.SelectedTeamIds.Select(id => HeroDatabase.Get(id).Name));
        status.Text = $"Chào {gm.CurrentUsername} | Vàng: {gm.Save.Gold} | Kim Nguyên Bảo: {gm.Save.Gems} | Thắng: {gm.Save.Wins} | Ải cao nhất: {gm.Save.LastStage} | Đội hình: {teamNames}";
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
