using Godot;
using System.Collections.Generic;

namespace LongTuong;

/// <summary>
/// Phase 2.29: the "Trang bị" screen (Hub button renamed from "Cường hóa"),
/// redesigned per Huynh's screenshot — 3 columns in the 1140x680 popup:
/// left = hero list, middle = 6 equipment rows (icon + name + enhance
/// level + refine badge, selected row gold-bordered), right = 5 system
/// tabs (Cường hóa/Tẩy luyện/Kế thừa/Đục lỗ/Phụ ma) with the action panel
/// below. Clicking a middle row selects the item; the right panel then
/// offers Cường hóa/Tẩy luyện/Tháo for it.
/// Huynh's decisions: 5 tabs with "Chưa mở." for the 3 systems that don't
/// exist yet (1a); Tướng Tinh + Chiêu mộ moved OUT of this popup (2b) —
/// star chart has its own Hub button + popup, recruit lives on the Hub's
/// Chiêu mộ button; row click drives the right panel (3 confirmed).
/// </summary>
[GlobalClass]
public partial class HeroScreen : Control
{
    private static readonly (string Id, string Name)[] SysTabs =
    {
        ("cuong_hoa", "Cường hóa"),
        ("tay_luyen", "Tẩy luyện"),
        ("ke_thua", "Kế thừa"),
        ("duc_lo", "Đục lỗ"),
        ("phu_ma", "Phụ ma"),
    };

    private string _selectedHero = "";
    private string _selectedSlot = "weapon";
    private string _selectedTab = "cuong_hoa";
    private string _status = "";
    private VBoxContainer? _heroListBox;
    private VBoxContainer? _equipListBox;
    private HBoxContainer? _tabRow;
    private Control? _tabContent;
    private readonly Dictionary<string, Button> _equipRows = new();

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        var title = GetNode<Label>("TitleLabel");
        title.Text = "Trang bị";
        UiTheme.StyleTitle(title);
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        if (!string.IsNullOrEmpty(gm.SelectedHeroId) && gm.Save.OwnedHeroIds.Contains(gm.SelectedHeroId))
            _selectedHero = gm.SelectedHeroId;
        else if (gm.Save.OwnedHeroIds.Count > 0)
            _selectedHero = gm.Save.OwnedHeroIds[0];
        _heroListBox = GetNode<VBoxContainer>("HeroListBox");
        _equipListBox = GetNode<VBoxContainer>("EquipListBox");
        _tabRow = GetNode<HBoxContainer>("RightPanel/TabRow");
        _tabContent = GetNode<Control>("RightPanel/TabContent");
        BuildHeroList();
        BuildEquipRows();
        BuildSysTabs();
        RefreshHeroList();
        RefreshEquipRows();
        RefreshRightPanel();
    }

    // ---------- Left column: heroes ----------

    private void BuildHeroList()
    {
        if (_heroListBox == null) return;
        var gm = GetNode<GameManager>("/root/GameManager");
        _heroListBox.AddThemeConstantOverride("separation", 6);
        foreach (var hero in HeroDatabase.Heroes)
        {
            bool owned = gm.Save.OwnedHeroIds.Contains(hero.Id);
            var id = hero.Id;
            var btn = new Button
            {
                Name = $"Hero_{id}",
                Text = owned ? hero.Name : $"{hero.Name} (KHÓA)",
                CustomMinimumSize = new Vector2(190, UiTheme.ButtonHeight),
                Disabled = !owned,
            };
            UiTheme.StyleButton(btn, false);
            btn.Pressed += () => { _selectedHero = id; RefreshHeroList(); RefreshEquipRows(); RefreshRightPanel(); };
            _heroListBox.AddChild(btn);
        }
    }

    private void RefreshHeroList()
    {
        if (_heroListBox == null) return;
        foreach (var btn in _heroListBox.GetChildren())
        {
            if (btn is Button b) UiTheme.StyleButton(b, b.Name == $"Hero_{_selectedHero}");
        }
    }

    // ---------- Middle column: 6 equipment rows ----------

    private void BuildEquipRows()
    {
        if (_equipListBox == null) return;
        _equipListBox.AddThemeConstantOverride("separation", 8);
        foreach (var slot in ProgressionService.Slots)
        {
            var slotId = slot.Id;
            var btn = new Button { Name = $"EquipRow_{slotId}", Text = "", CustomMinimumSize = new Vector2(380, 84) };
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            row.AddThemeConstantOverride("separation", 10);
            var icon = new PanelContainer { Name = "RowIconPanel", CustomMinimumSize = new Vector2(72, 72), MouseFilter = MouseFilterEnum.Ignore };
            icon.AddChild(CenteredLabel(AssetIcons.SlotSymbol(slotId), 24, Colors.White, "RowIcon"));
            row.AddChild(icon);
            var texts = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
            texts.AddChild(new Label { Name = "RowName", MouseFilter = MouseFilterEnum.Ignore });
            texts.AddChild(new Label { Name = "RowEnhance", MouseFilter = MouseFilterEnum.Ignore });
            texts.AddChild(new Label { Name = "RowRefine", MouseFilter = MouseFilterEnum.Ignore });
            row.AddChild(texts);
            var badge = new PanelContainer { Name = "RowBadge", CustomMinimumSize = new Vector2(40, 40), MouseFilter = MouseFilterEnum.Ignore };
            badge.AddChild(CenteredLabel("", 16, Colors.White, "RowBadgeLabel"));
            row.AddChild(badge);
            btn.AddChild(row);
            btn.Pressed += () => { _selectedSlot = slotId; RefreshEquipRows(); RefreshRightPanel(); };
            _equipListBox.AddChild(btn);
            _equipRows[slotId] = btn;
        }
    }

    private static T FindRowPart<T>(Node parent, string name) where T : Node
        => (T)parent.FindChild(name, true, false)!;

    private static Label CenteredLabel(string text, int fontSize, Color color, string name)
    {
        var l = new Label
        {
            Name = name,
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        l.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        l.AddThemeFontSizeOverride("font_size", fontSize);
        l.AddThemeColorOverride("font_color", color);
        return l;
    }

    private void RefreshEquipRows()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        foreach (var slot in ProgressionService.Slots)
        {
            if (!_equipRows.TryGetValue(slot.Id, out var btn)) continue;
            var eq = gm.Save.HeroEquipment[_selectedHero][slot.Id];
            bool selected = slot.Id == _selectedSlot;
            var slotColor = UiTheme.SlotColors.TryGetValue(slot.Id, out var c) ? c : UiTheme.Panel;
            btn.AddThemeStyleboxOverride("normal", RowBox(UiTheme.Panel, selected ? UiTheme.Gold : UiTheme.Muted, selected ? 3 : 1));
            btn.AddThemeStyleboxOverride("hover", RowBox(UiTheme.PanelHover, selected ? UiTheme.Gold : UiTheme.Muted, selected ? 3 : 1));
            btn.AddThemeStyleboxOverride("pressed", RowBox(UiTheme.Panel, selected ? UiTheme.Gold : UiTheme.Muted, selected ? 3 : 1));
            var icon = FindRowPart<PanelContainer>(btn, "RowIconPanel");
            icon.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = eq.IsEmpty ? UiTheme.SlotEmpty : slotColor,
                CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            });
            var nameLabel = FindRowPart<Label>(btn, "RowName");
            nameLabel.Text = eq.IsEmpty ? $"{slot.Name} (Trống)" : slot.Name;
            UiTheme.StyleBody(nameLabel, UiTheme.FontButton);
            nameLabel.AddThemeColorOverride("font_color", eq.IsEmpty ? UiTheme.Muted : slotColor);
            var enhLabel = FindRowPart<Label>(btn, "RowEnhance");
            enhLabel.Text = eq.IsEmpty ? "Cường hóa: —" : $"Cường hóa: +{eq.Level}/{ProgressionService.MaxEnhanceLevel}";
            UiTheme.StyleBody(enhLabel, UiTheme.FontSmall);
            enhLabel.AddThemeColorOverride("font_color", UiTheme.Muted);
            var refLabel = FindRowPart<Label>(btn, "RowRefine");
            refLabel.Text = eq.IsEmpty ? "Tẩy luyện: —"
                : $"Tẩy luyện: {ProgressionService.RefineNames[eq.RefineIndex % ProgressionService.RefineNames.Length]}";
            UiTheme.StyleBody(refLabel, UiTheme.FontSmall);
            refLabel.AddThemeColorOverride("font_color", UiTheme.Muted);
            var badge = FindRowPart<PanelContainer>(btn, "RowBadge");
            badge.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = eq.IsEmpty ? UiTheme.DisabledBg : UiTheme.HpColor,
                CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            });
            FindRowPart<Label>(badge, "RowBadgeLabel").Text = eq.IsEmpty ? "–" : $"{eq.RefineIndex}";
        }
    }

    private static StyleBoxFlat RowBox(Color bg, Color border, int borderWidth) => new()
    {
        BgColor = bg, BorderColor = border,
        BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth,
        BorderWidthTop = borderWidth, BorderWidthBottom = borderWidth,
        CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
        CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
        ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 6, ContentMarginBottom = 6,
    };

    // ---------- Right column: system tabs + action panel ----------

    private void BuildSysTabs()
    {
        if (_tabRow == null) return;
        _tabRow.AddThemeConstantOverride("separation", 6);
        foreach (var (id, name) in SysTabs)
        {
            var tabId = id;
            var btn = new Button
            {
                Name = $"SysTab_{id}",
                Text = name,
                CustomMinimumSize = new Vector2(0, 44),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            btn.AddThemeFontSizeOverride("font_size", UiTheme.FontSmallButton);
            btn.Pressed += () => { _selectedTab = tabId; RefreshSysTabs(); RefreshRightPanel(); };
            _tabRow.AddChild(btn);
        }
        RefreshSysTabs();
    }

    private void RefreshSysTabs()
    {
        if (_tabRow == null) return;
        foreach (var btn in _tabRow.GetChildren())
        {
            if (btn is Button b)
            {
                bool active = b.Name == $"SysTab_{_selectedTab}";
                b.AddThemeFontSizeOverride("font_size", UiTheme.FontSmallButton);
                b.AddThemeColorOverride("font_color", active ? UiTheme.DarkText : UiTheme.Text);
                var bg = active ? UiTheme.Gold : UiTheme.Panel;
                var box = new StyleBoxFlat
                {
                    BgColor = bg,
                    CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
                    CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
                };
                b.AddThemeStyleboxOverride("normal", box);
                b.AddThemeStyleboxOverride("hover", box);
                b.AddThemeStyleboxOverride("pressed", box);
            }
        }
    }

    private void RefreshRightPanel()
    {
        if (_tabContent == null) return;
        foreach (var child in _tabContent.GetChildren()) { _tabContent.RemoveChild(child); child.QueueFree(); }
        var gm = GetNode<GameManager>("/root/GameManager");
        var slot = ProgressionService.Slots.Find(s => s.Id == _selectedSlot);
        var eq = gm.Save.HeroEquipment[_selectedHero][_selectedSlot];
        if (_selectedTab == "ke_thua" || _selectedTab == "duc_lo" || _selectedTab == "phu_ma")
        {
            var note = CenteredLabel("Chưa mở.", UiTheme.FontButton, UiTheme.Muted, "LockedNote");
            _tabContent.AddChild(note);
            return;
        }
        if (eq.IsEmpty)
        {
            var note = CenteredLabel("Ô trống — chưa mang trang bị.", UiTheme.FontButton, UiTheme.Muted, "EmptyNote");
            _tabContent.AddChild(note);
            return;
        }
        var slotColor = UiTheme.SlotColors.TryGetValue(_selectedSlot, out var c) ? c : UiTheme.Panel;
        var box = new VBoxContainer();
        box.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        box.AddThemeConstantOverride("separation", 12);
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 16);
        var icon = new PanelContainer { CustomMinimumSize = new Vector2(96, 96) };
        icon.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = slotColor,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
        });
        icon.AddChild(CenteredLabel(AssetIcons.SlotSymbol(_selectedSlot), 34, Colors.White, "PanelIcon"));
        top.AddChild(icon);
        var info = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        info.AddThemeConstantOverride("separation", 6);
        var titleLabel = new Label { Text = $"{slot?.Name} +{eq.Level}" };
        UiTheme.StyleBody(titleLabel, 20);
        titleLabel.AddThemeColorOverride("font_color", slotColor);
        info.AddChild(titleLabel);
        if (_selectedTab == "cuong_hoa")
        {
            info.AddChild(InfoLine($"Cấp cường hóa: +{eq.Level}/{ProgressionService.MaxEnhanceLevel}"));
            info.AddChild(InfoLine(eq.Level >= ProgressionService.MaxEnhanceLevel ? "Chi phí: MAX" : $"Chi phí: {ProgressionService.EnhanceCost(eq.Level)} Vàng"));
            info.AddChild(InfoLine("Tỉ lệ thành công: 100%"));
        }
        else
        {
            var refineName = ProgressionService.RefineNames[eq.RefineIndex % ProgressionService.RefineNames.Length];
            info.AddChild(InfoLine($"Tẩy luyện hiện tại: {refineName}"));
            info.AddChild(InfoLine($"Chi phí: {ProgressionService.RefineCostGold} Vàng"));
            info.AddChild(InfoLine("Mỗi lần tẩy luyện chuyển sang dòng tiếp theo."));
        }
        top.AddChild(info);
        box.AddChild(top);
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 12);
        var mainBtn = new Button
        {
            Name = _selectedTab == "cuong_hoa" ? "ActionEnhance" : "ActionRefine",
            Text = _selectedTab == "cuong_hoa" ? "Cường hóa" : "Tẩy luyện",
            CustomMinimumSize = new Vector2(200, UiTheme.ButtonHeight),
        };
        UiTheme.StyleButton(mainBtn, true);
        if (_selectedTab == "cuong_hoa")
        {
            mainBtn.Disabled = eq.Level >= ProgressionService.MaxEnhanceLevel || gm.Save.Gold < ProgressionService.EnhanceCost(eq.Level);
            mainBtn.Pressed += OnEnhancePressed;
        }
        else
        {
            mainBtn.Disabled = gm.Save.Gold < ProgressionService.RefineCostGold;
            mainBtn.Pressed += OnRefinePressed;
        }
        actions.AddChild(mainBtn);
        var unequipBtn = new Button { Name = "ActionUnequip", Text = "Tháo", CustomMinimumSize = new Vector2(140, UiTheme.ButtonHeight) };
        UiTheme.StyleButton(unequipBtn, false);
        unequipBtn.Pressed += OnUnequipPressed;
        actions.AddChild(unequipBtn);
        box.AddChild(actions);
        var statusLabel = new Label { Name = "ActionStatus", Text = _status, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        UiTheme.StyleBody(statusLabel, UiTheme.FontSmall);
        statusLabel.AddThemeColorOverride("font_color", UiTheme.Muted);
        box.AddChild(statusLabel);
        _tabContent.AddChild(box);
    }

    private static Label InfoLine(string text)
    {
        var l = new Label { Text = text };
        UiTheme.StyleBody(l);
        return l;
    }

    private void OnEnhancePressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var slot = ProgressionService.Slots.Find(s => s.Id == _selectedSlot);
        if (ProgressionService.TryEnhance(gm.Save, _selectedHero, _selectedSlot))
        {
            var eq = gm.Save.HeroEquipment[_selectedHero][_selectedSlot];
            _status = $"Cường hóa {slot?.Name} thành công! (+{eq.Level})";
            gm.SaveGame();
        }
        else _status = "Cường hóa thất bại: đã tối đa hoặc không đủ Vàng!";
        RefreshEquipRows();
        RefreshRightPanel();
    }

    private void OnRefinePressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var slot = ProgressionService.Slots.Find(s => s.Id == _selectedSlot);
        if (ProgressionService.TryRefine(gm.Save, _selectedHero, _selectedSlot))
        {
            var eq = gm.Save.HeroEquipment[_selectedHero][_selectedSlot];
            var name = ProgressionService.RefineNames[eq.RefineIndex % ProgressionService.RefineNames.Length];
            _status = $"Tẩy luyện {slot?.Name} thành công: {name}!";
            gm.SaveGame();
        }
        else _status = "Tẩy luyện thất bại: không đủ Vàng!";
        RefreshEquipRows();
        RefreshRightPanel();
    }

    private void OnUnequipPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var slot = ProgressionService.Slots.Find(s => s.Id == _selectedSlot);
        if (EquipmentService.Unequip(gm.Save, _selectedHero, _selectedSlot))
        {
            _status = $"Đã tháo {slot?.Name} về túi!";
            gm.SaveGame();
        }
        else _status = "Ô này đang trống, không có gì để tháo!";
        RefreshEquipRows();
        RefreshRightPanel();
    }
}
