using Godot;
using System.Collections.Generic;

namespace LongTuong;

/// <summary>
/// Phase 2.25: the tavern popup, layout fixed to Huynh's sketch.
/// - Chiêu mộ view (default): centered title; a currency line
///   (Vàng | Bạc | KNB | custom-hero level); a tier soul balance
///   line as COLORED DOTS + numbers (no tier-name text); a grid of
///   general cards in 6 even columns (pitch 152) grouped by tier
///   rows (Lam 2, Tím 2, Cam 3 — roster unchanged, fewer cells than
///   the sketch's schematic 6/5/2). Each card: name on top, a tier
///   colored dot + recruit cost at the bottom; owned cards show
///   "đã chiêu mộ" instead. Clicking a card recruits at once.
///   Three buttons bottom-right: Tất thắng (x10) (200 Vàng),
///   Đối tửu Thường (150.000 Bạc), Đối tửu Cao cấp (300.000 Bạc).
///   The status line sits under the grid, full width.
/// - Đối tửu view: unchanged from 2.24 (3 generals + souls won each,
///   Kéo / Búa / Bao, "Quay lại" to the recruit view).
/// - Phase 2.25 currencies: drinks cost BẠC; all-win costs VÀNG.
///   All-win: 10 drinks, all 30 wins, HIGH-grade weights + Cam gate.
/// </summary>
[GlobalClass]
public partial class RecruitScene : Control
{
    private class CardWidgets
    {
        public Button Btn = null!;
        public Label Name = null!;
        public TextureRect Dot = null!;
        public Label Price = null!;
        public Label Owned = null!;
    }

    private Label? _titleLabel;
    private Label? _currencyLabel;
    private Label? _recruitStatusLabel;
    private Label? _drinkStatusLabel;
    private Button? _rockBtn;
    private Button? _scissorsBtn;
    private Button? _paperBtn;
    private readonly Dictionary<string, CardWidgets> _recruitCards = new();
    private readonly Dictionary<TavernTier, Label> _balanceNums = new();
    private readonly List<Label> _drinkLabels = new();

    private List<string> _drinkHeroes = new();
    private List<int> _drinkSouls = new();
    private int _rpsIndex;
    private bool _drinkActive;
    private string _recruitStatus = "";
    private string _drinkStatus = "";

    public static Color TierDotColor(TavernTier tier) => tier switch
    {
        TavernTier.Lam => new Color(0.15f, 0.55f, 1f),
        TavernTier.Tim => new Color(0.85f, 0.25f, 0.95f),
        _ => new Color(1f, 0.6f, 0.1f),
    };

    public static string Fmt(long n) =>
        n.ToString("N0", new System.Globalization.CultureInfo("vi-VN"));

    public static Texture2D MakeDotTexture(Color color)
    {
        var img = Image.CreateEmpty(20, 20, false, Image.Format.Rgba8);
        for (int y = 0; y < 20; y++)
            for (int x = 0; x < 20; x++)
            {
                float dx = x - 9.5f, dy = y - 9.5f;
                if (dx * dx + dy * dy <= 81) img.SetPixel(x, y, color);
            }
        return ImageTexture.CreateFromImage(img);
    }

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm0 = GetNode<GameManager>("/root/GameManager");
        _titleLabel = GetNode<Label>("TitleLabel");
        UiTheme.StyleTitle(_titleLabel);
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm0);
        UiTheme.StyleButton(GetNode<Button>("AllWinButton"), true);
        UiTheme.StyleButton(GetNode<Button>("NormalButton"));
        UiTheme.StyleButton(GetNode<Button>("HighButton"));
        UiTheme.StyleButton(GetNode<Button>("ToRecruitButton"));
        _scissorsBtn = GetNode<Button>("ScissorsButton");
        _rockBtn = GetNode<Button>("RockButton");
        _paperBtn = GetNode<Button>("PaperButton");
        UiTheme.StyleButton(_scissorsBtn);
        UiTheme.StyleButton(_rockBtn);
        UiTheme.StyleButton(_paperBtn);
        _currencyLabel = GetNode<Label>("CurrencyLabel");
        _recruitStatusLabel = GetNode<Label>("RecruitStatusLabel");
        _drinkStatusLabel = GetNode<Label>("DrinkStatusLabel");
        _recruitStatus = "Bấm vào tướng để chiêu mộ (trừ hồn theo phẩm).";
        BuildBalanceLine();
        BuildRecruitCards();
        BuildDrinkLabels();
        ShowRecruitView();
        Refresh();
    }

    private void BuildBalanceLine()
    {
        var box = new HBoxContainer { Name = "BalanceBox" };
        box.SetPosition(new Vector2(20, 78));
        box.SetSize(new Vector2(960, 30));
        box.AddThemeConstantOverride("separation", 10);
        var tiers = new[] { TavernTier.Lam, TavernTier.Tim, TavernTier.Cam };
        for (int i = 0; i < tiers.Length; i++)
        {
            var dot = new TextureRect { Texture = MakeDotTexture(TierDotColor(tiers[i])), CustomMinimumSize = new Vector2(20, 20) };
            var num = new Label { Name = tiers[i].ToString() + "SoulsLabel", Text = "0" };
            box.AddChild(dot);
            box.AddChild(num);
            _balanceNums[tiers[i]] = num;
            if (i < tiers.Length - 1) box.AddChild(new Label { Text = "|" });
        }
        AddChild(box);
    }

    private void BuildRecruitCards()
    {
        var box = GetNode<Control>("RecruitBox");
        var rowY = new Dictionary<TavernTier, float> { [TavernTier.Lam] = 0, [TavernTier.Tim] = 122, [TavernTier.Cam] = 244 };
        var col = new Dictionary<TavernTier, int> { [TavernTier.Lam] = 0, [TavernTier.Tim] = 0, [TavernTier.Cam] = 0 };
        foreach (var heroId in TavernData.PoolHeroIds)
        {
            var tier = TavernData.TierOf(heroId)!.Value;
            var btn = new Button();
            btn.SetPosition(new Vector2(40 + col[tier] * 152, rowY[tier]));
            btn.SetSize(new Vector2(130, 112));
            UiTheme.StyleButton(btn);
            var name = new Label { Name = "NameLabel", Text = HeroDatabase.Get(heroId).Name };
            name.SetPosition(new Vector2(8, 6));
            name.SetSize(new Vector2(114, 26));
            var dot = new TextureRect { Name = "DotRect", Texture = MakeDotTexture(TierDotColor(tier)), CustomMinimumSize = new Vector2(20, 20) };
            dot.SetPosition(new Vector2(8, 84));
            dot.SetSize(new Vector2(20, 20));
            var price = new Label { Name = "PriceLabel", Text = Fmt(TavernService.RecruitCostOf(heroId)) };
            price.SetPosition(new Vector2(32, 84));
            price.SetSize(new Vector2(90, 24));
            var owned = new Label { Name = "OwnedLabel", Text = "đã chiêu mộ", Visible = false };
            owned.SetPosition(new Vector2(8, 84));
            owned.SetSize(new Vector2(114, 24));
            foreach (var child in new Control[] { name, dot, price, owned })
            {
                child.MouseFilter = MouseFilterEnum.Ignore;
                btn.AddChild(child);
            }
            var id = heroId;
            btn.Pressed += () => OnRecruitCardPressed(id);
            box.AddChild(btn);
            _recruitCards[heroId] = new CardWidgets { Btn = btn, Name = name, Dot = dot, Price = price, Owned = owned };
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
        if (_titleLabel != null) _titleLabel.Text = "Chiêu mộ";
        SetVisible("BalanceBox", true);
        SetVisible("RecruitBox", true);
        SetVisible("RecruitStatusLabel", true);
        SetVisible("NormalButton", true);
        SetVisible("HighButton", true);
        SetVisible("AllWinButton", true);
        SetVisible("DrinkBox", false);
        SetVisible("ScissorsButton", false);
        SetVisible("RockButton", false);
        SetVisible("PaperButton", false);
        SetVisible("DrinkStatusLabel", false);
        SetVisible("ToRecruitButton", false);
    }

    private void ShowDrinkView()
    {
        if (_titleLabel != null) _titleLabel.Text = "Đối tửu";
        SetVisible("BalanceBox", false);
        SetVisible("RecruitBox", false);
        SetVisible("RecruitStatusLabel", false);
        SetVisible("NormalButton", false);
        SetVisible("HighButton", false);
        SetVisible("AllWinButton", false);
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
        if (_currencyLabel != null)
            _currencyLabel.Text = $"Vàng: {Fmt(save.Gold)} | Bạc: {Fmt(save.Silver)} | Kim Nguyên Bảo: {Fmt(save.Gems)} | Cấp tướng tự tạo: {TavernService.PlayerLevel(save)} (tướng Cam vào pool từ cấp 70)";
        foreach (var (tier, label) in _balanceNums)
            label.Text = Fmt(TavernService.TierSoulsOf(save, tier));
        foreach (var (id, card) in _recruitCards)
        {
            bool owned = save.OwnedHeroIds.Contains(id);
            card.Owned.Visible = owned;
            card.Dot.Visible = !owned;
            card.Price.Visible = !owned;
            card.Btn.Disabled = owned || !TavernService.CanRecruit(save, id);
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
            _recruitStatus = $"Không đủ hồn {TavernData.TierName(tier.Value)} (cần {Fmt(TavernData.RecruitCost(tier.Value))}, đang có {Fmt(TavernService.TierSoulsOf(gm.Save, tier.Value))}).";
        else _recruitStatus = "Không chiêu mộ được tướng này.";
        Refresh();
    }

    private void OnDrinkPressed(TavernGrade grade)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (!TavernService.StartDrink(gm.Save, grade, new System.Random(), out var heroes))
        {
            _recruitStatus = "Không đủ Bạc để đối tửu!";
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

    private void OnAllWinPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (!TavernService.StartAllWin(gm.Save, new System.Random(), out var totals))
        {
            _recruitStatus = $"Không đủ Vàng để tất thắng (cần {TavernData.AllWinPrice} Vàng)!";
            Refresh();
            return;
        }
        _recruitStatus = $"Tất thắng x10: +{totals[TavernTier.Lam]} hồn Lam, +{totals[TavernTier.Tim]} hồn Tím, +{totals[TavernTier.Cam]} hồn Cam.";
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
