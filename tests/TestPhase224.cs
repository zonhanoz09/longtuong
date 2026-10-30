using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.24 tests: tavern two-view layout from Huynh's sketch, and
/// souls pooled per tier.
/// - Initial view is Chiêu mộ: title, tier balance line, drink buttons
///   visible; drink view (3 generals, RPS, back) hidden; grid has 7
///   cards grouped by tier with the tier recruit cost.
/// - Owned general's card shows "đã chiêu mộ" and is disabled.
/// - Drink flow via real button signals: Normal => Đối tửu view with
///   RPS enabled and "Hồn thắng" cards; one RPS round updates the
///   status; Back => Chiêu mộ view again with souls kept.
/// - Tier pool sharing: 100 Lam souls recruit either Lam general,
///   and spending them drains the pool for the other.
/// - Recruit click: pressing an affordable card recruits and deducts
///   exactly the tier cost.
/// </summary>
public partial class TestPhase224 : Node
{
    private int _passed;
    private int _failed;

    private void Check(bool cond, string name)
    {
        if (cond) { _passed++; GD.Print($"[PASS] {name}"); }
        else { _failed++; GD.PrintErr($"[FAIL] {name}"); }
    }

    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var tavernScene = ResourceLoader.Load<PackedScene>("res://features/recruit/scenes/RecruitScene.tscn");
        Check(tavernScene != null, "tavern scene loads");
        if (tavernScene == null)
        {
            GD.Print("TEST PHASE2.24: 1 FAILURES");
            GetTree().Quit(1);
            return;
        }

        // --- Initial view is Chiêu mộ with a 7-card tier grid ---
        gm.CreateSeededSave("tester224");
        var tav = tavernScene.Instantiate<RecruitScene>();
        AddChild(tav);
        Check(tav.GetNode<Label>("TitleLabel").Text == "Chiêu mộ", "initial title is Chiêu mộ (2.25 title case)");
        var balBox = tav.GetNodeOrNull<HBoxContainer>("BalanceBox");
        Check(balBox != null && balBox.GetChildren().OfType<TextureRect>().Count() == 3
            && tav.GetNode<Label>("BalanceBox/LamSoulsLabel").Text == "0"
            && tav.GetNode<Label>("BalanceBox/TimSoulsLabel").Text == "0"
            && tav.GetNode<Label>("BalanceBox/CamSoulsLabel").Text == "0",
            "balance line shows 3 tier dots + pool numbers (2.25)");
        Check(tav.GetNode<Button>("NormalButton").Visible && tav.GetNode<Button>("HighButton").Visible && tav.GetNode<Button>("AllWinButton").Visible, "drink buttons visible in recruit view");
        Check(!tav.GetNode<Control>("DrinkBox").Visible && !tav.GetNode<Button>("RockButton").Visible && !tav.GetNode<Button>("ToRecruitButton").Visible,
            "drink view hidden initially");
        // 2.54: lọc card tướng (có NameLabel), bỏ nút Mua của shop thẻ EXP.
        var cards = tav.GetNode<Control>("RecruitBox/RecruitScroll/RecruitContent").GetChildren().OfType<Button>()
            .Where(b => b.GetNodeOrNull<Label>("NameLabel") != null).ToList();
        Check(cards.Count == 15, "recruit grid has 15 cards (2.43: 4 Lam, 5 Tim, 6 Cam; new ones greyed until their chapter)");
        string CardName(Button b) => b.GetChildren().OfType<Label>().FirstOrDefault(l => l.Name == "NameLabel")?.Text ?? "";
        string CardPrice(Button b) => b.GetChildren().OfType<Label>().FirstOrDefault(l => l.Name == "PriceLabel")?.Text ?? "";
        Check(cards.Any(b => CardName(b).Contains("Lưu Bị") && CardPrice(b) == "100")
            && cards.Any(b => CardName(b).Contains("Quan Vũ") && CardPrice(b) == "500")
            && cards.Any(b => CardName(b).Contains("Lữ Bố") && CardPrice(b) == "1.500"),
            "cards show tier recruit costs 100/500/1500 as dot + number");
        Check(cards.All(b => b.GetChildren().OfType<Label>().All(l => !l.Text.Contains("(Lam)") && !l.Text.Contains("(Tím)") && !l.Text.Contains("(Cam)"))),
            "no tier-name text on cards (dots only, 2.25)");
        tav.QueueFree();

        // --- Owned general's card shows "đã chiêu mộ" and is disabled ---
        gm.CreateSeededSave("tester224");
        gm.Save.OwnedHeroIds.Add("luu_bi");
        var tavOwned = tavernScene.Instantiate<RecruitScene>();
        AddChild(tavOwned);
        var ownedCards = tavOwned.GetNode<Control>("RecruitBox/RecruitScroll/RecruitContent").GetChildren().OfType<Button>().ToList();
        var luuBtn = ownedCards.FirstOrDefault(b => CardName(b).Contains("Lưu Bị"));
        var luuOwned = luuBtn?.GetChildren().OfType<Label>().FirstOrDefault(l => l.Name == "OwnedLabel");
        Check(luuBtn != null && luuOwned != null && luuOwned.Visible && luuOwned.Text.Contains("đã chiêu mộ") && luuBtn.Disabled, "owned card shows 'đã chiêu mộ' and is disabled");
        tavOwned.QueueFree();

        // --- Drink flow via real button signals ---
        gm.CreateSeededSave("tester224");
        var tavFlow = tavernScene.Instantiate<RecruitScene>();
        AddChild(tavFlow);
        tavFlow.GetNode<Button>("NormalButton").EmitSignal(Button.SignalName.Pressed);
        Check(tavFlow.GetNode<Label>("TitleLabel").Text == "Đối tửu", "after drink, title is Đối tửu");
        Check(tavFlow.GetNode<Control>("DrinkBox").Visible && tavFlow.GetNode<Button>("ToRecruitButton").Visible, "drink view visible after drink");
        Check(!tavFlow.GetNode<Button>("RockButton").Disabled && !tavFlow.GetNode<Button>("ScissorsButton").Disabled && !tavFlow.GetNode<Button>("PaperButton").Disabled,
            "RPS buttons enabled during drink");
        var drinkLabels = tavFlow.GetNode<Control>("DrinkBox").GetChildren().OfType<Label>().ToList();
        Check(drinkLabels.Count == 3 && drinkLabels.All(l => l.Text.Contains("Hồn thắng")), "drink view shows 3 generals with souls-won lines");
        int soulsBeforeBack = TavernService.TierSoulsOf(gm.Save, TavernTier.Lam)
            + TavernService.TierSoulsOf(gm.Save, TavernTier.Tim) + TavernService.TierSoulsOf(gm.Save, TavernTier.Cam);
        tavFlow.GetNode<Button>("RockButton").EmitSignal(Button.SignalName.Pressed);
        Check(tavFlow.GetNode<Label>("DrinkStatusLabel").Text.Contains("Bạn ra"), "RPS round updates the drink status");
        tavFlow.GetNode<Button>("ToRecruitButton").EmitSignal(Button.SignalName.Pressed);
        Check(tavFlow.GetNode<Label>("TitleLabel").Text == "Chiêu mộ" && !tavFlow.GetNode<Control>("DrinkBox").Visible,
            "back returns to recruit view");
        int soulsAfterBack = TavernService.TierSoulsOf(gm.Save, TavernTier.Lam)
            + TavernService.TierSoulsOf(gm.Save, TavernTier.Tim) + TavernService.TierSoulsOf(gm.Save, TavernTier.Cam);
        Check(soulsAfterBack >= soulsBeforeBack, "souls won are kept after going back (never lost)");
        tavFlow.QueueFree();

        // --- Tier pool sharing: 100 Lam souls serve either Lam general ---
        var sShare = SeedService.CreateSeededSave("tester224");
        sShare.TierSouls["Lam"] = 100;
        Check(TavernService.CanRecruit(sShare, "luu_bi") && TavernService.CanRecruit(sShare, "truong_phi"),
            "100 Lam souls can recruit either Lam general (shared pool)");
        Check(TavernService.Recruit(sShare, "truong_phi") && sShare.TierSouls["Lam"] == 0
            && !TavernService.CanRecruit(sShare, "luu_bi"),
            "recruiting one Lam general drains the shared pool for the other");

        // --- Recruit click: affordable card recruits and deducts exactly ---
        gm.CreateSeededSave("tester224");
        gm.Save.TierSouls["Lam"] = 100;
        var tavClick = tavernScene.Instantiate<RecruitScene>();
        AddChild(tavClick);
        var clickCards = tavClick.GetNode<Control>("RecruitBox/RecruitScroll/RecruitContent").GetChildren().OfType<Button>().ToList();
        var clickBtn = clickCards.FirstOrDefault(b => CardName(b).Contains("Lưu Bị"));
        Check(clickBtn != null && !clickBtn.Disabled, "affordable card is enabled");
        clickBtn!.EmitSignal(Button.SignalName.Pressed);
        Check(gm.Save.OwnedHeroIds.Contains("luu_bi") && gm.Save.TierSouls["Lam"] == 0,
            "clicking the card recruits and deducts exactly 100 Lam souls");
        tavClick.QueueFree();

        GD.Print(_failed == 0 ? "TEST PHASE2.24: ALL PASS" : $"TEST PHASE2.24: {_failed} FAILURES");
        GetTree().Quit(_failed == 0 ? 0 : 1);
    }
}
