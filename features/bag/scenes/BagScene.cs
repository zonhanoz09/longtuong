using Godot;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class BagScene : Control
{
    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        UiTheme.StyleButton(GetNode<Button>("BackButton"));
        GetNode<Button>("BackButton").Text = "← Quay lại Hub";
        var box = GetNode<VBoxContainer>("ItemBox");
        void Add(string text) { var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart }; UiTheme.StyleBody(l, 15); box.AddChild(l); }
        Add($"Vàng: {gm.Save.Gold} | Kim Nguyên Bảo: {gm.Save.Gems} | Trận thắng: {gm.Save.Wins} | Ải cao nhất: {gm.Save.LastStage}");
        Add($"Tướng Tinh: {gm.Save.StarNodesUnlocked}/12 ô | Tổng sao ải: {ProgressionService.StarChartPoints(gm.Save)}");
        Add("— Mảnh tướng —");
        foreach (var hero in HeroDatabase.Heroes)
            Add($"{hero.Name}: {gm.Save.Shards[hero.Id]} mảnh — Cấp {gm.Save.Heroes[hero.Id].Level} — Sao {gm.Save.Heroes[hero.Id].Stars}/5");
        Add("— Trang bị (cấp cường hóa) —");
        foreach (var hero in HeroDatabase.Heroes)
        {
            var eq = gm.Save.HeroEquipment[hero.Id];
            Add($"{hero.Name}: " + string.Join(", ", ProgressionService.Slots.Select(s => $"{s.Name} {eq[s.Id].Level}/10")));
        }
        Add("— Sao ải —");
        foreach (var stage in HeroDatabase.Stages)
        {
            gm.Save.StageStars.TryGetValue(stage.Id.ToString(), out var stars);
            Add($"Ải {stage.Id} {stage.Name}: {stars}/3 sao");
        }
    }

    private void OnBackPressed()
    {
        if (UiTheme.TryCloseHubPopup(this)) return;
        GetTree().ChangeSceneToFile("res://features/hub/scenes/HubScene.tscn");
    }
}
