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
        Add("— Trang bị trong túi (xem chỉ số: hover trong màn Danh sách tướng) —");
        if (gm.Save.BagEquipment.Count == 0) Add("Túi trang bị trống.");
        foreach (var item in gm.Save.BagEquipment)
        {
            var slotDef = ProgressionService.Slots.Find(s => s.Id == item.SlotId);
            Add($"{slotDef?.Name ?? item.SlotId} — Cấp {item.Level}");
        }
        Add("— Sao ải —");
        foreach (var stage in HeroDatabase.Stages)
        {
            gm.Save.StageStars.TryGetValue(stage.Id.ToString(), out var stars);
            Add($"Ải {stage.Id} {stage.Name}: {stars}/3 sao");
        }
    }

}
