using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.20: stage select no longer picks a team of its own — the battle
/// team is exactly the formation set in Bày trận ("bày như thế nào thì vào
/// trận như vậy"). This popup only picks the stage and shows the formation
/// team (with each hero's position) as read-only info. To change the team,
/// close this popup and open Bày trận.
/// Phase 2.39: chapter tabs (Phase 2.41: 8 chapters, own stage counts), reward line (1.000 Bạc + EXP
/// + 10% drops) and Quét ải for 3-star stages (daily limit).
/// </summary>
[GlobalClass]
public partial class StageSelect : Control
{
    private readonly List<string> _selected = new();
    private int _selectedStage = 1;
    private int _selectedChapter = 1;
    private Label? _infoLabel;
    private Button? _sweepButton;
    private static readonly System.Random _sweepRng = new();

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        FormationService.EnsureFormation(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        if (!UiTheme.IsInHubPopup(this)) UiTheme.AddCurrencyBar(this, gm);
        UiTheme.StyleButton(GetNode<Button>("StartButton"), true);
        _sweepButton = GetNode<Button>("SweepButton");
        UiTheme.StyleButton(_sweepButton, false);
        // The battle team is the formation team (main hero always included).
        _selected.AddRange(FormationService.FormationToTeam(gm.Save.Formation)
            .Where(id => gm.Save.OwnedHeroIds.Contains(id)));
        if (!_selected.Contains(FormationService.MainHeroId) && gm.Save.OwnedHeroIds.Contains(FormationService.MainHeroId))
            _selected.Insert(0, FormationService.MainHeroId);
        gm.SelectedTeamIds = new List<string>(_selected);
        // Default: the remembered stage if unlocked, else the next stage to clear.
        int maxUnlocked = System.Math.Min(gm.Save.LastStage + 1, HeroDatabase.Stages.Count);
        _selectedStage = System.Math.Clamp(gm.CurrentStageId, 1, maxUnlocked);
        _selectedChapter = HeroDatabase.GetStage(_selectedStage).Chapter;
        _infoLabel = GetNode<Label>("InfoLabel");
        BuildTeamLabels();
        BuildStageButtons();
        RefreshInfo();
    }

    private void BuildTeamLabels()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var box = GetNode<VBoxContainer>("HeroBox");
        foreach (var heroId in _selected)
        {
            var hero = HeroDatabase.Get(heroId);
            var pos = FormationService.PositionOfHero(gm.Save, heroId);
            var label = new Label
            {
                Text = $"{hero.Name} — {FormationService.PositionName(pos)}" +
                       (FormationService.IsMain(heroId) ? " (Main)" : ""),
            };
            UiTheme.StyleBody(label, UiTheme.FontBody);
            box.AddChild(label);
        }
        if (_selected.Count == 0)
        {
            var label = new Label { Text = "Chưa có đội hình — hãy vào Bày trận trước." };
            UiTheme.StyleBody(label, UiTheme.FontBody);
            box.AddChild(label);
        }
    }

    private void BuildStageButtons()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var box = GetNode<VBoxContainer>("StageBox");
        foreach (var child in box.GetChildren()) { box.RemoveChild(child); child.QueueFree(); }
        // Phase 2.41: the longest chapter has 12 stages — tighter rows keep it in the column.
        box.AddThemeConstantOverride("separation", 2);
        // Chapter tabs. Phase 2.41: 8 chapters with their own stage ranges,
        // laid out 4 x 2 so they fit the 580px stage column; the chapter
        // name shows in the tooltip and in the header under the tabs.
        var tabs = new GridContainer { Name = "ChapterTabs", Columns = 4 };
        tabs.AddThemeConstantOverride("h_separation", 6);
        tabs.AddThemeConstantOverride("v_separation", 6);
        foreach (var chapterDef in HeroDatabase.Chapters)
        {
            int chapter = chapterDef.Number;
            bool chapterOpen = chapterDef.FirstStage <= gm.Save.LastStage + 1;
            var tab = new Button
            {
                Name = $"Chapter_{chapter}",
                Text = $"Chương {chapter}{(chapterOpen ? "" : " (KHÓA)")}",
                TooltipText = chapterDef.Name,
                Disabled = !chapterOpen,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            UiTheme.StyleButton(tab, chapter == _selectedChapter);
            tab.CustomMinimumSize = new Vector2(0, 40);
            tab.Pressed += () => { _selectedChapter = chapter; CallDeferred(nameof(BuildStageButtons)); };
            tabs.AddChild(tab);
        }
        box.AddChild(tabs);
        var header = new Label { Name = "ChapterHeader", Text = $"Chương {_selectedChapter}: {HeroDatabase.Chapters[_selectedChapter - 1].Name}" };
        UiTheme.StyleBody(header, UiTheme.FontBody);
        box.AddChild(header);
        foreach (var stage in HeroDatabase.Stages.Where(s => s.Chapter == _selectedChapter))
        {
            bool unlocked = stage.Id <= gm.Save.LastStage + 1;
            gm.Save.StageStars.TryGetValue(stage.Id.ToString(), out var stars);
            var btn = new Button
            {
                Name = $"Stage_{stage.Id}",
                Text = $"Ải {stage.Id}: {stage.Name} — {stage.EnemyIds.Count} địch — Sao: {stars}/3{(unlocked ? "" : " (KHÓA)")}",
                Disabled = !unlocked,
                ToggleMode = true,
                ButtonPressed = stage.Id == _selectedStage,
            };
            var sid = stage.Id;
            btn.Pressed += () =>
            {
                if (!btn.ButtonPressed) { btn.ButtonPressed = true; return; }
                SelectStage(sid);
                foreach (var child in box.GetChildren().OfType<Button>()) if (child != btn) child.ButtonPressed = false;
            };
            box.AddChild(btn);
        }
    }

    public void SelectChapter(int chapter)
    {
        _selectedChapter = chapter;
        BuildStageButtons();
    }

    public void SelectStage(int stageId)
    {
        _selectedStage = stageId;
        RefreshInfo();
    }

    private void RefreshInfo(string? note = null)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var names = _selected.Select(id => HeroDatabase.Get(id).Name).ToList();
        var stage = HeroDatabase.GetStage(_selectedStage);
        int sweepsLeft = StageRewardService.SweepsLeft(gm.Save);
        bool canSweep = StageRewardService.CanSweep(gm.Save, _selectedStage);
        if (_sweepButton != null)
        {
            _sweepButton.Text = $"Quét ải ({sweepsLeft}/{StageRewardService.SweepsPerDay})";
            _sweepButton.Disabled = !canSweep || sweepsLeft <= 0 || _selected.Count == 0;
            _sweepButton.TooltipText = canSweep ? "" : "Cần 3 sao ở ải này để quét.";
        }
        if (_infoLabel != null)
            _infoLabel.Text =
                $"Đội hình từ Bày trận ({_selected.Count}/5): {string.Join(", ", names)} | Ải {_selectedStage}: {stage.Name}" +
                $" | Thưởng thắng: {StageRewardService.SilverPerWin:N0} Bạc + {StageRewardService.ExpPerHero(_selectedStage)} EXP mỗi tướng" +
                $", {StageRewardService.DropChance:P0} rơi Khoáng thạch / Đá tẩy luyện / Ngọc Lv1" +
                (note != null ? $" | {note}" : "");
    }

    private void OnStartPressed()
    {
        if (_selected.Count < 1 || _selected.Count > FormationService.MaxTeamSize)
        {
            RefreshInfo("Chưa có đội hình hợp lệ — hãy vào Bày trận trước!");
            return;
        }
        var gm = GetNode<GameManager>("/root/GameManager");
        int seed = (int)(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 1000000) + _selectedStage * 7919;
        gm.StartBattle(_selectedStage, new List<string>(_selected), seed);
        gm.SaveGame();
        GetTree().ChangeSceneToFile("res://features/battle/scenes/BattleScene.tscn");
    }

    private void OnSweepPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        var result = StageRewardService.TrySweep(gm.Save, _selected, _selectedStage, _sweepRng, out var reward);
        string note = result switch
        {
            StageRewardService.SweepResult.Success => $"Quét ải {_selectedStage}: {StageRewardService.Describe(reward!)}",
            StageRewardService.SweepResult.NotThreeStars => "Cần 3 sao ở ải này để quét!",
            StageRewardService.SweepResult.NoSweepsLeft => "Hết lượt quét hôm nay!",
            _ => "Chưa có đội hình — hãy vào Bày trận trước!",
        };
        if (result == StageRewardService.SweepResult.Success) gm.SaveGame();
        RefreshInfo(note);
    }
}
