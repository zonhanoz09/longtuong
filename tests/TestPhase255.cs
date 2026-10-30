using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.55: Càn quét Phó Bản — nút quét ải cao nhất đã qua, tốn 50 Vàng,
/// thưởng đầy đủ như đánh tay, không giới hạn/ngày.
/// </summary>
public partial class TestPhase255 : Node
{
    private int _fails;

    public override async void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("tester255");
        var save = gm.Save;
        // Mở phó bản đầu (cần qua chương 2 + Lv30).
        save.LastStage = 14;
        save.Heroes["custom_hero"].Level = 30;
        var d = DungeonDatabase.Get("co_mo");
        var diff = DungeonDifficulty.Normal;
        var team = new List<string> { "custom_hero" };

        Check(DungeonService.SweepGoldCost == 50, "sweep costs 50 gold");

        // Chưa qua ải nào -> không quét được.
        Check(DungeonService.HighestClearedIndex(save, d, diff) == -1, "no cleared stage at first");
        Check(DungeonService.TrySweep(save, d, diff, team).Result
            == DungeonService.SweepResult.NotCleared, "cannot sweep uncleared");

        // Giả lập đã qua ải 1 (thắng tay 1 lần).
        var r0 = DungeonService.Apply(save, DungeonService.StageKey(d, diff, 0), team);
        Check(r0 != null, "manual win applies");
        Check(DungeonService.HighestClearedIndex(save, d, diff) == 0, "highest cleared = stage 1");

        // Càn quét: trừ Vàng, có thưởng.
        int gold0 = save.Gold;
        int frag0 = ForgeService.FragmentCount(save, d.FragmentSetId);
        var (res, reward) = DungeonService.TrySweep(save, d, diff, team);
        Check(res == DungeonService.SweepResult.Success, "sweep succeeds");
        Check(save.Gold == gold0 - 50, "50 gold deducted");
        Check(reward != null && ForgeService.FragmentCount(save, d.FragmentSetId) > frag0,
            "sweep grants fragments");
        Check(DungeonService.NextStageIndex(save, d, diff) == 1, "progress unchanged by sweeping cleared stage");

        // Không đủ Vàng.
        save.Gold = 10;
        Check(DungeonService.TrySweep(save, d, diff, team).Result
            == DungeonService.SweepResult.NoGold, "no gold fails");
        save.Gold = gold0;
        // Không có đội.
        Check(DungeonService.TrySweep(save, d, diff, new List<string>()).Result
            == DungeonService.SweepResult.NoTeam, "no team fails");

        // Phá đảo -> quét được ải boss.
        for (int i = 1; i < DungeonDatabase.StageCount; i++)
            DungeonService.Apply(save, DungeonService.StageKey(d, diff, i), team);
        Check(DungeonService.HighestClearedIndex(save, d, diff) == DungeonDatabase.StageCount - 1,
            "cleared dungeon sweeps boss stage");

        // UI: nút Càn quét tồn tại, bấm được.
        var ui = ResourceLoader.Load<PackedScene>("res://features/dungeon/scenes/DungeonScene.tscn")
            .Instantiate<DungeonScene>();
        AddChild(ui);
        await ToSignal(GetTree(), "process_frame");
        var sweepBtn = ui.GetNode<Button>("SweepButton");
        Check(sweepBtn != null && sweepBtn.Visible, "sweep button exists");
        Check(sweepBtn.Text.Contains("50"), "sweep button shows gold cost");
        int gold1 = save.Gold;
        sweepBtn.EmitSignal(BaseButton.SignalName.Pressed);
        await ToSignal(GetTree(), "process_frame");
        Check(save.Gold == gold1 - 50, "UI sweep deducts gold");

        Done("PHASE 2.55");
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
