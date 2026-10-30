using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.28: the bag ("Túi") redesigned per Huynh's screenshot layout —
/// left tab column, 5x4 item grid with "+N" badges, "Chứa: X" line,
/// screenshot-style hover tooltip. Empty tabs show an empty grid + note.
/// </summary>
public partial class TestPhase228 : Node
{
    private int _fails;

    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("tester228");
        // Seeded saves start with 6 Lv0 items (one per slot); clear for a
        // deterministic bag, then add 3 known items.
        gm.Save.BagEquipment.Clear();
        gm.Save.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon", Level = 3, RefineIndex = 0 });
        gm.Save.BagEquipment.Add(new BagEquipmentItem { SlotId = "armor", Level = 1, RefineIndex = 0 });
        gm.Save.BagEquipment.Add(new BagEquipmentItem { SlotId = "helmet", Level = 5, RefineIndex = 0 });

        var scene = ResourceLoader.Load<PackedScene>("res://features/bag/scenes/BagScene.tscn");
        var bag = scene.Instantiate<Control>();
        AddChild(bag);

        var title = bag.GetNode<Label>("TitleLabel");
        Check(title.Text == "Túi", "title is Túi");
        Check(title.HorizontalAlignment == HorizontalAlignment.Center, "title centered");
        // Compact popup (Huynh: 1020x640 was too wide) — frame fits content.
        Check(HubScene.BagPopupSize == new Vector2(760, 600), "bag popup is 760x600");
        Check(title.Size.X == 760, "title spans the 760 frame");

        var tabBox = bag.GetNode<VBoxContainer>("TabBox");
        Check(tabBox.Position == new Vector2(16, 56) && tabBox.Size.X == 170, "tab column fitted at (16,56) x170");
        var scroll = bag.GetNode<ScrollContainer>("BagScroll");
        Check(scroll.Position == new Vector2(202, 56) && scroll.Size == new Vector2(542, 432), "grid scroll fitted at (202,56) 542x432");
        Check(bag.GetNode<Label>("CapacityLabel").Position == new Vector2(202, 496), "capacity line under the grid");
        var tabNames = tabBox.GetChildren().OfType<Button>().Select(b => b.Text).ToList();
        Check(tabNames.SequenceEqual(new[] { "Đạo cụ", "Trang bị", "Nguyên liệu", "Thiên Phú", "Bảo Vật", "Trang sức", "Cánh Giới Đơn", "Kho tạm" }),
            "8 tabs in screenshot order");
        var activeTabs = tabBox.GetChildren().OfType<Button>()
            .Where(b => b.GetThemeColor("font_color") == UiTheme.DarkText).ToList();
        Check(activeTabs.Count == 1 && activeTabs[0].Text == "Trang bị", "Trang bị tab active by default");

        var grid = bag.GetNode<GridContainer>("BagScroll/BagGrid");
        Check(grid.Columns == 5, "grid has 5 columns");
        Check(grid.GetChildCount() == 20, "grid shows 20 slots for 3 items");
        var itemBtns = grid.GetChildren().OfType<Button>().Where(b => !b.Disabled).ToList();
        Check(itemBtns.Count == 3, "3 item cells, rest empty");
        var symbols = itemBtns.Select(b => b.GetNodeOrNull<Label>("CellSymbol")?.Text).ToList();
        Check(symbols.Contains("VK") && symbols.Contains("GI") && symbols.Contains("MŨ"), "item cells show slot symbols");
        var badges = itemBtns.Select(b => b.GetNodeOrNull<Label>("CellBadge")?.Text).ToList();
        Check(badges.Contains("+3") && badges.Contains("+1") && badges.Contains("+5"), "badges show +N enhancement");
        Check(bag.GetNode<Label>("CapacityLabel").Text == "Chứa: 3", "capacity line Chứa: 3");
        Check(!bag.GetNode<Label>("EmptyNote").Visible, "empty note hidden when items exist");

        // Empty tab: Đạo cụ.
        var daoCu = tabBox.GetChildren().OfType<Button>().First(b => b.Text == "Đạo cụ");
        daoCu.EmitSignal(BaseButton.SignalName.Pressed);
        Check(grid.GetChildCount() == 20, "empty tab still shows 20 slots");
        Check(grid.GetChildren().OfType<Button>().All(b => b.Disabled), "empty tab slots all disabled");
        Check(bag.GetNode<Label>("EmptyNote").Visible, "empty note shown for empty tab");
        Check(bag.GetNode<Label>("CapacityLabel").Text == "Chứa: 0", "capacity line Chứa: 0 for empty tab");
        Check(bag.GetNode<Label>("ExpandLabel").Text == "Mở rộng túi", "Mở rộng túi label kept from screenshot");

        // Back to Trang bị: tooltip on hover.
        var trangBi = tabBox.GetChildren().OfType<Button>().First(b => b.Text == "Trang bị");
        trangBi.EmitSignal(BaseButton.SignalName.Pressed);
        var firstItem = grid.GetChildren().OfType<Button>().First(b => !b.Disabled);
        firstItem.EmitSignal(Control.SignalName.MouseEntered);
        var tooltip = bag.GetNode<PanelContainer>("BagTooltip");
        Check(tooltip.Visible, "tooltip shows on hover");
        var tipLabel = tooltip.GetNode<RichTextLabel>("BagTooltipLabel");
        Check(tipLabel.Text.Contains("Vũ khí") && tipLabel.Text.Contains("+3"), "unified tooltip has name + enhance");
        Check(tipLabel.Text.Contains("Tẩy luyện:") && tipLabel.Text.Contains("[color="), "unified tooltip has colored refine lines");
        firstItem.EmitSignal(Control.SignalName.MouseExited);
        Check(!tooltip.Visible, "tooltip hides on mouse exit");

        var cur = bag.GetNode<Label>("CurrencyBarLabel").Text;
        Check(cur.Contains("Bạc") && !cur.Contains("KNB"), "currency bar shows Bạc, no KNB");

        if (_fails == 0) GD.Print("TEST PHASE 2.28: ALL PASS");
        else GD.Print($"TEST PHASE 2.28: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }
}
