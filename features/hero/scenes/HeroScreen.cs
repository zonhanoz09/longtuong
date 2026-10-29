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
    private RichTextLabel? _tooltipLabel;
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
        _tooltipLabel = GetNode<RichTextLabel>("TooltipPanel/TooltipLabel");
        _tooltipLabel.AddThemeFontSizeOverride("normal_font_size", 14);
        _tooltipLabel.AddThemeColorOverride("default_color", UiTheme.Text);
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
                ShowTooltip(btn, EquipmentTooltip.ForEquipped(gm2.Save.HeroEquipment[_selectedHero][slotId], slotId, null));
            };
            btn.MouseExited += HideTooltip;
            _equipListBox.AddChild(btn);
            _equipRows[slotId] = btn;
        }
    }

    private static T FindRowPart<T>(Node parent, string name) where T : Node
        => (T)parent.FindChild(name, true, false)!;

    // ---------- Hover tooltip (unified BBCode via EquipmentTooltip) ----------

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
            nameLabel.Text = eq.IsEmpty ? $"{slot.Name} (Trống)" : $"{EquipmentSets.DisplayName(eq.SetId, slot.Id)} +{eq.Level}";
            UiTheme.StyleBody(nameLabel, UiTheme.FontButton);
            nameLabel.AddThemeColorOverride("font_color", eq.IsEmpty ? UiTheme.Muted : EquipmentSets.QualityColor(eq.Quality));
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
                // Phase 2.37: third line shows quality + tier (name already has the set).
                var set = EquipmentSets.Get(eq.SetId);
                refLabel.Text = $"{EquipmentSets.QualityName(eq.Quality)} · Lv{set.Tier}";
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
        if (_selectedTab == "cuong_hoa")
        {
            BuildEnhanceTab();
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
        // Phase 2.37: item name = "{Set} {Slot}" (Huynh), quality-colored.
        var titleLabel = new Label { Text = $"{EquipmentSets.DisplayName(eq.SetId, _selectedSlot)} +{eq.Level}" };
        UiTheme.StyleBody(titleLabel, 20);
        titleLabel.AddThemeColorOverride("font_color", EquipmentSets.QualityColor(eq.Quality));
        info.AddChild(titleLabel);
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
                // Phase 2.37 (Huynh): line text colored by roll grade;
                // the gold border still marks locked lines.
                lineBtn.AddThemeColorOverride("font_color",
                    EquipmentSets.QualityColor(ProgressionService.RefineLineGrade(line)));
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
            // Phase 2.37 (Huynh Q5a): cost grows with locked lines.
            int stoneCost = ProgressionService.RefineCostStones(gm.Save, _selectedHero, _selectedSlot);
            int goldCost = ProgressionService.RefineCostGold(gm.Save, _selectedHero, _selectedSlot);
            string costText = gm.Save.RefineStones >= stoneCost
                ? $"Chi phí: {stoneCost} Đá tẩy luyện (còn {gm.Save.RefineStones})"
                : $"Chi phí: {goldCost} Vàng (hết đá)";
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
            Name = "ActionRefine",
            Text = "Tẩy luyện",
            CustomMinimumSize = new Vector2(200, UiTheme.ButtonHeight),
        };
        UiTheme.StyleButton(mainBtn, true);
        int stoneNeed = ProgressionService.RefineCostStones(gm.Save, _selectedHero, _selectedSlot);
        int goldNeed = ProgressionService.RefineCostGold(gm.Save, _selectedHero, _selectedSlot);
        mainBtn.Disabled = gm.Save.RefineStones < stoneNeed && gm.Save.Gold < goldNeed;
        mainBtn.Pressed += OnRefinePressed;
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

    // Phase 2.37: Cường hóa tab rework per Huynh's sketch — header,
    // projected stats (HIỆN TẠI → DỰ KIẾN), resources, cooldown line,
    // contextual actions: Chế Tạo (empty slot), Cường Hóa + Đúc Lại
    // (equipped), Tháo.
    private void BuildEnhanceTab()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var save = gm.Save;
        var eq = save.HeroEquipment[_selectedHero][_selectedSlot];
        var slotDef = ProgressionService.Slots.Find(s => s.Id == _selectedSlot);
        var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 8);
        _tabContent!.AddChild(box);

        var set = EquipmentSets.Get(eq.IsEmpty ? ForgeService.CraftSetId : eq.SetId);
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 16);
        var icon = new PanelContainer { CustomMinimumSize = new Vector2(96, 96) };
        icon.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = eq.IsEmpty ? UiTheme.SlotEmpty : EquipmentSets.QualityColor(set.Quality),
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
        });
        icon.AddChild(CenteredLabel(AssetIcons.SlotSymbol(_selectedSlot), 34, Colors.White, "PanelIcon"));
        top.AddChild(icon);
        var info = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        info.AddThemeConstantOverride("separation", 6);
        var titleLabel = new Label
        {
            Text = eq.IsEmpty ? $"{set.Name} {slotDef?.Name}"
                              : $"{EquipmentSets.DisplayName(eq.SetId, _selectedSlot)} +{eq.Level}",
        };
        UiTheme.StyleBody(titleLabel, 20);
        titleLabel.AddThemeColorOverride("font_color", EquipmentSets.QualityColor(set.Quality));
        info.AddChild(titleLabel);
        info.AddChild(InfoLine(eq.IsEmpty
            ? $"Ô trống — có thể chế tạo {set.Name} (Lv{set.Tier})."
            : $"{set.Name} · Lv{set.Tier} · {EquipmentSets.QualityName(set.Quality)}"));
        top.AddChild(info);
        box.AddChild(top);

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 12);

        if (eq.IsEmpty)
        {
            // ---- Chế Tạo (Huynh 7b: only the starter Tân Thủ set) ----
            var preview = new EquipmentState
            {
                IsEmpty = false, Level = 0, SetId = ForgeService.CraftSetId,
                Tier = set.Tier, Quality = set.Quality,
            };
            AddProjection(box, "DỰ KIẾN SAU CHẾ TẠO:",
                EquipmentService.ProjectEquipChange(save, _selectedHero, _selectedSlot, preview));
            box.AddChild(SectionLabel("TÀI NGUYÊN TIÊU TỐN:"));
            int oreNeed = ForgeService.CraftOreCost(), silverNeed = ForgeService.CraftSilverCost();
            box.AddChild(ResourceLine("Bạc", save.Silver, silverNeed));
            box.AddChild(ResourceLine(ForgeService.OreName, ForgeService.OreCount(save), oreNeed));
            var craftBtn = new Button { Name = "ActionCraft", Text = "Chế Tạo", CustomMinimumSize = new Vector2(200, UiTheme.ButtonHeight) };
            UiTheme.StyleButton(craftBtn, true);
            craftBtn.Disabled = save.Silver < silverNeed || ForgeService.OreCount(save) < oreNeed;
            craftBtn.Pressed += () =>
            {
                var gm2 = GetNode<GameManager>("/root/GameManager");
                string msg = ForgeService.TryCraftIntoSlot(gm2.Save, _selectedHero, _selectedSlot, _rng) switch
                {
                    ForgeService.CraftResult.Success => $"Chế tạo thành công {EquipmentSets.DisplayName(ForgeService.CraftSetId, _selectedSlot)}!",
                    ForgeService.CraftResult.NoOre => "Không đủ Khoáng thạch!",
                    ForgeService.CraftResult.NoSilver => "Không đủ Bạc!",
                    _ => "Không chế tạo được!",
                };
                _status = msg;
                gm2.SaveGame();
                RefreshEquipRows(); RefreshRightPanel();
            };
            actions.AddChild(craftBtn);
        }
        else
        {
            // ---- Cường hóa ----
            int maxEnh = ProgressionService.MaxEnhanceForHero(save);
            box.AddChild(SectionLabel("CƯỜNG HÓA:"));
            box.AddChild(InfoLine($"Cấp: +{eq.Level}/{maxEnh}" + (eq.Level >= maxEnh ? " (MAX)" : "")));
            box.AddChild(InfoLine($"Tỉ lệ thành công: {ProgressionService.EnhanceSuccessRate(eq.Level + 1):P0}"));
            var hypEnh = EquipmentService.CopyState(eq);
            hypEnh.Level = eq.Level + 1;
            AddProjection(box, "DỰ KIẾN SAU CƯỜNG HÓA:",
                eq.Level >= maxEnh
                    ? new List<(string, double, double)>()
                    : EquipmentService.ProjectEquipChange(save, _selectedHero, _selectedSlot, hypEnh));
            int enhCost = ProgressionService.EnhanceCostSilver(eq.Level);
            box.AddChild(ResourceLine("Bạc", save.Silver, eq.Level >= maxEnh ? 0 : enhCost));
            var enhBtn = new Button { Name = "ActionEnhance", Text = "Cường hóa", CustomMinimumSize = new Vector2(200, UiTheme.ButtonHeight) };
            UiTheme.StyleButton(enhBtn, true);
            enhBtn.Disabled = eq.Level >= maxEnh || save.Silver < enhCost;
            enhBtn.Pressed += OnEnhancePressed;
            actions.AddChild(enhBtn);

            // ---- Đúc Lại ----
            string? next = ForgeService.NextSet(eq.SetId);
            if (next != null)
            {
                var target = EquipmentSets.Get(next);
                box.AddChild(SectionLabel("ĐÚC LẠI:"));
                var recastTitle = new Label { Text = $"{set.Name} → {target.Name} (Lv{target.Tier})", AutowrapMode = TextServer.AutowrapMode.WordSmart };
                UiTheme.StyleBody(recastTitle, UiTheme.FontButton);
                recastTitle.AddThemeColorOverride("font_color", EquipmentSets.QualityColor(target.Quality));
                box.AddChild(recastTitle);
                int mainLevel = ProgressionService.ProgressOf(save, "custom_hero").Level;
                bool hasBp = ForgeService.HasBlueprint(save, next);
                int oreNeed = ForgeService.RecastOreCost(next), silverNeed = ForgeService.RecastSilverCost(next);
                box.AddChild(ReqLine($"Bản vẽ {target.Name}", hasBp));
                box.AddChild(ReqLine($"Cấp tướng chính Lv{target.Tier} (hiện Lv{mainLevel})", mainLevel >= target.Tier));
                box.AddChild(ResourceLine("Bạc", save.Silver, silverNeed));
                box.AddChild(ResourceLine(ForgeService.OreName, ForgeService.OreCount(save), oreNeed));
                var hypRecast = EquipmentService.CopyState(eq);
                hypRecast.SetId = next; hypRecast.Tier = target.Tier; hypRecast.Quality = target.Quality;
                AddProjection(box, "DỰ KIẾN SAU ĐÚC LẠI:",
                    EquipmentService.ProjectEquipChange(save, _selectedHero, _selectedSlot, hypRecast));
                var recastBtn = new Button { Name = "ActionRecast", Text = "Đúc Lại", CustomMinimumSize = new Vector2(200, UiTheme.ButtonHeight) };
                UiTheme.StyleButton(recastBtn, true);
                recastBtn.Disabled = !hasBp || mainLevel < target.Tier || save.Silver < silverNeed || ForgeService.OreCount(save) < oreNeed;
                recastBtn.Pressed += () =>
                {
                    var gm2 = GetNode<GameManager>("/root/GameManager");
                    string msg = ForgeService.TryRecast(gm2.Save, _selectedHero, _selectedSlot, _rng) switch
                    {
                        ForgeService.RecastResult.Success => $"Đúc lại thành công lên {target.Name}!",
                        ForgeService.RecastResult.LowLevel => $"Cấp tướng chính chưa đủ (cần Lv{target.Tier})!",
                        ForgeService.RecastResult.NoBlueprint => $"Chưa học bản vẽ {target.Name}!",
                        ForgeService.RecastResult.NoOre => "Không đủ Khoáng thạch!",
                        ForgeService.RecastResult.NoSilver => "Không đủ Bạc!",
                        _ => "Không đúc lại được!",
                    };
                    _status = msg;
                    gm2.SaveGame();
                    RefreshEquipRows(); RefreshRightPanel();
                };
                actions.AddChild(recastBtn);
            }

            var unequipBtn = new Button { Name = "ActionUnequip", Text = "Tháo", CustomMinimumSize = new Vector2(140, UiTheme.ButtonHeight) };
            UiTheme.StyleButton(unequipBtn, false);
            unequipBtn.Pressed += OnUnequipPressed;
            actions.AddChild(unequipBtn);
        }
        box.AddChild(actions);

        // Cooldown line (Huynh Q6b: display only).
        box.AddChild(InfoLine("Cooldown cường hóa: Sẵn sàng"));

        foreach (var line in SetBonusService.DescribeForHero(save, _selectedHero))
        {
            var setLabel = new Label { Text = line, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            UiTheme.StyleBody(setLabel, UiTheme.FontSmall);
            setLabel.AddThemeColorOverride("font_color", UiTheme.Gold);
            box.AddChild(setLabel);
        }

        var statusLabel = new Label { Name = "ActionStatus", Text = _status, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        UiTheme.StyleBody(statusLabel, UiTheme.FontSmall);
        statusLabel.AddThemeColorOverride("font_color", UiTheme.Muted);
        box.AddChild(statusLabel);
    }

    private static Label ReqLine(string text, bool ok)
    {
        var l = InfoLine($"{(ok ? "✓" : "✗")} {text}");
        if (!ok) l.AddThemeColorOverride("font_color", new Color(1, 0.4f, 0.4f));
        return l;
    }

    private static Label ResourceLine(string name, int have, int need)
    {
        var l = InfoLine($"{name}: {need:N0} / {have:N0}");
        if (have < need) l.AddThemeColorOverride("font_color", new Color(1, 0.4f, 0.4f));
        return l;
    }

    private static void AddProjection(VBoxContainer box, string header, List<(string Name, double Before, double After)> rows)
    {
        box.AddChild(SectionLabel(header));
        if (rows.Count == 0)
        {
            box.AddChild(InfoLine("(không thay đổi)"));
            return;
        }
        foreach (var (name, b, a) in rows)
        {
            bool pct = name is "Bạo kích" or "Né tránh";
            string fb = pct ? $"{b * 100:0.0}%" : $"{b:N0}";
            string fa = pct ? $"{a * 100:0.0}%" : $"{a:N0}";
            string fd = pct ? $"{(a - b) * 100:+0.0;-0.0}%" : $"{(a - b):+N0;-N0}";
            box.AddChild(InfoLine($"{name}: {fb} → {fa} ({fd})"));
        }
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
