using Godot;
using System.Collections.Generic;

namespace LongTuong;

/// <summary>
/// Phase 2.24: the tavern popup with TWO views (from Huynh's sketch).
/// - Chiêu mộ view (default): a grid of generals grouped by tier rows
///   (Lam / Tím / Cam), each card showing the tier recruit cost.
///   Clicking a card recruits at once if the shared tier soul pool
///   covers the cost, else reports the shortage. Owned generals show
///   a "đã chiêu mộ" label. A balance line on top shows the three
///   tier soul pools. Two drink buttons sit bottom-right.
/// - Đối tửu view: the 3 drawn generals, each with the souls won from
///   that general this drink (the 2/3/1 numbers in the sketch), RPS
///   buttons in sketch order (Kéo / Búa / Bao), and a "Quay lại"
///   button back to the Chiêu mộ view. Going back mid-drink keeps
///   the souls already won and forfeits the remaining rounds.
/// Phase 2.24: souls are pooled per tier (not per general); the win
/// roll is 1-3 (was 1-5). RPS rules unchanged: sequential rounds,
/// draw = free rematch against the same general.
/// </summary>
[GlobalClass]
public partial class RecruitScene : Control
{
    private Label? _titleLabel;
    private Label? _goldLabel;
    private Label? _balanceLabel;
    private Label? _recruitStatusLabel;
    private Label? _drinkStatusLabel;
    private Button? _rockBtn;
    private Button? _scissorsBtn;
    private Button? _paperBtn;
    private readonly Dictionary<string, Button> _recruitCardBtns = new();
    private readonly List<Label> _drinkLabels = new();

    private List<string> _drinkHeroes = new();
    private List<int> _drinkSouls = new();
    private int _rpsIndex;
    private bool _drinkActive;
    private string _recruitStatus = "";
    private string _drinkStatus = "";

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm0 = GetNode<GameManager>("/root/GameManager");
        _titleLabel = GetNode<Label>("TitleLabel");
        UiTheme.StyleTitle(_titleLabel);
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm0);
        UiTheme.StyleButton(GetNode<Button>("NormalButton"), true);
        UiTheme.StyleButton(GetNode<Button>("HighButton"));
        UiTheme.StyleButton(GetNode<Button>("ToRecruitButton"));
        _scissorsBtn = GetNode<Button>("ScissorsButton");
        _rockBtn = GetNode<Button>("RockButton");
        _paperBtn = GetNode<Button>("PaperButton");
        UiTheme.StyleButton(_scissorsBtn);
        UiTheme.StyleButton(_rockBtn);
        UiTheme.StyleButton(_paperBtn);
        _goldLabel = GetNode<Label>("GoldLabel");
        _balanceLabel = GetNode<Label>("BalanceLabel");
        _recruitStatusLabel = GetNode<Label>("RecruitStatusLabel");
        _drinkStatusLabel = GetNode<Label>("DrinkStatusLabel");
        _recruitStatus = "Bấm vào tướng để chiêu mộ (trừ hồn theo phẩm).";
        BuildRecruitCards();
        BuildDrinkLabels();
        ShowRecruitView();
        Refresh();
    }

    private void BuildRecruitCards()
    {
        var box = GetNode<Control>("RecruitBox");
        var rowY = new Dictionary<TavernTier, float> { [TavernTier.Lam] = 0, [TavernTier.Tim] = 100, [TavernTier.Cam] = 200 };
        var col = new Dictionary<TavernTier, int> { [TavernTier.Lam] = 0, [TavernTier.Tim] = 0, [TavernTier.Cam] = 0 };
        foreach (var heroId in TavernData.PoolHeroIds)
        {
            var tier = TavernData.TierOf(heroId)!.Value;
            var btn = new Button();
            btn.SetPosition(new Vector2(col[tier] * 170, rowY[tier]));
            btn.SetSize(new Vector2(150, 90));
            UiTheme.StyleButton(btn);
            var id = heroId;
            btn.Pressed += () => OnRecruitCardPressed(id);
            box.AddChild(btn);
            _recruitCardBtns[heroId] = btn;
            col[tier]++;
        }
    }

    private void BuildDrinkLabels()
    {
        var box = GetNode<Control>("DrinkBox");
        for (int i = 0; i < 3; i++)
        {
            var label = new Label();
            label.SetPosition(new Vector2(i * 320, 0));
            label.SetSize(new Vector2(300, 140));
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            box.AddChild(label);
            _drinkLabels.Add(label);
        }
    }

    private void ShowRecruitView()
    {
        if (_titleLabel != null) _titleLabel.Text = "CHIÊU MỘ";
        SetVisible("BalanceLabel", true);
        SetVisible("RecruitBox", true);
        SetVisible("RecruitStatusLabel", true);
        SetVisible("NormalButton", true);
        SetVisible("HighButton", true);
        SetVisible("DrinkBox", false);
        SetVisible("ScissorsButton", false);
        SetVisible("RockButton", false);
        SetVisible("PaperButton", false);
        SetVisible("DrinkStatusLabel", false);
        SetVisible("ToRecruitButton", false);
    }

    private void ShowDrinkView()
    {
        if (_titleLabel != null) _titleLabel.Text = "ĐỐI TỬU";
        SetVisible("BalanceLabel", false);
        SetVisible("RecruitBox", false);
        SetVisible("RecruitStatusLabel", false);
        SetVisible("NormalButton", false);
        SetVisible("HighButton", false);
        SetVisible("DrinkBox", true);
        SetVisible("ScissorsButton", true);
        SetVisible("RockButton", true);
        SetVisible("PaperButton", true);
        SetVisible("DrinkStatusLabel", true);
        SetVisible("ToRecruitButton", true);
    }

    private void SetVisible(string node, bool visible)
    {
        var c = GetNodeOrNull<Control>(node);
        if (c != null) c.Visible = visible;
    }

    private void Refresh()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var save = gm.Save;
        ProgressionService.EnsureDefaults(save);
        if (_goldLabel != null)
            _goldLabel.Text = $"Vàng: {save.Gold} | Kim Nguyên Bảo: {save.Gems} | Cấp tướng tự tạo: {TavernService.PlayerLevel(save)} (tướng Cam vào pool từ cấp 70)";
        if (_balanceLabel != null)
            _balanceLabel.Text = $"Hồn Lam: {TavernService.TierSoulsOf(save, TavernTier.Lam)} | Hồn Tím: {TavernService.TierSoulsOf(save, TavernTier.Tim)} | Hồn Cam: {TavernService.TierSoulsOf(save, TavernTier.Cam)}";
        foreach (var (id, btn) in _recruitCardBtns)
        {
            var hero = HeroDatabase.Get(id);
            var tier = TavernData.TierOf(id);
            string tierName = tier == null ? "?" : TavernData.TierName(tier.Value);
            bool owned = save.OwnedHeroIds.Contains(id);
            btn.Text = owned ? $"{hero.Name} ({tierName})\nđã chiêu mộ" : $"{hero.Name} ({tierName})\n{TavernService.RecruitCostOf(id)} hồn";
            btn.Disabled = owned || !TavernService.CanRecruit(save, id);
        }
        for (int i = 0; i < _drinkLabels.Count; i++)
        {
            if (i >= _drinkHeroes.Count) { _drinkLabels[i].Text = ""; continue; }
            var id = _drinkHeroes[i];
            var hero = HeroDatabase.Get(id);
            var tier = TavernData.TierOf(id);
            string mark = !_drinkActive ? "✓ xong" : i < _rpsIndex ? "✓ xong" : i == _rpsIndex ? "← đang đấu" : "";
            int won = i < _drinkSouls.Count ? _drinkSouls[i] : 0;
            _drinkLabels[i].Text = $"{hero.Name} ({(tier == null ? "?" : TavernData.TierName(tier.Value))})\nHồn thắng: {won} {mark}";
        }
        if (_scissorsBtn != null) _scissorsBtn.Disabled = !_drinkActive;
        if (_rockBtn != null) _rockBtn.Disabled = !_drinkActive;
        if (_paperBtn != null) _paperBtn.Disabled = !_drinkActive;
        if (_recruitStatusLabel != null) _recruitStatusLabel.Text = _recruitStatus;
        if (_drinkStatusLabel != null) _drinkStatusLabel.Text = _drinkStatus;
    }

    private void OnRecruitCardPressed(string heroId)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var hero = HeroDatabase.Get(heroId);
        var tier = TavernData.TierOf(heroId);
        if (TavernService.Recruit(gm.Save, heroId))
        {
            _recruitStatus = $"Chiêu mộ thành công {hero.Name}!";
            gm.SaveGame();
        }
        else if (tier != null)
            _recruitStatus = $"Không đủ hồn {TavernData.TierName(tier.Value)} (cần {TavernData.RecruitCost(tier.Value)}, đang có {TavernService.TierSoulsOf(gm.Save, tier.Value)}).";
        else _recruitStatus = "Không chiêu mộ được tướng này.";
        Refresh();
    }

    private void OnDrinkPressed(TavernGrade grade)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (!TavernService.StartDrink(gm.Save, grade, new System.Random(), out var heroes))
        {
            _recruitStatus = "Không đủ Vàng để đối tửu!";
            Refresh();
            return;
        }
        _drinkHeroes = heroes;
        _drinkSouls = new List<int> { 0, 0, 0 };
        _rpsIndex = 0;
        _drinkActive = true;
        _drinkStatus = $"Đã rót rượu ({TavernData.GradeName(grade)})! Lần lượt oẳn tù tì với 3 tướng.";
        gm.SaveGame();
        ShowDrinkView();
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
                line += $"Thắng! +{result.SoulsGained} hồn.";
                _drinkSouls[_rpsIndex] += result.SoulsGained;
                _rpsIndex++;
                break;
            case RpsOutcome.Draw:
                line += "Hòa! Oẳn tù tì lại với tướng này.";
                break;
            default:
                line += "Thua! Không nhận được hồn.";
                _rpsIndex++;
                break;
        }
        if (_rpsIndex >= _drinkHeroes.Count)
        {
            _drinkActive = false;
            line += " Hết lượt đối tửu — bấm Quay lại để về Chiêu mộ.";
        }
        _drinkStatus = line;
        gm.SaveGame();
        Refresh();
    }

    private void OnScissorsPressed() => OnRpsPressed(1);
    private void OnRockPressed() => OnRpsPressed(0);
    private void OnPaperPressed() => OnRpsPressed(2);

    private void OnBackPressed()
    {
        // Souls already won stay in the tier pools; unplayed rounds are forfeited.
        _drinkActive = false;
        ShowRecruitView();
        Refresh();
    }
}
