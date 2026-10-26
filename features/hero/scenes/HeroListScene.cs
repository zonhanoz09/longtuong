using Godot;

namespace LongTuong;

[GlobalClass]
public partial class HeroListScene : Control
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
        var box = GetNode<VBoxContainer>("HeroBox");
        foreach (var hero in HeroDatabase.Heroes)
        {
            var p = gm.Save.Heroes[hero.Id];
            var eff = ProgressionService.GetEffectiveHero(gm.Save, hero.Id);
            var row = new HBoxContainer();
            bool owned = gm.Save.OwnedHeroIds.Contains(hero.Id);
            var label = new Label { Text = $"{hero.Name} ({HeroDatabase.RoleName(hero.Role)}) — Cấp {p.Level} — Sao {p.Stars}/5 — Mảnh {gm.Save.Shards[hero.Id]}{(owned ? "" : " — CHƯA SỞ HỮU (cần 10 mảnh để mở khóa)")} — HP {eff.Hp} / ATK {eff.Atk} / DEF {eff.Def} / Tốc {eff.Speed}", CustomMinimumSize = new Vector2(900, 0) };
            UiTheme.StyleBody(label, 15);
            var btn = new Button { Text = owned ? "Xem chi tiết" : "Chưa sở hữu", Disabled = !owned };
            UiTheme.StyleButton(btn);
            var id = hero.Id;
            btn.Pressed += () =>
            {
                gm.SelectedHeroId = id;
                var hub = UiTheme.FindHubScene(this);
                if (hub != null && hub.IsPopupOpen) hub.OpenPopup("res://features/hero/scenes/HeroScreen.tscn", HubScene.HeroPopupSize);
                else GetTree().ChangeSceneToFile("res://features/hero/scenes/HeroScreen.tscn");
            };
            row.AddChild(label);
            row.AddChild(btn);
            box.AddChild(row);
        }
    }

    private void OnBackPressed()
    {
        if (UiTheme.TryCloseHubPopup(this)) return;
        GetTree().ChangeSceneToFile("res://features/hub/scenes/HubScene.tscn");
    }
}
