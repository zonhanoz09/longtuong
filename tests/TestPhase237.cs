using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.37: Cường hóa tab rework — Chế Tạo (craft Tân Thủ into an
/// empty slot), Đúc Lại button in-tab, projected stats (HIỆN TẠI →
/// DỰ KIẾN), item names "{Set} {Slot}", refine cost scales with locked
/// lines, refine lines grade-colored in the tab.
/// </summary>
public partial class TestPhase237 : Node
{
    private int _fails;

    public override async void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("tester237");
        var rng = new System.Random(7);

        // Chế Tạo costs: 15 ore + 30000 Bạc (Tân Thủ Tier 30).
        Check(ForgeService.CraftSetId == "tan_thu", "craft set is Tan Thu");
        Check(ForgeService.CraftOreCost() == 15, "craft costs 15 ore");
        Check(ForgeService.CraftSilverCost() == 30000, "craft costs 30000 silver");

        // Gates: occupied -> ore -> silver.
        int w = gm.Save.BagEquipment.FindIndex(b => b.SlotId == "weapon");
        EquipmentService.EquipFromBag(gm.Save, "custom_hero", w);
        Check(ForgeService.TryCraftIntoSlot(gm.Save, "custom_hero", "weapon", rng)
            == ForgeService.CraftResult.SlotOccupied, "occupied slot fails");
        EquipmentService.Unequip(gm.Save, "custom_hero", "weapon");
        gm.Save.Materials[ForgeService.OreId] = 0;
        Check(ForgeService.TryCraftIntoSlot(gm.Save, "custom_hero", "armor", rng)
            == ForgeService.CraftResult.NoOre, "needs ore");
        gm.Save.Materials[ForgeService.OreId] = 999;
        gm.Save.Silver = 0;
        Check(ForgeService.TryCraftIntoSlot(gm.Save, "custom_hero", "armor", rng)
            == ForgeService.CraftResult.NoSilver, "needs silver");

        // Success: Tân Thủ armor directly into the slot.
        gm.Save.Silver = 100000;
        int silver0 = gm.Save.Silver, ore0 = ForgeService.OreCount(gm.Save);
        Check(ForgeService.TryCraftIntoSlot(gm.Save, "custom_hero", "armor", rng)
            == ForgeService.CraftResult.Success, "craft succeeds");
        var eq = gm.Save.HeroEquipment["custom_hero"]["armor"];
        Check(!eq.IsEmpty && eq.SetId == "tan_thu" && eq.Level == 0, "crafted Tan Thu into the slot");
        Check(eq.RefineLines.Count == 3 && eq.Sockets.Count == 1, "crafted item has 3 lines, 1 socket");
        Check(gm.Save.Silver == silver0 - 30000 && ForgeService.OreCount(gm.Save) == ore0 - 15,
            "craft consumes ore + silver");

        // Item naming.
        Check(EquipmentSets.DisplayName("chu_tuoc", "armor") == "Chu Tước Giáp", "Chu Tuoc Giap");
        Check(EquipmentSets.DisplayName("tan_thu", "weapon") == "Tân Thủ Vũ khí", "Tan Thu Vu khi");

        // Projection: enhancing the crafted armor changes HP/Def.
        var hyp = EquipmentService.CopyState(eq);
        hyp.Level = 1;
        var proj = EquipmentService.ProjectEquipChange(gm.Save, "custom_hero", "armor", hyp);
        Check(proj.Count > 0 && proj.Any(r => r.Name == "HP" && r.After > r.Before), "projection shows HP gain");
        Check(EquipmentService.ProjectEquipChange(gm.Save, "custom_hero", "armor", EquipmentService.CopyState(eq)).Count == 0,
            "identical state projects no change");

        // Refine cost scales with locked lines.
        Check(ProgressionService.RefineCostStones(gm.Save, "custom_hero", "armor") == 1, "0 locked -> 1 stone");
        ProgressionService.ToggleRefineLock(gm.Save, "custom_hero", "armor", 0);
        ProgressionService.ToggleRefineLock(gm.Save, "custom_hero", "armor", 1);
        Check(ProgressionService.RefineCostStones(gm.Save, "custom_hero", "armor") == 3, "2 locked -> 3 stones");
        Check(ProgressionService.RefineCostGold(gm.Save, "custom_hero", "armor") == 600, "gold fallback scales too");

        // UI: empty slot shows Chế Tạo; equipped shows Cường Hóa + Đúc Lại.
        var screenScene = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroScreen.tscn");
        var screen = screenScene.Instantiate<HeroScreen>();
        AddChild(screen);
        await ToSignal(GetTree(), "process_frame");
        await ToSignal(GetTree(), "process_frame");
        var content = screen.GetNode<Control>("RightPanel/TabContent");
        // No-scroll: content height must fit the 560px panel.
        var inner = (Control)content.GetChild(0);
        Check(inner.GetRect().Size.Y <= 560, $"content fits panel ({inner.GetRect().Size.Y:0}px <= 560)");
        // Default selection: custom_hero + weapon (equipped from the seed flow above? re-equip).
        var equipBox = screen.GetNode<VBoxContainer>("EquipListBox");
        var armorRow = equipBox.GetChildren().OfType<Button>().First(b => b.Name == "EquipRow_armor");
        armorRow.EmitSignal(BaseButton.SignalName.Pressed);
        content = screen.GetNode<Control>("RightPanel/TabContent");
        Check(content.FindChild("ActionEnhance", true, false) != null, "equipped slot shows Cường Hóa");
        Check(content.FindChild("ActionRecast", true, false) != null, "equipped slot shows Đúc Lại");
        Check(content.FindChild("ActionCraft", true, false) == null, "equipped slot has no Chế Tạo");
        var labels = content.FindChildren("*", "Label", true, false).OfType<Label>().Select(l => l.Text).ToList();
        Check(labels.Any(t => t.Contains("CƯỜNG HÓA:")), "enhance section shown");
        Check(labels.Any(t => t.Contains("ĐÚC LẠI:")), "recast section shown");
        Check(labels.Any(t => t.Contains("Cooldown cường hóa: Sẵn sàng")), "cooldown line shown");

        // Empty slot (boots is empty in the seeded save after craft? use helmet).
        var helmetRow = equipBox.GetChildren().OfType<Button>().First(b => b.Name == "EquipRow_helmet");
        helmetRow.EmitSignal(BaseButton.SignalName.Pressed);
        content = screen.GetNode<Control>("RightPanel/TabContent");
        var craftBtn = content.FindChild("ActionCraft", true, false) as Button;
        Check(craftBtn != null && craftBtn.Text == "Chế tạo", "empty slot shows Chế tạo");
        Check(content.FindChild("ActionEnhance", true, false) == null, "empty slot has no Cường Hóa");
        craftBtn.EmitSignal(BaseButton.SignalName.Pressed);
        var eqH = gm.Save.HeroEquipment["custom_hero"]["helmet"];
        Check(!eqH.IsEmpty && eqH.SetId == "tan_thu", "UI craft equips Tan Thu helmet");
        var status = content.FindChild("ActionStatus", true, false) as Label;
        Check(status != null && status.Text.Contains("thành công"), "UI craft reports success");

        // Row name uses the new "{Set} {Slot}" format (armor was crafted above).
        var rowName = armorRow.FindChild("RowName", true, false) as Label;
        Check(rowName != null && rowName.Text.Contains("Tân Thủ Giáp"), "row shows Set + Slot name");

        // 2.37b fixes: no Lò rèn button; projection deltas formatted "+35" (not "+N35").
        Check(screen.FindChild("ForgeOpenButton", true, false) == null, "Lo ren button removed");
        Check(screen.FindChild("EnhanceScroll", true, false) == null, "no scroll container (fits popup)");
        var allLabels = screen.FindChildren("*", "Label", true, false).OfType<Label>().Select(l => l.Text).ToList();
        Check(allLabels.Any(t => t.StartsWith("▸") && t.Contains("+") && !t.Contains("+N")),
            "delta format is +35, not +N35");
        // Inline blueprint purchase (helmet is now Tan Thu -> Chu Tuoc, no blueprint learned).
        content = screen.GetNode<Control>("RightPanel/TabContent"); // re-fetch after rebuild
        var bpBuy = content.FindChildren("*", "Button", true, false).OfType<Button>()
            .FirstOrDefault(b => b.Text.Contains("Mua") && b.Text.Contains("Vàng"));
        Check(bpBuy != null, "inline blueprint buy button shown");
        int goldBefore = gm.Save.Gold;
        bpBuy.EmitSignal(BaseButton.SignalName.Pressed);
        Check(ForgeService.HasBlueprint(gm.Save, "chu_tuoc"), "UI buys the blueprint");
        Check(gm.Save.Gold == goldBefore - ForgeService.BlueprintPrices["chu_tuoc"], "blueprint costs gold");

        if (_fails == 0) GD.Print("TEST PHASE 2.37: ALL PASS");
        else GD.Print($"TEST PHASE 2.37: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }
}
