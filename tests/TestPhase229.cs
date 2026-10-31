using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.29: "Trang bị" equipment screen redesigned per Huynh's
/// screenshot — hub button renamed from "Cường hóa", 3-column layout
/// (hero list | 6 equipment rows | 5 system tabs), row click drives the
/// right panel (Cường hóa/Tẩy luyện/Tháo), locked tabs show "Chưa mở.",
/// Tướng Tinh moved to its own hub button + popup.
/// </summary>
public partial class TestPhase229 : Node
{
    private int _fails;

    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("tester229");

        // Hub: renamed button + new star chart button.
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        Check(hub.GetNode<Label>("FunctionBar/EnhanceButtonIcon/Label").Text == "Trang bị", "hub button renamed to Trang bị");
        var starBtn = hub.GetNodeOrNull<Button>("FunctionBar/StarChartButtonIcon/StarChartButton");
        Check(starBtn != null && hub.GetNode<Label>("FunctionBar/StarChartButtonIcon/Label").Text.Contains("Tướng"), "hub has Tướng Tinh button");
        Check(HubScene.StarChartPopupSize == new Vector2(520, 400), "star chart popup size 520x400");

        // Equipment screen layout.
        var screenScene = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroScreen.tscn");
        var screen = screenScene.Instantiate<HeroScreen>();
        AddChild(screen);
        var title = screen.GetNode<Label>("TitleLabel");
        Check(title.Text == "Trang bị" && title.HorizontalAlignment == HorizontalAlignment.Center, "title Trang bị centered");
        var heroBox = screen.GetNode<VBoxContainer>("HeroListBox");
        Check(heroBox.GetChildCount() == gm.Save.OwnedHeroIds.Count, "left column lists owned heroes only");
        Check(heroBox.GetChildren().OfType<Button>().All(b => !b.Text.Contains("KHÓA")), "no locked heroes shown");
        var equipBox = screen.GetNode<VBoxContainer>("EquipListBox");
        Check(equipBox.GetChildren().OfType<Button>().Count() == 6, "middle column has 6 equipment rows");
        var tabRow = screen.GetNode<HBoxContainer>("RightPanel/TabRow");
        var tabNames = tabRow.GetChildren().OfType<Button>().Select(b => b.Text).ToList();
        Check(tabNames.SequenceEqual(new[] { "Cường hóa", "Tẩy luyện", "Kế thừa", "Đục lỗ", "Phụ ma" }), "5 system tabs in order (2.37: Nang cap removed)");

        // Equip a weapon, select its row, enhance via the right panel.
        int weaponIdx = gm.Save.BagEquipment.FindIndex(b => b.SlotId == "weapon");
        Check(EquipmentService.EquipFromBag(gm.Save, "custom_hero", weaponIdx), "equip weapon to custom hero");
        var screen2 = screenScene.Instantiate<HeroScreen>();
        AddChild(screen2);
        var equipBox2 = screen2.GetNode<VBoxContainer>("EquipListBox");
        var weaponRow = equipBox2.GetChildren().OfType<Button>().First(b => b.Name == "EquipRow_weapon");
        weaponRow.EmitSignal(BaseButton.SignalName.Pressed);
        var content = screen2.GetNode<Control>("RightPanel/TabContent");
        var enhanceBtn = content.FindChild("ActionEnhance", true, false) as Button;
        Check(enhanceBtn != null, "right panel shows enhance action for the selected row");
        int silverBefore = gm.Save.Silver;
        enhanceBtn.EmitSignal(BaseButton.SignalName.Pressed);
        Check(gm.Save.HeroEquipment["custom_hero"]["weapon"].Level == 1, "enhance raises weapon to +1");
        Check(gm.Save.Silver == silverBefore - ProgressionService.EnhanceCostSilver(0), "enhance costs 100 silver");

        // Refine tab.
        var tabRow2 = screen2.GetNode<HBoxContainer>("RightPanel/TabRow");
        tabRow2.GetChildren().OfType<Button>().First(b => b.Text == "Tẩy luyện").EmitSignal(BaseButton.SignalName.Pressed);
        var refineBtn = content.FindChild("ActionRefine", true, false) as Button;
        Check(refineBtn != null, "refine tab shows refine action");
        int stonesBefore = gm.Save.RefineStones;
        refineBtn.EmitSignal(BaseButton.SignalName.Pressed);
        var rlines = gm.Save.HeroEquipment["custom_hero"]["weapon"].RefineLines;
        Check(rlines.Count == 3 && gm.Save.RefineStones == stonesBefore - 1, "refine consumes a stone");
        var lineBtn = content.FindChild("RefineLine_0", true, false) as Button;
        Check(lineBtn != null, "refine line button exists");
        lineBtn.EmitSignal(BaseButton.SignalName.Pressed);
        Check(gm.Save.HeroEquipment["custom_hero"]["weapon"].RefineLines[0].Locked, "line lock toggles via UI");

        // Locked tab (2.35 opened Kế thừa; Phụ ma stays locked).
        tabRow2.GetChildren().OfType<Button>().First(b => b.Text == "Phụ ma").EmitSignal(BaseButton.SignalName.Pressed);
        Check(content.GetChildren().OfType<Label>().Any(l => l.Text == "Chưa mở."), "unopened system tab shows Chưa mở.");

        // Unequip via the right panel.
        tabRow2.GetChildren().OfType<Button>().First(b => b.Text == "Cường hóa").EmitSignal(BaseButton.SignalName.Pressed);
        var unequipBtn = content.FindChild("ActionUnequip", true, false) as Button;
        Check(unequipBtn != null, "right panel has unequip button");
        unequipBtn.EmitSignal(BaseButton.SignalName.Pressed);
        Check(gm.Save.HeroEquipment["custom_hero"]["weapon"].IsEmpty, "unequip returns item to bag");

        // Star chart popup from the hub.
        starBtn.EmitSignal(BaseButton.SignalName.Pressed);
        Check(hub.IsPopupOpen, "Tướng Tinh button opens the star chart popup");
        hub.ClosePopup();

        // Hub Chiêu mộ button still opens the tavern (recruit moved out of hero screen).
        hub.GetNode<Button>("FunctionBar/RecruitButtonIcon/RecruitButton").EmitSignal(BaseButton.SignalName.Pressed);
        Check(hub.IsPopupOpen, "hub Chiêu mộ opens the tavern");

        if (_fails == 0) GD.Print("TEST PHASE 2.29: ALL PASS");
        else GD.Print($"TEST PHASE 2.29: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }
}
