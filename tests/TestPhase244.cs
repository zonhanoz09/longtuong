using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.44 — set fragments ("mảnh chế tạo"): every Đúc Lại into set X
/// also costs M fragments of X (blueprint still bought with Vàng); Tân Thủ
/// needs none; "material" reward drops add to Materials; the Cường hóa tab
/// shows the fragment row and still fits the 560px panel.
/// </summary>
public partial class TestPhase244 : Node
{
    private int _fails;

    public override async void _Ready()
    {
        CheckCosts();
        CheckRecast();
        CheckCraftAndRewards();
        await CheckUi();

        if (_fails == 0) GD.Print("TEST PHASE 2.44: ALL PASS");
        else GD.Print($"TEST PHASE 2.44: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private void CheckCosts()
    {
        Check(ForgeService.RecastFragmentCost("chu_tuoc") == 10 && ForgeService.RecastFragmentCost("bach_ho") == 15
            && ForgeService.RecastFragmentCost("thanh_long") == 20 && ForgeService.RecastFragmentCost("huyen_vu") == 25
            && ForgeService.RecastFragmentCost("ky_lan") == 30, "fragments per recast 10/15/20/25/30");
        Check(ForgeService.RecastFragmentCost("tan_thu") == 0, "Tân Thủ needs no fragments");
        Check(ForgeService.FragmentId("chu_tuoc") == "frag_chu_tuoc" && ForgeService.FragmentName("chu_tuoc") == "Mảnh Chu Tước", "fragment id + name");
        var s = SeedService.CreateSeededSave("t244a");
        Check(ForgeService.FragmentCount(s, "bach_ho") == 0, "new account has no fragments");
        ForgeService.AddFragments(s, "bach_ho", 3);
        ForgeService.AddFragments(s, "bach_ho", 4);
        Check(ForgeService.FragmentCount(s, "bach_ho") == 7 && s.Materials["frag_bach_ho"] == 7, "AddFragments stacks in Materials");
    }

    private void CheckRecast()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("t244b");
        int w = gm.Save.BagEquipment.FindIndex(b => b.SlotId == "weapon");
        EquipmentService.EquipFromBag(gm.Save, "custom_hero", w);
        gm.Save.Heroes["custom_hero"].Level = 50;
        gm.Save.Blueprints["chu_tuoc"] = true;
        gm.Save.Materials[ForgeService.OreId] = 100;
        gm.Save.Silver = 200000;
        var rng = new System.Random(3);

        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", rng) == ForgeService.RecastResult.NoFragments, "no fragments: refused");
        ForgeService.AddFragments(gm.Save, "chu_tuoc", 9);
        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", rng) == ForgeService.RecastResult.NoFragments, "9 of 10 fragments: refused");
        Check(ForgeService.OreCount(gm.Save) == 100 && gm.Save.Silver == 200000 && ForgeService.FragmentCount(gm.Save, "chu_tuoc") == 9
            && gm.Save.HeroEquipment["custom_hero"]["weapon"].SetId == "tan_thu", "a refused recast charges nothing");
        // Other sets' fragments do not count.
        ForgeService.AddFragments(gm.Save, "bach_ho", 99);
        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", rng) == ForgeService.RecastResult.NoFragments, "fragments of another set do not count");
        ForgeService.AddFragments(gm.Save, "chu_tuoc", 5);
        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", rng) == ForgeService.RecastResult.Success, "14 fragments: recast succeeds");
        Check(ForgeService.FragmentCount(gm.Save, "chu_tuoc") == 4, "recast consumed exactly 10 fragments (14 -> 4)");
        Check(ForgeService.FragmentCount(gm.Save, "bach_ho") == 99, "other fragments untouched");
        Check(ForgeService.OreCount(gm.Save) == 75 && gm.Save.Silver == 150000, "ore + silver costs unchanged (25 ore, 50000 Bạc)");
        Check(gm.Save.HeroEquipment["custom_hero"]["weapon"].SetId == "chu_tuoc", "item became Chu Tước");
        // Order of gates: ore is checked before fragments, fragments before silver.
        gm.Save.Heroes["custom_hero"].Level = 70;
        gm.Save.Blueprints["bach_ho"] = true;
        gm.Save.Materials[ForgeService.OreId] = 0;
        gm.Save.Materials["frag_bach_ho"] = 0;
        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", rng) == ForgeService.RecastResult.NoOre, "ore gate comes before fragments");
        gm.Save.Materials[ForgeService.OreId] = 999;
        gm.Save.Silver = 0;
        Check(ForgeService.TryRecast(gm.Save, "custom_hero", "weapon", rng) == ForgeService.RecastResult.NoFragments, "fragments gate comes before silver");
    }

    private void CheckCraftAndRewards()
    {
        var s = SeedService.CreateSeededSave("t244c");
        s.Materials[ForgeService.OreId] = 50;
        s.Silver = 100000;
        Check(ForgeService.OreCount(s) == 50 && ForgeService.FragmentCount(s, "chu_tuoc") == 0, "setup: ore, no fragments");
        Check(ForgeService.TryCraftIntoSlot(s, "custom_hero", "weapon", new System.Random(1)) == ForgeService.CraftResult.Success,
            "Chế Tạo (Tân Thủ) needs no fragments");

        var reward = new BattleReward { Silver = 0, ExpPerHero = 0 };
        reward.Drops.Add(new RewardDrop { Kind = StageRewardService.MaterialKind, ItemId = ForgeService.FragmentId("thanh_long"), Name = "Mảnh Thanh Long", Count = 3 });
        reward.Drops.Add(new RewardDrop { Kind = StageRewardService.OreKind, ItemId = ForgeService.OreId, Name = ForgeService.OreName, Count = 1 });
        StageRewardService.Apply(s, new[] { "custom_hero" }, reward);
        StageRewardService.Apply(s, new[] { "custom_hero" }, reward);
        Check(ForgeService.FragmentCount(s, "thanh_long") == 6, "material drops add to Materials (3 + 3)");
        Check(ForgeService.OreCount(s) == 50 - 15 + 2, "ore drops still work next to material drops");
        // Old seeded drops are unchanged: same seed => same three rolls.
        var a = StageRewardService.Roll(5, StageRewardService.DropRng(1234));
        Check(a.Drops.All(d => d.Kind != StageRewardService.MaterialKind), "campaign Roll never produces material drops");
    }

    private async System.Threading.Tasks.Task CheckUi()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("t244d");
        int w = gm.Save.BagEquipment.FindIndex(b => b.SlotId == "weapon");
        EquipmentService.EquipFromBag(gm.Save, "custom_hero", w);
        gm.Save.Heroes["custom_hero"].Level = 50;
        gm.Save.Silver = 100000;
        gm.Save.Blueprints["chu_tuoc"] = true;
        gm.Save.Materials[ForgeService.OreId] = 999;
        var screen = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroScreen.tscn").Instantiate<HeroScreen>();
        AddChild(screen);
        await ToSignal(GetTree(), "process_frame");
        await ToSignal(GetTree(), "process_frame");
        var content = screen.GetNode<Control>("RightPanel/TabContent");
        var frag = content.FindChild("FragmentRow", true, false) as Label;
        Check(frag != null && frag.Text == "Mảnh Chu Tước: 10 / 0", $"fragment row shows need / have ({frag?.Text})");
        var recast = content.FindChild("ActionRecast", true, false) as Button;
        Check(recast != null && recast.Disabled, "recast button disabled without fragments");
        var inner = (Control)content.GetChild(0);
        Check(inner.GetRect().Size.Y <= 560, $"recast panel with fragment row fits 560px ({inner.GetRect().Size.Y:0}px)");

        ForgeService.AddFragments(gm.Save, "chu_tuoc", 10);
        screen.QueueFree();
        var screen2 = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroScreen.tscn").Instantiate<HeroScreen>();
        AddChild(screen2);
        await ToSignal(GetTree(), "process_frame");
        await ToSignal(GetTree(), "process_frame");
        var content2 = screen2.GetNode<Control>("RightPanel/TabContent");
        var recast2 = content2.FindChild("ActionRecast", true, false) as Button;
        Check(recast2 != null && !recast2.Disabled, "recast button enabled with 10 fragments");
        var frag2 = content2.FindChild("FragmentRow", true, false) as Label;
        Check(frag2 != null && frag2.Text == "Mảnh Chu Tước: 10 / 10", "fragment row shows 10 / 10");
        var inner2 = (Control)content2.GetChild(0);
        Check(inner2.GetRect().Size.Y <= 560, $"panel fits 560px with enough fragments ({inner2.GetRect().Size.Y:0}px)");
        recast2!.EmitSignal(BaseButton.SignalName.Pressed);
        Check(gm.Save.HeroEquipment["custom_hero"]["weapon"].SetId == "chu_tuoc" && ForgeService.FragmentCount(gm.Save, "chu_tuoc") == 0,
            "UI recast works and spends the 10 fragments");
        screen2.QueueFree();
    }
}
