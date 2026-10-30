using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.34: rèn — blueprints and ore. Phase 2.38 removed the
/// standalone forge popup (TryForge into the bag).
/// </summary>
public partial class TestPhase234 : Node
{
    private int _fails;

    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("tester234");

        // Blueprint catalog.
        Check(ForgeService.BlueprintPrices.Count == 5, "5 forgeable sets (no Tan Thu)");
        Check(ForgeService.BlueprintPrices["chu_tuoc"] == 50000, "Chu Tuoc blueprint 50000 gold");
        Check(ForgeService.RecastOreCost("chu_tuoc") == 25, "25 ore for a Chu Tuoc recast");
        Check(ForgeService.OrePriceGold == 100, "ore 100 gold each");

        // Buy blueprint.
        Check(!ForgeService.HasBlueprint(gm.Save, "chu_tuoc"), "no blueprint at start");
        int gold0 = gm.Save.Gold;
        Check(ForgeService.TryBuyBlueprint(gm.Save, "chu_tuoc"), "buy blueprint");
        Check(gm.Save.Gold == gold0 - 50000 && ForgeService.HasBlueprint(gm.Save, "chu_tuoc"), "blueprint costs gold");
        Check(!ForgeService.TryBuyBlueprint(gm.Save, "chu_tuoc"), "cannot buy twice");
        gm.Save.Gold = 0;
        Check(!ForgeService.TryBuyBlueprint(gm.Save, "bach_ho"), "cannot buy without gold");

        // Buy ore.
        gm.Save.Gold = 100000;
        Check(ForgeService.TryBuyOre(gm.Save, 10), "buy ore");
        Check(ForgeService.OreCount(gm.Save) == 10, "10 ore in stock");
        Check(!ForgeService.TryBuyOre(gm.Save, 0), "buy zero ore fails");

        // Phase 2.38: the standalone Lò Rèn popup (forge into the bag) was
        // removed as dead code; blueprints/ore are bought inline in the
        // Cường hóa tab and used by Đúc Lại (see TestPhase237).
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        hub.GetNode<Button>("FunctionBar/EnhanceButton").EmitSignal(BaseButton.SignalName.Pressed);
        Check(hub.IsPopupOpen, "Trang bị popup opens");
        var heroScreen = hub.GetNode("PopupFrame/PopupContent").GetChild(0);
        Check(heroScreen.FindChild("ForgeOpenButton", true, false) == null, "Lo ren button removed (2.37b)");
        Check(!ResourceLoader.Exists("res://features/forge/scenes/ForgeScene.tscn"), "ForgeScene removed (2.38)");

        if (_fails == 0) GD.Print("TEST PHASE 2.34: ALL PASS");
        else GD.Print($"TEST PHASE 2.34: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }
}
