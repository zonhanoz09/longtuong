using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.23: the tavern popup (rewritten from the old recruit scene).
/// No tier-selection buttons: each drink draws 3 generals by the grade's
/// tier weights (Normal 70/25/5, High 60/30/10; Cam weight 0 below
/// custom-hero level 70). The player then plays rock-paper-scissors
/// against the 3 generals in turn: win => +1-5 souls of that general,
/// draw => a free rematch against the same general, lose => nothing.
/// The soul list below has one recruit button per pool hero, enabled
/// only when that hero is unowned and has enough souls.
/// </summary>
[GlobalClass]
public partial class RecruitScene : Control
{
    private Label? _statusLabel;
    private Label? _goldLabel;
    private Label? _generalsLabel;
    private Label? _soulsLabel;
    private Button? _rockBtn;
    private Button? _scissorsBtn;
    private Button? _paperBtn;
    private readonly Dictionary<string, Button> _recruitBtns = new();

    private List<string> _drinkHeroes = new();
    private int _rpsIndex;
    private bool _drinkActive;
    private string _status = "";

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm0 = GetNode<GameManager>("/root/GameManager");
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm0);
        UiTheme.StyleButton(GetNode<Button>("NormalButton"), true);
        UiTheme.StyleButton(GetNode<Button>("HighButton"));
        _rockBtn = GetNode<Button>("RockButton");
        _scissorsBtn = GetNode<Button>("ScissorsButton");
        _paperBtn = GetNode<Button>("PaperButton");
        UiTheme.StyleButton(_rockBtn);
        UiTheme.StyleButton(_scissorsBtn);
        UiTheme.StyleButton(_paperBtn);
        _statusLabel = GetNode<Label>("StatusLabel");
        _goldLabel = GetNode<Label>("GoldLabel");
        _generalsLabel = GetNode<Label>("GeneralsLabel");
        _soulsLabel = GetNode<Label>("SoulsLabel");
        _status = "Chọn mức đối tửu để bắt đầu. Thắng oẳn tù tì nhận 1–5 Võ Hồn của tướng đó.";
        BuildRecruitButtons();
        Refresh();
    }

    private void BuildRecruitButtons()
    {
        var box = GetNode<Control>("RecruitBox");
        float y = 0;
        foreach (var heroId in TavernData.PoolHeroIds)
        {
            var btn = new Button();
            btn.SetPosition(new Vector2(0, y));
            btn.SetSize(new Vector2(420, 36));
            UiTheme.StyleButton(btn);
            var id = heroId;
            btn.Pressed += () =>
            {
                var gm = GetNode<GameManager>("/root/GameManager");
                if (TavernService.Recruit(gm.Save, id))
                {
                    _status = $"Chiêu mộ thành công {HeroDatabase.Get(id).Name}!";
                    gm.SaveGame();
                }
                else _status = "Chưa đủ Võ Hồn để chiêu mộ tướng này.";
                Refresh();
            };
            box.AddChild(btn);
            _recruitBtns[heroId] = btn;
            y += 44;
        }
    }

    private void Refresh()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var save = gm.Save;
        ProgressionService.EnsureDefaults(save);
        if (_goldLabel != null)
            _goldLabel.Text = $"Vàng: {save.Gold} | Kim Nguyên Bảo: {save.Gems} | Cấp tướng tự tạo: {TavernService.PlayerLevel(save)} (tướng Cam vào pool từ cấp 70)";
        if (_generalsLabel != null)
        {
            if (_drinkHeroes.Count == 0) _generalsLabel.Text = "Chưa đối tửu lần nào.";
            else
            {
                var lines = new List<string>();
                for (int i = 0; i < _drinkHeroes.Count; i++)
                {
                    var id = _drinkHeroes[i];
                    var hero = HeroDatabase.Get(id);
                    var tier = TavernData.TierOf(id);
                    string mark = !_drinkActive ? "✓ xong" : i < _rpsIndex ? "✓ xong" : i == _rpsIndex ? "← đang đấu" : "";
                    lines.Add($"{hero.Name} ({(tier == null ? "?" : TavernData.TierName(tier.Value))}) — Võ Hồn: {TavernService.SoulsOf(save, id)}/{TavernService.RecruitCostOf(id)} {mark}");
                }
                _generalsLabel.Text = string.Join("\n", lines);
            }
        }
        if (_soulsLabel != null)
        {
            var lines = TavernData.PoolHeroIds.Select(id =>
            {
                var hero = HeroDatabase.Get(id);
                var tier = TavernData.TierOf(id);
                string owned = save.OwnedHeroIds.Contains(id) ? " — đã sở hữu" : "";
                return $"{hero.Name} ({(tier == null ? "?" : TavernData.TierName(tier.Value))}): {TavernService.SoulsOf(save, id)}/{TavernService.RecruitCostOf(id)} hồn{owned}";
            });
            _soulsLabel.Text = "Võ Hồn:\n" + string.Join("\n", lines);
        }
        if (_rockBtn != null) _rockBtn.Disabled = !_drinkActive;
        if (_scissorsBtn != null) _scissorsBtn.Disabled = !_drinkActive;
        if (_paperBtn != null) _paperBtn.Disabled = !_drinkActive;
        foreach (var (id, btn) in _recruitBtns)
        {
            var hero = HeroDatabase.Get(id);
            bool owned = save.OwnedHeroIds.Contains(id);
            btn.Text = owned ? $"{hero.Name} — đã sở hữu" : $"Chiêu mộ {hero.Name} ({TavernService.SoulsOf(save, id)}/{TavernService.RecruitCostOf(id)} hồn)";
            btn.Disabled = owned || !TavernService.CanRecruit(save, id);
        }
        if (_statusLabel != null) _statusLabel.Text = _status;
    }

    private void OnDrinkPressed(TavernGrade grade)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (!TavernService.StartDrink(gm.Save, grade, new System.Random(), out var heroes))
        {
            _status = "Không đủ Vàng để đối tửu!";
            Refresh();
            return;
        }
        _drinkHeroes = heroes;
        _rpsIndex = 0;
        _drinkActive = true;
        _status = $"Đã rót rượu ({TavernData.GradeName(grade)})! Lần lượt oẳn tù tì với 3 tướng.";
        gm.SaveGame();
        Refresh();
    }

    private void OnNormalPressed() => OnDrinkPressed(TavernGrade.Normal);
    private void OnHighPressed() => OnDrinkPressed(TavernGrade.High);

    private void OnRpsPressed(int move)
    {
        if (!_drinkActive || _rpsIndex >= _drinkHeroes.Count) return;
        var gm = GetNode<GameManager>("/root/GameManager");
        var heroId = _drinkHeroes[_rpsIndex];
        var hero = HeroDatabase.Get(heroId);
        var result = TavernService.PlayRps(gm.Save, heroId, move, new System.Random());
        string line = $"Bạn ra {TavernService.MoveNames[move]}, {hero.Name} ra {TavernService.MoveNames[result.GeneralMove]}: ";
        switch (result.Outcome)
        {
            case RpsOutcome.Win:
                line += $"Thắng! +{result.SoulsGained} Võ Hồn {hero.Name}.";
                _rpsIndex++;
                break;
            case RpsOutcome.Draw:
                line += "Hòa! Được đánh lại miễn phí với tướng này.";
                break;
            default:
                line += "Thua! Không nhận được hồn.";
                _rpsIndex++;
                break;
        }
        if (_rpsIndex >= _drinkHeroes.Count)
        {
            _drinkActive = false;
            line += " Hết lượt đối tửu — đối tửu lần nữa hoặc chiêu mộ tướng đủ hồn.";
        }
        _status = line;
        gm.SaveGame();
        Refresh();
    }

    private void OnRockPressed() => OnRpsPressed(0);
    private void OnScissorsPressed() => OnRpsPressed(1);
    private void OnPaperPressed() => OnRpsPressed(2);
}
