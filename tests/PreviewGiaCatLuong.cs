using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LongTuong;

/// <summary>Artist review scene. Preview loops do not change battle/save state.</summary>
public partial class PreviewGiaCatLuong : Control
{
    private readonly List<(BattleActor Actor, string Pose)> _actors = new();
    private readonly List<BattleFrameEffect> _effects = new();
    private double _time;
    private bool _capture, _record, _paused;
    private int _frame;
    private bool _subscribed;
    public override void _Ready()
    {
        _capture = OS.GetCmdlineUserArgs().Contains("--capture");
        _record = OS.GetCmdlineUserArgs().Contains("--record");
        UiTheme.ApplyBackground(this);
        var hero = HeroDatabase.Get("gia_cat_luong");
        foreach (var clip in new[] { "idle", "attack", "hit", "ultimate", "death" })
            if (AssetCatalog.Clip(hero.Id, clip)?.Frames.Count != 8) { GD.PushError("Missing full Gia Cat Luong clip: " + clip); GetTree().Quit(1); return; }
        foreach (var fx in new[] { "projectile", "normal", "ultimate", "cast", "aura" })
            if (AssetCatalog.Effect(hero.Id, fx)?.Frames.Count != 8) { GD.PushError("Missing Gia Cat Luong VFX: " + fx); GetTree().Quit(1); return; }
        var portrait = AssetIcons.MakePortrait(hero.Id, new Vector2(88, 88));
        portrait.Position = new Vector2(22, 20); AddChild(portrait);
        AddText("GIA CÁT LƯỢNG — BÁT QUÁI TRẬN", new Vector2(132, 23), UiTheme.FontTitle, UiTheme.Gold);
        AddText("Chờ • Đánh thường • Trúng đòn • Tuyệt kỹ • Ngã", new Vector2(132, 65), UiTheme.FontBody, UiTheme.Text);
        var battle = new Button { Text = "Trận mẫu", Position = new Vector2(1100, 26), Size = new Vector2(150, 56) };
        UiTheme.StyleButton(battle, true); AddChild(battle);
        battle.Pressed += () => GetTree().ChangeSceneToFile("res://tests/PreviewBattle.tscn");
        var names = new[] { ("idle", "CHỜ"), ("attack", "ĐÁNH THƯỜNG"), ("hit", "TRÚNG ĐÒN"), ("ultimate", "TUYỆT KỸ"), ("death", "NGÃ") };
        for (int i = 0; i < names.Length; i++)
        {
            float x = 24 + i*248;
            var panel = new PanelContainer { Position = new Vector2(x, 136), Size = new Vector2(232, 352), MouseFilter = MouseFilterEnum.Ignore };
            panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = UiTheme.Panel, CornerRadiusTopLeft=10, CornerRadiusTopRight=10, CornerRadiusBottomLeft=10, CornerRadiusBottomRight=10 });
            AddChild(panel);
            var actor = new BattleActor { Fighter = new Fighter(hero, true), Position = new Vector2(x+116, 436), Scale = Vector2.One*2.0f };
            AddChild(actor); _actors.Add((actor, names[i].Item1));
            AddText(names[i].Item2, new Vector2(x+12, 148), UiTheme.FontBody, UiTheme.GoldHover);
        }
        AddText("HIỆU ỨNG RIÊNG", new Vector2(24, 510), UiTheme.FontHeading, UiTheme.Gold);
        var effects = new[] { ("projectile","QUẠT LỬA"), ("normal","VA CHẠM"), ("ultimate","BÁT QUÁI"), ("cast","TỤ KHÍ"), ("aura","HÀO QUANG") };
        for (int i=0; i<effects.Length; i++)
        {
            var effect = new BattleFrameEffect { Clip = AssetCatalog.Effect(hero.Id, effects[i].Item1)!, Position = new Vector2(140+i*248,606),
                Scale = i==4 ? new Vector2(1.1f,0.4f) : Vector2.One, TextureFilter=TextureFilterEnum.Linear };
            AddChild(effect); _effects.Add(effect);
            AddText(effects[i].Item2, new Vector2(62+i*248,665), UiTheme.FontSmall, UiTheme.Muted);
        }
        if (_capture || _record)
        {
            if (_record) Directory.CreateDirectory("/tmp/longtuong-gcl-frames");
            RenderingServer.FramePostDraw += OnFrameDrawn;
            _subscribed = true;
        }
        Sample(_capture ? 0.4 : 0);
    }
    private void AddText(string text, Vector2 position, int size, Color color)
    {
        var label = new Label { Text=text, Position=position, MouseFilter=MouseFilterEnum.Ignore };
        UiTheme.StyleBody(label,size); label.AddThemeColorOverride("font_color",color); AddChild(label);
    }
    public override void _Process(double delta)
    {
        if (!_paused) _time += delta;
        Sample(_record ? _frame/20.0 : _capture ? 0.4 : _time);
    }
    private void Sample(double time)
    {
        foreach (var (actor,pose) in _actors)
        {
            actor.SampleAura(time); // Deterministic preview; combat advances via its shared clock.
            double local = time % 1.0;
            actor.DisplayAlive = pose != "death";
            actor.SamplePose(pose, pose=="idle" ? time : local);
        }
        foreach (var effect in _effects) { effect.Time = time % effect.Clip.Duration; effect.QueueRedraw(); }
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventKey key && key.Pressed && !key.Echo && key.Keycode==Key.Space) _paused = !_paused;
    }
    private void OnFrameDrawn()
    {
        using var image = GetViewport().GetTexture().GetImage();
        string path = _record ? $"/tmp/longtuong-gcl-frames/{_frame:000}.png" : "/tmp/gia_cat_luong_full.png";
        if (image.SavePng(path) != Error.Ok) { GD.PrintErr("Cannot save preview " + path); GetTree().Quit(1); return; }
        _frame++;
        if (!_record || _frame>=80) { Unsubscribe(); GetTree().Quit(); }
    }
    private void Unsubscribe()
    {
        if (!_subscribed) return;
        RenderingServer.FramePostDraw -= OnFrameDrawn;
        _subscribed = false;
    }
    public override void _ExitTree() => Unsubscribe();
}
