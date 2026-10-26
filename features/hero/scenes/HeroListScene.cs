using Godot;

namespace LongTuong;

/// <summary>
/// Hero list as tabs (Phase 2.4): one tab per hero, each tab shows the full
/// detail of that hero — level/stars/shards, effective stats, skills, and the
/// 4 equipment pieces it is carrying. No Back button: the popup X closes it.
/// Owned heroes get a button to open the Equipment/Enhance popup for that hero.
/// </summary>
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
        var tabs = GetNode<TabContainer>("HeroTabs");
        int selectedIndex = 0;
        foreach (var hero in HeroDatabase.Heroes)
        {
            var p = gm.Save.Heroes[hero.Id];
            var eff = ProgressionService.GetEffectiveHero(gm.Save, hero.Id);
            bool owned = gm.Save.OwnedHeroIds.Contains(hero.Id);
            if (hero.Id == gm.SelectedHeroId) selectedIndex = tabs.GetTabCount();
            var page = new VBoxContainer();
            tabs.AddChild(page);
            tabs.SetTabTitle(page.GetIndex(), owned ? hero.Name : $"{hero.Name} (Khóa)");
            void Add(string text, int size = 15)
            {
                var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
                UiTheme.StyleBody(l, size);
                page.AddChild(l);
            }
            Add($"{hero.Name} — {HeroDatabase.RoleName(hero.Role)}", 20);
            Add(owned
                ? $"Cấp {p.Level} (EXP {p.Exp}/{ProgressionService.ExpToNext(p.Level)}) — Sao {p.Stars}/5 — Mảnh: {gm.Save.Shards[hero.Id]} — Tăng sao tốn {ProgressionService.StarUpCost(p.Stars)} mảnh"
                : $"CHƯA SỞ HỮU — cần 10 mảnh để mở khóa (hiện có {gm.Save.Shards[hero.Id]} mảnh). Chiêu mộ hoặc thắng trận để nhận mảnh.");
            Add($"Chỉ số hiệu lực: HP {eff.Hp} / ATK {eff.Atk} / DEF {eff.Def} / Tốc {eff.Speed} / Bạo {eff.Crit:P0} / Né {eff.Dodge:P0}");
            Add($"Đánh thường: {hero.NormalName} | Tuyệt kỹ: {hero.UltName} | Bị động: {hero.PassiveName} — {hero.PassiveDesc}");
            Add("Trang bị mang theo:", 17);
            foreach (var slot in ProgressionService.Slots)
            {
                var eq = gm.Save.HeroEquipment[hero.Id][slot.Id];
                Add($"  {slot.Name}: Cường hóa {eq.Level}/10 — Tẩy luyện: {ProgressionService.RefineNames[eq.RefineIndex]}");
            }
            if (owned)
            {
                var id = hero.Id;
                var btn = new Button { Text = "Mở trang bị & cường hóa cho tướng này" };
                UiTheme.StyleButton(btn, true);
                btn.Pressed += () =>
                {
                    gm.SelectedHeroId = id;
                    var hub = UiTheme.FindHubScene(this);
                    if (hub != null && hub.IsPopupOpen) hub.OpenPopup("res://features/hero/scenes/HeroScreen.tscn", HubScene.HeroPopupSize);
                    else GetTree().ChangeSceneToFile("res://features/hero/scenes/HeroScreen.tscn");
                };
                page.AddChild(btn);
            }
        }
        tabs.CurrentTab = selectedIndex;
    }
}
