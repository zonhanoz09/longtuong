using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.34: rèn — blueprints, ore, forge gates, forged items,
/// and the forge popup flow from the Trang bị screen.
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
        Check(ForgeService.OrePerForge("chu_tuoc") == 25, "25 ore per Chu Tuoc forge");
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

        // Forge gates (ordered: BadSet -> NoBlueprint -> LowLevel -> NoOre).
        var rng = new System.Random(1);
        Check(ForgeService.TryForge(gm.Save, "tan_thu", "weapon", rng) == ForgeService.ForgeResult.BadSet, "Tan Thu not forgeable");
        Check(ForgeService.TryForge(gm.Save, "bach_ho", "weapon", rng) == ForgeService.ForgeResult.NoBlueprint, "forge needs blueprint");
        Check(ForgeService.TryForge(gm.Save, "chu_tuoc", "weapon", rng) == ForgeService.ForgeResult.LowLevel, "forge needs main level 50");
        gm.Save.Heroes["custom_hero"].Level = 50;
        Check(ForgeService.TryForge(gm.Save, "chu_tuoc", "weapon", rng) == ForgeService.ForgeResult.NoOre, "forge needs ore (10 < 25)");
        ForgeService.TryBuyOre(gm.Save, 20);

        // Successful forge.
        int bagBefore = gm.Save.BagEquipment.Count;
        int oreBefore = ForgeService.OreCount(gm.Save);
        Check(ForgeService.TryForge(gm.Save, "chu_tuoc", "weapon", rng) == ForgeService.ForgeResult.Success, "forge succeeds");
        Check(gm.Save.BagEquipment.Count == bagBefore + 1, "forged item lands in bag");
        var item = gm.Save.BagEquipment[^1];
        Check(item.SetId == "chu_tuoc" && item.Tier == 50 && item.Quality == "Luc" && item.SlotId == "weapon",
            "forged item has set fields");
        Check(item.RefineLines.Count == 4, "forged Luc item has 4 lines");
        Check(ForgeService.OreCount(gm.Save) == oreBefore - 25, "forge consumes ore");

        // UI: Trang bị popup -> Lò rèn -> buy ore + forge.
        gm.Save.Gold = 200000;
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        hub.GetNode<Button>("FunctionBar/EnhanceButton").EmitSignal(BaseButton.SignalName.Pressed);
        Check(hub.IsPopupOpen, "Trang bị popup opens");
        var heroScreen = hub.GetNode("PopupFrame/PopupContent").GetChild(0);
        Check(heroScreen.FindChild("ForgeOpenButton", true, false) == null, "Lo ren button removed (2.37b)");
        // Forge popup still works standalone (blueprint/ore shop).
        var forgeScene = ResourceLoader.Load<PackedScene>("res://features/forge/scenes/ForgeScene.tscn");
        var forgeWidget = forgeScene.Instantiate<ForgeScene>();
        AddChild(forgeWidget);
        Check(forgeWidget.FindChild("BlueprintRow_chu_tuoc", true, false) != null, "blueprint rows listed");
        var oreBtn = forgeWidget.FindChild("OreBuyButton", true, false) as Button;
        oreBtn.EmitSignal(BaseButton.SignalName.Pressed);
        oreBtn = forgeWidget.FindChild("OreBuyButton", true, false) as Button;
        oreBtn.EmitSignal(BaseButton.SignalName.Pressed);
        oreBtn = forgeWidget.FindChild("OreBuyButton", true, false) as Button;
        oreBtn.EmitSignal(BaseButton.SignalName.Pressed);
        Check(ForgeService.OreCount(gm.Save) >= 25, "UI ore purchase works");
        int bagBeforeUi = gm.Save.BagEquipment.Count;
        var forgeBtn = forgeWidget.FindChild("ForgeButton", true, false) as Button;
        forgeBtn.EmitSignal(BaseButton.SignalName.Pressed);
        Check(gm.Save.BagEquipment.Count == bagBeforeUi + 1, "UI forge adds an item to the bag");
        var status = forgeWidget.FindChild("ForgeStatus", true, false) as Label;
        Check(status != null && status.Text.Contains("thành công"), "UI forge reports success");

        if (_fails == 0) GD.Print("TEST PHASE 2.34: ALL PASS");
        else GD.Print($"TEST PHASE 2.34: {_fails} FAILURES");
        GetTree().Quit();
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }
}
