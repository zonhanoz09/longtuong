using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.33: khảm ngọc — gem table, sockets, socket/unsocket,
/// unlock, merge 3→1, buy, and the Đục lỗ tab UI.
/// </summary>
public partial class TestPhase233 : Node
{
    private int _fails;

    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("tester233");

        // Gem table.
        Check(GemService.Defs.Count == 6, "6 gem types");
        Check(GemService.GetDef("speed")!.PerLevel == 3, "speed gem +3/level");
        Check(GemService.MaxGemLevel == 10, "max gem level 10");
        Check(GemService.ParseGemId("speed_3") == ("speed", 3), "parse gem id");
        Check(GemService.MakeGemId("atk", 2) == "atk_2", "make gem id");
        Check(GemService.DescribeGem("crit_2").Contains("Lv2"), "describe gem");

        // Sockets by quality.
        Check(GemService.SocketsForQuality("Lam") == 1 && GemService.SocketsForQuality("Do") == 4, "sockets per quality 1..4");
        Check(GemService.UnlockCostSilver(1) == 20000, "unlock cost 20000 for 2nd socket");

        // Seed gems.
        Check(GemService.GemCount(gm.Save, "speed_1") == 2, "seed 2 speed gems");

        // Socket a gem -> stats.
        int w = gm.Save.BagEquipment.FindIndex(b => b.SlotId == "weapon");
        EquipmentService.EquipFromBag(gm.Save, "custom_hero", w);
        var base0 = ProgressionService.GetEffectiveHero(gm.Save, "custom_hero");
        Check(GemService.TrySocket(gm.Save, "custom_hero", "weapon", 0, "speed_1"), "socket speed gem");
        var eff = ProgressionService.GetEffectiveHero(gm.Save, "custom_hero");
        Check(eff.Speed == base0.Speed + 3, "socketed gem adds speed");
        Check(GemService.GemCount(gm.Save, "speed_1") == 1, "gem consumed from pouch");
        Check(!GemService.TrySocket(gm.Save, "custom_hero", "weapon", 0, "atk_1"), "cannot socket into occupied socket");
        Check(!GemService.TrySocket(gm.Save, "custom_hero", "weapon", 5, "atk_1"), "bad socket index fails");
        Check(!GemService.TrySocket(gm.Save, "custom_hero", "weapon", 0, "nope_1"), "unknown gem fails");

        // Unsocket returns the gem.
        Check(GemService.TryUnsocket(gm.Save, "custom_hero", "weapon", 0), "unsocket works");
        Check(GemService.GemCount(gm.Save, "speed_1") == 2, "gem returned to pouch");
        var eff2 = ProgressionService.GetEffectiveHero(gm.Save, "custom_hero");
        Check(eff2.Speed == base0.Speed, "stats revert after unsocket");

        // Unlock 2nd socket on a Tím item.
        gm.Save.BagEquipment.Clear();
        gm.Save.BagEquipment.Add(new BagEquipmentItem { SlotId = "armor", Quality = "Tim", SetId = "chu_tuoc", Tier = 50 });
        EquipmentService.EquipFromBag(gm.Save, "custom_hero", 0);
        var eqA = gm.Save.HeroEquipment["custom_hero"]["armor"];
        Check(eqA.Sockets.Count == 2 && eqA.Sockets[0].Unlocked && !eqA.Sockets[1].Unlocked, "Tim item: 2 sockets, first open");
        int silverBefore = gm.Save.Silver;
        Check(GemService.TryUnlockSocket(gm.Save, "custom_hero", "armor", 1), "unlock 2nd socket");
        Check(gm.Save.Silver == silverBefore - 20000 && eqA.Sockets[1].Unlocked, "unlock costs 20000 silver");

        // Merge 3->1.
        gm.Save.GemInventory["crit_1"] = 3;
        Check(GemService.TryMerge(gm.Save, "crit", 1), "merge 3 crit_1");
        Check(GemService.GemCount(gm.Save, "crit_1") == 0 && GemService.GemCount(gm.Save, "crit_2") == 1, "merge produces crit_2");
        gm.Save.GemInventory["crit_10"] = 3;
        Check(!GemService.TryMerge(gm.Save, "crit", 10), "merge at max level fails");
        Check(!GemService.TryMerge(gm.Save, "dodge", 1), "merge without 3 gems fails");

        // Buy gem.
        int goldBefore = gm.Save.Gold;
        Check(GemService.TryBuyGem(gm.Save, new System.Random(1)), "buy gem succeeds");
        Check(gm.Save.Gold == goldBefore - GemService.BuyGemGold, "buy costs 2000 gold");
        int pouchTotal = gm.Save.GemInventory.Values.Sum();
        Check(pouchTotal > 0, "bought gem lands in pouch");
        gm.Save.Gold = 0;
        Check(!GemService.TryBuyGem(gm.Save, new System.Random(1)), "buy fails without gold");

        // UI: Đục lỗ tab shows sockets, not "Chưa mở.".
        var screenScene = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroScreen.tscn");
        var screen = screenScene.Instantiate<HeroScreen>();
        AddChild(screen);
        var tabRow = screen.GetNode<HBoxContainer>("RightPanel/TabRow");
        tabRow.GetChildren().OfType<Button>().First(b => b.Text == "Đục lỗ").EmitSignal(BaseButton.SignalName.Pressed);
        var content = screen.GetNode<Control>("RightPanel/TabContent");
        Check(content.FindChild("SocketRow_0", true, false) != null, "gem tab shows socket rows");
        Check(content.FindChild("LockedNote", true, false) == null, "gem tab is open (no Chưa mở.)");
        Check(content.FindChild("GemBuyButton", true, false) != null, "gem pouch has buy button");
        // Socket flow via UI: Khảm -> pick a gem.
        gm.Save.Gold = 100000;
        var sockAction = content.FindChild("SocketAction_0", true, false) as Button;
        Check(sockAction != null && sockAction.Text == "Khảm", "empty socket offers Khảm");
        sockAction.EmitSignal(BaseButton.SignalName.Pressed);
        var pickBtn = content.FindChild("GemPick_speed_1", true, false) as Button;
        Check(pickBtn != null, "gem picker lists owned gems");
        pickBtn.EmitSignal(BaseButton.SignalName.Pressed);
        var eqW = gm.Save.HeroEquipment["custom_hero"]["weapon"];
        Check(eqW.Sockets[0].GemId == "speed_1", "UI socket flow equips the gem");

        if (_fails == 0) GD.Print("TEST PHASE 2.33: ALL PASS");
        else GD.Print($"TEST PHASE 2.33: {_fails} FAILURES");
        GetTree().Quit();
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }
}
