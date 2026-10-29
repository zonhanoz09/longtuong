using Godot;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase28 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CallDeferred("RunChecks");
    }

    private void RunChecks()
    {
        Check(ProgressionService.Slots.Count == 6, "6 equipment slots total");
        Check(ProgressionService.Slots.Any(s => s.Id == "cloak" && s.Name == "Choàng"), "cloak slot exists (Choàng)");
        Check(ProgressionService.Slots.Any(s => s.Id == "bracer" && s.Name == "Uyển"), "bracer slot exists (Uyển)");
        var s = new SaveData { Gold = 100000 };
        ProgressionService.EnsureDefaults(s);
        Check(s.HeroEquipment["quan_vu"].Count == 6 && s.HeroEquipment["quan_vu"].Values.All(eq => eq.IsEmpty), "fresh heroes have 6 empty slots");
        var seeded = SeedService.CreateSeededSave("testhero");
        Check(seeded.BagEquipment.Count == 6 && seeded.BagEquipment.Select(b => b.SlotId).Distinct().Count() == 6, "seed bag has 6 items, one per slot");
        // Equipping the new slots raises the right stats.
        var before = ProgressionService.GetEffectiveHero(seeded, seeded.OwnedHeroIds[0]);
        var cloakIdx = seeded.BagEquipment.FindIndex(b => b.SlotId == "cloak");
        var bracerIdx = seeded.BagEquipment.FindIndex(b => b.SlotId == "bracer");
        Check(EquipmentService.EquipFromBag(seeded, seeded.OwnedHeroIds[0], cloakIdx), "equip cloak from bag");
        var afterCloak = ProgressionService.GetEffectiveHero(seeded, seeded.OwnedHeroIds[0]);
        Check(afterCloak.Hp > before.Hp, "equipped cloak raises HP");
        bracerIdx = seeded.BagEquipment.FindIndex(b => b.SlotId == "bracer");
        Check(EquipmentService.EquipFromBag(seeded, seeded.OwnedHeroIds[0], bracerIdx), "equip bracer from bag");
        var afterBracer = ProgressionService.GetEffectiveHero(seeded, seeded.OwnedHeroIds[0]);
        Check(afterBracer.Atk > afterCloak.Atk, "equipped bracer raises ATK");
        // Legacy save with only the old 4 slots gains 2 empty new slots.
        var legacy = new SaveData { Gold = 100000 };
        ProgressionService.EnsureDefaults(legacy);
        legacy.HeroEquipment["quan_vu"].Remove("cloak");
        legacy.HeroEquipment["quan_vu"].Remove("bracer");
        ProgressionService.EnsureDefaults(legacy);
        Check(legacy.HeroEquipment["quan_vu"].ContainsKey("cloak") && legacy.HeroEquipment["quan_vu"]["cloak"].IsEmpty
            && legacy.HeroEquipment["quan_vu"].ContainsKey("bracer") && legacy.HeroEquipment["quan_vu"]["bracer"].IsEmpty, "legacy save gains empty cloak + bracer slots");
        // HeroScreen shows 6 equipment rows (inside its scroll container).
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("testhero");
        var screenScene = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroScreen.tscn");
        Check(screenScene != null, "hero screen scene loads");
        if (screenScene != null)
        {
            var screen = screenScene.Instantiate<Control>();
            AddChild(screen);
            var rows = screen.GetNode<VBoxContainer>("EquipListBox").GetChildren().OfType<Button>().ToList();
            Check(rows.Count == 6, "hero screen has 6 equipment rows (2.29 middle column)");
            screen.QueueFree();
        }
        GD.Print(_failures == 0 ? "TEST PHASE2.8: ALL PASS" : $"TEST PHASE2.8: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }
}
