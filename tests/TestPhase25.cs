using Godot;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase25 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        CheckEmptySlots();
        CheckEquipUnequipSwap();
        CheckSeedAndLegacy();
        CheckHeroListDoubleClick();
        GD.Print(_failures == 0 ? "TEST PHASE2.5: ALL PASS" : $"TEST PHASE2.5: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }

    private SaveData Fresh()
    {
        var s = new SaveData { Gold = 100000 };
        ProgressionService.EnsureDefaults(s);
        return s;
    }

    private void CheckEmptySlots()
    {
        var s = Fresh();
        Check(s.HeroEquipment["quan_vu"].Values.All(eq => eq.IsEmpty), "fresh heroes start with all equipment slots empty");
        var scaled = ProgressionService.GetScaledBase("quan_vu", 1);
        var eff = ProgressionService.GetEffectiveHero(s, "quan_vu");
        Check(eff.Hp == scaled.Hp && eff.Atk == scaled.Atk, "empty slots add no equipment bonus (effective == scaled base)");
        Check(ProgressionService.TryEnhance(s, "quan_vu", "weapon", new System.Random()) == ProgressionService.EnhanceResult.Failed_Empty, "enhance fails on empty slot");
        Check(!ProgressionService.TryRefine(s, "quan_vu", "weapon"), "refine fails on empty slot");
    }

    private void CheckEquipUnequipSwap()
    {
        var s = Fresh();
        s.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon", Level = 3, RefineIndex = 0 });
        var before = ProgressionService.GetEffectiveHero(s, "quan_vu");
        Check(EquipmentService.EquipFromBag(s, "quan_vu", 0) && s.BagEquipment.Count == 0 && !s.HeroEquipment["quan_vu"]["weapon"].IsEmpty, "equip from bag fills slot and removes bag item");
        var after = ProgressionService.GetEffectiveHero(s, "quan_vu");
        Check(after.Atk > before.Atk, "equipped weapon raises ATK");
        // Equip a second weapon into occupied slot: old one returns to bag, total items preserved.
        s.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon", Level = 5, RefineIndex = 1 });
        Check(EquipmentService.EquipFromBag(s, "quan_vu", 0) && s.BagEquipment.Count == 1 && s.BagEquipment[0].Level == 3, "equip into occupied slot swaps old item back to bag");
        Check(EquipmentService.Unequip(s, "quan_vu", "weapon") && s.HeroEquipment["quan_vu"]["weapon"].IsEmpty && s.BagEquipment.Count == 2, "unequip returns item to bag and empties slot");
        Check(!EquipmentService.Unequip(s, "quan_vu", "weapon"), "unequip fails on already empty slot");
        Check(!EquipmentService.EquipFromBag(s, "quan_vu", 99), "equip fails with out-of-range bag index");
        // Unequip all.
        s.BagEquipment.Clear();
        foreach (var slot in ProgressionService.Slots)
            s.BagEquipment.Add(new BagEquipmentItem { SlotId = slot.Id, Level = 0, RefineIndex = 0 });
        for (int i = 0; i < ProgressionService.Slots.Count; i++) EquipmentService.EquipFromBag(s, "quan_vu", 0);
        Check(EquipmentService.UnequipAll(s, "quan_vu") == ProgressionService.Slots.Count && s.HeroEquipment["quan_vu"].Values.All(eq => eq.IsEmpty) && s.BagEquipment.Count == ProgressionService.Slots.Count, "unequip all empties all slots and returns all items");
    }

    private void CheckSeedAndLegacy()
    {
        var seeded = SeedService.CreateSeededSave("testhero");
        var starter = seeded.OwnedHeroIds[0];
        Check(seeded.HeroEquipment[starter].Values.All(eq => eq.IsEmpty), "seeded starter has no equipped items");
        Check(seeded.BagEquipment.Count == 6 && seeded.BagEquipment.Select(b => b.SlotId).Distinct().Count() == 6, "seeded bag has 6 basic items, one per slot");
        // Legacy save: equipment without IsEmpty field deserializes to false (equipped).
        var legacy = Fresh();
        legacy.HeroEquipment["quan_vu"]["weapon"].IsEmpty = false;
        legacy.HeroEquipment["quan_vu"]["weapon"].Level = 5;
        var scaled = ProgressionService.GetScaledBase("quan_vu", 1);
        var eff = ProgressionService.GetEffectiveHero(legacy, "quan_vu");
        Check(eff.Atk > scaled.Atk, "legacy equipped item still gives bonus");
    }

    private InputEventMouseButton MouseEvent(bool doubleClick)
    {
        return new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, DoubleClick = doubleClick };
    }

    private void CheckHeroListDoubleClick()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("testhero");
        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads for hero list UI test");
        if (hubScene == null) return;
        var hub = hubScene.Instantiate<HubScene>();
        AddChild(hub);
        hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
        Check(HubScene.HeroListPopupSize == new Vector2(1000, 600), "hero list popup is 1000x600 (2.18)");
        var heroList = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        Check(heroList != null, "hero list popup opens");
        if (heroList == null) { hub.QueueFree(); return; }
        var grid = heroList.GetNode<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        var gridButtons = grid.GetChildren().OfType<Button>().ToList();
        Check(gridButtons.Count == 28 && gridButtons.Count(b => b.Disabled == false) == 6, "grid has 28 cells, 6 filled from seed");
        if (gridButtons.Count == 0) { hub.ClosePopup(); hub.QueueFree(); return; }
        int bagBefore = gm.Save.BagEquipment.Count;
        // Single click on a grid item must NOT equip.
        gridButtons[0].EmitSignal(Control.SignalName.GuiInput, MouseEvent(false));
        Check(gm.Save.BagEquipment.Count == bagBefore, "single click on grid item does not equip");
        // Double-click a grid item equips it to the current tab hero.
        gridButtons[0].EmitSignal(Control.SignalName.GuiInput, MouseEvent(true));
        Check(gm.Save.BagEquipment.Count == bagBefore - 1, "double-click grid item equips it");
        // Reopen: the equipped item must NOT be in the grid anymore.
        hub.ClosePopup();
        hub.OpenPopup("res://features/hero/scenes/HeroListScene.tscn", HubScene.HeroListPopupSize);
        var heroList2 = hub.GetNode<Control>("PopupFrame/PopupContent").GetChild(0) as Control;
        var grid2 = heroList2.GetNode<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        Check(grid2.GetChildren().OfType<Button>().Count(b => b.Disabled == false) == bagBefore - 1, "equipped item is no longer in the grid");
        // Phase 2.16: equipped slots flank the portrait, 3 left + 3 right.
        var left2 = heroList2.GetNode<VBoxContainer>("DetailBox/PortraitRow/LeftEquippedBox").GetChildren().OfType<Button>().ToList();
        var right2 = heroList2.GetNode<VBoxContainer>("DetailBox/PortraitRow/RightEquippedBox").GetChildren().OfType<Button>().ToList();
        var equipped2 = left2.Concat(right2).ToList();
        Check(left2.Count == 3 && right2.Count == 3, "hero tab page has 3+3 equipped slots flanking the portrait after reopen");
        // Double-click the equipped slot unequips it back to the grid.
        equipped2[0].EmitSignal(Control.SignalName.GuiInput, MouseEvent(true));
        Check(gm.Save.BagEquipment.Count == bagBefore, "double-click equipped slot unequips it back to bag");
        hub.ClosePopup();
        hub.QueueFree();
    }
}
