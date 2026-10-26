using Godot;

namespace LongTuong;

[GlobalClass]
public partial class HeroListScene : Control
{
    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        var box = GetNode<VBoxContainer>("HeroBox");
        foreach (var hero in HeroDatabase.Heroes)
        {
            var p = gm.Save.Heroes[hero.Id];
            var eff = ProgressionService.GetEffectiveHero(gm.Save, hero.Id);
            var row = new HBoxContainer();
            var label = new Label { Text = $"{hero.Name} ({HeroDatabase.RoleName(hero.Role)}) — Cấp {p.Level} — Sao {p.Stars}/5 — Mảnh {gm.Save.Shards[hero.Id]} — HP {eff.Hp} / ATK {eff.Atk} / DEF {eff.Def} / Tốc {eff.Speed}", CustomMinimumSize = new Vector2(900, 0) };
            var btn = new Button { Text = "Xem chi tiết" };
            var id = hero.Id;
            btn.Pressed += () =>
            {
                gm.SelectedHeroId = id;
                GetTree().ChangeSceneToFile("res://features/hero/scenes/HeroScreen.tscn");
            };
            row.AddChild(label);
            row.AddChild(btn);
            box.AddChild(row);
        }
    }

    private void OnBackPressed() => GetTree().ChangeSceneToFile("res://features/hub/scenes/HubScene.tscn");
}
