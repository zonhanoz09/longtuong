using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

// First art pass: original procedural silhouettes, no external textures required.
public partial class BattleActor : Node2D
{
    public Fighter Fighter { get; set; } = null!;
    public bool Current { get; set; }
    public bool? FacingRightOverride { get; set; }
    public bool FacesRight => FacingRightOverride ?? Fighter.IsPlayer;
    public float Motion { get; set; }
    public BattleArt Art => BattleArtCatalog.For(Fighter.Def, Fighter.IsPlayer);
    public Color Accent => Art.Accent;
    public Texture2D? BodyTexture { get; private set; }
    private readonly Dictionary<string, FrameClip?> _clips = new();
    public string Pose { get; private set; } = "idle";
    public double PoseTime { get; private set; }
    public bool DisplayAlive { get; set; } = true;
    public Vector2 Recoil { get; set; }
    private Vector2 _castAnchor, _hitAnchor;
    private FrameClip? _aura;
    private double _auraTime;
    public FrameClip? PoseClip(string name) => _clips.GetValueOrDefault(name)
        ?? (name == "ultimate" ? _clips.GetValueOrDefault("attack") : null);
    public Texture2D? FrameTexture => PoseClip(Pose)?.Sample(PoseTime)
        ?? (Pose == "idle" ? null : BodyTexture)
        ?? _clips.GetValueOrDefault("idle")?.Sample(0) ?? BodyTexture;
    public int FrameIndex => PoseClip(Pose)?.FrameIndex(PoseTime) ?? 0;
    public Vector2 FramePivot => PoseClip(Pose)?.Pivot(PoseTime)
        ?? ((Pose == "idle" || BodyTexture == null) ? _clips.GetValueOrDefault("idle")?.Pivot(0) : null)
        ?? new Vector2(0.5f, 460f/512f);
    private float PoseScale => PoseClip(Pose)?.DisplayScale ?? ((BodyTexture == null || Pose == "idle") ? _clips.GetValueOrDefault("idle")?.DisplayScale ?? 1 : 1);
    // Per-frame author pivots align atlas poses without altering the artwork.
    public Rect2 BodyDrawRect => FrameTexture == null ? default : new Rect2(
        -FrameTexture.GetWidth() * 112f * PoseScale / FrameTexture.GetHeight() * FramePivot.X,
        -112f * PoseScale * FramePivot.Y,
        FrameTexture.GetWidth() * 112f * PoseScale / FrameTexture.GetHeight(), 112f * PoseScale);

    public override void _Ready()
    {
        TextureFilter = TextureFilterEnum.Linear;
        BodyTexture = AssetCatalog.Body(Fighter.Def.Id);
        foreach (var name in new[] { "idle", "attack", "ultimate", "hit", "death" })
            _clips[name] = AssetCatalog.Clip(Fighter.Def.Id, name);
        _aura = AssetCatalog.Effect(Fighter.Def.Id, "aura");
        _castAnchor = AssetCatalog.Anchor(Fighter.Def.Id, "cast");
        _hitAnchor = AssetCatalog.Anchor(Fighter.Def.Id, "hit");
        DisplayAlive = Fighter.Alive;
        QueueRedraw();
    }

    public void SamplePose(string pose, double time)
    {
        Pose = pose;
        PoseTime = System.Math.Max(0, time);
        QueueRedraw();
    }
    public void Idle()
    {
        Recoil = Vector2.Zero;
        if (DisplayAlive && Pose != "idle") SamplePose("idle", 0);
    }
    public void SampleAura(double time)
    {
        _auraTime = System.Math.Max(0, time);
        QueueRedraw();
    }
    public void AdvanceIdle(double delta)
    {
        _auraTime += delta;
        if (Pose == "idle") SamplePose("idle", PoseTime + delta);
    }
    public Vector2 AnchorWorld(string name)
    {
        var point = name == "cast" ? _castAnchor : _hitAnchor;
        var texture = FrameTexture;
        float width = (texture == null ? 112 : texture.GetWidth() * 112f / texture.GetHeight()) * PoseScale;
        var local = new Vector2((point.X - FramePivot.X) * width * (FacesRight ? 1 : -1), (point.Y - FramePivot.Y) * 112 * PoseScale);
        return ToGlobal(local);
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0, new Vector2(1.4f, 0.35f));
        DrawCircle(new Vector2(0, 2), 25, UiTheme.Shadow);
        if (_aura != null && DisplayAlive)
        {
            DrawSetTransform(Vector2.Zero, 0, new Vector2(1, 0.36f));
            DrawTextureRect(_aura.Sample(_auraTime), new Rect2(-52, -52, 104, 104), false);
        }
        DrawSetTransform(Vector2.Zero, 0, new Vector2(1.4f, 0.35f));
        if (Current) DrawArc(new Vector2(0, 2), 29, 0, Mathf.Tau, 48, UiTheme.Gold, 2, true);
        DrawSetTransform(new Vector2(Motion * (FacesRight ? 1 : -1), 0) + Recoil,
            DisplayAlive || PoseClip("death") != null ? 0 : (FacesRight ? -0.9f : 0.9f),
            new Vector2(FacesRight ? 1 : -1, 1));
        if (FrameTexture != null)
        {
            var clip = PoseClip(Pose);
            var source = clip?.SourcePolygons[clip.FrameIndex(PoseTime)];
            if (source != null && FrameTexture is AtlasTexture atlas && atlas.Atlas != null)
            {
                var size = FrameTexture.GetSize();
                var points = source.Select(p => (p-atlas.Region.Position+atlas.Margin.Position)/size*BodyDrawRect.Size+BodyDrawRect.Position).ToArray();
                var uv = source.Select(p => p/atlas.Atlas.GetSize()).ToArray();
                DrawColoredPolygon(points, Colors.White, uv, atlas.Atlas);
            }
            else DrawTextureRect(FrameTexture, BodyDrawRect, false);
            DrawSetTransform(Vector2.Zero);
            return;
        }
        var c = Accent;
        // Cloak, boots, lamellar torso and shoulder plates.
        DrawColoredPolygon(new[] { new Vector2(-17,-65), new Vector2(11,-62), new Vector2(28,-5), new Vector2(-29,-5) }, c.Darkened(0.55f));
        DrawLine(new Vector2(-10,-21), new Vector2(-15,-2), UiTheme.Iron, 10, true);
        DrawLine(new Vector2(10,-21), new Vector2(17,-2), UiTheme.Iron, 10, true);
        DrawColoredPolygon(new[] { new Vector2(-16,-63), new Vector2(16,-63), new Vector2(21,-23), new Vector2(-21,-23) }, c);
        for (int y = -55; y < -26; y += 8) DrawLine(new Vector2(-15,y), new Vector2(16,y), c.Darkened(0.35f), 2, true);
        DrawLine(new Vector2(-19,-61), new Vector2(-23,-43), UiTheme.SilverCoin, 9, true);
        DrawLine(new Vector2(19,-61), new Vector2(26,-43), UiTheme.SilverCoin, 9, true);
        DrawLine(new Vector2(-19,-25), new Vector2(20,-25), UiTheme.Gold, 5, true);
        DrawCircle(new Vector2(0,-77), 12, UiTheme.Skin);
        DrawArc(new Vector2(0,-79), 13, Mathf.Pi, Mathf.Tau, 20, c.Darkened(0.3f), 7, true);
        DrawLine(new Vector2(5,-77), new Vector2(9,-77), UiTheme.Iron, 2, true);
        if (Art.Plumes)
        {
            DrawArc(new Vector2(-16,-101), 20, -2.4f, 0.5f, 20, c, 4, true);
            DrawArc(new Vector2(14,-101), 20, -2.7f, 0.2f, 20, c, 4, true);
        }
        if (Fighter.Def.Id is "quan_vu" or "truong_phi")
            DrawColoredPolygon(new[] { new Vector2(-7,-69), new Vector2(8,-69), new Vector2(3,-46), new Vector2(-3,-50) }, UiTheme.Iron);
        if (Art.Weapon is BattleWeapon.Fan or BattleWeapon.Scroll or BattleWeapon.Lute)
        {
            DrawColoredPolygon(new[] { new Vector2(-13,-88), new Vector2(-9,-103), new Vector2(9,-103), new Vector2(13,-88) }, c.Darkened(0.6f));
            DrawColoredPolygon(new[] { new Vector2(-14,-35), new Vector2(14,-35), new Vector2(24,-3), new Vector2(-24,-3) }, c.Darkened(0.15f));
        }
        DrawWeapon(Art.Weapon, c);
        DrawSetTransform(Vector2.Zero);
    }
    private void DrawWeapon(BattleWeapon weapon, Color c)
    {
        switch (weapon)
        {
            case BattleWeapon.Fan:
                for (int i = 0; i < 7; i++) DrawLine(new Vector2(24,-40), new Vector2(9+i*5,-65), UiTheme.Text, 4, true);
                break;
            case BattleWeapon.Ribbon:
                DrawColoredPolygon(new[] { new Vector2(-12,-46), new Vector2(12,-46), new Vector2(31,-6), new Vector2(-29,-6) }, c);
                DrawArc(new Vector2(19,-50), 23, -1.5f, 1.8f, 24, c.Lightened(0.3f), 5, true);
                DrawCircle(new Vector2(-12,-84), 6, UiTheme.Gold);
                break;
            case BattleWeapon.Scroll:
                DrawRect(new Rect2(17,-60,25,27), UiTheme.GoldHover);
                DrawLine(new Vector2(17,-61), new Vector2(17,-32), UiTheme.Wood, 4, true);
                DrawLine(new Vector2(42,-61), new Vector2(42,-32), UiTheme.Wood, 4, true);
                for (int i = 0; i < 3; i++) DrawLine(new Vector2(23,-54+i*7), new Vector2(36,-54+i*7), c.Darkened(0.4f), 2, true);
                break;
            case BattleWeapon.Lute:
                DrawColoredPolygon(new[] { new Vector2(-22,-58), new Vector2(32,-50), new Vector2(29,-32), new Vector2(-25,-40) }, UiTheme.Wood);
                for (int i = 0; i < 4; i++) DrawLine(new Vector2(-22,-54+i*3), new Vector2(30,-46+i*3), UiTheme.Gold, 1, true);
                break;
            case BattleWeapon.Sword:
            case BattleWeapon.TwinSwords:
            case BattleWeapon.Knives:
                DrawBlade(new Vector2(25,-38), weapon == BattleWeapon.Knives ? 22 : 45);
                if (weapon != BattleWeapon.Sword) DrawBlade(new Vector2(-25,-35), weapon == BattleWeapon.Knives ? 22 : 38);
                break;
            default:
                DrawPolearm(28, weapon);
                if (weapon == BattleWeapon.TwinHalberds) DrawPolearm(-27, weapon);
                break;
        }
    }

    private void DrawBlade(Vector2 grip, float length)
    {
        DrawLine(grip + new Vector2(0,7), grip, UiTheme.Wood, 4, true);
        DrawLine(grip - new Vector2(7,0), grip + new Vector2(7,0), UiTheme.Gold, 3, true);
        DrawColoredPolygon(new[] { grip + new Vector2(-3,-3), grip + new Vector2(1,-length), grip + new Vector2(5,-3) }, UiTheme.SilverCoin);
    }

    private void DrawPolearm(float x, BattleWeapon weapon)
    {
        DrawLine(new Vector2(x,-8), new Vector2(x+4,-94), UiTheme.Wood, 4, true);
        if (weapon == BattleWeapon.Glaive)
            DrawArc(new Vector2(x+1,-88), 18, -1.9f, 0.5f, 24, UiTheme.SilverCoin, 9, true);
        else
        {
            DrawColoredPolygon(new[] { new Vector2(x-3,-89), new Vector2(x+5,-105), new Vector2(x+10,-88) }, UiTheme.SilverCoin);
            if (weapon is BattleWeapon.Halberd or BattleWeapon.TwinHalberds)
                DrawArc(new Vector2(x+7,-83), 11, -1.5f, 1.5f, 18, UiTheme.SilverCoin, 5, true);
        }
    }

}

public partial class BattleArena : Node2D
{
    public override void _Draw()
    {
        DrawRect(new Rect2(0, 60, 1280, 570), UiTheme.ArenaSky);
        DrawCircle(new Vector2(1000, 150), 42, UiTheme.Gold.Darkened(0.55f));
        DrawColoredPolygon(new[] { new Vector2(0,270), new Vector2(150,160), new Vector2(300,260), new Vector2(510,150), new Vector2(740,265), new Vector2(960,170), new Vector2(1280,255), new Vector2(1280,400), new Vector2(0,400) }, UiTheme.ArenaMountains);
        DrawRect(new Rect2(0,180,1280,450), UiTheme.ArenaGround);
        for (int x = 0; x < 1280; x += 45)
        {
            DrawLine(new Vector2(x,135), new Vector2(x,190), UiTheme.Wood, 8, true);
            DrawColoredPolygon(new[] { new Vector2(x-4,135), new Vector2(x,124), new Vector2(x+4,135) }, UiTheme.Wood);
        }
        DrawLine(new Vector2(0,165), new Vector2(1280,165), UiTheme.Wood.Darkened(0.2f), 5);
        foreach (int x in new[] { 65, 1215 })
        {
            DrawLine(new Vector2(x,105), new Vector2(x,285), UiTheme.Wood, 5, true);
            DrawColoredPolygon(new[] { new Vector2(x,110), new Vector2(x+54,119), new Vector2(x+46,174), new Vector2(x,166) }, x < 640 ? UiTheme.PlayerBlue.Darkened(0.45f) : UiTheme.EnemyRed.Darkened(0.45f));
            DrawLine(new Vector2(x+15,128), new Vector2(x+32,151), UiTheme.Gold, 3, true);
        }
        for (int i = 0; i < 45; i++)
        {
            float x = (i * 173) % 1280, y = 330 + (i * 67) % 275;
            DrawLine(new Vector2(x,y), new Vector2(x+12,y-2), UiTheme.ArenaDust, 1, true);
        }
        DrawArc(new Vector2(640,450), 110, 0, Mathf.Tau, 64, UiTheme.ArenaDust, 2, true);
    }
}

public partial class BattleImpactVisual : Node2D
{
    public Color Tint { get; set; }
    public float Progress { get; set; }
    public bool Healing { get; set; }
    public bool Ultimate { get; set; }
    public string HeroId { get; set; } = "";
    public BattleMotif Motif { get; set; }
    public override void _Draw()
    {
        var c = Tint;
        c.A *= 1 - Progress;
        float r = (Ultimate ? 65 : 35) * (0.4f + Progress);
        if (Ultimate && Motif == BattleMotif.Trigram)
        {
            // Eight trigrams around each actual AoE target.
            for (int i = 0; i < 8; i++)
            {
                var d = Vector2.FromAngle(i * Mathf.Tau / 8 + Progress);
                var tangent = d.Orthogonal();
                for (int j = 0; j < 3; j++)
                {
                    var p = d * (r + j * 5);
                    DrawLine(p - tangent * 7, p + tangent * 7, c, 2, true);
                }
            }
        }
        if (Ultimate && Motif == BattleMotif.Dragon)
        {
            DrawArc(new Vector2(-12, 0), r * 1.2f, -2.3f, 0.6f, 40, c, 9, true);
            DrawCircle(Vector2.FromAngle(0.6f) * r * 1.2f + new Vector2(-12, 0), 7, c);
        }
        if (Ultimate) DrawMotif(c, r);
        if (Healing)
        {
            DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 40, c, 3, true);
            for (int i = 0; i < 6; i++)
            {
                var p = Vector2.FromAngle(i * Mathf.Tau / 6) * r;
                if (HeroId == "dieu_thuyen")
                    DrawCircle(p + new Vector2(5, -5), 5, c);
                DrawLine(p - new Vector2(0,6), p + new Vector2(0,6), c, 3, true);
                DrawLine(p - new Vector2(6,0), p + new Vector2(6,0), c, 3, true);
            }
        }
        else
        {
            DrawArc(Vector2.Zero, r, -2.5f + Progress, 0.5f + Progress, 36, c, Ultimate ? 8 : 4, true);
            for (int i = 0; i < 8; i++)
            {
                var d = Vector2.FromAngle(i * Mathf.Tau / 8);
                DrawLine(d * r * 0.5f, d * r, c, 2, true);
            }
        }
    }
    private void DrawMotif(Color c, float r)
    {
        switch (Motif)
        {
            case BattleMotif.Roar:
            case BattleMotif.Water:
                for (int i = 0; i < 3; i++) DrawArc(Vector2.Zero, r + i*9, 0, Mathf.Tau, 40, c, 2, true);
                break;
            case BattleMotif.Cross:
            case BattleMotif.Raid:
                DrawLine(new Vector2(-r,-r), new Vector2(r,r), c, 6, true);
                DrawLine(new Vector2(-r,r), new Vector2(r,-r), c, Motif == BattleMotif.Cross ? 6 : 2, true);
                break;
            case BattleMotif.Thrust:
            case BattleMotif.Charge:
                for (int i = -1; i <= 1; i++) DrawLine(new Vector2(-r*1.5f,i*13), new Vector2(r,i*8), c, i == 0 ? 6 : 2, true);
                break;
            case BattleMotif.Tiger:
                for (int i = -1; i <= 1; i++) DrawArc(new Vector2(i*12,0), r, -2.6f, -0.2f, 30, c, 5, true);
                break;
            case BattleMotif.Stone:
                for (int i = 0; i < 7; i++)
                {
                    var p = Vector2.FromAngle(i * Mathf.Tau/7) * r;
                    DrawColoredPolygon(new[] { p+new Vector2(-6,4), p+new Vector2(0,-8), p+new Vector2(7,5) }, c);
                }
                break;
            case BattleMotif.Poison:
                for (int i = 0; i < 6; i++) DrawCircle(Vector2.FromAngle(i*Mathf.Tau/6 + Progress) * r*0.65f, 10+10*Progress, c);
                break;
            case BattleMotif.Fire:
            case BattleMotif.FireKnife:
                for (int i = -2; i <= 2; i++)
                {
                    float x = i*r*0.35f;
                    DrawColoredPolygon(new[] { new Vector2(x-10,25), new Vector2(x+5,-r*(0.6f+0.1f*i*i)), new Vector2(x+13,25) }, c);
                }
                break;
        }
    }

}


public partial class BattleProjectile : Node2D
{
    public Color Tint { get; set; }
    public BattleWeapon Weapon { get; set; }
    public Vector2 Direction { get; set; }
    public FrameClip? Clip { get; set; }
    public double Time { get; set; }
    public override void _Draw()
    {
        var d = Direction.Normalized();
        if (Clip != null)
        {
            var texture = Clip.Sample(Time, true);
            DrawSetTransform(Vector2.Zero, d.Angle());
            float width = 40f * texture.GetWidth() / texture.GetHeight();
            var pivot = Clip.Pivot(Time);
            DrawTextureRect(texture, new Rect2(-width*pivot.X, -40*pivot.Y, width, 40), false);
            DrawSetTransform(Vector2.Zero);
            return;
        }
        DrawLine(-d*30, Vector2.Zero, Tint.Darkened(0.35f), 4, true);
        DrawCircle(Vector2.Zero, 6, Tint);
        if (Weapon == BattleWeapon.Knives)
            DrawColoredPolygon(new[] { d*14, -d*6+d.Orthogonal()*5, -d*6-d.Orthogonal()*5 }, UiTheme.SilverCoin);
        else
            DrawArc(Vector2.Zero, 11, 0, Mathf.Tau, 24, Tint, 2, true);
    }
}


public partial class BattleFrameEffect : Node2D
{
    public FrameClip Clip { get; set; } = null!;
    public double Time { get; set; }
    public override void _Draw()
    {
        var texture = Clip.Sample(Time);
        float width = 100f * texture.GetWidth() / texture.GetHeight();
        var pivot = Clip.Pivot(Time);
        DrawTextureRect(texture, new Rect2(-width*pivot.X, -100*pivot.Y, width, 100), false);
    }
}
