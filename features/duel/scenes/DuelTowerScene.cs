using Godot;
using System.Collections.Generic;

namespace LongTuong;

/// <summary>
/// Phase 2.47–2.48: Ải Vượt Tướng popup. Tab "Leo tầng": current floor,
/// next opponent + reward, Khiêu chiến (main hero alone, full HP). Tab
/// "Cửa hàng": spend Chiến tích (Phase 2.48, MeritShopService).
/// </summary>
[GlobalClass]
public partial class DuelTowerScene : Control
{
    public const string TabClimb = "climb", TabShop = "shop";
    private string _tab = TabClimb;
    private Label? _merit;
    private Label? _status;
    private VBoxContainer? _content;

    public string CurrentTab => _tab;

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        FormationService.EnsureFormation(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        _merit = GetNode<Label>("MeritLabel");
        _status = GetNode<Label>("StatusLabel");
        _content = GetNode<VBoxContainer>("Content");
        UiTheme.StyleBody(_merit, UiTheme.FontBody);
        _merit.AddThemeColorOverride("font_color", UiTheme.Gold);
        UiTheme.StyleBody(_status, UiTheme.FontBody);
        var climb = GetNode<Button>("TabRow/ClimbTab");
        var shop = GetNode<Button>("TabRow/ShopTab");
        climb.Pressed += () => ShowTab(TabClimb);
        shop.Pressed += () => ShowTab(TabShop);
        ShowTab(TabClimb);
    }

    public void ShowTab(string tab)
    {
        _tab = tab;
        UiTheme.StyleButton(GetNode<Button>("TabRow/ClimbTab"), tab == TabClimb);
        UiTheme.StyleButton(GetNode<Button>("TabRow/ShopTab"), tab == TabShop);
        Rebuild();
    }

    private void Rebuild()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var save = gm.Save;
        _merit!.Text = $"Chiến tích: {save.Merit:N0}  |  Bạc: {save.Silver:N0}";
        foreach (var child in _content!.GetChildren()) { _content.RemoveChild(child); child.QueueFree(); }
        if (_tab == TabClimb) BuildClimb(save); else BuildShop(save);
    }

    private Label Line(string text, int size = UiTheme.FontBody)
    {
        var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        UiTheme.StyleBody(l, size);
        _content!.AddChild(l);
        return l;
    }

    private void BuildClimb(SaveData save)
    {
        Line($"Đã vượt: {save.DuelFloor}/{DuelTowerService.MaxFloor} tầng (giữ vĩnh viễn)").Name = "FloorLabel";
        Line("Chủ tướng đơn đấu 1v1 — HP hồi đầy mỗi tầng.", UiTheme.FontSmall);
        var reason = DuelTowerService.LockReason(save);
        var btn = new Button { Name = "ChallengeButton", SizeFlagsHorizontal = SizeFlags.ShrinkBegin, CustomMinimumSize = new Vector2(280, 56) };
        if (reason == "")
        {
            int floor = DuelTowerService.NextFloor(save);
            var foe = HeroDatabase.GetCombatant(DuelTowerService.OpponentFor(floor));
            var r = DuelTowerService.RewardForFloor(floor);
            Line($"Tầng {floor}: {foe.Name} (sức mạnh x{DuelTowerService.ScaleFor(floor):0.00})").Name = "OpponentLabel";
            Line($"Thưởng lần đầu: +{r.Silver:N0} Bạc, +{r.Merit} Chiến tích").Name = "RewardLabel";
            btn.Text = $"Khiêu chiến tầng {floor}";
        }
        else
        {
            Line(reason);
            btn.Text = "Khiêu chiến";
            btn.Disabled = true;
        }
        UiTheme.StyleButton(btn, true);
        btn.Pressed += OnChallengePressed;
        _content!.AddChild(btn);
    }

    private void BuildShop(SaveData save)
    {
        Line("Đổi Chiến tích lấy nguyên liệu rèn:", UiTheme.FontSmall);
        foreach (var item in MeritShopService.Items)
        {
            var row = new HBoxContainer { Name = $"Shop_{item.Id}" };
            row.AddThemeConstantOverride("separation", 12);
            var name = new Label { Name = "ItemLabel", Text = $"{item.Name} — {item.Price} Chiến tích (có {MeritShopService.Owned(save, item.Id)})", CustomMinimumSize = new Vector2(440, 0) };
            UiTheme.StyleBody(name, UiTheme.FontBody);
            var buy = new Button { Name = "BuyButton", Text = "Đổi", Disabled = save.Merit < item.Price, CustomMinimumSize = new Vector2(100, 0) };
            UiTheme.StyleButton(buy, false);
            buy.CustomMinimumSize = new Vector2(100, 40);
            var id = item.Id;
            buy.Pressed += () => OnBuyPressed(id);
            row.AddChild(name);
            row.AddChild(buy);
            _content!.AddChild(row);
        }
    }

    public void OnBuyPressed(string itemId)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var item = MeritShopService.Get(itemId);
        bool ok = MeritShopService.TryBuy(gm.Save, itemId, 1);
        _status!.Text = ok ? $"Đã đổi 1 {item?.Name}!" : "Không đủ Chiến tích!";
        if (ok) gm.SaveGame();
        Rebuild();
    }

    /// Sets up the floor battle in GameManager. False when it cannot start.
    public bool PrepareBattle()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var reason = DuelTowerService.LockReason(gm.Save);
        if (reason != "") { _status!.Text = reason; return false; }
        int floor = DuelTowerService.NextFloor(gm.Save);
        int seed = (int)(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 1000000) + floor * 7919;
        gm.StartModeBattle(BattleMode.Duel, DuelTowerService.Key(floor), DuelTowerService.StageForFloor(floor),
            new List<string> { DuelTowerService.HeroId }, seed);
        gm.SaveGame();
        return true;
    }

    private void OnChallengePressed()
    {
        if (PrepareBattle()) GetTree().ChangeSceneToFile("res://features/battle/scenes/BattleScene.tscn");
    }
}
