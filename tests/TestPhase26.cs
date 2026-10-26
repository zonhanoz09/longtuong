using Godot;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase26 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        // Service level: detail text carries stats, item buttons must not.
        var detail = EquipmentService.DetailText("weapon", 3, 0, "Trạng thái: Trong túi");
        Check(detail.Contains("Tấn công:") && detail.Contains("Tẩy luyện:"), "detail text contains item stats and refine");
        Check(detail.Contains("Trạng thái: Trong túi"), "detail text contains status");

        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("testhero");
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads for 2.6 UI test");
        if (hubScene == null) { GetTree().Quit(1); return; }
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        Check(heroList != null, "hero list popup opens");
        if (heroList == null) { hub.QueueFree(); GetTree().Quit(1); return; }
        // Fixed layout sizes.
        var ownedScroll = heroList.GetNode<ScrollContainer>("OwnedScroll");
        var detailBox = heroList.GetNode<VBoxContainer>("DetailBox");
        var rightBox = heroList.GetNode<VBoxContainer>("RightBox");
        var tooltipPanel = heroList.GetNode<PanelContainer>("RightBox/TooltipPanel");
        var tooltip = heroList.GetNode<Label>("RightBox/TooltipPanel/TooltipLabel");
        Check(ownedScroll.Size == new Vector2(260, 540), "owned scroll is fixed 260x540");
        Check(detailBox.Size == new Vector2(500, 580), "detail column is fixed 500x580");
        Check(rightBox.Size == new Vector2(340, 580), "right column is fixed 340x580");
        Check(tooltipPanel.CustomMinimumSize == new Vector2(320, 200), "tooltip panel is fixed 320x200");
        Check(tooltip.Text == "Hover vào trang bị để xem chi tiết.", "tooltip shows default hint initially");
        // No stats on item buttons.
        var ownedBox = heroList.GetNode<VBoxContainer>("OwnedScroll/OwnedListBox");
        var ownedButtons = ownedBox.GetChildren().OfType<Button>().ToList();
        var rows = detailBox.GetChildren().OfType<HBoxContainer>().ToList();
        var equipped = rows.Count >= 3 ? rows[2].GetChildren().OfType<Button>().ToList() : new System.Collections.Generic.List<Button>();
        string[] statWords = { "ATK", "HP", "Tấn công", "Phòng thủ", "Tẩy luyện", "Bạo", "Né" };
        Check(ownedButtons.Count == 4 && ownedButtons.All(b => !statWords.Any(w => b.Text.Contains(w))), "owned item buttons show no stats");
        Check(equipped.Count == 4 && equipped.All(b => !statWords.Any(w => b.Text.Contains(w))), "equipped slot buttons show no stats");
        Check(ownedButtons.All(b => b.CustomMinimumSize == new Vector2(240, 48)), "owned item buttons are fixed 240x48");
        Check(equipped.All(b => b.CustomMinimumSize == new Vector2(115, 56)), "equipped slot buttons are fixed 115x56");
        // Hover shows detail with stats in the tooltip panel.
        ownedButtons[0].EmitSignal(Control.SignalName.MouseEntered);
        Check(tooltip.Text.Contains("Tẩy luyện:") && tooltip.Text.Contains("Trạng thái: Trong túi"), "hover on owned item shows detail + stats in tooltip");
        ownedButtons[0].EmitSignal(Control.SignalName.MouseExited);
        Check(tooltip.Text == "Hover vào trang bị để xem chi tiết.", "mouse exit restores default tooltip");
        // Equip to a locked hero (all heroes can wear items), then reopen: owned list still has 4 items, one marked with that hero.
        var selector = rows[0].GetChildren().OfType<Button>().Concat(rows[1].GetChildren().OfType<Button>()).ToList();
        var starter = gm.Save.OwnedHeroIds[0];
        var locked = HeroDatabase.Heroes.First(h => h.Id != starter).Id;
        var lockedBtn = selector.First(b => b.Text == HeroDatabase.Get(locked).Name);
        lockedBtn.EmitSignal(Button.SignalName.Pressed);
        ownedButtons[0].EmitSignal(Control.SignalName.GuiInput, new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, DoubleClick = true });
        Check(!gm.Save.HeroEquipment[locked]["weapon"].IsEmpty, "bag item equips to a locked hero (all heroes)");
        hub.ClosePopup();
        hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
        var heroList2 = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        var owned2 = heroList2.GetNode<VBoxContainer>("OwnedScroll/OwnedListBox").GetChildren().OfType<Button>().ToList();
        Check(owned2.Count == 4, "owned list still has 4 items after equipping (bag + equipped)");
        Check(owned2.Any(b => b.Text.Contains(HeroDatabase.Get(locked).Name)), "owned list marks the equipped item with its hero");
        hub.ClosePopup();
        hub.QueueFree();
        GD.Print(_failures == 0 ? "TEST PHASE2.6: ALL PASS" : $"TEST PHASE2.6: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }
}
