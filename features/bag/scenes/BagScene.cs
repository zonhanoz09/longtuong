using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.28: the bag ("Túi") redesigned per Huynh's screenshot layout —
/// left vertical tab column (Đạo cụ/Trang bị/Nguyên liệu/Thiên Phú/Bảo Vật/
/// Trang sức/Cánh Giới Đơn/Kho tạm), right 5x4 item grid with "+N"
/// enhancement badges, "Chứa: X" capacity line, hover tooltip styled like
/// the screenshot (name in slot color + "Cường hóa: +N" + stat lines).
/// Huynh's decisions: empty tabs show an empty grid + note (no disabled
/// tabs); capacity is count-only, no limit; equipment badges show "+N";
/// grid is 5 columns x 4 rows = 20 slots, scrolls past 20.
/// Only the Trang bị tab has real data — the other systems don't exist
/// yet, so their tabs show an empty grid. View-only: equipping still
/// happens in the Võ tướng screen.
/// Compact layout (Huynh: popup was too wide): 760x600 frame, content
/// fitted — 170px tab column + 536px grid (5x96 + 4x14) + 16px margins.
/// </summary>
[GlobalClass]
public partial class BagScene : Control
{
    private static readonly (string Id, string Name)[] Tabs =
    {
        ("dao_cu", "Đạo cụ"),
        ("trang_bi", "Trang bị"),
        ("nguyen_lieu", "Nguyên liệu"),
        ("thien_phu", "Thiên Phú"),
        ("bao_vat", "Bảo Vật"),
        ("trang_suc", "Trang sức"),
        ("canh_gioi_don", "Cánh Giới Đơn"),
        ("kho_tam", "Kho tạm"),
    };
    private const int Columns = 5;
    private const int VisibleRows = 4;
    private const int CellSize = 96;

    private string _selectedTab = "trang_bi";
    private VBoxContainer? _tabBox;
    private GridContainer? _grid;
    private Label? _capacityLabel;
    private Label? _emptyNote;
    private PanelContainer? _tooltip;
    private RichTextLabel? _tooltipLabel;

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        var title = GetNode<Label>("TitleLabel");
        title.Text = "Túi";
        UiTheme.StyleTitle(title);
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        BuildTabs();
        BuildGridArea();
        BuildTooltip();
        SelectTab(_selectedTab);
    }

    private void BuildTabs()
    {
        var tabBox = new VBoxContainer { Name = "TabBox" };
        tabBox.SetPosition(new Vector2(16, 56));
        tabBox.SetSize(new Vector2(170, 490));
        tabBox.AddThemeConstantOverride("separation", 6);
        foreach (var (id, name) in Tabs)
        {
            var tabId = id;
            var btn = new Button { Name = $"Tab_{id}", Text = name, CustomMinimumSize = new Vector2(170, UiTheme.ButtonHeight) };
            UiTheme.StyleButton(btn, false);
            btn.Pressed += () => SelectTab(tabId);
            tabBox.AddChild(btn);
        }
        AddChild(tabBox);
        _tabBox = tabBox;
        // "Mở rộng túi" from the screenshot — decorative for now: the bag
        // has no capacity limit (Huynh's decision 2a), so this is a muted
        // label, not a working button.
        var expand = new Label
        {
            Name = "ExpandLabel",
            Text = "Mở rộng túi",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        expand.SetPosition(new Vector2(16, 552));
        expand.SetSize(new Vector2(170, 28));
        UiTheme.StyleBody(expand, UiTheme.FontSmall);
        expand.AddThemeColorOverride("font_color", UiTheme.Muted);
        AddChild(expand);
    }

    private void BuildGridArea()
    {
        var scroll = new ScrollContainer { Name = "BagScroll" };
        scroll.SetPosition(new Vector2(202, 56));
        scroll.SetSize(new Vector2(542, 432));
        _grid = new GridContainer { Name = "BagGrid", Columns = Columns };
        _grid.AddThemeConstantOverride("h_separation", 14);
        _grid.AddThemeConstantOverride("v_separation", 14);
        scroll.AddChild(_grid);
        AddChild(scroll);
        _capacityLabel = new Label { Name = "CapacityLabel", Text = "" };
        _capacityLabel.SetPosition(new Vector2(202, 496));
        _capacityLabel.SetSize(new Vector2(400, 28));
        UiTheme.StyleBody(_capacityLabel);
        AddChild(_capacityLabel);
        _emptyNote = new Label
        {
            Name = "EmptyNote",
            Text = "Chưa có vật phẩm.",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _emptyNote.SetPosition(new Vector2(202, 220));
        _emptyNote.SetSize(new Vector2(542, 40));
        UiTheme.StyleBody(_emptyNote, UiTheme.FontButton);
        _emptyNote.AddThemeColorOverride("font_color", UiTheme.Muted);
        AddChild(_emptyNote);
    }

    private void BuildTooltip()
    {
        _tooltip = new PanelContainer { Name = "BagTooltip", Visible = false };
        _tooltip.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = UiTheme.Panel,
            BorderColor = UiTheme.Gold,
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 10, ContentMarginBottom = 10,
        });
        _tooltipLabel = new RichTextLabel
        {
            Name = "BagTooltipLabel",
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            CustomMinimumSize = new Vector2(300, 0),
        };
        _tooltipLabel.AddThemeFontSizeOverride("normal_font_size", 14);
        _tooltipLabel.AddThemeColorOverride("default_color", UiTheme.Text);
        _tooltip.AddChild(_tooltipLabel);
        AddChild(_tooltip);
    }

    private void SelectTab(string tabId)
    {
        _selectedTab = tabId;
        if (_tabBox != null)
        {
            foreach (var btn in _tabBox.GetChildren().OfType<Button>())
                UiTheme.StyleButton(btn, btn.Name == $"Tab_{tabId}");
        }
        RebuildGrid();
    }

    private void RebuildGrid()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (_grid == null) return;
        foreach (var child in _grid.GetChildren()) { _grid.RemoveChild(child); child.QueueFree(); }
        var items = _selectedTab == "trang_bi"
            ? gm.Save.BagEquipment
            : new System.Collections.Generic.List<BagEquipmentItem>();
        int cells = System.Math.Max(Columns * VisibleRows, items.Count);
        for (int i = 0; i < cells; i++)
        {
            if (i < items.Count) _grid.AddChild(MakeItemCell(items[i]));
            else _grid.AddChild(MakeEmptyCell());
        }
        if (_capacityLabel != null) _capacityLabel.Text = $"Chứa: {items.Count}";
        if (_emptyNote != null) _emptyNote.Visible = items.Count == 0;
    }

    private Button MakeItemCell(BagEquipmentItem item)
    {
        var btn = new Button { Text = "", CustomMinimumSize = new Vector2(CellSize, CellSize) };
        var slotColor = UiTheme.SlotColors.TryGetValue(item.SlotId, out var c) ? c : UiTheme.Panel;
        var normal = new StyleBoxFlat
        {
            BgColor = slotColor, BorderColor = UiTheme.Gold,
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 0, ContentMarginRight = 0, ContentMarginTop = 0, ContentMarginBottom = 0,
        };
        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = new Color(System.Math.Min(slotColor.R * 1.3f, 1f), System.Math.Min(slotColor.G * 1.3f, 1f), System.Math.Min(slotColor.B * 1.3f, 1f));
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", normal);
        btn.AddThemeStyleboxOverride("disabled", normal);
        var symbol = new Label
        {
            Name = "CellSymbol",
            Text = AssetIcons.SlotSymbol(item.SlotId),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        symbol.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        symbol.AddThemeFontSizeOverride("font_size", 26);
        symbol.AddThemeColorOverride("font_color", Colors.White);
        btn.AddChild(symbol);
        // Enhancement badge, bottom-right (screenshot style).
        var badge = new Label
        {
            Name = "CellBadge",
            Text = $"+{item.Level}",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        badge.AnchorLeft = 1; badge.AnchorTop = 1; badge.AnchorRight = 1; badge.AnchorBottom = 1;
        badge.OffsetLeft = -50; badge.OffsetTop = -28; badge.OffsetRight = -6; badge.OffsetBottom = -4;
        badge.AddThemeFontSizeOverride("font_size", 15);
        badge.AddThemeColorOverride("font_color", Colors.White);
        btn.AddChild(badge);
        btn.MouseEntered += () => ShowTooltip(item);
        btn.MouseExited += HideTooltip;
        return btn;
    }

    private static Button MakeEmptyCell()
    {
        var btn = new Button { Text = "", CustomMinimumSize = new Vector2(CellSize, CellSize), Disabled = true };
        btn.AddThemeStyleboxOverride("normal", new StyleBoxFlat
        {
            BgColor = UiTheme.SlotEmpty, BorderColor = UiTheme.Muted,
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
        });
        btn.AddThemeStyleboxOverride("disabled", new StyleBoxFlat
        {
            BgColor = UiTheme.SlotEmpty, BorderColor = UiTheme.Muted,
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
        });
        return btn;
    }

    private void ShowTooltip(BagEquipmentItem item)
    {
        if (_tooltip == null || _tooltipLabel == null) return;
        // Unified tooltip (same BBCode as Võ tướng / Trang bị popups).
        string bbcode = EquipmentTooltip.ForBagItem(item, null);
        _tooltipLabel.Text = bbcode;
        int lines = System.Math.Max(1, bbcode.Split('\n').Length);
        float height = 16 + lines * 20 + 8;
        _tooltip.CustomMinimumSize = new Vector2(320, height);
        _tooltip.Size = new Vector2(320, height);
        _tooltip.Visible = true;
        var mp = GetLocalMousePosition() + new Vector2(16, 12);
        mp.X = System.Math.Min(mp.X, 760 - 340);
        mp.Y = System.Math.Min(mp.Y, 600 - height);
        _tooltip.Position = mp;
    }

    private void HideTooltip()
    {
        if (_tooltip != null) _tooltip.Visible = false;
    }
}
