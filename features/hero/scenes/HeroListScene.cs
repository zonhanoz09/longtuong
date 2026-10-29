using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.19 hero/equipment screen (popup 1000x600, all sizes fixed), with a
/// Phase 2.12 scene-first pilot: the static layout now lives in
/// HeroListScene.tscn (detail labels, portrait placeholder, EXP bar,
/// equipped slots, unequip-all button) and the code only fills in data.
/// Phase 2.16 layout (from Huynh's Vo Tuong popup sketch): the 6 equipped
/// slots flank the portrait, 3 on the left (weapon/armor/helmet) and 3 on
/// the right (boots/cloak/bracer), instead of a single row below it.
/// What stays in code is genuinely data-driven: one tab button per hero
/// (from the HeroTabButton.tscn template) and one grid/equipped cell per
/// item (from the EquipmentCell.tscn template) — their count depends on the
/// save, so they cannot be static scene nodes.
/// - Vertical tabs: one tab button per hero stacked in a left column (the
///   2.4 tab-per-hero pattern, but tabs stacked vertically on the left
///   instead of horizontally on top). Godot 4.7 TabContainer only supports
///   Top/Bottom tabs, so vertical tabs are a button column + a shared detail
///   panel — same look and behavior. Clicking a tab shows that hero's detail.
/// - Right panel: equipment grid, 4 columns, 7 rows (28 cells) of 64x64
///   square icon-only cells; rows grow past 7 inside a ScrollContainer when
///   the bag holds more than 28 items (Phase 2.7 approved option).
/// - The grid holds ONLY bag items. Items equipped on a hero are NOT in the
///   grid — they only appear on that hero's 6 equipped icon slots.
/// - Cells show ONLY an icon (a slot-type color square until real sprites
///   exist): no name, no level, no stats, no text at all. Hovering a cell or
///   an equipped slot shows a floating tooltip BESIDE it (right side first,
///   flipped left when there is no room, Y clamped inside the popup).
/// - Single click only selects/highlights; double-click equips (grid -> tab
///   hero, any hero including locked) or unequips (equipped slot -> bag).
/// </summary>
[GlobalClass]
public partial class HeroListScene : Control
{
    // Phase 2.27: slot colors live in UiTheme now. The DetailPortrait
    // ColorRect doubles as the procedural portrait: panel background,
    // the hero's given-name char, and a tier-colored ring.
    private ColorRect? _portraitRect;

    private VBoxContainer? _tabButtons;
    private GridContainer? _grid;
    private Label? _gridTitle;
    private PanelContainer? _tooltipPanel;
    private RichTextLabel? _tooltipLabel;
    // Detail area: static scene nodes (HeroListScene.tscn), code only sets text/values.
    private Label? _nameLabel;
    private ProgressBar? _expBar;
    private Dictionary<string, Label>? _statValues;
    private Label? _skillsValueLabel;
    private Label? _skillsDetailLabel;
    private Button? _dismissButton;
    private VBoxContainer? _leftEquippedBox;
    private VBoxContainer? _rightEquippedBox;
    private PackedScene? _tabButtonTemplate;
    private PackedScene? _cellTemplate;
    private string _selectedHeroId = "";

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        _tabButtons = GetNode<VBoxContainer>("TabButtons");
        _grid = GetNode<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        _gridTitle = GetNode<Label>("GridPanel/GridTitleLabel");
        UiTheme.StyleBody(_gridTitle, 17);
        _tooltipPanel = GetNode<PanelContainer>("TooltipPanel");
        _tooltipLabel = GetNode<RichTextLabel>("TooltipPanel/TooltipLabel");
        _tooltipLabel.AddThemeFontSizeOverride("normal_font_size", 14);
        _tooltipLabel.AddThemeColorOverride("default_color", UiTheme.Text);
        _tooltipLabel.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        _tooltipLabel.CustomMinimumSize = new Vector2(280, 0);
        _tooltipPanel.MouseFilter = MouseFilterEnum.Ignore;
        _tooltipLabel.MouseFilter = MouseFilterEnum.Ignore;
        _tooltipPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = UiTheme.Panel, BorderColor = UiTheme.Gold,
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
        });
        _nameLabel = GetNode<Label>("DetailBox/DetailNameLabel");
        _expBar = GetNode<ProgressBar>("DetailBox/DetailExpBar");
        _statValues = new Dictionary<string, Label>
        {
            ["atk"] = GetNode<Label>("DetailBox/DetailStatsGrid/StatAtkValue"),
            ["hp"] = GetNode<Label>("DetailBox/DetailStatsGrid/StatHpValue"),
            ["speed"] = GetNode<Label>("DetailBox/DetailStatsGrid/StatSpeedValue"),
            ["power"] = GetNode<Label>("DetailBox/DetailStatsGrid/StatPowerValue"),
            ["def"] = GetNode<Label>("DetailBox/DetailStatsGrid/StatDefValue"),
            ["crit"] = GetNode<Label>("DetailBox/DetailStatsGrid/StatCritValue"),
            ["dodge"] = GetNode<Label>("DetailBox/DetailStatsGrid/StatDodgeValue"),
            ["role"] = GetNode<Label>("DetailBox/DetailStatsGrid/StatRoleValue"),
        };
        _skillsValueLabel = GetNode<Label>("DetailBox/SkillsRow/SkillsValueLabel");
        _skillsDetailLabel = GetNode<Label>("DetailBox/SkillsDetailLabel");
        _dismissButton = GetNode<Button>("DetailBox/DismissRow/DismissButton");
        // Small bottom-right button (sketch scale), red border for a
        // destructive action — deliberately NOT UiTheme.StyleButton, which
        // forces 56px height and would push the detail column past 540px.
        _dismissButton.AddThemeFontSizeOverride("font_size", UiTheme.FontSmallButton);
        _dismissButton.AddThemeColorOverride("font_color", UiTheme.Text);
        var dismissNormal = new StyleBoxFlat { BgColor = UiTheme.Panel, BorderColor = UiTheme.EnemyRed, BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 };
        var dismissHover = (StyleBoxFlat)dismissNormal.Duplicate(); dismissHover.BgColor = UiTheme.DangerHover;
        var dismissDisabled = new StyleBoxFlat { BgColor = new Color(0.25f, 0.25f, 0.28f), BorderColor = UiTheme.Muted, BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 };
        _dismissButton.AddThemeStyleboxOverride("normal", dismissNormal);
        _dismissButton.AddThemeStyleboxOverride("hover", dismissHover);
        _dismissButton.AddThemeStyleboxOverride("pressed", dismissNormal);
        _dismissButton.AddThemeStyleboxOverride("disabled", dismissDisabled);
        _dismissButton.AddThemeColorOverride("font_disabled_color", UiTheme.Muted);
        _dismissButton.Pressed += OnDismissPressed;
        _leftEquippedBox = GetNode<VBoxContainer>("DetailBox/PortraitRow/LeftEquippedBox");
        _rightEquippedBox = GetNode<VBoxContainer>("DetailBox/PortraitRow/RightEquippedBox");
        // Middle header tabs (sketch): Trang bi is the active view; Trang suc
        // has no separate system in this game (the 6 slots already include
        // cloak/bracer), so it stays locked grey instead of faking content.
        var equipmentTab = GetNode<Button>("DetailBox/EquipmentTabs/EquipmentTabButton");
        equipmentTab.Text = "Trang bị";
        UiTheme.StyleButton(equipmentTab, true);
        var accessoryTab = GetNode<Button>("DetailBox/EquipmentTabs/AccessoryTabButton");
        accessoryTab.Text = "Trang sức";
        UiTheme.StyleButton(accessoryTab, false);
        accessoryTab.Disabled = true;
        _tabButtonTemplate = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroTabButton.tscn");
        _cellTemplate = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/EquipmentCell.tscn");
        _selectedHeroId = gm.Save.OwnedHeroIds.Contains(gm.SelectedHeroId) ? gm.SelectedHeroId
            : gm.Save.OwnedHeroIds.Count > 0 ? gm.Save.OwnedHeroIds[0] : "";
        _portraitRect = GetNode<ColorRect>("DetailBox/PortraitRow/DetailPortrait");
        BuildTabs();
        BuildGrid();
    }

    private string CurrentHeroId() => _selectedHeroId;

    private void RefreshPortrait(string heroId)
    {
        if (_portraitRect == null) return;
        var charLabel = _portraitRect.GetChildren().OfType<Label>().FirstOrDefault();
        if (charLabel == null)
        {
            charLabel = new Label
            {
                Name = "PortraitCharLabel",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            charLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _portraitRect.AddChild(charLabel);
        }
        var ring = _portraitRect.GetChildren().OfType<PanelContainer>().FirstOrDefault();
        if (ring == null)
        {
            ring = new PanelContainer { Name = "PortraitRing", MouseFilter = MouseFilterEnum.Ignore };
            ring.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _portraitRect.AddChild(ring);
        }
        if (string.IsNullOrEmpty(heroId)) { charLabel.Text = ""; return; }
        var color = AssetIcons.PortraitBorderColor(heroId);
        _portraitRect.Color = UiTheme.Panel;
        charLabel.Text = AssetIcons.PortraitChar(heroId);
        charLabel.AddThemeColorOverride("font_color", color);
        charLabel.AddThemeFontSizeOverride("font_size", 60);
        ring.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0),
            BorderColor = color,
            BorderWidthLeft = 3, BorderWidthRight = 3, BorderWidthTop = 3, BorderWidthBottom = 3,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 0, ContentMarginRight = 0, ContentMarginTop = 0, ContentMarginBottom = 0,
        });
    }

    private static void ClearBox(Node box)
    {
        foreach (var child in box.GetChildren())
        {
            box.RemoveChild(child);
            child.QueueFree();
        }
    }

    private Button InstantiateTabButton()
    {
        if (_tabButtonTemplate != null) return _tabButtonTemplate.Instantiate<Button>();
        return new Button { CustomMinimumSize = new Vector2(UiTheme.TabWidth, UiTheme.ButtonHeight) };
    }

    private Button MakeIconButton(Color bg, bool filled, string symbol = "")
    {
        Button btn;
        if (_cellTemplate != null) btn = _cellTemplate.Instantiate<Button>();
        else btn = new Button { Text = "", CustomMinimumSize = new Vector2(UiTheme.IconGridSize, UiTheme.IconGridSize) };
        if (symbol != "")
        {
            // Phase 2.27: representative character on the icon (the old
            // icons were bare color squares).
            var label = new Label
            {
                Name = "IconSymbolLabel",
                Text = symbol,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            label.AddThemeFontSizeOverride("font_size", 20);
            label.AddThemeColorOverride("font_color", Colors.White);
            label.MouseFilter = MouseFilterEnum.Ignore;
            btn.AddChild(label);
        }
        var normal = new StyleBoxFlat
        {
            BgColor = bg, BorderColor = filled ? UiTheme.Gold : UiTheme.Muted,
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 0, ContentMarginRight = 0, ContentMarginTop = 0, ContentMarginBottom = 0,
        };
        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = new Color(Math.Min(bg.R * 1.3f, 1f), Math.Min(bg.G * 1.3f, 1f), Math.Min(bg.B * 1.3f, 1f));
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", normal);
        btn.AddThemeStyleboxOverride("disabled", normal);
        return btn;
    }

    private void ShowTooltip(Control cell, string text)
    {
        if (_tooltipPanel == null || _tooltipLabel == null) return;
        _tooltipLabel.Text = text;
        // Detail lines are short and fit 280px, so height is deterministic
        // without waiting a frame for layout.
        int lines = Math.Max(1, text.Split('\n').Length);
        float height = 16 + lines * 20 + 8;
        _tooltipPanel.CustomMinimumSize = new Vector2(280, height);
        _tooltipPanel.Size = new Vector2(280, height);
        _tooltipPanel.Visible = true;
        _tooltipPanel.MoveToFront();
        var cellGlobal = cell.GetGlobalRect();
        var rootGlobal = GetGlobalRect();
        var cellPos = cellGlobal.Position - rootGlobal.Position;
        float x = cellPos.X + cellGlobal.Size.X + 8;
        if (x + 280 > 1000) x = cellPos.X - 280 - 8;
        if (x < 0) x = 0;
        float y = cellPos.Y;
        if (y + height > 600) y = 600 - height;
        if (y < 0) y = 0;
        _tooltipPanel.Position = new Vector2(x, y);
    }

    private void HideTooltip()
    {
        if (_tooltipPanel != null) _tooltipPanel.Visible = false;
    }

    private void RebuildAll()
    {
        BuildTabs();
        BuildGrid();
    }

    private void OnDismissPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (!ProgressionService.DismissHero(gm.Save, _selectedHeroId)) return;
        // SaveGame copies SelectedTeamIds back into Save.TeamIds, so sync
        // the GameManager state first, then save, then rebuild on the new
        // default hero (the dismissed tab now shows as locked).
        gm.SelectedTeamIds = new List<string>(gm.Save.TeamIds);
        gm.SelectedHeroId = gm.Save.TeamIds.Count > 0 ? gm.Save.TeamIds[0] : gm.Save.OwnedHeroIds[0];
        _selectedHeroId = gm.SelectedHeroId;
        gm.SaveGame();
        CallDeferred("RebuildAll");
    }

    private void BuildTabs()
    {
        if (!IsInsideTree() || _tabButtons == null || _nameLabel == null
            || _expBar == null || _statValues == null || _skillsValueLabel == null
            || _skillsDetailLabel == null || _dismissButton == null
            || _leftEquippedBox == null || _rightEquippedBox == null) return;
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        ClearBox(_tabButtons);
        // Phase 2.19: the popup shows ONLY owned (recruited) heroes — no
        // locked tabs at all. A dismissed hero therefore vanishes from this
        // list entirely (it can come back only by re-unlocking with shards).
        var ownedHeroes = HeroDatabase.Heroes.Where(h => gm.Save.OwnedHeroIds.Contains(h.Id)).ToList();
        if (ownedHeroes.Count == 0)
        {
            // Unreachable in practice (EnsureDefaults always keeps the two
            // permanent heroes), but never crash on an empty roster.
            _selectedHeroId = "";
            _nameLabel.Text = "Chưa sở hữu tướng nào";
            ClearBox(_leftEquippedBox);
            ClearBox(_rightEquippedBox);
            _dismissButton.Disabled = true;
            return;
        }
        if (!gm.Save.OwnedHeroIds.Contains(_selectedHeroId))
            _selectedHeroId = ownedHeroes[0].Id;
        // Vertical tab buttons (Godot 4.7 TabContainer only supports Top/Bottom,
        // so vertical tabs are a button column — same look and behavior).
        foreach (var hero in ownedHeroes)
        {
            var btn = InstantiateTabButton();
            btn.Text = hero.Name;
            UiTheme.StyleButton(btn, hero.Id == _selectedHeroId);
            var id = hero.Id;
            btn.Pressed += () => { _selectedHeroId = id; gm.SelectedHeroId = id; CallDeferred("RebuildAll"); };
            _tabButtons.AddChild(btn);
        }
        // Detail of the selected tab hero: fill the static scene nodes.
        var heroDef = HeroDatabase.Get(_selectedHeroId);
        var p = gm.Save.Heroes[_selectedHeroId];
        var eff = ProgressionService.GetEffectiveHero(gm.Save, _selectedHeroId);
        _nameLabel.Text = $"{heroDef.Name} — Cấp {p.Level} — Loại: {HeroDatabase.RoleName(heroDef.Role)}";
        RefreshPortrait(_selectedHeroId);
        // Phase 2.17: the portrait label and the EXP/stars/shards text line are
        // removed; only the experience bar stays (locked heroes show an empty bar,
        // their locked state is already shown by the tab's "(Khóa)" suffix).
        _expBar.MinValue = 0;
        _expBar.MaxValue = p.Level >= ProgressionService.MaxLevel ? 1 : ProgressionService.ExpToNext(p.Level);
        _expBar.Value = p.Level >= ProgressionService.MaxLevel ? 1 : p.Exp;
        int power = eff.Hp + eff.Atk * 5;
        // Stats grid, in the sketch's 2-pair-per-row layout, using only stats
        // the engine really has (no invented Tri stat, no invented army
        // position: the sketch's "Trí" / "Trung quân" have no engine data, so
        // they are not faked here — values sit in fixed label/value columns
        // so every row lines up straight).
        _statValues["atk"].Text = $"{eff.Atk}";
        _statValues["hp"].Text = $"{eff.Hp}";
        _statValues["speed"].Text = $"{eff.Speed}";
        _statValues["power"].Text = $"~{power}";
        _statValues["def"].Text = $"{eff.Def}";
        _statValues["crit"].Text = $"{eff.Crit:P0}";
        _statValues["dodge"].Text = $"{eff.Dodge:P0}";
        _statValues["role"].Text = HeroDatabase.RoleName(heroDef.Role);
        // Skills row (sketch): "Kỹ năng" + the ult name on one line, with
        // the normal/passive detail in a smaller line below it.
        _skillsValueLabel.Text = heroDef.UltName;
        _skillsDetailLabel.Text = $"Đánh thường: {heroDef.NormalName} | Bị động: {heroDef.PassiveName} — {heroDef.PassiveDesc}";
        // Dismiss: only for an owned, non-permanent hero when another hero
        // remains (see ProgressionService.DismissHero for the full rules).
        bool selectedOwned = gm.Save.OwnedHeroIds.Contains(_selectedHeroId);
        _dismissButton.Disabled = !selectedOwned || _selectedHeroId == "custom_hero"
            || gm.Save.OwnedHeroIds.Count <= 1;
        // 6 equipped icon slots (equipped items live ONLY here, never in the
        // grid), flanking the portrait: first 3 slots left, last 3 right.
        ClearBox(_leftEquippedBox);
        ClearBox(_rightEquippedBox);
        var equippedButtons = new List<Button>();
        int slotIndex = 0;
        foreach (var slot in ProgressionService.Slots)
        {
            var eq = gm.Save.HeroEquipment[_selectedHeroId][slot.Id];
            var slotBtn = MakeIconButton(eq.IsEmpty ? UiTheme.SlotEmpty : UiTheme.SlotColors[slot.Id], !eq.IsEmpty,
                eq.IsEmpty ? "" : AssetIcons.SlotSymbol(slot.Id));
            // Equipped icons are 56x56 (grid cells stay 64x64): 3x56 + 2x6 gap
            // = 180, exactly the portrait height, so all three columns line up.
            slotBtn.CustomMinimumSize = new Vector2(UiTheme.IconEquippedSize, UiTheme.IconEquippedSize);
            var heroId = _selectedHeroId; var slotId = slot.Id;
            string slotDetail = EquipmentTooltip.ForEquipped(eq, slotId, $"Đang mang: {heroDef.Name}");
            slotBtn.MouseEntered += () => ShowTooltip(slotBtn, slotDetail);
            slotBtn.MouseExited += HideTooltip;
            slotBtn.GuiInput += (InputEvent e) =>
            {
                if (e is InputEventMouseButton mb && mb.Pressed)
                {
                    if (mb.DoubleClick)
                    {
                        if (EquipmentService.Unequip(gm.Save, heroId, slotId)) { gm.SaveGame(); CallDeferred("RebuildAll"); }
                    }
                    else
                    {
                        foreach (var b in equippedButtons) b.Modulate = Colors.White;
                        slotBtn.Modulate = UiTheme.Gold;
                    }
                }
            };
            equippedButtons.Add(slotBtn);
            (slotIndex < 3 ? _leftEquippedBox : _rightEquippedBox).AddChild(slotBtn);
            slotIndex++;
        }
        HideTooltip();
    }

    private void BuildGrid()
    {
        if (!IsInsideTree() || _grid == null) return;
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        ClearBox(_grid);
        if (_gridTitle != null) _gridTitle.Text = $"TRANG BỊ TRONG TÚI ({gm.Save.BagEquipment.Count})";
        // 4 columns x 7 rows = 28 cells; beyond 28 items, rows grow (scroll).
        int totalCells = Math.Max(28, ((gm.Save.BagEquipment.Count + 3) / 4) * 4);
        var gridButtons = new List<Button>();
        for (int i = 0; i < totalCells; i++)
        {
            if (i < gm.Save.BagEquipment.Count)
            {
                var item = gm.Save.BagEquipment[i];
                var btn = MakeIconButton(UiTheme.SlotColors[item.SlotId], true, AssetIcons.SlotSymbol(item.SlotId));
                var bagIndex = i;
                string detail = EquipmentTooltip.ForBagItem(item, "Trạng thái: Trong túi");
                btn.MouseEntered += () => ShowTooltip(btn, detail);
                btn.MouseExited += HideTooltip;
                btn.GuiInput += (InputEvent e) =>
                {
                    if (e is InputEventMouseButton mb && mb.Pressed)
                    {
                        if (mb.DoubleClick)
                        {
                            // Equip to the hero of the current tab — any hero, including locked.
                            if (EquipmentService.EquipFromBag(gm.Save, CurrentHeroId(), bagIndex)) { gm.SaveGame(); CallDeferred("RebuildAll"); }
                        }
                        else
                        {
                            foreach (var b in gridButtons) b.Modulate = Colors.White;
                            btn.Modulate = UiTheme.Gold;
                        }
                    }
                };
                gridButtons.Add(btn);
                _grid.AddChild(btn);
            }
            else
            {
                var empty = MakeIconButton(UiTheme.SlotEmpty, false);
                empty.Disabled = true;
                _grid.AddChild(empty);
            }
        }
        HideTooltip();
    }
}
