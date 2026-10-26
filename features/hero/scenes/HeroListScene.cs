using Godot;
using System;
using System.Collections.Generic;

namespace LongTuong;

/// <summary>
/// Phase 2.7 hero/equipment screen (popup 1180x660, all sizes fixed):
/// - Vertical tabs: one tab button per hero stacked in a left column (the
///   2.4 tab-per-hero pattern, but tabs stacked vertically on the left
///   instead of horizontally on top). Godot 4.7 TabContainer only supports
///   Top/Bottom tabs, so vertical tabs are a button column + a shared detail
///   panel — same look and behavior. Clicking a tab shows that hero's detail.
/// - Right panel: equipment grid, 4 columns, 7 rows (28 cells) of 64x64
///   square icon-only cells; rows grow past 7 inside a ScrollContainer when
///   the bag holds more than 28 items (Phase 2.7 approved option).
/// - The grid holds ONLY bag items. Items equipped on a hero are NOT in the
///   grid — they only appear on that hero's 4 equipped icon slots.
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
    private VBoxContainer? _detailBox;
    private string _selectedHeroId = "";
    private GridContainer? _grid;
    private Label? _gridTitle;
    private PanelContainer? _tooltipPanel;
    private Label? _tooltipLabel;

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        _tabButtons = GetNode<VBoxContainer>("TabButtons");
        _detailBox = GetNode<VBoxContainer>("DetailBox");
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

    // 2.6 layout lesson: autowrap labels inside a VBoxContainer need Fill +
    // the column width, otherwise they collapse to width 1 with huge height.
    private Label MakeLabel(string text, int size = 15, float width = 540)
    {
        var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.Fill, CustomMinimumSize = new Vector2(width, 0) };
        UiTheme.StyleBody(l, size);
        return l;
    }

    private Button MakeIconButton(Color bg, bool filled)
    {
        var btn = new Button { Text = "", CustomMinimumSize = new Vector2(64, 64) };
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
        if (x + 280 > 1180) x = cellPos.X - 280 - 8;
        if (x < 0) x = 0;
        float y = cellPos.Y;
        if (y + height > 660) y = 660 - height;
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
        if (!IsInsideTree() || _tabButtons == null || _detailBox == null) return;
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        ClearBox(_tabButtons);
        ClearBox(_detailBox);
        // Vertical tab buttons (Godot 4.7 TabContainer only supports Top/Bottom,
        // so vertical tabs are a button column — same look and behavior).
        foreach (var hero in HeroDatabase.Heroes)
        {
            bool owned = gm.Save.OwnedHeroIds.Contains(hero.Id);
            var btn = new Button { Text = owned ? hero.Name : $"{hero.Name} (Khóa)", CustomMinimumSize = new Vector2(160, 56) };
            UiTheme.StyleButton(btn, hero.Id == _selectedHeroId);
            btn.CustomMinimumSize = new Vector2(160, 56);
            var id = hero.Id;
            btn.Pressed += () => { _selectedHeroId = id; if (gm.Save.OwnedHeroIds.Contains(id)) gm.SelectedHeroId = id; CallDeferred("RebuildAll"); };
            _tabButtons.AddChild(btn);
        }
        // Detail of the selected tab hero.
        var heroDef = HeroDatabase.Get(_selectedHeroId);
        var p = gm.Save.Heroes[_selectedHeroId];
        var eff = ProgressionService.GetEffectiveHero(gm.Save, _selectedHeroId);
        bool selectedOwned = gm.Save.OwnedHeroIds.Contains(_selectedHeroId);
        var page = _detailBox;
        page.AddChild(MakeLabel($"{heroDef.Name} — Cấp {p.Level} — Loại: {HeroDatabase.RoleName(heroDef.Role)}", 20));
        page.AddChild(new ColorRect { Color = new Color(0.20f, 0.35f, 0.60f), CustomMinimumSize = new Vector2(540, 140) });
        page.AddChild(MakeLabel($"Chân dung: {heroDef.Name} (placeholder, chưa có sprite final)", 13));
        page.AddChild(MakeLabel(selectedOwned
            ? $"EXP: {p.Exp}/{ProgressionService.ExpToNext(p.Level)} — Sao: {p.Stars}/5 — Mảnh: {gm.Save.Shards[_selectedHeroId]}"
            : $"CHƯA SỞ HỮU (cần 10 mảnh để mở khóa, hiện có {gm.Save.Shards[_selectedHeroId]} mảnh)"));
        var expBar = new ProgressBar { MinValue = 0, MaxValue = p.Level >= ProgressionService.MaxLevel ? 1 : ProgressionService.ExpToNext(p.Level), Value = p.Level >= ProgressionService.MaxLevel ? 1 : p.Exp, CustomMinimumSize = new Vector2(540, 20) };
        page.AddChild(expBar);
        int power = eff.Hp + eff.Atk * 5;
        page.AddChild(MakeLabel($"Chỉ số hiệu lực — Lực chiến ~{power}: HP {eff.Hp} / ATK {eff.Atk} / DEF {eff.Def} / Tốc {eff.Speed} / Bạo {eff.Crit:P0} / Né {eff.Dodge:P0}"));
        page.AddChild(MakeLabel($"Đánh thường: {heroDef.NormalName} | Tuyệt kỹ: {heroDef.UltName} | Bị động: {heroDef.PassiveName} — {heroDef.PassiveDesc}"));
        // 4 equipped icon slots (equipped items live ONLY here, never in the grid).
        var equippedBox = new HBoxContainer();
        page.AddChild(equippedBox);
        var equippedButtons = new List<Button>();
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
            equippedBox.AddChild(slotBtn);
        }
        var unequipAll = new Button { Text = "Tháo tất cả trang bị", CustomMinimumSize = new Vector2(540, 56) };
        UiTheme.StyleButton(unequipAll);
        unequipAll.CustomMinimumSize = new Vector2(540, 56);
        var unequipHero = _selectedHeroId;
        unequipAll.Pressed += () => { if (EquipmentService.UnequipAll(gm.Save, unequipHero) >= 0) { gm.SaveGame(); CallDeferred("RebuildAll"); } };
        page.AddChild(unequipAll);
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
