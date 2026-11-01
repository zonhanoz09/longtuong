using Godot;
using System.Linq;

namespace LongTuong;

// Visual review sheet for the full roster, using the same nodes as live battles.
public partial class PreviewBattleArtGallery : Control
{
    public override async void _Ready()
    {
        GetWindow().ContentScaleSize = new Vector2I(1280, 960);
        GetWindow().Size = new Vector2I(1280, 960);
        UiTheme.ApplyBackground(this);
        var title = new Label { Text = "LONG TƯỚNG — MẪU TƯỚNG & TUYỆT KỸ", Position = new Vector2(25, 10) };
        UiTheme.StyleTitle(title);
        AddChild(title);
        for (int i = 0; i < HeroDatabase.Heroes.Count; i++)
        {
            var hero = HeroDatabase.Heroes[i];
            var tile = new PanelContainer { Position = new Vector2(20 + i%4*315, 65 + i/4*220), Size = new Vector2(300, 205) };
            tile.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = UiTheme.Panel, CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });
            AddChild(tile);
            var canvas = new Control { MouseFilter = MouseFilterEnum.Ignore };
            tile.AddChild(canvas);
            var actor = new BattleActor { Fighter = new Fighter(hero, true), Position = new Vector2(75, 140), Scale = Vector2.One * 1.1f };
            canvas.AddChild(actor);
            canvas.AddChild(new BattleImpactVisual { Position = new Vector2(215, 85), Tint = actor.Accent, Healing = hero.UltKind == UltKind.Heal, Ultimate = true, HeroId = hero.Id, Motif = actor.Art.Motif, Progress = 0.35f });
            var name = new Label { Text = hero.Name, Position = new Vector2(12, 147) };
            UiTheme.StyleBody(name, UiTheme.FontBody);
            name.AddThemeColorOverride("font_color", actor.Accent.Lerp(UiTheme.Text, 0.3f));
            canvas.AddChild(name);
            var skill = new Label { Text = hero.UltName, Position = new Vector2(12, 173) };
            UiTheme.StyleBody(skill, UiTheme.FontSmall);
            canvas.AddChild(skill);
        }
        if (OS.GetCmdlineUserArgs().Contains("--capture"))
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var result = GetViewport().GetTexture().GetImage().SavePng("res://docs/previews/battle-art-gallery.png");
            GD.Print("Gallery capture: " + result);
            GetTree().Quit(result == Error.Ok ? 0 : 1);
        }
    }
}
