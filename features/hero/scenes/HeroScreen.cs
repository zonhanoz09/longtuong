using Godot;
using System.Collections.Generic;
using System.Linq;

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
        ("nang_cap", "Nâng cấp"),
        ("phu_ma", "Phụ ma"),
    };

    private string _selectedHero = "";
    private string _selectedSlot = "weapon";
    private string _selectedTab = "cuong_hoa";
    private string _status = "";
    // Phase 2.33: socket index awaiting a gem pick (-1 = none).
    private int _pendingSocket = -1;
    // Phase 2.35: bag index of the inherit source (-1 = none).
    private int _inheritSource = -1;
    // Hover tooltip (gem info on equipment rows).
    private PanelContainer? _tooltipPanel;
    private Label? _tooltipLabel;
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
        // Phase 2.34: forge entry (FunctionBar is full; forge lives here).
        var forgeBtn = new Button
        {
            Name = "ForgeOpenButton",
            Text = "Lò rèn",
            CustomMinimumSize = new Vector2(140, 40),
        };
        // x=940: ends at 1080, clear of the hub's X button (starts ~1112).
        forgeBtn.SetPosition(new Vector2(940, 4));
        UiTheme.StyleButton(forgeBtn, false);
        forgeBtn.Pressed += () => FindHub()?.OpenPopup("res://features/forge/scenes/ForgeScene.tscn", ForgeScene.PopupSize);
        AddChild(forgeBtn);
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        if (!string.IsNullOrEmpty(gm.SelectedHeroId) && gm.Save.OwnedHeroIds.Contains(gm.SelectedHeroId))
            _selectedHero = gm.SelectedHeroId;
        else if (gm.Save.OwnedHeroIds.Count > 0)
            _selectedHero = gm.Save.OwnedHeroIds[0];
        _heroListBox = GetNode<VBoxContainer>("HeroListBox");
        _equipListBox = GetNode<VBoxContainer>("EquipListBox");
        _tabRow = GetNode<HBoxContainer>("RightPanel/TabRow");
        _tabContent = GetNode<Control>("RightPanel/TabContent");
        _tooltipPanel = GetNode<PanelContainer>("TooltipPanel");
        _tooltipLabel = GetNode<Label>("TooltipPanel/TooltipLabel");
        UiTheme.StyleBody(_tooltipLabel, 14);
        _tooltipLabel.CustomMinimumSize = new Vector2(280, 0);
        _tooltipPanel.MouseFilter = MouseFilterEnum.Ignore;
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
            // Phase 2.29b (Huynh): non-owned heroes are hidden entirely —
            // the equipment screen only lists owned heroes.
            if (!gm.Save.OwnedHeroIds.Contains(hero.Id)) continue;
            var id = hero.Id;
            var btn = new Button
            {
                Name = $"Hero_{id}",
                Text = hero.Name,
                CustomMinimumSize = new Vector2(190, UiTheme.ButtonHeight),
            };
            UiTheme.StyleButton(btn, false);
            btn.Pressed += () => { _selectedHero = id; _pendingSocket = -1; _inheritSource = -1; RefreshHeroList(); RefreshEquipRows(); RefreshRightPanel(); };
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
            btn.Pressed += () => { _selectedSlot = slotId; _pendingSocket = -1; _inheritSource = -1; RefreshEquipRows(); RefreshRightPanel(); };
            btn.MouseEntered += () =>
            {
                var gm2 = GetNode<GameManager>("/root/GameManager");
                ShowTooltip(btn, GemTooltipText(gm2.Save.HeroEquipment[_selectedHero][slotId]));
            };
            btn.MouseExited += HideTooltip;
            _equipListBox.AddChild(btn);
            _equipRows[slotId] = btn;
        }
    }

    private static T FindRowPart<T>(Node parent, string name) where T : Node
        => (T)parent.FindChild(name, true, false)!;

    // ---------- Hover tooltip: socketed gems ----------

    private static string GemTooltipText(EquipmentState eq)
    {
        if (eq.IsEmpty) return "Ô trống — chưa mang trang bị.";
        var lines = new List<string> { "Ngọc khảm:" };
        for (int i = 0; i < eq.Sockets.Count; i++)
        {
            var s = eq.Sockets[i];
            string desc = !s.Unlocked ? "(chưa mở)"
                : string.IsNullOrEmpty(s.GemId) ? "(trống)"
                : GemService.DescribeGem(s.GemId);
            lines.Add($"Lỗ {i + 1}: {desc}");
        }
        return string.Join("\n", lines);
    }

    private void ShowTooltip(Control cell, string text)
    {
        if (_tooltipPanel == null || _tooltipLabel == null) return;
        _tooltipLabel.Text = text;
        int lines = System.Math.Max(1, text.Split('\n').Length);
        float height = 16 + lines * 20 + 8;
        _tooltipPanel.CustomMinimumSize = new Vector2(280, height);
        _tooltipPanel.Size = new Vector2(280, height);
        _tooltipPanel.Visible = true;
        _tooltipPanel.MoveToFront();
        var cellGlobal = cell.GetGlobalRect();
        var rootGlobal = GetGlobalRect();
        var cellPos = cellGlobal.Position - rootGlobal.Position;
        float x = cellPos.X + cellGlobal.Size.X + 8;
        if (x + 280 > 1140) x = cellPos.X - 280 - 8;
        if (x < 0) x = 0;
        float y = cellPos.Y;
        if (y + height > 680) y = 680 - height;
        if (y < 0) y = 0;
        _tooltipPanel.Position = new Vector2(x, y);
    }

    private void HideTooltip()
    {
        if (_tooltipPanel != null) _tooltipPanel.Visible = false;
    }

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
            enhLabel.Text = eq.IsEmpty ? "Cường hóa: —" : $"Cường hóa: +{eq.Level}/{ProgressionService.MaxEnhanceForHero(gm.Save)}";
            UiTheme.StyleBody(enhLabel, UiTheme.FontSmall);
            enhLabel.AddThemeColorOverride("font_color", UiTheme.Muted);
            var refLabel = FindRowPart<Label>(btn, "RowRefine");
            if (eq.IsEmpty)
            {
                refLabel.Text = "Tẩy luyện: —";
                UiTheme.StyleBody(refLabel, UiTheme.FontSmall);
                refLabel.AddThemeColorOverride("font_color", UiTheme.Muted);
            }
            else
            {
                // Phase 2.30: third line shows the set (quality-colored).
                var set = EquipmentSets.Get(eq.SetId);
                refLabel.Text = $"{set.Name} · Lv{set.Tier}";
                UiTheme.StyleBody(refLabel, UiTheme.FontSmall);
                refLabel.AddThemeColorOverride("font_color", EquipmentSets.QualityColor(eq.Quality));
            }
            var badge = FindRowPart<PanelContainer>(btn, "RowBadge");
            badge.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = eq.IsEmpty ? UiTheme.DisabledBg : UiTheme.HpColor,
                CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            });
            FindRowPart<Label>(badge, "RowBadgeLabel").Text = eq.IsEmpty ? "–" : $"{eq.RefineLines.Count}";
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
            btn.Pressed += () => { _selectedTab = tabId; _pendingSocket = -1; _inheritSource = -1; RefreshSysTabs(); RefreshRightPanel(); };
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
        if (_selectedTab == "phu_ma")
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
        if (_selectedTab == "duc_lo")
        {
            BuildGemTab();
            return;
        }
        if (_selectedTab == "ke_thua")
        {
            BuildInheritTab();
            return;
        }
        if (_selectedTab == "nang_cap")
        {
            BuildUpgradeTab();
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
            // Phase 2.31b: silver cost, hero-level cap, success rate.
            int maxEnh = ProgressionService.MaxEnhanceForHero(gm.Save);
            info.AddChild(InfoLine($"Cấp cường hóa: +{eq.Level}/{maxEnh} (tối đa theo cấp tướng chính)"));
            info.AddChild(InfoLine(eq.Level >= maxEnh ? "Chi phí: MAX" : $"Chi phí: {ProgressionService.EnhanceCostSilver(eq.Level)} Bạc"));
            info.AddChild(InfoLine($"Tỉ lệ thành công: {ProgressionService.EnhanceSuccessRate(eq.Level + 1):P0}"));
        }
        else
        {
            // Phase 2.32: lines with lock toggles.
            info.AddChild(InfoLine($"Tẩy luyện ({eq.RefineLines.Count} dòng) — bấm dòng để khóa/mở:"));
            for (int li = 0; li < eq.RefineLines.Count; li++)
            {
                var line = eq.RefineLines[li];
                var lineBtn = new Button
                {
                    Name = $"RefineLine_{li}",
                    Text = $"{li + 1}. {EquipmentService.DescribeRefineLine(line)}",
                    CustomMinimumSize = new Vector2(0, 44),
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                lineBtn.AddThemeFontSizeOverride("font_size", UiTheme.FontSmallButton);
                bool locked = line.Locked;
                lineBtn.AddThemeColorOverride("font_color", locked ? UiTheme.Gold : UiTheme.Text);
                var lb = new StyleBoxFlat
                {
                    BgColor = locked ? UiTheme.PanelHover : UiTheme.Panel,
                    BorderColor = locked ? UiTheme.Gold : UiTheme.Muted,
                    BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
                    CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
                    CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
                };
                lineBtn.AddThemeStyleboxOverride("normal", lb);
                lineBtn.AddThemeStyleboxOverride("hover", lb);
                lineBtn.AddThemeStyleboxOverride("pressed", lb);
                int captured = li;
                lineBtn.Pressed += () =>
                {
                    var gm2 = GetNode<GameManager>("/root/GameManager");
                    ProgressionService.ToggleRefineLock(gm2.Save, _selectedHero, _selectedSlot, captured);
                    gm2.SaveGame();
                    RefreshEquipRows();
                    RefreshRightPanel();
                };
                info.AddChild(lineBtn);
            }
            string costText = gm.Save.RefineStones >= ProgressionService.RefineStoneCost
                ? $"Chi phí: {ProgressionService.RefineStoneCost} Đá tẩy luyện (còn {gm.Save.RefineStones})"
                : $"Chi phí: {ProgressionService.RefineGoldFallback} Vàng (hết đá)";
            info.AddChild(InfoLine(costText));
        }
        top.AddChild(info);
        box.AddChild(top);
        // Phase 2.30: active set bonuses for the selected hero.
        foreach (var line in SetBonusService.DescribeForHero(gm.Save, _selectedHero))
        {
            var setLabel = new Label { Text = line, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            UiTheme.StyleBody(setLabel, UiTheme.FontSmall);
            setLabel.AddThemeColorOverride("font_color", UiTheme.Gold);
            box.AddChild(setLabel);
        }
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
            int maxEnh = ProgressionService.MaxEnhanceForHero(gm.Save);
            mainBtn.Disabled = eq.Level >= maxEnh || gm.Save.Silver < ProgressionService.EnhanceCostSilver(eq.Level);
            mainBtn.Pressed += OnEnhancePressed;
        }
        else
        {
            mainBtn.Disabled = gm.Save.RefineStones < ProgressionService.RefineStoneCost
                && gm.Save.Gold < ProgressionService.RefineGoldFallback;
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

    // ---------- Phase 2.33: gem socketing ("Đục lỗ" tab) ----------

    private void BuildGemTab()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var save = gm.Save;
        GemService.EnsureSockets(save.HeroEquipment[_selectedHero][_selectedSlot]);
        var eq = save.HeroEquipment[_selectedHero][_selectedSlot];
        var scroll = new ScrollContainer();
        scroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(box);
        _tabContent!.AddChild(scroll);

        box.AddChild(SectionLabel("Lỗ khảm:"));
        for (int i = 0; i < eq.Sockets.Count; i++)
        {
            var sock = eq.Sockets[i];
            var row = new HBoxContainer { Name = $"SocketRow_{i}" };
            row.AddThemeConstantOverride("separation", 8);
            string infoText;
            string actionText;
            if (!sock.Unlocked)
            {
                infoText = $"Lỗ {i + 1}: (chưa mở)";
                actionText = $"Mở ({GemService.UnlockCostGold(i)} Vàng)";
            }
            else if (string.IsNullOrEmpty(sock.GemId))
            {
                infoText = $"Lỗ {i + 1}: (trống)";
                actionText = "Khảm";
            }
            else
            {
                infoText = $"Lỗ {i + 1}: {GemService.DescribeGem(sock.GemId)}";
                actionText = "Gỡ";
            }
            var infoLabel = new Label { Text = infoText, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            UiTheme.StyleBody(infoLabel, UiTheme.FontSmall);
            row.AddChild(infoLabel);
            var si = i;
            var actionBtn = new Button
            {
                Name = $"SocketAction_{i}",
                Text = actionText,
                CustomMinimumSize = new Vector2(150, 44),
            };
            actionBtn.AddThemeFontSizeOverride("font_size", UiTheme.FontSmallButton);
            UiTheme.StyleButton(actionBtn, false);
            if (!sock.Unlocked)
                actionBtn.Disabled = save.Gold < GemService.UnlockCostGold(i);
            actionBtn.Pressed += () => OnSocketAction(si);
            row.AddChild(actionBtn);
            box.AddChild(row);
        }

        if (_pendingSocket >= 0 && _pendingSocket < eq.Sockets.Count)
        {
            box.AddChild(SectionLabel($"Chọn ngọc khảm vào lỗ {_pendingSocket + 1}:"));
            var owned = save.GemInventory.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key).ToList();
            if (owned.Count == 0)
                box.AddChild(InfoLine("Túi ngọc trống — mua ngọc ở bên dưới."));
            foreach (var (gemId, count) in owned)
            {
                var pickRow = new HBoxContainer();
                pickRow.AddThemeConstantOverride("separation", 8);
                var pickLabel = new Label { Text = $"{GemService.DescribeGem(gemId)} ×{count}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
                UiTheme.StyleBody(pickLabel, UiTheme.FontSmall);
                pickRow.AddChild(pickLabel);
                var pickBtn = new Button { Name = $"GemPick_{gemId}", Text = "Khảm", CustomMinimumSize = new Vector2(110, 40) };
                pickBtn.AddThemeFontSizeOverride("font_size", UiTheme.FontSmallButton);
                UiTheme.StyleButton(pickBtn, true);
                var gid = gemId;
                pickBtn.Pressed += () => OnGemPick(gid);
                pickRow.AddChild(pickBtn);
                box.AddChild(pickRow);
            }
        }

        box.AddChild(new HSeparator());
        box.AddChild(SectionLabel("Túi ngọc:"));
        var pouch = save.GemInventory.Where(kv => kv.Value > 0).OrderBy(kv => kv.Key).ToList();
        if (pouch.Count == 0)
            box.AddChild(InfoLine("Chưa có ngọc."));
        foreach (var (gemId, count) in pouch)
        {
            var (type, level) = GemService.ParseGemId(gemId);
            var gemRow = new HBoxContainer();
            gemRow.AddThemeConstantOverride("separation", 8);
            var gemLabel = new Label { Text = $"{GemService.DescribeGem(gemId)} ×{count}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            UiTheme.StyleBody(gemLabel, UiTheme.FontSmall);
            gemRow.AddChild(gemLabel);
            if (count >= 3 && level < GemService.MaxGemLevel)
            {
                var mergeBtn = new Button { Name = $"GemMerge_{type}_{level}", Text = "Ghép 3→1", CustomMinimumSize = new Vector2(110, 40) };
                mergeBtn.AddThemeFontSizeOverride("font_size", UiTheme.FontSmallButton);
                UiTheme.StyleButton(mergeBtn, false);
                var gt = type; var gl = level;
                mergeBtn.Pressed += () =>
                {
                    var gm2 = GetNode<GameManager>("/root/GameManager");
                    _status = GemService.TryMerge(gm2.Save, gt, gl)
                        ? $"Ghép thành công: {GemService.DescribeGem(GemService.MakeGemId(gt, gl + 1))}!"
                        : "Ghép thất bại!";
                    gm2.SaveGame();
                    RefreshEquipRows();
                    RefreshRightPanel();
                };
                gemRow.AddChild(mergeBtn);
            }
            box.AddChild(gemRow);
        }
        var buyBtn = new Button
        {
            Name = "GemBuyButton",
            Text = $"Mua ngọc ngẫu nhiên Lv1 ({GemService.BuyGemGold} Vàng)",
            CustomMinimumSize = new Vector2(0, UiTheme.ButtonHeight),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        UiTheme.StyleButton(buyBtn, false);
        buyBtn.Disabled = save.Gold < GemService.BuyGemGold;
        buyBtn.Pressed += () =>
        {
            var gm2 = GetNode<GameManager>("/root/GameManager");
            _status = GemService.TryBuyGem(gm2.Save, _rng)
                ? "Mua ngọc thành công!"
                : "Không đủ Vàng!";
            gm2.SaveGame();
            RefreshRightPanel();
        };
        box.AddChild(buyBtn);

        if (!string.IsNullOrEmpty(_status))
        {
            var statusLabel = new Label { Text = _status, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            UiTheme.StyleBody(statusLabel, UiTheme.FontSmall);
            statusLabel.AddThemeColorOverride("font_color", UiTheme.Muted);
            box.AddChild(statusLabel);
        }
    }

    // ---------- Phase 2.35: Kế thừa (inherit) tab ----------

    private void BuildInheritTab()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var save = gm.Save;
        var eq = save.HeroEquipment[_selectedHero][_selectedSlot];
        var slot = ProgressionService.Slots.Find(s => s.Id == _selectedSlot);
        var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 8);
        _tabContent!.AddChild(box);

        var set = EquipmentSets.Get(eq.SetId);
        box.AddChild(InfoLine($"Món đang mang: {slot?.Name} {set.Name} +{eq.Level} ({eq.RefineLines.Count} dòng tẩy)"));
        box.AddChild(SectionLabel("Chọn món cũ trong túi (cùng ô) — món cũ sẽ mất:"));
        var sources = save.BagEquipment
            .Select((item, idx) => (item, idx))
            .Where(t => t.item.SlotId == _selectedSlot)
            .ToList();
        if (sources.Count == 0)
            box.AddChild(InfoLine("Trong túi không có món nào cùng ô."));
        foreach (var (item, idx) in sources)
        {
            var itemSet = EquipmentSets.Get(item.SetId);
            var srcBtn = new Button
            {
                Name = $"InheritSrc_{idx}",
                Text = $"{itemSet.Name} +{item.Level} ({item.RefineLines.Count} dòng){(idx == _inheritSource ? " ✓" : "")}",
                ToggleMode = true,
                ButtonPressed = idx == _inheritSource,
                CustomMinimumSize = new Vector2(0, 44),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            srcBtn.AddThemeFontSizeOverride("font_size", UiTheme.FontSmallButton);
            var bi = idx;
            srcBtn.Pressed += () => { _inheritSource = bi; RefreshRightPanel(); };
            box.AddChild(srcBtn);
        }
        box.AddChild(InfoLine($"Chi phí: {EquipmentService.InheritCostSilver} Bạc"));
        var inheritBtn = new Button
        {
            Name = "InheritButton",
            Text = "Kế thừa",
            CustomMinimumSize = new Vector2(0, UiTheme.ButtonHeight),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Disabled = _inheritSource < 0 || save.Silver < EquipmentService.InheritCostSilver,
        };
        UiTheme.StyleButton(inheritBtn, true);
        inheritBtn.Pressed += () =>
        {
            var gm2 = GetNode<GameManager>("/root/GameManager");
            bool ok = EquipmentService.TryInherit(gm2.Save, _selectedHero, _selectedSlot, _inheritSource);
            _status = ok ? "Kế thừa thành công! Món cũ đã mất." : "Kế thừa thất bại: kiểm tra Bạc và món đã chọn!";
            _inheritSource = -1;
            gm2.SaveGame();
            RefreshEquipRows();
            RefreshRightPanel();
        };
        box.AddChild(inheritBtn);
        var statusLabel = new Label { Name = "InheritStatus", Text = _status, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        UiTheme.StyleBody(statusLabel, UiTheme.FontSmall);
        statusLabel.AddThemeColorOverride("font_color", UiTheme.Muted);
        box.AddChild(statusLabel);
    }

    // ---------- Phase 2.36: Nâng cấp (upgrade tier) tab ----------

    private void BuildUpgradeTab()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var save = gm.Save;
        var eq = save.HeroEquipment[_selectedHero][_selectedSlot];
        var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 8);
        _tabContent!.AddChild(box);
        var cur = EquipmentSets.Get(eq.SetId);
        string? next = ForgeService.NextSet(eq.SetId);
        if (next == null)
        {
            box.AddChild(CenteredLabel("Đã đạt bậc cao nhất.", UiTheme.FontButton, UiTheme.Gold, "MaxTierNote"));
            return;
        }
        var target = EquipmentSets.Get(next);
        var titleLabel = new Label
        {
            Text = $"{cur.Name} (Lv{cur.Tier}) → {target.Name} (Lv{target.Tier})",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        UiTheme.StyleBody(titleLabel, UiTheme.FontButton);
        titleLabel.AddThemeColorOverride("font_color", EquipmentSets.QualityColor(target.Quality));
        box.AddChild(titleLabel);
        int mainLevel = ProgressionService.ProgressOf(save, "custom_hero").Level;
        int cost = ForgeService.UpgradeSilverCost(next);
        box.AddChild(InfoLine($"Yêu cầu cấp tướng chính: {target.Tier} (hiện {mainLevel})"));
        box.AddChild(InfoLine($"Chi phí: {cost} Bạc"));
        box.AddChild(InfoLine("Giữ nguyên cường hóa, tẩy luyện, lỗ khảm và ngọc; tự bù dòng/lỗ mới."));
        var upBtn = new Button
        {
            Name = "UpgradeButton",
            Text = "Nâng cấp",
            CustomMinimumSize = new Vector2(0, UiTheme.ButtonHeight),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Disabled = mainLevel < target.Tier || save.Silver < cost,
        };
        UiTheme.StyleButton(upBtn, true);
        upBtn.Pressed += () =>
        {
            var gm2 = GetNode<GameManager>("/root/GameManager");
            string msg = ForgeService.TryUpgradeTier(gm2.Save, _selectedHero, _selectedSlot, _rng) switch
            {
                ForgeService.UpgradeResult.Success => $"Nâng cấp thành công lên {target.Name}!",
                ForgeService.UpgradeResult.LowLevel => $"Cấp tướng chính chưa đủ (cần Lv{target.Tier})!",
                ForgeService.UpgradeResult.NoSilver => "Không đủ Bạc!",
                _ => "Không nâng cấp được!",
            };
            _status = msg;
            gm2.SaveGame();
            RefreshEquipRows();
            RefreshRightPanel();
        };
        box.AddChild(upBtn);
        var statusLabel = new Label { Name = "UpgradeStatus", Text = _status, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        UiTheme.StyleBody(statusLabel, UiTheme.FontSmall);
        statusLabel.AddThemeColorOverride("font_color", UiTheme.Muted);
        box.AddChild(statusLabel);
    }

    // Finds the hosting HubScene by walking up (works in game and in tests).
    private HubScene? FindHub()
    {
        Node? n = this;
        while (n != null)
        {
            if (n is HubScene hub) return hub;
            n = n.GetParent();
        }
        return null;
    }

    private static Label SectionLabel(string text)
    {
        var l = new Label { Text = text };
        UiTheme.StyleBody(l, UiTheme.FontButton);
        l.AddThemeColorOverride("font_color", UiTheme.Gold);
        return l;
    }

    private void OnSocketAction(int socketIndex)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var save = gm.Save;
        var eq = save.HeroEquipment[_selectedHero][_selectedSlot];
        var sock = eq.Sockets[socketIndex];
        if (!sock.Unlocked)
        {
            _status = GemService.TryUnlockSocket(save, _selectedHero, _selectedSlot, socketIndex)
                ? $"Đã mở lỗ khảm {socketIndex + 1}!"
                : "Không đủ Bạc để mở lỗ!";
            gm.SaveGame();
        }
        else if (string.IsNullOrEmpty(sock.GemId))
        {
            _pendingSocket = socketIndex;
        }
        else
        {
            _status = GemService.TryUnsocket(save, _selectedHero, _selectedSlot, socketIndex)
                ? "Đã gỡ ngọc về túi!"
                : "Gỡ ngọc thất bại!";
            gm.SaveGame();
        }
        RefreshEquipRows();
        RefreshRightPanel();
    }

    private void OnGemPick(string gemId)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        _status = GemService.TrySocket(gm.Save, _selectedHero, _selectedSlot, _pendingSocket, gemId)
            ? $"Đã khảm {GemService.DescribeGem(gemId)}!"
            : "Khảm thất bại!";
        _pendingSocket = -1;
        gm.SaveGame();
        RefreshEquipRows();
        RefreshRightPanel();
    }

    // Phase 2.31b/2.32: shared RNG so rapid clicks don't re-seed identically.
    private static readonly System.Random _rng = new();

    private void OnEnhancePressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var slot = ProgressionService.Slots.Find(s => s.Id == _selectedSlot);
        int cost = ProgressionService.EnhanceCostSilver(gm.Save.HeroEquipment[_selectedHero][_selectedSlot].Level);
        switch (ProgressionService.TryEnhance(gm.Save, _selectedHero, _selectedSlot, _rng))
        {
            case ProgressionService.EnhanceResult.Success:
                var eq = gm.Save.HeroEquipment[_selectedHero][_selectedSlot];
                _status = $"Cường hóa {slot?.Name} thành công! (+{eq.Level})";
                gm.SaveGame();
                break;
            case ProgressionService.EnhanceResult.Failed_Maxed:
                _status = $"Đã đạt cấp tối đa (+{ProgressionService.MaxEnhanceForHero(gm.Save)}, theo cấp tướng chính)!";
                break;
            case ProgressionService.EnhanceResult.Failed_NoSilver:
                _status = "Không đủ Bạc! Rung Cây Rung Tiền để kiếm thêm.";
                break;
            case ProgressionService.EnhanceResult.Failed_Roll:
                _status = $"Cường hóa thất bại! (mất {cost} Bạc)";
                gm.SaveGame();
                break;
            default:
                _status = "Ô này đang trống, không có gì để cường hóa!";
                break;
        }
        RefreshEquipRows();
        RefreshRightPanel();
    }

    private void OnRefinePressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var slot = ProgressionService.Slots.Find(s => s.Id == _selectedSlot);
        if (ProgressionService.TryRefine(gm.Save, _selectedHero, _selectedSlot, _rng))
        {
            _status = $"Tẩy luyện {slot?.Name} thành công!";
            gm.SaveGame();
        }
        else _status = "Tẩy luyện thất bại: ô trống hoặc không đủ đá/Vàng!";
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
