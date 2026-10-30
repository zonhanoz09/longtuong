using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.53: Thẻ kinh nghiệm — mua ở view Chiêu mộ (Vàng), ăn ở popup
/// Võ tướng (click đúp ô thẻ trong lưới, cộng EXP trực tiếp).
/// </summary>
public partial class TestPhase253 : Node
{
    private int _fails;

    public override async void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("tester238");
        var save = gm.Save;

        // Card defs (Huynh đã chốt số).
        Check(ExpCardService.Cards.Count == 3, "3 card types");
        var small = ExpCardService.Get("exp_small")!;
        Check(small.Exp == 1000 && small.PriceGold == 200, "Tiểu +1000 / 200 Vàng");
        var mid = ExpCardService.Get("exp_mid")!;
        Check(mid.Exp == 5000 && mid.PriceGold == 800, "Trung +5000 / 800 Vàng");
        var big = ExpCardService.Get("exp_big")!;
        Check(big.Exp == 20000 && big.PriceGold == 3000, "Đại +20000 / 3000 Vàng");

        // Mua: trừ Vàng, +1 thẻ.
        int gold0 = save.Gold;
        Check(ExpCardService.TryBuyCard(save, "exp_mid"), "buy Trung ok");
        Check(save.Gold == gold0 - 800, "gold deducted");
        Check(ExpCardService.Count(save, "exp_mid") == 1, "count = 1");
        // Không đủ Vàng.
        save.Gold = 100;
        Check(!ExpCardService.TryBuyCard(save, "exp_big"), "cannot buy without gold");
        Check(ExpCardService.Count(save, "exp_big") == 0, "count stays 0");
        save.Gold = gold0;

        // Ăn: cộng EXP, lên cấp.
        save.ExpCards["exp_small"] = 2;
        var p = ProgressionService.ProgressOf(save, "custom_hero");
        int lv0 = p.Level;
        var (res, exp, oldLv, newLv) = ExpCardService.TryFeedCard(save, "custom_hero", "exp_small");
        Check(res == ExpCardService.FeedResult.Success, "feed ok");
        Check(exp == 1000 && ExpCardService.Count(save, "exp_small") == 1, "card consumed");
        Check(newLv >= oldLv && newLv >= lv0, "level not decreased");
        // Ăn khi hết thẻ.
        save.ExpCards["exp_big"] = 0;
        Check(ExpCardService.TryFeedCard(save, "custom_hero", "exp_big").Result
            == ExpCardService.FeedResult.NoCard, "no card fails");
        // Max cấp.
        p.Level = ProgressionService.MaxLevel;
        save.ExpCards["exp_small"] = 1;
        var r2 = ExpCardService.TryFeedCard(save, "custom_hero", "exp_small");
        Check(r2.Result == ExpCardService.FeedResult.MaxLevel, "max level fails");
        Check(ExpCardService.Count(save, "exp_small") == 1, "card not consumed at max");

        // UI: shop trong view Chiêu mộ.
        var recruitScene = ResourceLoader.Load<PackedScene>("res://features/recruit/scenes/RecruitScene.tscn");
        var recruit = recruitScene.Instantiate<Control>();
        AddChild(recruit);
        await ToSignal(GetTree(), "process_frame");
        var box = recruit.GetNode<Control>("RecruitBox");
        var buyBtns = box.FindChildren("*", "Button", true, false)
            .OfType<Button>().Where(b => b.Text.StartsWith("Mua (")).ToList();
        Check(buyBtns.Count == 3, "3 buy buttons in Chiêu mộ view");
        // Mua qua nút UI.
        save.Gold = 100000;
        int cnt0 = ExpCardService.Count(save, "exp_small");
        buyBtns[0].EmitSignal(Button.SignalName.Pressed);
        await ToSignal(GetTree(), "process_frame");
        Check(ExpCardService.Count(save, "exp_small") == cnt0 + 1, "UI buy adds card");

        // UI: ô thẻ trong lưới Võ tướng.
        var heroScene = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroListScene.tscn");
        var hero = heroScene.Instantiate<Control>();
        AddChild(hero);
        await ToSignal(GetTree(), "process_frame");
        await ToSignal(GetTree(), "process_frame");
        var grid = hero.GetNode<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        var cardBtns = grid.GetChildren().OfType<Button>()
            .Where(b => b.GetChildren().OfType<Label>()
                .Any(l => l.Name == "IconSymbolLabel" && l.Text == "EXP")).ToList();
        Check(cardBtns.Count == 3, "3 EXP card cells in grid");
        // Click đúp ô thẻ = ăn cho tướng hiện tại.
        p.Level = 1; p.Exp = 0;
        save.ExpCards["exp_small"] = 1;
        int lvBefore = p.Level;
        var click = new InputEventMouseButton
        {
            Pressed = true, DoubleClick = true, ButtonIndex = MouseButton.Left,
        };
        cardBtns[0].EmitSignal(Control.SignalName.GuiInput, click);
        await ToSignal(GetTree(), "process_frame");
        Check(ExpCardService.Count(save, "exp_small") == 0, "double-click feeds card");
        Check(ProgressionService.ProgressOf(save, "custom_hero").Level >= lvBefore, "hero gained levels");

        Done("PHASE 2.53");
    }

    private void Check(bool cond, string name)
    {
        if (!cond) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private void Done(string name)
    {
        GD.Print(_fails == 0 ? $"TEST {name}: ALL PASS" : $"TEST {name}: {_fails} FAILURES");
        GetTree().Quit();
    }
}
