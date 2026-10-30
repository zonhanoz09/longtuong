using Godot;
using System.Collections.Generic;

namespace LongTuong;

/// <summary>
/// Phase 2.42: "Hoạt Động" popup — one card per campaign-gated PvE mode
/// (Phó Bản, Vượt Tướng, Thí Luyện, Tổ Đội). Locked cards are greyed out
/// with the unlock condition; an open card replaces this popup with the
/// mode's own popup in the Hub host (2.46–2.51).
/// </summary>
[GlobalClass]
public partial class ActivityScene : Control
{
    // Mode popup per feature (scene path + content size).
    public static readonly Dictionary<Feature, (string Path, Vector2 Size)> ModeScenes = new()
    {
        [Feature.Dungeon] = ("res://features/dungeon/scenes/DungeonScene.tscn", HubScene.DungeonPopupSize),
        [Feature.Duel] = ("res://features/duel/scenes/DuelTowerScene.tscn", HubScene.DuelPopupSize),
        [Feature.Trial] = ("res://features/trial/scenes/TrialTowerScene.tscn", HubScene.TrialPopupSize),
        [Feature.Party] = ("res://features/party/scenes/PartyScene.tscn", HubScene.PartyPopupSize),
    };

    public static readonly Feature[] CardOrder = { Feature.Dungeon, Feature.Duel, Feature.Trial, Feature.Party };

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        var gm = GetNode<GameManager>("/root/GameManager");
        ProgressionService.EnsureDefaults(gm.Save);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        var grid = GetNode<GridContainer>("CardGrid");
        foreach (var feature in CardOrder)
        {
            var req = FeatureUnlockService.Requirements[feature];
            bool open = FeatureUnlockService.IsUnlocked(gm.Save, feature);
            var card = new Button
            {
                Name = $"Card_{feature}",
                Text = open ? req.Name : $"{req.Name}\n(KHÓA)\n{FeatureUnlockService.LockReason(gm.Save, feature)}",
                TooltipText = open ? "" : FeatureUnlockService.LockReason(gm.Save, feature),
                Disabled = !open,
                CustomMinimumSize = new Vector2(440, 230),
            };
            UiTheme.StyleButton(card, open);
            card.CustomMinimumSize = new Vector2(440, 230);
            var f = feature;
            card.Pressed += () => OnCardPressed(f);
            grid.AddChild(card);
        }
    }

    public void OnCardPressed(Feature feature)
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (!FeatureUnlockService.IsUnlocked(gm.Save, feature)) return;
        var mode = ModeScenes[feature];
        UiTheme.FindHubScene(this)?.OpenPopup(mode.Path, mode.Size);
    }
}
