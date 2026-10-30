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
        NameLabel.AddThemeFontSizeOverride("font_size", UiTheme.FontBody);
        NameLabel.AddThemeColorOverride("font_color", UiTheme.Text);
        HpBar = MakeBar(UiTheme.HpColor, UiTheme.HpBarSize);
        RageBar = MakeBar(UiTheme.PlayerBlue, UiTheme.RageBarSize);
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
        bar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = UiTheme.BarBackground });
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
        // Phase 2.27: hero name in the tier color (gold for tierless).
        var tier = TavernData.TierOf(f.Def.Id);
        NameLabel.AddThemeColorOverride("font_color", tier == null ? UiTheme.Gold : UiTheme.TierColor(tier.Value));
        HpBar.MaxValue = f.MaxHp;
        HpBar.Value = f.Hp;
        RageBar.MaxValue = 100;
        RageBar.Value = f.Rage;
        Modulate = f.Alive ? Colors.White : UiTheme.DeadGrey;
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
/// middle). Phase 2.39: enemies are drawn on their real engine slots
/// (StageDef.EnemySlot: Tiền giữa, Trung trên, Trung dưới, Hậu giữa,
/// Trung giữa) and the win popup shows Bạc + drops in the reward squares.
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

        if (gm.CurrentMode == BattleMode.Party)
        {
            // Phase 2.51: Tổ Đội — 3 teams in a row against one boss.
            _partyBossId = gm.CurrentModeKey;
            _partyBossMaxHp = new BattleEngine(new List<HeroDef>(), PartyService.BossStage(_partyBossId), 0).EnemyFighters[0].MaxHp;
            _partyBossHp = _partyBossMaxHp;
            StartPartyTeam(gm, 0);
        }
        else
        {
            // Phase 2.45: non-campaign modes fight their own generated stage.
            var stage = gm.CurrentMode != BattleMode.Campaign && gm.CurrentStage != null
                ? gm.CurrentStage
                : HeroDatabase.GetStage(gm.CurrentStageId);
            var effectiveTeam = gm.SelectedTeamIds.Select(id => ProgressionService.GetEffectiveHero(gm.Save, id)).ToList();
            var positions = gm.SelectedTeamIds.Select(id => FormationService.PositionOfHero(gm.Save, id)).ToList();
            _engine = new BattleEngine(effectiveTeam, positions, stage, gm.CurrentSeed);
            BuildPlayerGrid(gm.Save.Formation ?? new Dictionary<string, string>());
            BuildEnemyGrid();
        }

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
            BgColor = UiTheme.SlotEmpty,
            BorderColor = UiTheme.Muted,
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 48, CornerRadiusTopRight = 48,
            CornerRadiusBottomLeft = 48, CornerRadiusBottomRight = 48,
        });
        var label = mount.GetNode<Label>("MountLabel");
        UiTheme.StyleBody(label, 15);
    }

    // Phase 2.51: Tổ Đội state (unused in other modes).
    private string _partyBossId = "";
    private int _partyIndex;
    private int _partyBossHp, _partyBossMaxHp;
    private readonly List<PartyRun> _partyRuns = new();
    private Label? _partyLabel;
    private Label? _bossHpLabel;
    public int PartyTeamIndex => _partyIndex;
    public IReadOnlyList<PartyRun> PartyRuns => _partyRuns;

    private void StartPartyTeam(GameManager gm, int index)
    {
        _partyIndex = index;
        var team = PartyService.TeamFor(gm.Save, index, gm.CurrentSeed);
        _engine = PartyService.EngineFor(team, _partyBossId, gm.CurrentSeed, index, _partyBossHp);
        foreach (var gridName in new[] { "PlayerGrid", "EnemyGrid" })
        {
            var grid = GetNode<GridContainer>(gridName);
            foreach (var child in grid.GetChildren()) { grid.RemoveChild(child); child.QueueFree(); }
        }
        _cells.Clear();
        // The player's team stands on its formation; bots on the enemy-style slots.
        var layout = new Dictionary<string, string>();
        if (index == 0) layout = gm.Save.Formation ?? layout;
        else for (int i = 0; i < team.Heroes.Count; i++) layout[HeroDatabase.DefaultEnemySlots[i]] = team.Heroes[i].Id;
        BuildPlayerGrid(layout);
        BuildEnemyGrid();
        if (_partyLabel == null)
        {
            _partyLabel = new Label { Name = "PartyLabel", Position = new Vector2(20, 60), Size = new Vector2(400, 30) };
            UiTheme.StyleBody(_partyLabel, 18);
            _partyLabel.AddThemeColorOverride("font_color", UiTheme.Gold);
            AddChild(_partyLabel);
            _bossHpLabel = new Label { Name = "BossHpLabel", Position = new Vector2(760, 60), Size = new Vector2(500, 30), HorizontalAlignment = HorizontalAlignment.Right };
            UiTheme.StyleBody(_bossHpLabel, 18);
            AddChild(_bossHpLabel);
        }
        _partyLabel.Text = team.Label;
    }

    private void BuildPlayerGrid(Dictionary<string, string> formation)
    {
        var grid = GetNode<GridContainer>("PlayerGrid");
        var byId = _engine!.PlayerFighters.ToDictionary(f => f.Def.Id);
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
        // Phase 2.39: enemies are drawn on their real engine slots.
        for (int i = 0; i < _engine!.EnemyFighters.Count; i++)
            placed[_engine.Stage.EnemySlot(i)] = _engine.EnemyFighters[i];
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
        if (_bossHpLabel != null && _engine.EnemyFighters.Count > 0)
            _bossHpLabel.Text = $"HP boss: {_engine.EnemyFighters[0].Hp:N0} / {_partyBossMaxHp:N0}";
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
        var gm = GetNode<GameManager>("/root/GameManager");
        bool won = _engine.PlayerWon;
        if (gm.CurrentMode == BattleMode.Party)
        {
            // Phase 2.51: record this team's run; the boss HP carries over.
            int after = _engine.EnemyFighters[0].Hp;
            _partyRuns.Add(new PartyRun { Label = _partyLabel?.Text ?? "", BossHpBefore = _partyBossHp, BossHpAfter = after, Turns = _engine.TurnCount });
            _partyBossHp = after;
            won = after <= 0;
            if (!won && _partyIndex < PartyService.Teams - 1)
            {
                StartPartyTeam(gm, _partyIndex + 1);
                Refresh();
                return;
            }
        }
        _finished = true;
        _timer?.Stop();
        if (_speedButton != null) _speedButton.Disabled = true;
        int stars = gm.CurrentMode == BattleMode.Party ? (won ? 3 : 0) : _engine.Stars();
        // Rewards are applied immediately on win; the popup is display-only.
        var reward = gm.ApplyBattleResult(won, stars);
        ShowResultPopup(won, stars, reward);
        Refresh();
    }

    private void ShowResultPopup(bool won, int stars, BattleReward? reward)
    {
        bool win = won;
        var popup = new Control { Name = "ResultPopup" };
        int pw = 560, ph = (win ? 400 : 280) + (_partyRuns.Count > 0 ? 110 : 0);
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
        if (_partyRuns.Count > 0)
        {
            // Phase 2.51: damage dealt by each team to the boss.
            var dmg = new Label
            {
                Name = "PartyDamageLabel",
                Text = string.Join("\n", _partyRuns.Select(r => $"{r.Label}: {r.Damage:N0} sát thương")),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            UiTheme.StyleBody(dmg, 15);
            vbox.AddChild(dmg);
            if (win)
            {
                var chest = new Label { Name = "PartyChestLabel", Text = "Rương chiến công", HorizontalAlignment = HorizontalAlignment.Center };
                UiTheme.StyleBody(chest, 16);
                chest.AddThemeColorOverride("font_color", UiTheme.Gold);
                vbox.AddChild(chest);
            }
        }
        if (win)
        {
            var starsLabel = new Label { Text = $"Sao: {stars}/3", HorizontalAlignment = HorizontalAlignment.Center };
            UiTheme.StyleBody(starsLabel, 18);
            vbox.AddChild(starsLabel);
            var rewardLabel = new Label
            {
                Text = $"Thưởng: +{reward?.Silver ?? 0:N0} Bạc, +{reward?.ExpPerHero ?? 0} EXP mỗi tướng"
                    + ((reward?.Merit ?? 0) > 0 ? $", +{reward!.Merit} Chiến tích" : ""),
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
            // Phase 2.39: dropped items fill the first squares (gold border),
            // the rest stay empty.
            var drops = reward?.Drops ?? new List<RewardDrop>();
            for (int i = 0; i < 5; i++)
            {
                var drop = i < drops.Count ? drops[i] : null;
                var square = new PanelContainer { CustomMinimumSize = new Vector2(64, 64) };
                square.AddThemeStyleboxOverride("panel", new StyleBoxFlat
                {
                    BgColor = UiTheme.SlotEmpty,
                    BorderColor = drop != null ? UiTheme.Gold : UiTheme.Muted,
                    BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
                    CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
                    CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
                });
                if (drop != null)
                {
                    square.TooltipText = $"{drop.Name} x{drop.Count}";
                    var dropLabel = new Label
                    {
                        Name = "DropLabel",
                        Text = drop.Name,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    };
                    UiTheme.StyleBody(dropLabel, UiTheme.FontSmall);
                    square.AddChild(dropLabel);
                }
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
