using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.25 tests: dual currency (Vàng premium / Bạc common), all-win
/// drinks, and the sketch-exact recruit layout.
/// - Drink prices are Bạc: Normal 150.000, High 300.000; drinks never
///   touch Gold. All-win costs a flat 200 Vàng upfront.
/// - StartAllWin: fails below 200 Gold with no charge; at 200 Gold it
///   drains Gold to 0 and grants 30-90 souls total, matching a replica
///   Random exactly (HIGH-grade weights + Cam gate, 1-3 souls per win).
/// - Seeded saves start with 1.000.000 Bạc; old saves without the flag
///   get the grant once, never twice.
/// - Layout: centered title, 6-column pitch 152, cards with dot
///   TextureRect + price (no tier-name text), 3 buttons bottom-right
///   with prices, balance line with 3 dots + pool numbers.
/// - All-win click flow via real button signal: status summary
///   "+X hồn Lam, +Y hồn Tím, +Z hồn Cam" and Gold -200.
/// </summary>
public partial class TestPhase225 : Node
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
        Check(TavernData.DrinkPrice(TavernGrade.Normal) == 150000 && TavernData.DrinkPrice(TavernGrade.High) == 300000,
            "drink prices are 150000/300000 silver");
        Check(TavernData.AllWinPrice == 200 && TavernData.AllWinDrinks == 10, "all-win is 10 drinks for a flat 200 gold");

        // --- Drinks cost silver, never gold ---
        var sD = SeedService.CreateSeededSave("tester225");
        sD.Silver = 100000;
        Check(!TavernService.StartDrink(sD, TavernGrade.Normal, new Random(1), out _) && sD.Silver == 100000,
            "normal drink fails below 150000 silver");
        sD.Silver = 150000;
        Check(TavernService.StartDrink(sD, TavernGrade.Normal, new Random(2), out _) && sD.Silver == 0 && sD.Gold == SeedService.SeededGold,
            "normal drink charges exactly 150000 silver, gold untouched");

        // --- All-win: insufficient gold fails clean; success matches replica ---
        var sPoor = SeedService.CreateSeededSave("tester225");
        sPoor.Gold = 199;
        int poolsBefore = sPoor.TierSouls.Values.Sum();
        Check(!TavernService.StartAllWin(sPoor, new Random(42), out _) && sPoor.Gold == 199 && sPoor.TierSouls.Values.Sum() == poolsBefore,
            "all-win fails at 199 gold with no charge");
        var sWin = SeedService.CreateSeededSave("tester225"); // custom level 1 < 70
        sWin.Gold = 200;
        var rng = new Random(42);
        var rep = new Random(42);
        var expected = new Dictionary<TavernTier, int> { [TavernTier.Lam] = 0, [TavernTier.Tim] = 0, [TavernTier.Cam] = 0 };
        for (int d = 0; d < 10; d++)
            for (int i = 0; i < 3; i++)
            {
                int roll = rep.Next(90); // High 60/30, Cam 0 below 70
                var tier = roll < 60 ? TavernTier.Lam : TavernTier.Tim;
                var pool = TavernData.HeroesOfTier(sWin, tier);
                _ = pool[rep.Next(pool.Count)];
                expected[tier] += rep.Next(1, 4);
            }
        Check(TavernService.StartAllWin(sWin, rng, out var totals) && sWin.Gold == 0,
            "all-win charges exactly 200 gold upfront");
        Check(totals[TavernTier.Lam] == expected[TavernTier.Lam] && totals[TavernTier.Tim] == expected[TavernTier.Tim]
            && totals[TavernTier.Cam] == expected[TavernTier.Cam] && totals[TavernTier.Cam] == 0,
            "all-win totals match replica exactly (High weights, Cam gated below 70)");
        int gained = totals.Values.Sum();
        Check(gained >= 30 && gained <= 90, $"all-win grants 30-90 souls total (got {gained})");
        Check(sWin.TierSouls["Lam"] == expected[TavernTier.Lam] && sWin.TierSouls["Tim"] == expected[TavernTier.Tim],
            "all-win souls land in the tier pools");

        // --- Starter silver: seeded saves + one-time grant for old saves ---
        var sSeed = SeedService.CreateSeededSave("tester225");
        Check(sSeed.Silver == SeedService.SeededSilver && sSeed.SilverSeeded, "seeded save starts with starter silver");
        var sOld = new SaveData();
        ProgressionService.EnsureDefaults(sOld);
        Check(sOld.Silver == SeedService.SeededSilver && sOld.SilverSeeded, "old save gets the starter silver grant once");
        sOld.Silver = 500;
        ProgressionService.EnsureDefaults(sOld);
        Check(sOld.Silver == 500, "grant is not repeated on later loads");

        // --- Layout: centered title, 6-column pitch, dots, 3 buttons ---
        var gm = GetNode<GameManager>("/root/GameManager");
        var tavernScene = ResourceLoader.Load<PackedScene>("res://features/recruit/scenes/RecruitScene.tscn");
        Check(tavernScene != null, "tavern scene loads");
        if (tavernScene != null)
        {
            gm.CreateSeededSave("tester225");
            var tav = tavernScene.Instantiate<RecruitScene>();
            AddChild(tav);
            Check(tav.GetNode<Label>("TitleLabel").HorizontalAlignment == HorizontalAlignment.Center, "title is centered");
            // 2.54: chỉ card tướng (có NameLabel), bỏ nút Mua của shop thẻ EXP.
            var cards = tav.GetNode<Control>("RecruitBox/RecruitScroll/RecruitContent").GetChildren().OfType<Button>()
                .Where(b => b.GetNodeOrNull<Label>("NameLabel") != null).ToList();
            var lamCards = cards.Where(b => b.GetChildren().OfType<Label>().First(l => l.Name == "NameLabel").Text is "Lưu Bị" or "Trương Phi").ToList();
            Check(lamCards.Count == 2 && lamCards[1].Position.X - lamCards[0].Position.X == 152 && lamCards[0].Position.Y == lamCards[1].Position.Y,
                "Lam row: 2 cards, pitch exactly 152, same row");
            Check(cards.All(b => b.GetChildren().OfType<TextureRect>().Any()), "every card has a tier dot");
            Check(tav.GetNode<Button>("AllWinButton").Visible && tav.GetNode<Button>("AllWinButton").Text.Contains("200 Vàng"),
                "all-win button visible with 200 gold price");
            Check(tav.GetNode<Button>("NormalButton").Text.Contains("150.000 Bạc") && tav.GetNode<Button>("HighButton").Text.Contains("300.000 Bạc"),
                "drink buttons show silver prices");
            var btnRow = new[] { tav.GetNode<Button>("AllWinButton"), tav.GetNode<Button>("NormalButton"), tav.GetNode<Button>("HighButton") };
            Check(btnRow.All(b => b.Position.Y == btnRow[0].Position.Y) && btnRow[2].Position.X + btnRow[2].Size.X <= 980,
                "3 buttons share one row, right-aligned within the panel");
            Check(tav.GetNode<Label>("CurrencyBox/SilverLabel").Text.Contains("Bạc"), "currency box shows silver");

            // --- All-win click flow via real button signal ---
            int goldBefore = gm.Save.Gold;
            int poolsBeforeClick = gm.Save.TierSouls.Values.Sum();
            tav.GetNode<Button>("AllWinButton").EmitSignal(Button.SignalName.Pressed);
            Check(tav.GetNode<Label>("RecruitStatusLabel").Text.Contains("Tất thắng x10") && tav.GetNode<Label>("RecruitStatusLabel").Text.Contains("hồn Lam"),
                "all-win click shows the summary in the status line");
            Check(gm.Save.Gold == goldBefore - 200 && gm.Save.TierSouls.Values.Sum() >= poolsBeforeClick + 30,
                "all-win click charges 200 gold and grants at least 30 souls");
            tav.QueueFree();
        }

        GD.Print(_failed == 0 ? "TEST PHASE2.25: ALL PASS" : $"TEST PHASE2.25: {_failed} FAILURES");
        GetTree().Quit(_failed == 0 ? 0 : 1);
    }
}
