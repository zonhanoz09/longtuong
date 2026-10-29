using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.26 tests: Kim Nguyên Bảo removed from the game.
/// - Only Vàng + Bạc remain: the Hub currency label, the tavern
///   currency line and the bag header all show Bạc and never KNB
///   or "Kim Nguyên Bảo".
/// - Seeded saves carry no gems concept: gold 100.000 + silver
///   1.000.000, and battle wins grant gold/EXP only (covered by
///   TestPhase222; here we guard the displays).
/// </summary>
public partial class TestPhase226 : Node
{
    private int _failed;

    private void Check(bool cond, string name)
    {
        if (!cond) { _failed++; GD.PrintErr($"[FAIL] {name}"); }
        else GD.Print($"[PASS] {name}");
    }

    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("tester226");

        var hubScene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        Check(hubScene != null, "hub scene loads");
        if (hubScene != null)
        {
            var hub = hubScene.Instantiate<HubScene>();
            AddChild(hub);
            var cur = hub.GetNode<Label>("TopBar/AvatarPanel/AvatarBox/AvatarCurrencyLabel").Text;
            Check(cur.Contains("Bạc") && !cur.Contains("KNB") && !cur.Contains("Kim Nguyên Bảo"), "hub currency shows silver, never KNB");
            hub.QueueFree();
        }

        var tavernScene = ResourceLoader.Load<PackedScene>("res://features/recruit/scenes/RecruitScene.tscn");
        Check(tavernScene != null, "tavern scene loads");
        if (tavernScene != null)
        {
            var tav = tavernScene.Instantiate<RecruitScene>();
            AddChild(tav);
            var cur = tav.GetNode<Label>("CurrencyBox/SilverLabel").Text;
            Check(cur.Contains("Bạc") && !cur.Contains("KNB") && !cur.Contains("Kim Nguyên Bảo"), "tavern currency shows silver, never KNB");
            tav.QueueFree();
        }

        var bagScene = ResourceLoader.Load<PackedScene>("res://features/bag/scenes/BagScene.tscn");
        Check(bagScene != null, "bag scene loads");
        if (bagScene != null)
        {
            var bag = bagScene.Instantiate<Control>();
            AddChild(bag);
            var header = bag.GetNode<VBoxContainer>("ItemBox").GetChildren().OfType<Label>().FirstOrDefault()?.Text ?? "";
            Check(header.Contains("Bạc") && !header.Contains("KNB") && !header.Contains("Kim Nguyên Bảo"), "bag header shows silver, never KNB");
            bag.QueueFree();
        }

        GD.Print(_failed == 0 ? "TEST PHASE2.26: ALL PASS" : $"TEST PHASE2.26: {_failed} FAILURES");
        GetTree().Quit(_failed == 0 ? 0 : 1);
    }
}
