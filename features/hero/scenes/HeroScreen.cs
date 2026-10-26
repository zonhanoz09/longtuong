using Godot;
using System.Collections.Generic;

namespace LongTuong;

[GlobalClass]
public partial class HeroScreen : Control
{
    private string _selected = "quan_vu";
    private string _status = "";
    private Label? _detailLabel;
    private Label? _starLabel;
    private Label? _recruitLabel;
    private readonly Dictionary<string, (Label Label, Button Enhance, Button Refine)> _equipRows = new();

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        if (!string.IsNullOrEmpty(gm.SelectedHeroId)) _selected = gm.SelectedHeroId;
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        UiTheme.StyleButton(GetNode<Button>("StarUpButton"), true);
        UiTheme.StyleButton(GetNode<Button>("UnlockStarButton"));
        UiTheme.StyleButton(GetNode<Button>("RecruitButton"));
        UiTheme.StyleButton(GetNode<Button>("BackButton"));
        GetNode<Button>("BackButton").Text = "← Quay lại Hub";
        if (!gm.Save.OwnedHeroIds.Contains(_selected) && gm.Save.OwnedHeroIds.Count > 0) _selected = gm.Save.OwnedHeroIds[0];
        ProgressionService.EnsureDefaults(gm.Save);
        _detailLabel = GetNode<Label>("DetailLabel");
        _starLabel = GetNode<Label>("StarLabel");
        _recruitLabel = GetNode<Label>("RecruitLabel");
        BuildHeroButtons();
        BuildEquipRows();
        Refresh();
    }

    private void BuildHeroButtons()
    {
        var box = GetNode<VBoxContainer>("HeroBox");
        foreach (var hero in HeroDatabase.Heroes)
        {
            var gm2 = GetNode<GameManager>("/root/GameManager");
            bool owned = gm2.Save.OwnedHeroIds.Contains(hero.Id);
            var btn = new Button { Text = owned ? hero.Name : $"{hero.Name} (KHÓA)", ToggleMode = true, ButtonPressed = hero.Id == _selected, Disabled = !owned };
            UiTheme.StyleButton(btn);
            var id = hero.Id;
            btn.Pressed += () =>
            {
                if (!btn.ButtonPressed) { btn.ButtonPressed = true; return; }
                _selected = id;
                foreach (var child in box.GetChildren()) if (child is Button b && b != btn) b.ButtonPressed = false;
                Refresh();
            };
            box.AddChild(btn);
        }
    }

    private void BuildEquipRows()
    {
        var box = GetNode<VBoxContainer>("EquipBox");
        foreach (var slot in ProgressionService.Slots)
        {
            var row = new HBoxContainer();
            var label = new Label { CustomMinimumSize = new Vector2(520, 0) };
            var enh = new Button { Text = "Cường hóa" };
            var refBtn = new Button { Text = "Tẩy luyện" };
            UiTheme.StyleButton(enh);
            UiTheme.StyleButton(refBtn);
            var slotId = slot.Id;
            enh.Pressed += () =>
            {
                var gm = GetNode<GameManager>("/root/GameManager");
                if (ProgressionService.TryEnhance(gm.Save, _selected, slotId)) { _status = $"Cường hóa {slot.Name} thành công!"; gm.SaveGame(); }
                else _status = "Cường hóa thất bại: đã tối đa hoặc không đủ Vàng!";
                Refresh();
            };
            refBtn.Pressed += () =>
            {
                var gm = GetNode<GameManager>("/root/GameManager");
                if (ProgressionService.TryRefine(gm.Save, _selected, slotId)) { _status = $"Tẩy luyện {slot.Name} thành công!"; gm.SaveGame(); }
                else _status = "Tẩy luyện thất bại: không đủ Vàng!";
                Refresh();
            };
            row.AddChild(label);
            row.AddChild(enh);
            row.AddChild(refBtn);
            box.AddChild(row);
            _equipRows[slot.Id] = (label, enh, refBtn);
        }
    }

    private void Refresh()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var save = gm.Save;
        ProgressionService.EnsureDefaults(save);
        var hero = HeroDatabase.Get(_selected);
        var p = save.Heroes[_selected];
        var eff = ProgressionService.GetEffectiveHero(save, _selected);
        var next = p.Level >= ProgressionService.MaxLevel ? "MAX" : $"{p.Exp}/{ProgressionService.ExpToNext(p.Level)}";
        if (_detailLabel != null)
            _detailLabel.Text = $"{hero.Name} ({HeroDatabase.RoleName(hero.Role)}) — Cấp {p.Level} (EXP {next}) — Sao {p.Stars}/5 — Mảnh: {save.Shards[_selected]}\n" +
                $"Hiệu lực: HP {eff.Hp} / ATK {eff.Atk} / DEF {eff.Def} / Tốc {eff.Speed} / Bạo {eff.Crit:P0} / Né {eff.Dodge:P0}\n" +
                $"Đánh thường: {hero.NormalName} | Tuyệt kỹ: {hero.UltName} | Bị động: {hero.PassiveName} — {hero.PassiveDesc}\n" +
                $"Vàng: {save.Gold} | Kim Nguyên Bảo: {save.Gems} | Trận thắng: {save.Wins}\n{_status}";
        foreach (var slot in ProgressionService.Slots)
        {
            var eq = save.HeroEquipment[_selected][slot.Id];
            var (label, enh, _) = _equipRows[slot.Id];
            label.Text = $"{slot.Name}: Cường hóa {eq.Level}/10 (tốn {ProgressionService.EnhanceCost(eq.Level)} Vàng) — Tẩy luyện: {ProgressionService.RefineNames[eq.RefineIndex]} (tốn 100 Vàng)";
            enh.Disabled = eq.Level >= ProgressionService.MaxEnhanceLevel;
        }
        if (_starLabel != null)
        {
            int points = ProgressionService.StarChartPoints(save);
            int nextCost = (save.StarNodesUnlocked + 1) * 2;
            _starLabel.Text = $"Tướng Tinh: đã mở {save.StarNodesUnlocked}/12 ô (+3% HP/ATK toàn đội mỗi ô) — Điểm sao ải: {points} — Ô tiếp theo cần {nextCost} điểm. Tăng sao tốn {ProgressionService.StarUpCost(p.Stars)} mảnh (hiện có {save.Shards[_selected]}).";
        }
        if (_recruitLabel != null)
            _recruitLabel.Text = $"Chiêu mộ: 100 Vàng = 5 mảnh 1 tướng ngẫu nhiên. Tỉ lệ công khai: {RecruitService.RatesText()}.";
    }

    private void OnStarUpPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (ProgressionService.TryStarUp(gm.Save, _selected)) { _status = "Tăng sao thành công!"; gm.SaveGame(); }
        else _status = "Tăng sao thất bại: đã tối đa hoặc không đủ mảnh!";
        Refresh();
    }

    private void OnUnlockStarPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (ProgressionService.TryUnlockStarNode(gm.Save)) { _status = "Mở ô Tướng Tinh thành công!"; gm.SaveGame(); }
        else _status = "Chưa đủ điểm sao ải để mở ô Tướng Tinh!";
        Refresh();
    }

    private void OnRecruitPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (RecruitService.TryRecruit(gm.Save, new System.Random(), out var heroId))
        {
            _status = $"Chiêu mộ được 5 mảnh {HeroDatabase.Get(heroId).Name}!";
            gm.SaveGame();
        }
        else _status = "Không đủ Vàng để chiêu mộ (cần 100)!";
        Refresh();
    }

    private void OnBackPressed()
    {
        if (UiTheme.TryCloseHubPopup(this)) return;
        GetTree().ChangeSceneToFile("res://features/hub/scenes/HubScene.tscn");
    }
}
