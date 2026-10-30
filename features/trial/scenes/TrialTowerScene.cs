using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.49: Thí Luyện Tháp popup — current floor, next floor's enemies
/// and gem reward, Khiêu chiến with the Bày trận team.
/// </summary>
[GlobalClass]
public partial class TrialTowerScene : Control
{
    private Label? _floor;
    private Label? _info;
    private Label? _status;
    private Button? _challenge;

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        FormationService.EnsureFormation(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        _floor = GetNode<Label>("FloorLabel");
        _info = GetNode<Label>("InfoLabel");
        _status = GetNode<Label>("StatusLabel");
        _challenge = GetNode<Button>("ChallengeButton");
        UiTheme.StyleBody(_floor, 18);
        UiTheme.StyleBody(_info, UiTheme.FontBody);
        UiTheme.StyleBody(_status, UiTheme.FontBody);
        UiTheme.StyleButton(_challenge, true);
        Refresh();
    }

    private void Refresh()
    {
        var save = GetNode<GameManager>("/root/GameManager").Save;
        _floor!.Text = $"Đã vượt: {save.TrialFloor}/{TrialTowerService.MaxFloor} tầng";
        var reason = TrialTowerService.LockReason(save);
        if (reason != "")
        {
            _info!.Text = reason;
            _challenge!.Text = "Khiêu chiến";
            _challenge.Disabled = true;
            return;
        }
        int floor = TrialTowerService.NextFloor(save);
        var stage = TrialTowerService.StageForFloor(floor);
        var names = stage.EnemyIds.Select(id => HeroDatabase.GetCombatant(id).Name + (id == stage.BossId ? " (BOSS)" : ""));
        int n = TrialTowerService.GemCount(floor);
        _info!.Text = $"Tầng {floor}: {string.Join(", ", names)}\nSức mạnh x{stage.Scale:0.00}\n" +
                      $"Thưởng lần đầu: {n} ngọc Lv{TrialTowerService.GemLevel(floor)} (loại ngẫu nhiên)\nĐánh bằng đội hình Bày trận.";
        int team = FormationService.FormationToTeam(save.Formation).Count(id => save.OwnedHeroIds.Contains(id));
        _challenge!.Text = $"Khiêu chiến tầng {floor}";
        _challenge.Disabled = team == 0;
    }

    public bool PrepareBattle()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var reason = TrialTowerService.LockReason(gm.Save);
        if (reason != "") { _status!.Text = reason; return false; }
        var team = FormationService.FormationToTeam(gm.Save.Formation).Where(id => gm.Save.OwnedHeroIds.Contains(id)).ToList();
        if (team.Count == 0) { _status!.Text = "Chưa có đội hình — hãy vào Bày trận trước!"; return false; }
        int floor = TrialTowerService.NextFloor(gm.Save);
        int seed = (int)(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 1000000) + floor * 7919;
        gm.StartModeBattle(BattleMode.Trial, TrialTowerService.Key(floor), TrialTowerService.StageForFloor(floor), team, seed);
        gm.SaveGame();
        return true;
    }

    private void OnChallengePressed()
    {
        if (PrepareBattle()) GetTree().ChangeSceneToFile("res://features/battle/scenes/BattleScene.tscn");
    }
}
