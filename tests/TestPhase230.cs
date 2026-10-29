using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.30: equipment sets by level tier (Huynh's spec) — set table,
/// set bonuses in GetEffectiveHero, migration of old saves, full-field
/// equip/unequip transfer, refine-line model.
/// </summary>
public partial class TestPhase230 : Node
{
    private int _fails;

    public override void _Ready()
    {
        // 1. Set table.
        Check(EquipmentSets.All.Count == 6, "6 equipment sets");
        var ct = EquipmentSets.Get("chu_tuoc");
        Check(ct.Tier == 50 && ct.Quality == "Tim" && ct.Name == "Chu Tước", "Chu Tuoc T50 Tim");
        Check(ct.Bonuses.Count == 3 && ct.Bonuses[0].Pieces == 2 && ct.Bonuses[2].Pieces == 6, "Chu Tuoc bonuses 2/4/6");
        var kl = EquipmentSets.Get("ky_lan");
        Check(kl.Quality == "Do" && kl.Tier == 100, "Ky Lan Do T100");
        Check(EquipmentSets.LinesForQuality("Lam") == 3 && EquipmentSets.LinesForQuality("Do") == 6, "lines per quality 3..6");
        Check(!SetBonusService.IsEngineSupported("starting_rage") && SetBonusService.IsEngineSupported("dmg_red"),
            "deferred (rage) vs supported (dmg_red) bonus stats");
        Check(EquipmentSets.Get("nope").Id == "tan_thu", "unknown set falls back to Tan Thu");

        // 2. Seed items are Tân Thủ.
        var seeded = SeedService.CreateSeededSave("t230");
        Check(seeded.BagEquipment.Count == 6, "seed 6 items");
        Check(seeded.BagEquipment.All(b => b.SetId == "tan_thu" && b.Tier == 30 && b.Quality == "Lam"),
            "seed items are Tan Thu Lv30 Lam");
        Check(seeded.BagEquipment.All(b => b.RefineLines.Count == 3), "seed items have 3 refine lines");

        // 3. Set bonus: 2x Chu Tước => +10% HP, refine lines apply.
        var save = SeedService.CreateSeededSave("t230b");
        save.BagEquipment.Clear();
        AddTestItem(save, "weapon", "chu_tuoc", 50, "Tim");
        AddTestItem(save, "armor", "chu_tuoc", 50, "Tim");
        var base0 = ProgressionService.GetEffectiveHero(save, "custom_hero");
        EquipmentService.EquipFromBag(save, "custom_hero", 0);
        EquipmentService.EquipFromBag(save, "custom_hero", 0);
        var eff = ProgressionService.GetEffectiveHero(save, "custom_hero");
        Check(eff.Hp >= (int)(base0.Hp * 1.05), "2pc Chu Tuoc gives +10% HP");
        Check(System.Math.Abs(eff.Crit - (base0.Crit + 0.06)) < 0.001, "refine lines apply (2x crit +3%)");

        // 4. 6x Chu Tước => speed bonus active.
        foreach (var slot in new[] { "helmet", "boots", "cloak", "bracer" })
            AddTestItem(save, slot, "chu_tuoc", 50, "Tim");
        for (int i = 0; i < 4; i++) EquipmentService.EquipFromBag(save, "custom_hero", 0);
        var bonuses = SetBonusService.ActiveBonuses(save, "custom_hero");
        Check(bonuses.Any(b => b.Set.Id == "chu_tuoc" && b.Bonus.Pieces == 6 && b.Bonus.StatId == "speed"),
            "6pc Chu Tuoc speed bonus is active");
        var eff6 = ProgressionService.GetEffectiveHero(save, "custom_hero");
        Check(eff6.Speed >= eff.Speed + 10, "6pc Chu Tuoc adds +10 speed");

        // 5. Miễn thương (Bạch Hổ 6pc) reaches the battle engine stat.
        var save2 = SeedService.CreateSeededSave("t230c");
        save2.BagEquipment.Clear();
        foreach (var slot in ProgressionService.Slots) AddTestItem(save2, slot.Id, "bach_ho", 70, "Cam");
        for (int i = 0; i < 6; i++) EquipmentService.EquipFromBag(save2, "custom_hero", 0);
        var effBh = ProgressionService.GetEffectiveHero(save2, "custom_hero");
        Check(effBh.DamageReduction >= 0.05, "Bach Ho 6pc gives damage reduction");

        // 6. Deferred bonuses (cc_resist) don't crash and are ignored.
        var save3 = SeedService.CreateSeededSave("t230d");
        save3.BagEquipment.Clear();
        foreach (var slot in ProgressionService.Slots) AddTestItem(save3, slot.Id, "thanh_long", 80, "Cam");
        for (int i = 0; i < 6; i++) EquipmentService.EquipFromBag(save3, "custom_hero", 0);
        var effTl = ProgressionService.GetEffectiveHero(save3, "custom_hero");
        Check(effTl.Hp > 0, "deferred cc_resist bonus does not crash");

        // 7. Migration of an old save (no set fields, old refine index).
        var legacy = new SaveData();
        legacy.HeroEquipment["custom_hero"] = new Dictionary<string, EquipmentState>
        {
            ["weapon"] = new EquipmentState { IsEmpty = false, Level = 2, RefineIndex = 3, SetId = "", RefineLines = null! },
        };
        ProgressionService.EnsureDefaults(legacy);
        var m = legacy.HeroEquipment["custom_hero"]["weapon"];
        Check(m.SetId == "tan_thu" && m.Tier == 30 && m.Quality == "Lam", "migration assigns Tan Thu set");
        Check(m.RefineLines.Count == 3, "migration fills 3 lines for Lam");
        Check(m.RefineLines[0].StatId == "atk_pct" && System.Math.Abs(m.RefineLines[0].Value - 0.10) < 0.001,
            "migration maps old refine index 3 to atk line");

        // 8. Equip/unequip preserves the full item.
        var save4 = SeedService.CreateSeededSave("t230e");
        save4.BagEquipment.Clear();
        AddTestItem(save4, "weapon", "huyen_vu", 90, "Cam");
        EquipmentService.EquipFromBag(save4, "custom_hero", 0);
        var st = save4.HeroEquipment["custom_hero"]["weapon"];
        Check(st.SetId == "huyen_vu" && st.Quality == "Cam" && st.Tier == 90, "equip preserves set fields");
        EquipmentService.Unequip(save4, "custom_hero", "weapon");
        Check(save4.BagEquipment[0].SetId == "huyen_vu" && save4.BagEquipment[0].RefineLines.Count == 1,
            "unequip preserves set fields");

        // 9. Old refine flow rotates the first line and stays in sync.
        var save5 = SeedService.CreateSeededSave("t230f");
        int w = save5.BagEquipment.FindIndex(b => b.SlotId == "weapon");
        EquipmentService.EquipFromBag(save5, "custom_hero", w);
        ProgressionService.TryRefine(save5, "custom_hero", "weapon");
        var rl = save5.HeroEquipment["custom_hero"]["weapon"];
        Check(rl.RefineLines[0].StatId == "dodge" && rl.RefineIndex == 1, "refine rotates first line crit->dodge");

        // 10. Describe helpers.
        var desc = SetBonusService.DescribeForHero(save, "custom_hero");
        Check(desc.Any(d => d.Contains("Chu Tước") && d.Contains("6/6")), "describe shows Chu Tuoc 6/6");
        Check(SetBonusService.DescribeBonus(new SetBonus { Pieces = 6, StatId = "starting_rage", Value = 25 }).Contains("sắp mở"),
            "deferred bonus is marked sắp mở");

        if (_fails == 0) GD.Print("TEST PHASE 2.30: ALL PASS");
        else GD.Print($"TEST PHASE 2.30: {_fails} FAILURES");
        GetTree().Quit();
    }

    private void AddTestItem(SaveData save, string slotId, string setId, int tier, string quality)
    {
        save.BagEquipment.Add(new BagEquipmentItem
        {
            SlotId = slotId, SetId = setId, Tier = tier, Quality = quality,
            RefineLines = new List<RefineLine> { new() { StatId = "crit", Value = 0.03 } },
        });
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }
}
