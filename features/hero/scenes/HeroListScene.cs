using Godot;
using System;
using System.Collections.Generic;

namespace LongTuong;

/// <summary>
/// Phase 2.17 hero/equipment screen (popup 1000x560, all sizes fixed), with a
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
    private static readonly Dictionary<string, Color> SlotColors = new()
    {
        ["weapon"] = new Color(0.65f, 0.25f, 0.20f),
        ["armor"] = new Color(0.20f, 0.35f, 0.60f),
        ["helmet"] = new Color(0.25f, 0.55f, 0.30f),
        ["boots"] = new Color(0.65f, 0.55f, 0.20f),
        ["cloak"] = new Color(0.50f, 0.30f, 0.75f),
        ["bracer"] = new Color(0.20f, 0.60f, 0.60f),
    };
    private static readonly Color EmptyColor = new(0.12f, 0.12f, 0.16f);

    private VBoxContainer? _tabButtons;
    private GridContainer? _grid;
    private Label? _gridTitle;
    private PanelContainer? _tooltipPanel;
    private Label? _tooltipLabel;
    // Detail area: static scene nodes (HeroListScene.tscn), code only sets text/values.
    private Label? _nameLabel;
    private ProgressBar? _expBar;
    private Label? _statsLabel;
    private Label? _skillsLabel;
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
        _tooltipLabel = GetNode<Label>("TooltipPanel/TooltipLabel");
        UiTheme.StyleBody(_tooltipLabel, 14);
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
        _statsLabel = GetNode<Label>("DetailBox/DetailStatsLabel");
        _skillsLabel = GetNode<Label>("DetailBox/DetailSkillsLabel");
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
            : gm.Save.OwnedHeroIds.Count > 0 ? gm.Save.OwnedHeroIds[0] : HeroDatabase.Heroes[0].Id;
        BuildTabs();
        BuildGrid();
    }

    private string CurrentHeroId() => _selectedHeroId;

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
        return new Button { CustomMinimumSize = new Vector2(160, 56) };
    }

    private Button MakeIconButton(Color bg, bool filled)
    {
        Button btn;
        if (_cellTemplate != null) btn = _cellTemplate.Instantiate<Button>();
        else btn = new Button { Text = "", CustomMinimumSize = new Vector2(64, 64) };
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
        if (y + height > 560) y = 560 - height;
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

    private void BuildTabs()
    {
        if (!IsInsideTree() || _tabButtons == null || _nameLabel == null
            || _expBar == null || _statsLabel == null || _skillsLabel == null
            || _leftEquippedBox == null || _rightEquippedBox == null) return;
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        ClearBox(_tabButtons);
        // Vertical tab buttons (Godot 4.7 TabContainer only supports Top/Bottom,
        // so vertical tabs are a button column — same look and behavior).
        foreach (var hero in HeroDatabase.Heroes)
        {
            bool owned = gm.Save.OwnedHeroIds.Contains(hero.Id);
            var btn = InstantiateTabButton();
            btn.Text = owned ? hero.Name : $"{hero.Name} (Khóa)";
            UiTheme.StyleButton(btn, hero.Id == _selectedHeroId);
            var id = hero.Id;
            btn.Pressed += () => { _selectedHeroId = id; if (gm.Save.OwnedHeroIds.Contains(id)) gm.SelectedHeroId = id; CallDeferred("RebuildAll"); };
            _tabButtons.AddChild(btn);
        }
        // Detail of the selected tab hero: fill the static scene nodes.
        var heroDef = HeroDatabase.Get(_selectedHeroId);
        var p = gm.Save.Heroes[_selectedHeroId];
        var eff = ProgressionService.GetEffectiveHero(gm.Save, _selectedHeroId);
        _nameLabel.Text = $"{heroDef.Name} — Cấp {p.Level} — Loại: {HeroDatabase.RoleName(heroDef.Role)}";
        // Phase 2.17: the portrait label and the EXP/stars/shards text line are
        // removed; only the experience bar stays (locked heroes show an empty bar,
        // their locked state is already shown by the tab's "(Khóa)" suffix).
        _expBar.MinValue = 0;
        _expBar.MaxValue = p.Level >= ProgressionService.MaxLevel ? 1 : ProgressionService.ExpToNext(p.Level);
        _expBar.Value = p.Level >= ProgressionService.MaxLevel ? 1 : p.Exp;
        int power = eff.Hp + eff.Atk * 5;
        // Stats grid, in the sketch's layout, using only stats the engine
        // really has (no invented Tri stat): Suc = ATK, Sinh menh = HP.
        _statsLabel.Text = $"Sức: {eff.Atk}    Sinh mệnh: {eff.Hp}    Nhanh: {eff.Speed}    Chiến lực: ~{power}\nPhòng thủ: {eff.Def}    Bạo: {eff.Crit:P0}    Né: {eff.Dodge:P0}    Vị trí trong quân: {HeroDatabase.RoleName(heroDef.Role)}";
        _skillsLabel.Text = $"Kỹ năng — Đánh thường: {heroDef.NormalName} | Tuyệt kỹ: {heroDef.UltName} | Bị động: {heroDef.PassiveName} — {heroDef.PassiveDesc}";
        // 6 equipped icon slots (equipped items live ONLY here, never in the
        // grid), flanking the portrait: first 3 slots left, last 3 right.
        ClearBox(_leftEquippedBox);
        ClearBox(_rightEquippedBox);
        var equippedButtons = new List<Button>();
        int slotIndex = 0;
        foreach (var slot in ProgressionService.Slots)
        {
            var eq = gm.Save.HeroEquipment[_selectedHeroId][slot.Id];
            var slotBtn = MakeIconButton(eq.IsEmpty ? EmptyColor : SlotColors[slot.Id], !eq.IsEmpty);
            var heroId = _selectedHeroId; var slotId = slot.Id;
            string slotDetail = eq.IsEmpty ? "Ô trống — chưa mang trang bị."
                : EquipmentService.DetailText(slotId, eq.Level, eq.RefineIndex, $"Đang mang: {heroDef.Name}");
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
                var btn = MakeIconButton(SlotColors[item.SlotId], true);
                var bagIndex = i;
                string detail = EquipmentService.DetailText(item.SlotId, item.Level, item.RefineIndex, "Trạng thái: Trong túi");
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
                var empty = MakeIconButton(EmptyColor, false);
                empty.Disabled = true;
                _grid.AddChild(empty);
            }
        }
        HideTooltip();
    }
}
