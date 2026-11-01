using Godot;
using System.Collections.Generic;

namespace LongTuong;

// Launch this scene to review battle art with an isolated test account.
public partial class PreviewBattle : Node
{
    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("Mẫu chiến đấu");
        gm.Save.OwnedHeroIds.AddRange(new[] { "quan_vu", "gia_cat_luong", "dieu_thuyen", "trieu_van" });
        FormationService.EnsureFormation(gm.Save);
        FormationService.TryPlace(gm.Save, "quan_vu", "tien_mid", null, out _);
        FormationService.TryPlace(gm.Save, "gia_cat_luong", "hau_top", null, out _);
        FormationService.TryPlace(gm.Save, "dieu_thuyen", "hau_bot", null, out _);
        FormationService.TryPlace(gm.Save, "trieu_van", "trung_top", null, out _);
        var team = FormationService.FormationToTeam(gm.Save.Formation);
        gm.SelectedTeamIds = new List<string>(team);
        gm.StartBattle(1, new List<string>(team), 12345);
        AddChild(ResourceLoader.Load<PackedScene>("res://features/battle/scenes/BattleScene.tscn").Instantiate());
    }
}
