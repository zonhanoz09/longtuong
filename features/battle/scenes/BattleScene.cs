using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

// One hero cell on the battlefield: name + HP bar + rage bar. The cell's
// border turns gold for the current actor; dead heroes are greyed out.
public partial class BattleHeroCell : PanelContainer
{
    public Label NameLabel { get; private set; } = null!;
    public ProgressBar HpBar { get; private set; } = null!;
    public ProgressBar RageBar { get; private set; } = null!;

    public BattleHeroCell()
    {
        CustomMinimumSize = new Vector2(140, 64);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 2);
        NameLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        NameLabel.AddThemeFontSizeOverride("font_size", 16);
        NameLabel.AddThemeColorOverride("font_color", UiTheme.Text);
        HpBar = MakeBar(new Color(0.25f, 0.75f, 0.30f), new Vector2(132, 14));
        RageBar = MakeBar(new Color(0.20f, 0.60f, 1.00f), new Vector2(132, 10));
        box.AddChild(NameLabel);
        box.AddChild(HpBar);
        box.AddChild(RageBar);
        AddChild(box);
        SetBorder(false);
    }

    private static ProgressBar MakeBar(Color fill, Vector2 size)
    {
        var bar = new ProgressBar
        {
            MinValue = 0,
            ShowPercentage = false,
            CustomMinimumSize = size,
            SizeFlagsHorizontal = SizeFlags.Fill,
        };
        bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = fill });
        bar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0.15f, 0.15f, 0.20f) });
        return bar;
    }

    public void SetBorder(bool current)
    {
        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = UiTheme.Panel,
            BorderColor = current ? UiTheme.Gold : UiTheme.Muted,
            BorderWidthLeft = current ? 3 : 1,
            BorderWidthRight = current ? 3 : 1,
            BorderWidthTop = current ? 3 : 1,
            BorderWidthBottom = current ? 3 : 1,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 4, ContentMarginRight = 4,
            ContentMarginTop = 4, ContentMarginBottom = 4,
        });
    }

    public void Refresh(Fighter f, bool isCurrent)
    {
        NameLabel.Text = f.Name;
        HpBar.MaxValue = f.MaxHp;
        HpBar.Value = f.Hp;
        RageBar.MaxValue = 100;
        RageBar.Value = f.Rage;
        Modulate = f.Alive ? Colors.White : new Color(0.5f, 0.5f, 0.5f);
        SetBorder(isCurrent && f.Alive);
    }
}

/// <summary>
/// Phase 2.21 battle screen ("ĐẤU TRƯỜNG") from Huynh's sketch.
/// Fully automatic: the battle runs turn by turn for viewing (one action
/// per tick, ~1s at x1), ultimates auto-cast at 100 rage, and the only
/// button during battle is Tăng tốc (x1 → x2 → x3 → x1). No battle log.
/// Left: the player's formation grid (Hậu / Trung / Tiền columns, 7 slots,
/// empty slots blank); right: the enemy grid mirrored (Tiền closest to the
/// middle), with a temporary enemy layout (Tiền giữa + Trung trên +
/// Trung dưới) until enemy formations are updated in a later phase.
/// A "Thú cưỡi" placeholder circle sits above each side (no mount system
/// yet). When the battle ends, a result popup appears (Thắng/Thua trận);
/// the win popup shows stars, the reward text and 5 empty reward-item
/// squares (no item drops yet); Thoát returns to the Hub. Rewards are
/// applied immediately on win, the popup is display-only.
/// </summary>
[GlobalClass]
public partial class BattleScene : Control
{
    private BattleEngine? _engine;
    private readonly Dictionary<Fighter, BattleHeroCell> _cells = new();
    private Label? _turnLabel;
    private Button? _speedButton;
    private Timer? _timer;
    private int _speed = 1;
    private bool _finished;
    private Control? _resultPopup;

    // Temporary enemy layout (visual only; enemies have no engine positions
    // yet): first enemy to Tiền giữa, then Trung trên, Trung dưới, then any
    // extra enemies to the remaining slots in this order.
    private static readonly string[] EnemySlotOrder =
        { "tien_mid", "trung_top", "trung_bot", "trung_mid", "hau_mid", "hau_top", "hau_bot" };

    // Enemy grid cells in row-major order, columns = Tiền / Trung / Hậu
    // (mirrored vs the player); null = no Tiền slot on top/bottom rows.
    private static readonly string?[] EnemyGridOrder =
        { null, "trung_top", "hau_top", "tien_mid", "trung_mid", "hau_mid", null, "trung_bot", "hau_bot" };

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        _turnLabel = GetNode<Label>("TurnLabel");
        UiTheme.StyleBody(_turnLabel, 17);
        _speedButton = GetNode<Button>("SpeedButton");
        UiTheme.StyleButton(_speedButton, true);
        StyleMount(GetNode<PanelContainer>("PlayerMount"));
        StyleMount(GetNode<PanelContainer>("EnemyMount"));

        var stage = HeroDatabase.GetStage(gm.CurrentStageId);
        var effectiveTeam = gm.SelectedTeamIds.Select(id => ProgressionService.GetEffectiveHero(gm.Save, id)).ToList();
        var positions = gm.SelectedTeamIds.Select(id => FormationService.PositionOfHero(gm.Save, id)).ToList();
        _engine = new BattleEngine(effectiveTeam, positions, stage, gm.CurrentSeed);

        BuildPlayerGrid(gm);
        BuildEnemyGrid();

        _timer = new Timer { WaitTime = 1.0, OneShot = false, Autostart = false };
        _timer.Timeout += OnBattleTick;
        AddChild(_timer);
        _timer.Start();
        Refresh();
    }

    private static void StyleMount(PanelContainer mount)
    {
        mount.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.12f, 0.16f),
            BorderColor = UiTheme.Muted,
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 48, CornerRadiusTopRight = 48,
            CornerRadiusBottomLeft = 48, CornerRadiusBottomRight = 48,
        });
        var label = mount.GetNode<Label>("MountLabel");
        UiTheme.StyleBody(label, 15);
    }

    private void BuildPlayerGrid(GameManager gm)
    {
        var grid = GetNode<GridContainer>("PlayerGrid");
        var byId = _engine!.PlayerFighters.ToDictionary(f => f.Def.Id);
        var formation = gm.Save.Formation ?? new Dictionary<string, string>();
        foreach (var cell in FormationService.GridOrder)
        {
            if (cell == null || !formation.TryGetValue(cell, out var heroId)
                || string.IsNullOrEmpty(heroId) || !byId.TryGetValue(heroId, out var fighter))
            {
                grid.AddChild(new Control { CustomMinimumSize = new Vector2(140, 64) });
                continue;
            }
            var heroCell = new BattleHeroCell();
            _cells[fighter] = heroCell;
            grid.AddChild(heroCell);
        }
    }

    private void BuildEnemyGrid()
    {
        var grid = GetNode<GridContainer>("EnemyGrid");
        var placed = new Dictionary<string, Fighter>();
        for (int i = 0; i < _engine!.EnemyFighters.Count && i < EnemySlotOrder.Length; i++)
            placed[EnemySlotOrder[i]] = _engine.EnemyFighters[i];
        foreach (var cell in EnemyGridOrder)
        {
            if (cell == null || !placed.TryGetValue(cell, out var fighter))
            {
                grid.AddChild(new Control { CustomMinimumSize = new Vector2(140, 64) });
                continue;
            }
            var heroCell = new BattleHeroCell();
            _cells[fighter] = heroCell;
            grid.AddChild(heroCell);
        }
    }

    private void Refresh()
    {
        if (_engine == null) return;
        var current = _engine.Current;
        foreach (var (fighter, cell) in _cells) cell.Refresh(fighter, current == fighter);
        if (_turnLabel != null)
            _turnLabel.Text = _engine.IsOver ? "Trận đã kết thúc."
                : current == null ? "—"
                : $"Lượt: {current.Name} ({(current.IsPlayer ? "Ta" : "Địch")}) — Nộ {current.Rage}/100";
    }

    // One visible turn: the current actor acts (ultimate if rage is full),
    // then the engine advances to the next actor. Shared by the timer tick
    // and by tests (which drive the battle without waiting for real time).
    public void StepBattle()
    {
        if (_engine == null || _engine.IsOver || _finished) return;
        var cur = _engine.Current;
        if (cur == null) return;
        if (cur.Rage >= 100) _engine.ActCurrentUltimate(); else _engine.ActCurrentNormal();
        Refresh();
        FinishIfOver();
    }

    private void OnBattleTick() => StepBattle();

    private void OnSpeedPressed()
    {
        if (_finished) return;
        _speed = _speed % 3 + 1;
        if (_speedButton != null) _speedButton.Text = $"Tăng tốc x{_speed}";
        if (_timer != null)
        {
            _timer.WaitTime = 1.0 / _speed;
            _timer.Stop();
            _timer.Start();
        }
    }

    private void FinishIfOver()
    {
        if (_engine == null || !_engine.IsOver || _finished) return;
        _finished = true;
        _timer?.Stop();
        if (_speedButton != null) _speedButton.Disabled = true;
        var gm = GetNode<GameManager>("/root/GameManager");
        var stage = HeroDatabase.GetStage(gm.CurrentStageId);
        int stars = _engine.Stars();
        int rewardGold = _engine.PlayerWon ? stage.RewardGold : 0;
        int expPerHero = 40 + gm.CurrentStageId * 10;
        // Rewards are applied immediately on win; the popup is display-only.
        gm.ApplyBattleResult(_engine.PlayerWon, stars, rewardGold);
        ShowResultPopup(_engine.PlayerWon, stars, rewardGold, expPerHero);
        Refresh();
    }

    private void ShowResultPopup(bool won, int stars, int gold, int expPerHero)
    {
        bool win = won;
        var popup = new Control { Name = "ResultPopup" };
        int pw = 560, ph = win ? 400 : 280;
        popup.SetAnchorsPreset(Control.LayoutPreset.Center);
        popup.OffsetLeft = -pw / 2.0f;
        popup.OffsetTop = -ph / 2.0f;
        popup.OffsetRight = pw / 2.0f;
        popup.OffsetBottom = ph / 2.0f;
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = UiTheme.Panel,
            BorderColor = UiTheme.Gold,
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            ContentMarginLeft = 20, ContentMarginRight = 20,
            ContentMarginTop = 20, ContentMarginBottom = 20,
        });
        var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        vbox.AddThemeConstantOverride("separation", 12);
        var title = new Label
        {
            Text = win ? "Thắng trận" : "Thua trận",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.AddThemeColorOverride("font_color", win ? UiTheme.Gold : UiTheme.EnemyRed);
        vbox.AddChild(title);
        if (win)
        {
            var starsLabel = new Label { Text = $"Sao: {stars}/3", HorizontalAlignment = HorizontalAlignment.Center };
            UiTheme.StyleBody(starsLabel, 18);
            vbox.AddChild(starsLabel);
            var rewardLabel = new Label
            {
                Text = $"Thưởng: +{gold} Vàng, +{expPerHero} EXP mỗi tướng",
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            UiTheme.StyleBody(rewardLabel, 16);
            vbox.AddChild(rewardLabel);
            var header = new Label { Text = "Phần thưởng", HorizontalAlignment = HorizontalAlignment.Center };
            UiTheme.StyleBody(header, 18);
            vbox.AddChild(header);
            var squares = new HBoxContainer { Name = "RewardSquares", Alignment = BoxContainer.AlignmentMode.Center };
            squares.AddThemeConstantOverride("separation", 12);
            for (int i = 0; i < 5; i++)
            {
                var square = new PanelContainer { CustomMinimumSize = new Vector2(64, 64) };
                square.AddThemeStyleboxOverride("panel", new StyleBoxFlat
                {
                    BgColor = new Color(0.12f, 0.12f, 0.16f),
                    BorderColor = UiTheme.Muted,
                    BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
                    CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
                    CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
                });
                squares.AddChild(square);
            }
            vbox.AddChild(squares);
        }
        else
        {
            var msg = new Label { Text = "Thử lại với đội hình khác.", HorizontalAlignment = HorizontalAlignment.Center };
            UiTheme.StyleBody(msg, 16);
            vbox.AddChild(msg);
        }
        var exit = new Button
        {
            Text = "Thoát",
            CustomMinimumSize = new Vector2(160, 56),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
        };
        UiTheme.StyleButton(exit, true);
        exit.Pressed += OnExitPressed;
        vbox.AddChild(exit);
        panel.AddChild(vbox);
        popup.AddChild(panel);
        AddChild(popup);
        _resultPopup = popup;
    }

    private void OnExitPressed()
    {
        GetTree().ChangeSceneToFile("res://features/hub/scenes/HubScene.tscn");
    }
}
