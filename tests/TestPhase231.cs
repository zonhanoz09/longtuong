using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.31: Cây Rung Tiền (minimal Bạc income) + enhance rework
/// (Bạc cost 100x2^level, max follows main hero level, success roll).
/// </summary>
public partial class TestPhase231 : Node
{
    private int _fails;

    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("tester231");

        // --- Money tree unit ---
        Check(MoneyTreeService.FreeShakesPerDay == 10, "10 free shakes per day");
        Check(MoneyTreeService.ShakeReward(gm.Save) == 6000, "shake reward 5000+1000xLv1 = 6000");
        int silver0 = gm.Save.Silver;
        Check(MoneyTreeService.TryShake(gm.Save), "first shake succeeds");
        Check(gm.Save.Silver == silver0 + 6000 && gm.Save.MoneyTreeUsed == 1, "shake grants silver, counts use");
        for (int i = 0; i < 9; i++) MoneyTreeService.TryShake(gm.Save);
        Check(MoneyTreeService.ShakesLeft(gm.Save) == 0, "no shakes left after 10");
        Check(gm.Save.RefineStones == SeedService.SeededRefineStones + MoneyTreeService.CompletionStoneBonus,
            "completing 10 shakes grants 3 refine stones");
        int silverAfter10 = gm.Save.Silver;
        Check(!MoneyTreeService.TryShake(gm.Save) && gm.Save.Silver == silverAfter10, "11th shake fails, no silver");
        // New day resets.
        gm.Save.MoneyTreeDate = "2000-01-01";
        Check(MoneyTreeService.ShakesLeft(gm.Save) == 10, "new day resets shakes");
        Check(MoneyTreeService.TryShake(gm.Save), "shake works again after reset");
        // Reward scales with main hero level.
        gm.Save.Heroes["custom_hero"].Level = 10;
        Check(MoneyTreeService.ShakeReward(gm.Save) == 15000, "shake reward scales with level (15000 at Lv10)");

        // --- Hub: money tree button + popup flow ---
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        var treeBtn = hub.GetNodeOrNull<Button>("FunctionBar/MoneyTreeButton");
        Check(treeBtn != null && treeBtn.Text.Contains("Tiền"), "hub has Cây Tiền button");
        Check(HubScene.MoneyTreePopupSize == new Vector2(440, 360), "money tree popup size 440x360");
        gm.Save.MoneyTreeDate = "2000-01-02"; // fresh day for the UI flow
        gm.Save.MoneyTreeUsed = 0;
        treeBtn.EmitSignal(BaseButton.SignalName.Pressed);
        Check(hub.IsPopupOpen, "Cây Tiền opens the money tree popup");
        var widget = hub.GetNode("PopupFrame/PopupContent").GetChild(0);
        var shakeBtn = widget.FindChild("ShakeButton", true, false) as Button;
        Check(shakeBtn != null && !shakeBtn.Disabled, "popup has an enabled shake button");
        int silverBeforeUi = gm.Save.Silver;
        shakeBtn.EmitSignal(BaseButton.SignalName.Pressed);
        Check(gm.Save.Silver > silverBeforeUi, "UI shake grants silver");
        hub.ClosePopup();

        if (_fails == 0) GD.Print("TEST PHASE 2.31: ALL PASS");
        else GD.Print($"TEST PHASE 2.31: {_fails} FAILURES");
        GetTree().Quit();
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }
}
