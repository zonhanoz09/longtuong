using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.46: Phó Bản Trang Bị screen. Left: the 5 dungeons (locked by
/// level), right: Thường / Khó, the 4 stages with their bosses, the set
/// fragment it drops, and Vào trận. The battle team is the formation team
/// (like the campaign). Rewards are applied by GameManager when the
/// battle ends; the result popup shows the fragment in the reward squares.
/// </summary>
[GlobalClass]
public partial class DungeonScene : Control
{
    private string _dungeonId = DungeonDatabase.All[0].Id;
    private DungeonDifficulty _diff = DungeonDifficulty.Normal;
    private Label? _info;
    private Label? _status;
    private Button? _enter;
    private Button? _sweep;
    private HBoxContainer? _diffRow;
    private VBoxContainer? _list;

    public string SelectedDungeonId => _dungeonId;
    public DungeonDifficulty SelectedDifficulty => _diff;

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        FormationService.EnsureFormation(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        _info = GetNode<Label>("InfoLabel");
        _status = GetNode<Label>("StatusLabel");
        _enter = GetNode<Button>("EnterButton");
        _sweep = GetNode<Button>("SweepButton");
        _diffRow = GetNode<HBoxContainer>("DifficultyRow");
        _list = GetNode<VBoxContainer>("DungeonList");
        UiTheme.StyleBody(_info, UiTheme.FontBody);
        UiTheme.StyleBody(_status, UiTheme.FontBody);
        UiTheme.StyleButton(_enter, true);
        UiTheme.StyleButton(_sweep, false);
        // Start on the first dungeon the player can enter.
        var first = DungeonDatabase.All.FirstOrDefault(d => DungeonService.CanEnter(gm.Save, d, DungeonDifficulty.Normal));
        if (first != null) _dungeonId = first.Id;
        Rebuild();
    }

    private void Rebuild()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var save = gm.Save;
        foreach (var child in _list!.GetChildren()) { _list.RemoveChild(child); child.QueueFree(); }
        foreach (var d in DungeonDatabase.All)
        {
            bool open = DungeonService.CanEnter(save, d, DungeonDifficulty.Normal);
            var btn = new Button
            {
                Name = $"Dungeon_{d.Id}",
                Text = $"{d.Name} (Lv{d.MinLevel}){(open ? "" : " (KHÓA)")}",
                TooltipText = open ? "" : DungeonService.LockReason(save, d, DungeonDifficulty.Normal),
                Disabled = !open,
                ToggleMode = true,
                ButtonPressed = d.Id == _dungeonId,
                CustomMinimumSize = new Vector2(0, 56),
            };
            UiTheme.StyleButton(btn, d.Id == _dungeonId);
            var id = d.Id;
            btn.Pressed += () => { _dungeonId = id; Rebuild(); };
            _list.AddChild(btn);
        }

        var dungeon = DungeonDatabase.Get(_dungeonId);
        foreach (var child in _diffRow!.GetChildren()) { _diffRow.RemoveChild(child); child.QueueFree(); }
        foreach (var diff in new[] { DungeonDifficulty.Normal, DungeonDifficulty.Hard })
        {
            bool ok = DungeonService.CanEnter(save, dungeon, diff);
            var btn = new Button
            {
                Name = diff == DungeonDifficulty.Hard ? "DiffHard" : "DiffNormal",
                Text = diff == DungeonDifficulty.Hard ? "Khó (địch x1,5 — mảnh x2)" : "Thường",
                TooltipText = ok ? "" : DungeonService.LockReason(save, dungeon, diff),
                Disabled = !ok,
                ToggleMode = true,
                ButtonPressed = diff == _diff,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            UiTheme.StyleButton(btn, diff == _diff);
            var d2 = diff;
            btn.Pressed += () => { _diff = d2; Rebuild(); };
            _diffRow.AddChild(btn);
        }
        // Khó is shown even if locked, but it cannot stay selected then.
        if (!DungeonService.CanEnter(save, dungeon, _diff)) _diff = DungeonDifficulty.Normal;

        int next = DungeonService.NextStageIndex(save, dungeon, _diff);
        var lines = new List<string>
        {
            $"{dungeon.Name} — {(_diff == DungeonDifficulty.Hard ? "Khó" : "Thường")} (cần Lv{dungeon.MinLevel})",
            $"Rơi: {ForgeService.FragmentName(dungeon.FragmentSetId)} (đang có {ForgeService.FragmentCount(save, dungeon.FragmentSetId)})" +
            $" + {DungeonService.SilverPerWin:N0} Bạc mỗi trận thắng",
            "",
        };
        for (int i = 0; i < dungeon.Stages.Count; i++)
        {
            var st = dungeon.Stages[i];
            string boss = st.BossId != null ? $" — BOSS {HeroDatabase.GetCombatant(st.BossId).Name}" : "";
            string named = string.Join(", ", st.EnemyIds.Distinct().Where(id => id != st.BossId && EnemyDatabase.Find(id) is { } e && !EnemyDatabase.Soldiers.Contains(e))
                .Select(id => HeroDatabase.GetCombatant(id).Name));
            string mark = i < next ? "✓" : i == next ? "▶" : "·";
            lines.Add($"{mark} Ải {i + 1}: {st.Name}{boss}  [{DungeonService.FragmentsFor(i, _diff)} mảnh]" + (named != "" ? $"  ({named})" : ""));
        }
        _info!.Text = string.Join("\n", lines);
        int teamCount = FormationService.FormationToTeam(save.Formation).Count(id => save.OwnedHeroIds.Contains(id));
        _enter!.Text = $"Vào trận (ải {next + 1}/{dungeon.Stages.Count})";
        _enter.Disabled = !DungeonService.CanEnter(save, dungeon, _diff) || teamCount == 0;
        _enter.TooltipText = teamCount == 0 ? "Chưa có đội hình — hãy vào Bày trận trước." : "";
        // Phase 2.55: Càn quét ải cao nhất đã qua (tốn Vàng).
        int cleared = DungeonService.HighestClearedIndex(save, dungeon, _diff);
        _sweep!.Text = cleared >= 0
            ? $"Càn quét ải {cleared + 1} ({DungeonService.SweepGoldCost:N0} Vàng)"
            : $"Càn quét ({DungeonService.SweepGoldCost:N0} Vàng)";
        _sweep.Disabled = cleared < 0 || !DungeonService.CanEnter(save, dungeon, _diff)
            || teamCount == 0 || save.Gold < DungeonService.SweepGoldCost;
        _sweep.TooltipText = cleared < 0 ? "Chưa qua ải nào — hãy đánh tay trước." : "";
    }

    /// Select a dungeon / difficulty (used by tests and by the buttons).
    public void Select(string dungeonId, DungeonDifficulty diff)
    {
        _dungeonId = dungeonId;
        _diff = diff;
        Rebuild();
    }

    /// Sets up the battle in GameManager. False (with a status line) when it cannot start.
    public bool PrepareBattle()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var dungeon = DungeonDatabase.Get(_dungeonId);
        if (!DungeonService.CanEnter(gm.Save, dungeon, _diff))
        {
            if (_status != null) _status.Text = DungeonService.LockReason(gm.Save, dungeon, _diff);
            return false;
        }
        var team = FormationService.FormationToTeam(gm.Save.Formation).Where(id => gm.Save.OwnedHeroIds.Contains(id)).ToList();
        if (team.Count == 0)
        {
            if (_status != null) _status.Text = "Chưa có đội hình — hãy vào Bày trận trước!";
            return false;
        }
        int index = DungeonService.NextStageIndex(gm.Save, dungeon, _diff);
        int seed = (int)(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 1000000) + index * 7919;
        gm.StartModeBattle(BattleMode.Dungeon, DungeonService.StageKey(dungeon, _diff, index),
            DungeonService.StageFor(dungeon, _diff, index), team, seed);
        gm.SaveGame();
        return true;
    }

    private void OnEnterPressed()
    {
        if (PrepareBattle()) GetTree().ChangeSceneToFile("res://features/battle/scenes/BattleScene.tscn");
    }

    // Phase 2.55: Càn quét — trừ Vàng, trả thưởng ngay, không vào trận.
    private void OnSweepPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var save = gm.Save;
        var dungeon = DungeonDatabase.Get(_dungeonId);
        var team = FormationService.FormationToTeam(save.Formation).Where(id => save.OwnedHeroIds.Contains(id)).ToList();
        var (res, reward) = DungeonService.TrySweep(save, dungeon, _diff, team);
        _status!.Text = res switch
        {
            DungeonService.SweepResult.Success =>
                $"Càn quét thành công! -{DungeonService.SweepGoldCost:N0} Vàng. Thưởng: {StageRewardService.Describe(reward!)}",
            DungeonService.SweepResult.NoGold => "Không đủ Vàng để càn quét!",
            DungeonService.SweepResult.NoTeam => "Chưa có đội hình — hãy vào Bày trận trước!",
            DungeonService.SweepResult.NotCleared => "Chưa qua ải nào — hãy đánh tay trước!",
            _ => DungeonService.LockReason(save, dungeon, _diff),
        };
        gm.SaveGame();
        Rebuild();
    }
}
