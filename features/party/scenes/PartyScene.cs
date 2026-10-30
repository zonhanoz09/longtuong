using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.51: Chiến Dịch Tổ Đội popup — 4 bosses (opened one after
/// another), preview of the 2 bot teams for this attempt, Xuất chinh.
/// The bots are built from the same seed the battle will use, so the
/// preview matches the fight.
/// </summary>
[GlobalClass]
public partial class PartyScene : Control
{
    private string _bossId = PartyService.Bosses[0].Id;
    private int _seed;
    private VBoxContainer? _bossList;
    private Label? _info;
    private Label? _status;
    private Button? _go;

    public string SelectedBossId => _bossId;
    public int Seed => _seed;

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        FormationService.EnsureFormation(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        _bossList = GetNode<VBoxContainer>("BossList");
        _info = GetNode<Label>("InfoLabel");
        _status = GetNode<Label>("StatusLabel");
        _go = GetNode<Button>("GoButton");
        UiTheme.StyleBody(_info, UiTheme.FontBody);
        UiTheme.StyleBody(_status, UiTheme.FontBody);
        UiTheme.StyleButton(_go, true);
        _seed = (int)(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 1000000);
        // Default to the last open boss.
        var open = PartyService.Bosses.LastOrDefault(b => PartyService.IsBossOpen(gm.Save, b.Id));
        if (open != null) _bossId = open.Id;
        Rebuild();
    }

    public void SetSeed(int seed) { _seed = seed; Rebuild(); }

    public void SelectBoss(string bossId) { _bossId = bossId; Rebuild(); }

    private void Rebuild()
    {
        var save = GetNode<GameManager>("/root/GameManager").Save;
        foreach (var child in _bossList!.GetChildren()) { _bossList.RemoveChild(child); child.QueueFree(); }
        foreach (var boss in PartyService.Bosses)
        {
            bool open = PartyService.IsBossOpen(save, boss.Id);
            int wins = save.PartyCleared.TryGetValue(boss.Id, out var n) ? n : 0;
            var btn = new Button
            {
                Name = $"Boss_{boss.Id}",
                Text = $"{HeroDatabase.GetCombatant(boss.Id).Name}{(open ? (wins > 0 ? $" (thắng {wins})" : "") : " (KHÓA)")}",
                TooltipText = open ? "" : PartyService.LockReason(save, boss.Id),
                Disabled = !open,
                ToggleMode = true,
                ButtonPressed = boss.Id == _bossId,
            };
            UiTheme.StyleButton(btn, boss.Id == _bossId);
            var id = boss.Id;
            btn.Pressed += () => SelectBoss(id);
            _bossList.AddChild(btn);
        }

        var player = PartyService.PlayerTeam(save);
        var lines = new List<string>();
        var b = PartyService.GetBoss(_bossId);
        lines.Add($"Boss: {HeroDatabase.GetCombatant(_bossId).Name} (HP x{PartyService.BossHpMultiplier:0}, sức mạnh x{b.Scale:0.0})");
        lines.Add($"Mỗi đội tối đa {PartyService.MaxTurnsPerTeam} lượt, HP boss giữ nguyên qua các đội.");
        lines.Add($"Thưởng: {PartyService.SilverPerWin:N0} Bạc + {PartyService.MeritPerWin} Chiến tích + {PartyService.FragmentsPerWin} {ForgeService.FragmentName(b.FragmentSetId)}");
        lines.Add("");
        lines.Add($"{player.Label}: {string.Join(", ", player.Heroes.Select(h => h.Name))} — lực chiến {PowerService.TeamPower(player.Heroes):N0}");
        for (int i = 1; i < PartyService.Teams; i++)
        {
            var bot = PartyService.BuildBotTeam(save, i, _seed);
            lines.Add($"{bot.Label}: {string.Join(", ", bot.Heroes.Select(h => h.Name))} — lực chiến {PowerService.TeamPower(bot.Heroes):N0}");
        }
        _info!.Text = string.Join("\n", lines);
        bool canGo = PartyService.IsBossOpen(save, _bossId) && player.Heroes.Count > 0;
        _go!.Disabled = !canGo;
        _go.TooltipText = player.Heroes.Count == 0 ? "Chưa có đội hình — hãy vào Bày trận trước." : PartyService.LockReason(save, _bossId);
    }

    public bool PrepareBattle()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var reason = PartyService.LockReason(gm.Save, _bossId);
        if (reason != "") { _status!.Text = reason; return false; }
        var team = FormationService.FormationToTeam(gm.Save.Formation).Where(id => gm.Save.OwnedHeroIds.Contains(id)).ToList();
        if (team.Count == 0) { _status!.Text = "Chưa có đội hình — hãy vào Bày trận trước!"; return false; }
        gm.StartModeBattle(BattleMode.Party, _bossId, PartyService.BossStage(_bossId), team, _seed);
        gm.SaveGame();
        return true;
    }

    private void OnGoPressed()
    {
        if (PrepareBattle()) GetTree().ChangeSceneToFile("res://features/battle/scenes/BattleScene.tscn");
    }
}
