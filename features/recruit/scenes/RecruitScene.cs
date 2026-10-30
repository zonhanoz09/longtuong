using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.25: the tavern popup, layout fixed to Huynh's sketch.
/// - Chiêu mộ view (default): centered title; a currency line
///   (Vàng | Bạc | custom-hero level); a tier soul balance
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
    private Label? _goldLabel;
    private Label? _silverLabel;
    private Label? _levelLabel;
    private Label? _recruitStatusLabel;
    private Label? _drinkStatusLabel;
    private Button? _rockBtn;
    private Button? _scissorsBtn;
    private Button? _paperBtn;
    private readonly Dictionary<string, CardWidgets> _recruitCards = new();
    // Phase 2.38: shop thẻ EXP (nửa phải RecruitBox).
    private readonly Dictionary<string, (Label Count, Button Buy)> _expShopRows = new();
    private readonly Dictionary<TavernTier, Label> _balanceNums = new();
    private readonly List<Label> _drinkLabels = new();

    private List<string> _drinkHeroes = new();
    private List<int> _drinkSouls = new();
    private int _rpsIndex;
    private bool _drinkActive;
    private string _recruitStatus = "";
    private string _drinkStatus = "";

    public static string Fmt(long n) =>
        n.ToString("N0", new System.Globalization.CultureInfo("vi-VN"));

    public static Texture2D MakeDotTexture(Color color)
    {
        int size = UiTheme.TierDotSize;
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        float c = size / 2f - 0.5f, r = size / 2f - 1;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                if (dx * dx + dy * dy <= r * r) img.SetPixel(x, y, color);
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
        BuildCurrencyBox();
        _recruitStatusLabel = GetNode<Label>("RecruitStatusLabel");
        _drinkStatusLabel = GetNode<Label>("DrinkStatusLabel");
        _recruitStatus = "Bấm vào tướng để chiêu mộ (trừ hồn theo phẩm).";
        BuildBalanceLine();
        BuildRecruitCards();
        BuildExpShop();
        BuildDrinkLabels();
        ShowRecruitView();
        Refresh();
    }

    private void BuildCurrencyBox()
    {
        // Phase 2.27: currency line with coin icons (gold/silver coins
        // from AssetIcons) instead of a single text label.
        var box = new HBoxContainer { Name = "CurrencyBox" };
        box.SetPosition(new Vector2(20, 45));
        box.SetSize(new Vector2(960, 25));
        box.AddThemeConstantOverride("separation", 8);
        box.AddChild(AssetIcons.MakeCoinIcon(true, 24));
        _goldLabel = new Label { Name = "GoldLabel", Text = "" };
        box.AddChild(_goldLabel);
        box.AddChild(AssetIcons.MakeCoinIcon(false, 24));
        _silverLabel = new Label { Name = "SilverLabel", Text = "" };
        box.AddChild(_silverLabel);
        _levelLabel = new Label { Name = "LevelLabel", Text = "" };
        box.AddChild(_levelLabel);
        AddChild(box);
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
            var dot = new TextureRect { Texture = MakeDotTexture(UiTheme.TierColor(tiers[i])), CustomMinimumSize = new Vector2(UiTheme.TierDotSize, UiTheme.TierDotSize) };
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
            var dot = new TextureRect { Name = "DotRect", Texture = MakeDotTexture(UiTheme.TierColor(tier)), CustomMinimumSize = new Vector2(UiTheme.TierDotSize, UiTheme.TierDotSize) };
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

    // Phase 2.54 (fix vỡ layout): RecruitBox cuộn dọc; shop thẻ EXP nằm
    // DƯỚI lưới tướng (roster đã lên 15 tướng, 5 cột — nửa phải không còn trống).
    private void BuildExpShop()
    {
        var box = GetNode<Control>("RecruitBox");
        // Chuyển các card tướng có sẵn vào content cuộn được.
        var content = new Control { Name = "RecruitContent", MouseFilter = Control.MouseFilterEnum.Ignore };
        content.SetPosition(Vector2.Zero);
        // Lưới 3 hàng (356px) + shop (tiêu đề 30 + 3 hàng x 100 + đệm).
        content.CustomMinimumSize = new Vector2(960, 710);
        foreach (var child in box.GetChildren().ToArray())
        {
            box.RemoveChild(child);
            content.AddChild(child);
        }
        var scroll = new ScrollContainer { Name = "RecruitScroll" };
        scroll.SetPosition(Vector2.Zero);
        scroll.SetSize(new Vector2(960, 356));
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        box.AddChild(scroll);
        scroll.AddChild(content);

        float y0 = 376;
        var title = new Label { Text = "THẺ KINH NGHIỆM", MouseFilter = Control.MouseFilterEnum.Ignore };
        title.Position = new Vector2(40, y0);
        title.Size = new Vector2(400, 28);
        UiTheme.StyleBody(title, UiTheme.FontButton);
        title.AddThemeColorOverride("font_color", UiTheme.Gold);
        content.AddChild(title);
        var hint = new Label { Text = "Ăn ở popup Võ tướng (click đúp ô thẻ).", MouseFilter = Control.MouseFilterEnum.Ignore };
        hint.Position = new Vector2(40, y0 + 28);
        hint.Size = new Vector2(400, 20);
        UiTheme.StyleBody(hint, UiTheme.FontSmall);
        hint.AddThemeColorOverride("font_color", UiTheme.Muted);
        content.AddChild(hint);
        for (int i = 0; i < ExpCardService.Cards.Count; i++)
        {
            var def = ExpCardService.Cards[i];
            float y = y0 + 56 + i * 100;
            var icon = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            icon.Position = new Vector2(40, y);
            icon.Size = new Vector2(64, 64);
            icon.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color(0.75f, 0.55f, 0.15f),
                CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            });
            var sym = new Label { Text = "EXP", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
            sym.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            UiTheme.StyleBody(sym, UiTheme.FontSmallButton);
            sym.AddThemeColorOverride("font_color", Colors.White);
            icon.AddChild(sym);
            content.AddChild(icon);
            var name = new Label { Text = $"{def.Name}\n+{def.Exp:N0} EXP", MouseFilter = Control.MouseFilterEnum.Ignore };
            name.Position = new Vector2(112, y);
            name.Size = new Vector2(180, 64);
            UiTheme.StyleBody(name, UiTheme.FontSmall);
            content.AddChild(name);
            var count = new Label { Text = "", MouseFilter = Control.MouseFilterEnum.Ignore };
            count.Position = new Vector2(112, y + 64);
            count.Size = new Vector2(180, 22);
            UiTheme.StyleBody(count, UiTheme.FontSmall);
            count.AddThemeColorOverride("font_color", UiTheme.Muted);
            content.AddChild(count);
            var buy = new Button { Name = $"ExpShopBuy_{def.Id}", Text = $"Mua ({def.PriceGold:N0} Vàng)" };
            buy.Position = new Vector2(300, y + 12);
            buy.Size = new Vector2(170, 40);
            UiTheme.StyleButton(buy, false);
            buy.AddThemeFontSizeOverride("font_size", UiTheme.FontSmallButton);
            var id = def.Id;
            buy.Pressed += () =>
            {
                var gm2 = GetNode<GameManager>("/root/GameManager");
                _recruitStatus = ExpCardService.TryBuyCard(gm2.Save, id)
                    ? $"Đã mua {def.Name}!"
                    : "Không đủ Vàng!";
                gm2.SaveGame();
                Refresh();
            };
            content.AddChild(buy);
            _expShopRows[def.Id] = (count, buy);
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
        if (_goldLabel != null) _goldLabel.Text = $"Vàng: {Fmt(save.Gold)}";
        if (_silverLabel != null) _silverLabel.Text = $"Bạc: {Fmt(save.Silver)}";
        if (_levelLabel != null) _levelLabel.Text = $"| Cấp tướng tự tạo: {TavernService.PlayerLevel(save)} (tướng Cam vào pool từ cấp 70)";
        foreach (var (tier, label) in _balanceNums)
            label.Text = Fmt(TavernService.TierSoulsOf(save, tier));
        foreach (var (id, card) in _recruitCards)
        {
            bool owned = save.OwnedHeroIds.Contains(id);
            // Phase 2.43: heroes of a not-yet-cleared chapter are greyed out.
            bool locked = !owned && !TavernData.IsInPool(save, id);
            card.Owned.Text = locked ? $"Qua chương {TavernData.UnlockChapter[id]}" : "đã chiêu mộ";
            card.Owned.Visible = owned || locked;
            card.Dot.Visible = !owned && !locked;
            card.Price.Visible = !owned && !locked;
            card.Btn.Disabled = owned || locked || !TavernService.CanRecruit(save, id);
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
        // Phase 2.38: refresh shop thẻ EXP.
        foreach (var def in ExpCardService.Cards)
        {
            if (!_expShopRows.TryGetValue(def.Id, out var row)) continue;
            row.Count.Text = $"Đang có: {ExpCardService.Count(save, def.Id)}";
            row.Buy.Disabled = save.Gold < def.PriceGold;
        }
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
