using Godot;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.27: asset standardization (procedural, per Huynh: 1c — no real
/// art files, improve placeholders by code; keep dark+gold style; square
/// portraits; icons + portraits both).
/// - UiTheme is the single source of truth: palette, font scale, sizes.
/// - AssetIcons: slot symbols, portrait chars, coin icons, square portraits.
/// - Integration: HeroList portrait holder, equipment icon symbols,
///   formation tier rings, Hub avatar portrait, tavern currency coin box.
/// </summary>
public partial class TestPhase227 : Node
{
    private int _fails;

    public override void _Ready()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CreateSeededSave("tester227");

        PaletteChecks();
        AssetIconsChecks();
        HeroListPortraitChecks(gm);
        FormationRingChecks(gm);
        HubAvatarChecks(gm);
        TavernCurrencyChecks(gm);

        if (_fails == 0) GD.Print("TEST PHASE 227: ALL PASS");
        else GD.Print($"TEST PHASE 227: {_fails} FAILURES");
        GetTree().Quit(_fails == 0 ? 0 : 1);
    }

    private void Check(bool ok, string name)
    {
        if (!ok) { _fails++; GD.PrintErr("FAIL: " + name); }
    }

    private static System.Collections.Generic.IEnumerable<T> FindDescendant<T>(Node node) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T t) yield return t;
            foreach (var sub in FindDescendant<T>(child)) yield return sub;
        }
    }

    private void PaletteChecks()
    {
        Check(UiTheme.TierColor(TavernTier.Lam) == new Color(0.15f, 0.55f, 1f), "palette: Lam");
        Check(UiTheme.TierColor(TavernTier.Tim) == new Color(0.85f, 0.25f, 0.95f), "palette: Tim");
        Check(UiTheme.TierColor(TavernTier.Cam) == new Color(1f, 0.6f, 0.1f), "palette: Cam");
        Check(UiTheme.TierColor(TavernTier.Do) == new Color(0.90f, 0.15f, 0.15f), "palette: Do fallback");
        Check(UiTheme.SlotColors.Count == 6, "palette: 6 slot colors");
        Check(UiTheme.SlotColors["weapon"] == new Color(0.65f, 0.25f, 0.20f), "palette: weapon color kept");
        Check(UiTheme.SlotEmpty == new Color(0.12f, 0.12f, 0.16f), "palette: SlotEmpty");
        Check(UiTheme.HpColor == new Color(0.25f, 0.75f, 0.30f), "palette: HpColor");
        Check(UiTheme.BarBackground == new Color(0.15f, 0.15f, 0.20f), "palette: BarBackground");
        Check(UiTheme.FontTitle == 28 && UiTheme.FontBody == 16 && UiTheme.FontSmall == 13
            && UiTheme.FontButton == 17 && UiTheme.FontSmallButton == 15, "font scale constants");
        Check(UiTheme.IconGridSize == 64 && UiTheme.IconEquippedSize == 56
            && UiTheme.FormationSlotSize == 96 && UiTheme.TierDotSize == 20
            && UiTheme.TabWidth == 160 && UiTheme.ButtonHeight == 56
            && UiTheme.PopupCloseSize == 40, "size constants");
    }

    private void AssetIconsChecks()
    {
        var symbols = new[] { "weapon", "armor", "helmet", "boots", "cloak", "bracer" }
            .Select(AssetIcons.SlotSymbol).ToList();
        Check(symbols.All(s => s != "" && s != "?"), "slot symbols non-empty");
        Check(symbols.Distinct().Count() == 6, "slot symbols distinct");
        Check(AssetIcons.PortraitChar("luu_bi") == "Bị", "portrait char luu_bi");
        Check(AssetIcons.PortraitChar("quan_vu") == "Vũ", "portrait char quan_vu");
        Check(AssetIcons.PortraitChar("truong_phi") == "Phi", "portrait char truong_phi");
        Check(AssetIcons.PortraitChar("trieu_van") == "Vân", "portrait char trieu_van");
        Check(AssetIcons.PortraitChar("dieu_thuyen") == "Thuyền", "portrait char dieu_thuyen");
        Check(AssetIcons.PortraitChar("gia_cat_luong") == "Lượng", "portrait char gia_cat_luong");
        Check(AssetIcons.PortraitChar("lu_bo") == "Bố", "portrait char lu_bo");
        Check(AssetIcons.PortraitChar("custom_hero") == "T", "portrait char custom (tester227 -> T)");
        Check(AssetIcons.PortraitBorderColor("quan_vu") == UiTheme.TierColor(TavernTier.Tim), "portrait border Tim");
        Check(AssetIcons.PortraitBorderColor("luu_bi") == UiTheme.TierColor(TavernTier.Lam), "portrait border Lam");
        Check(AssetIcons.PortraitBorderColor("custom_hero") == UiTheme.Gold, "portrait border custom = gold");
        var goldCoin = AssetIcons.MakeCoinIcon(true, 24);
        Check(goldCoin.CustomMinimumSize == new Vector2(24, 24), "gold coin size");
        Check(goldCoin.GetChildren().OfType<Label>().FirstOrDefault()?.Text == "V", "gold coin letter V");
        var silverCoin = AssetIcons.MakeCoinIcon(false, 24);
        Check(silverCoin.GetChildren().OfType<Label>().FirstOrDefault()?.Text == "B", "silver coin letter B");
        var portrait = AssetIcons.MakePortrait("quan_vu", new Vector2(260, 180));
        Check(portrait.CustomMinimumSize == new Vector2(260, 180), "portrait square 260x180");
        Check(portrait.GetNode<Label>("PortraitCharLabel").Text == "Vũ", "portrait char label Vũ");
    }

    private void HeroListPortraitChecks(GameManager gm)
    {
        var scene = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroListScene.tscn");
        var heroList = scene.Instantiate<HeroListScene>();
        AddChild(heroList);
        var row = heroList.GetNode<HBoxContainer>("DetailBox/PortraitRow");
        var portrait = heroList.GetNode<ColorRect>("DetailBox/PortraitRow/DetailPortrait");
        Check(row.GetChildren().IndexOf(portrait) == 1, "portrait at row index 1 (2.16 layout kept)");
        Check(portrait.Color == UiTheme.Panel, "portrait bg is panel color");
        var charLabel = portrait.GetNodeOrNull<Label>("PortraitCharLabel");
        Check(charLabel?.Text == "T", "portrait shows selected hero char T");
        var ring = portrait.GetNodeOrNull<PanelContainer>("PortraitRing");
        Check((ring?.GetThemeStylebox("panel") as StyleBoxFlat)?.BorderColor == UiTheme.Gold,
            "portrait ring is gold for the tierless custom hero");
        // Equipment grid symbol: grant a weapon item, then build a fresh
        // scene (RebuildAll is private and tab switches rebuild deferred).
        gm.Save.BagEquipment.Add(new BagEquipmentItem { SlotId = "weapon", Level = 1, RefineIndex = 0 });
        var heroList2 = scene.Instantiate<HeroListScene>();
        AddChild(heroList2);
        var grid = heroList2.GetNode<GridContainer>("GridPanel/GridScroll/EquipmentGrid");
        var symbolLabel = grid.GetChildren().OfType<Button>()
            .Select(b => b.GetNodeOrNull<Label>("IconSymbolLabel"))
            .FirstOrDefault(l => l != null && l.Text == "VK");
        Check(symbolLabel != null, "grid weapon icon shows VK symbol");
    }

    private void FormationRingChecks(GameManager gm)
    {
        gm.Save.OwnedHeroIds.Add("quan_vu");
        string err;
        Check(FormationService.TryPlace(gm.Save, "quan_vu", "hau_top", null, out err), "2.27: place quan_vu hau_top");
        var scene = ResourceLoader.Load<PackedScene>("res://features/formation/scenes/FormationScene.tscn");
        var formation = scene.Instantiate<FormationScene>();
        AddChild(formation);
        var slotBtn = FindDescendant<FormationSlotButton>(formation)
            .FirstOrDefault(b => b.HeroId == "quan_vu");
        Check(slotBtn != null, "formation slot button for quan_vu");
        var style = slotBtn?.GetThemeStylebox("normal") as StyleBoxFlat;
        Check(style != null && style.BorderColor == UiTheme.TierColor(TavernTier.Tim), "formation ring is Tim color");
    }

    private void HubAvatarChecks(GameManager gm)
    {
        var scene = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn");
        var hub = scene.Instantiate<HubScene>();
        AddChild(hub);
        var portrait = hub.GetNode<ColorRect>("TopBar/AvatarPanel/AvatarBox/AvatarPortrait");
        var charLabel = portrait.GetNodeOrNull<Label>("AvatarPortraitChar");
        Check(charLabel != null, "hub avatar has portrait char label");
        Check(charLabel?.Text == "T", "hub avatar char T for tester227");
        var heroLabel = hub.GetNode<Label>("TopBar/AvatarPanel/AvatarBox/AvatarHeroLabel");
        Check(heroLabel.Text.Contains("tester227"), "hub hero label shows custom hero");
        // Currency label still text (kept), with Bạc shown and no KNB.
        var cur = hub.GetNode<Label>("TopBar/AvatarPanel/AvatarBox/AvatarCurrencyLabel").Text;
        Check(cur.Contains("Bạc") && !cur.Contains("KNB"), "hub currency text Bạc, no KNB");
    }

    private void TavernCurrencyChecks(GameManager gm)
    {
        var scene = ResourceLoader.Load<PackedScene>("res://features/recruit/scenes/RecruitScene.tscn");
        var tav = scene.Instantiate<RecruitScene>();
        AddChild(tav);
        var box = tav.GetNodeOrNull<HBoxContainer>("CurrencyBox");
        Check(box != null, "tavern has CurrencyBox");
        var coins = box?.GetChildren().OfType<PanelContainer>().ToList() ?? new();
        Check(coins.Count == 2, "CurrencyBox has 2 coin icons");
        Check(tav.GetNode<Label>("CurrencyBox/GoldLabel").Text.Contains("100.000"), "tavern gold label");
        Check(tav.GetNode<Label>("CurrencyBox/SilverLabel").Text.Contains("1.000.000"), "tavern silver label");
        Check(tav.GetNode<Label>("CurrencyBox/LevelLabel").Text.Contains("cấp 70"), "tavern level label");
    }
}
