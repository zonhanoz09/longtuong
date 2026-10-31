using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.48 — Cửa hàng Chiến tích (tab in the Ải Vượt Tướng popup).
/// </summary>
public partial class TestPhase248 : Node
{
    private int _fails;

    public override void _Ready()
    {
        CheckService();
        CheckUi();

        if (_fails == 0) GD.Print("TEST PHASE 2.48: ALL PASS");
        else GD.Print($"TEST PHASE 2.48: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private void CheckService()
    {
        var prices = MeritShopService.Items.ToDictionary(i => i.Id, i => i.Price);
        Check(prices.Count == 7 && prices["ore"] == 20 && prices["refine_stone"] == 30 && prices["frag_chu_tuoc"] == 50 && prices["frag_bach_ho"] == 80
            && prices["frag_thanh_long"] == 120 && prices["frag_huyen_vu"] == 180 && prices["frag_ky_lan"] == 250, "price table");
        var s = SeedService.CreateSeededSave("t248a");
        s.Merit = 100;
        int ore = ForgeService.OreCount(s), stones = s.RefineStones;
        Check(MeritShopService.TryBuy(s, "ore", 2) && s.Merit == 60 && ForgeService.OreCount(s) == ore + 2, "2 Khoáng thạch for 40");
        Check(MeritShopService.TryBuy(s, "refine_stone", 1) && s.Merit == 30 && s.RefineStones == stones + 1, "1 Đá tẩy luyện for 30");
        Check(!MeritShopService.TryBuy(s, "frag_chu_tuoc", 1) && s.Merit == 30 && ForgeService.FragmentCount(s, "chu_tuoc") == 0, "not enough: nothing changes");
        s.Merit = 50;
        Check(MeritShopService.TryBuy(s, "frag_chu_tuoc", 1) && s.Merit == 0 && ForgeService.FragmentCount(s, "chu_tuoc") == 1, "1 Mảnh Chu Tước for 50");
        s.Merit = 1000;
        Check(!MeritShopService.TryBuy(s, "nope", 1) && !MeritShopService.TryBuy(s, "ore", 0) && !MeritShopService.TryBuy(s, "ore", -3) && s.Merit == 1000,
            "unknown item / bad count refused");
        // Bought fragments are usable by Đúc Lại.
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("t248b");
        int w = gm.Save.BagEquipment.FindIndex(b => b.SlotId == "weapon");
        EquipmentService.EquipFromBag(gm.Save, "custom_hero", w);
        gm.Save.Heroes["custom_hero"].Level = 50;
        gm.Save.Blueprints["chu_tuoc"] = true;
        gm.Save.Materials[ForgeService.OreId] = 100;
        gm.Save.Silver = 100000;
        gm.Save.Merit = 500;
        Check(MeritShopService.TryBuy(gm.Save, "frag_chu_tuoc", 10) && gm.Save.Merit == 0, "10 Mảnh Chu Tước for 500");
        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", new System.Random(1)) == ForgeService.RecastResult.Success,
            "bought fragments are spent by Đúc Lại");
    }

    private void CheckUi()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("t248c");
        gm.Save.LastStage = 20;
        gm.Save.Heroes["custom_hero"].Level = 20;
        gm.Save.Merit = 60;
        var ui = ResourceLoader.Load<PackedScene>("res://features/duel/scenes/DuelTowerScene.tscn").Instantiate<DuelTowerScene>();
        AddChild(ui);
        ui.GetNode<Button>("TabRow/ShopTab").EmitSignal(BaseButton.SignalName.Pressed);
        Check(ui.CurrentTab == DuelTowerScene.TabShop, "Cửa hàng tab opens");
        var rows = ui.GetNode<VBoxContainer>("Content").GetChildren().OfType<HBoxContainer>().ToList();
        Check(rows.Count == 7, "7 shop rows");
        var oreBtn = ui.FindChild("Shop_ore", true, false)?.GetNode<Button>("BuyButton");
        var kyLanBtn = ui.FindChild("Shop_frag_ky_lan", true, false)?.GetNode<Button>("BuyButton");
        Check(oreBtn != null && !oreBtn.Disabled && kyLanBtn != null && kyLanBtn.Disabled, "affordable items enabled, others disabled");
        Check(ui.GetNode<Label>("MeritLabel").Text.StartsWith("Chiến tích: 60"), "balance shown");
        int ore = ForgeService.OreCount(gm.Save);
        oreBtn!.EmitSignal(BaseButton.SignalName.Pressed);
        Check(gm.Save.Merit == 40 && ForgeService.OreCount(gm.Save) == ore + 1, "pressing Đổi buys 1 Khoáng thạch");
        Check(ui.GetNode<Label>("MeritLabel").Text.StartsWith("Chiến tích: 40") && ui.GetNode<Label>("StatusLabel").Text.Contains("Đã đổi"), "balance + status refresh");
        var oreLabel = ui.FindChild("Shop_ore", true, false)?.GetNode<Label>("ItemLabel");
        Check(oreLabel != null && oreLabel.Text.Contains($"(có {ore + 1})"), "row shows the new owned count");
        ui.OnBuyPressed("frag_ky_lan");
        Check(gm.Save.Merit == 40 && ui.GetNode<Label>("StatusLabel").Text == "Không đủ Chiến tích!", "can't afford: refused with a message");
        var saved = SaveManager.Load(gm.SavePath);
        Check(saved.Merit == 40, "purchase saved");
        ui.GetNode<Button>("TabRow/ClimbTab").EmitSignal(BaseButton.SignalName.Pressed);
        Check(ui.CurrentTab == DuelTowerScene.TabClimb && ui.FindChild("ChallengeButton", true, false) != null, "back to Leo tầng");
        ui.QueueFree();

        // The Hoạt Động card opens this popup.
        var hub = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn").Instantiate<HubScene>();
        AddChild(hub);
        hub.GetNode<Button>("ActivityButtonIcon/ActivityButton").EmitSignal(BaseButton.SignalName.Pressed);
        var content = hub.GetNode<Control>("PopupFrame/PopupContent");
        (content.GetChild(0) as ActivityScene)!.GetNode<Button>("CardGrid/Card_Duel").EmitSignal(BaseButton.SignalName.Pressed);
        var duel = content.GetChildren().OfType<DuelTowerScene>().FirstOrDefault();
        Check(duel != null && duel.Size == HubScene.DuelPopupSize, "Vượt Tướng card opens the 720x520 popup");
        hub.ClosePopup();
        hub.QueueFree();
    }
}
