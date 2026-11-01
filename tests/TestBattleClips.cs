using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LongTuong;

public partial class TestBattleClips : Node
{
    private int _failed;
    private void Check(bool ok, string name)
    {
        if (ok) GD.Print("PASS: " + name);
        else { _failed++; GD.PrintErr("FAIL: " + name); }
    }
    private static void Frames(string root, string group, string id, string type, string name, int count)
    {
        string dir = Path.Combine(root, group, id, type, name);
        Directory.CreateDirectory(dir);
        for (int i = 0; i < count; i++)
        {
            using var image = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
            image.Fill(i % 2 == 0 ? UiTheme.Jade : UiTheme.Gold);
            image.SavePng(Path.Combine(dir, $"{i:000}.png"));
        }
    }
    private (Control Host, Dictionary<Fighter, BattleHeroCell> Cells) View(BattleEngine engine)
    {
        var host = new Control { Size = new Vector2(1280, 720) };
        AddChild(host);
        var cells = new Dictionary<Fighter, BattleHeroCell>();
        int index = 0;
        foreach (var f in engine.PlayerFighters.Concat(engine.EnemyFighters))
        {
            var cell = new BattleHeroCell { Position = new Vector2(f.IsPlayer ? 100 : 800, 100 + index++ * 110) };
            host.AddChild(cell); cell.Refresh(f, engine.Current == f); cells[f] = cell;
        }
        return (host, cells);
    }
    private static void Commit(Dictionary<Fighter, BattleHeroCell> cells, FighterSnapshot s) => cells[s.Fighter].Refresh(s.Fighter, false, s.Hp, s.Rage);
    private static BattleEngine Engine(string hero, int hp = 560, int seed = 12345)
    {
        var e = new BattleEngine(new List<string> { hero }, HeroDatabase.GetStage(1), seed);
        foreach (var f in e.EnemyFighters) { f.Hp = hp; f.Def.Dodge = 0; }
        return e;
    }
    private async System.Threading.Tasks.Task Capture(string name)
    {
        if (!OS.GetCmdlineUserArgs().Contains("--capture")) return;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        GetViewport().GetTexture().GetImage().SavePng("/tmp/longtuong-clips-" + name + ".png");
    }
    public override async void _Ready()
    {
        string root = Path.Combine(GameManager.DataDir, "clip-fixtures-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            AssetCatalog.UseTestDirectory(root);
            Frames(root, "heroes", "gia_cat_luong", "animations", "idle", 3);
            Frames(root, "heroes", "gia_cat_luong", "animations", "attack", 6);
            Frames(root, "heroes", "quan_vu", "animations", "attack", 6);
            Frames(root, "enemies", "hc_binh", "animations", "hit", 4);
            Frames(root, "enemies", "hc_binh", "animations", "death", 3);
            Frames(root, "heroes", "gia_cat_luong", "effects", "normal", 2);
            Frames(root, "heroes", "gia_cat_luong", "effects", "projectile", 2);
            string config = Path.Combine(root, "heroes", "gia_cat_luong", "animation.json");
            const string validConfig = "{\"version\":1,\"clips\":{\"attack\":{\"fps\":12,\"event_frame\":2}},\"anchors\":{\"cast\":[0.7,0.4]}}";
            File.WriteAllText(config, validConfig);
            var idle = AssetCatalog.Clip("gia_cat_luong", "idle")!;
            var attack = AssetCatalog.Clip("gia_cat_luong", "attack")!;
            Check(idle.Frames.Count == 3 && idle.Fps == 8 && idle.Loop, "directory auto-registers idle without import");
            Check(idle.FrameIndex(double.MaxValue) >= 0 && idle.FrameIndex(3.0/8) == 0 && attack.FrameIndex(100) == 5, "idle loops, one-shot clamps final frame");
            Check(attack.EventFrame == 2 && Math.Abs(attack.Duration - 0.5) < 0.001, "optional event marker and FPS are applied");
            Check(ReferenceEquals(attack, AssetCatalog.Clip("gia_cat_luong", "attack")), "clip data is shared across instances");
            Check(AssetCatalog.Clip("gia_cat_luong", "unknown") == null, "unknown clip falls back");
            string frame1 = Path.Combine(root, "heroes", "gia_cat_luong", "animations", "attack", "001.png");
            var frameBytes = File.ReadAllBytes(frame1);
            File.Delete(frame1);
            Check(ReferenceEquals(attack, AssetCatalog.Clip("gia_cat_luong", "attack")), "incomplete replacement retains whole last-good clip");
            File.WriteAllBytes(frame1, frameBytes);
            using (var mismatch = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8)) mismatch.SavePng(frame1);
            Check(ReferenceEquals(attack, AssetCatalog.Clip("gia_cat_luong", "attack")), "mismatched canvas cannot replace part of a live clip");
            File.WriteAllBytes(frame1, frameBytes);
            File.WriteAllText(config, "{\"version\":1,\"clips\":{\"attack\":{\"fps\":0,\"event_frame\":999}}}");
            var defaults = AssetCatalog.Clip("gia_cat_luong", "attack")!;
            Check(defaults.Fps == 12 && defaults.EventFrame == 3, "invalid metadata uses safe defaults");
            File.WriteAllText(config, validConfig);

            var atlasDir = Path.Combine(root, "heroes", "luu_bi", "animations", "idle");
            Directory.CreateDirectory(atlasDir);
            using (var atlasImage = Image.CreateEmpty(64,32,false,Image.Format.Rgba8))
            {
                atlasImage.Fill(UiTheme.Jade);
                atlasImage.FillRect(new Rect2I(32,0,32,32),UiTheme.Gold);
                atlasImage.SavePng(Path.Combine(atlasDir,"atlas.png"));
            }
            File.WriteAllText(Path.Combine(root,"heroes","luu_bi","animation.json"),
                "{\"version\":1,\"clips\":{\"idle\":{\"sheet\":\"atlas.png\",\"columns\":2,\"rows\":1,\"frames\":2,\"canvas\":[64,64],\"regions\":[[0,0,32,32],[32,0,32,32]],\"offsets\":[[16,16],[10,20]],\"pivots\":[[0.5,0.75],[0.4,0.8]],\"display_scale\":1.1}}}");
            var atlas = AssetCatalog.Clip("luu_bi","idle")!;
            Check(atlas.Frames.Count==2 && atlas.Frames.All(f=>f.GetSize()==new Vector2(64,64)), "atlas margins preserve virtual canvas size");
            Check(((AtlasTexture)atlas.Frames[0]).Atlas==((AtlasTexture)atlas.Frames[1]).Atlas
                && ((AtlasTexture)atlas.Frames[1]).Region.Position==new Vector2(32,0), "atlas frames share one texture with correct UV regions");
            var atlasActor = new BattleActor { Fighter=new Fighter(HeroDatabase.Get("luu_bi"),true) };
            AddChild(atlasActor); atlasActor.AdvanceIdle(0.13);
            Check(atlasActor.FramePivot.IsEqualApprox(new Vector2(0.4f,0.8f))
                && (atlasActor.BodyDrawRect.Position+atlasActor.BodyDrawRect.Size*atlasActor.FramePivot).IsEqualApprox(Vector2.Zero), "per-frame pivot and display scale keep the foot at origin");
            atlasActor.QueueFree();
            Frames(root,"heroes","luu_bi","animations","idle",2);
            var replacement=AssetCatalog.Clip("luu_bi","idle")!;
            Check(replacement.Frames.Count==2&&replacement.Frames.All(f=>f is ImageTexture),"dropping numbered PNGs overrides an atlas without editing config");

            var engine = Engine("gia_cat_luong", 1);
            var (host, cells) = View(engine);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var caster = cells[engine.PlayerFighters[0]].Actor!;
            caster.AdvanceIdle(0.13);
            Check(caster.FrameIndex == 1 && caster.FrameTexture != null && caster.BodyTexture == null, "idle frames work without body.png");
            Check(ReferenceEquals(caster.PoseClip("ultimate"), caster.PoseClip("attack")), "ultimate borrows attack clip metadata");
            engine.ActCurrentNormal();
            var action = engine.LastAction!;
            var target = cells[action.Impacts[0].Target];
            int hpBefore = (int)target.HpBar.Value;
            var home = caster.Position;
            using (var playback = new BattlePresentationController(host, cells, action, s => Commit(cells, s)))
            {
                playback.Advance(playback.ReleaseAt - 0.001);
                Check(!host.GetChildren().OfType<BattleProjectile>().Any() && (int)target.HpBar.Value == hpBefore && target.Actor!.DisplayAlive,
                    "engine death stays hidden during windup");
                playback.Advance(0.002);
                var projectile = host.GetChildren().OfType<BattleProjectile>().Single();
                Check(projectile.Clip != null && (int)target.HpBar.Value == hpBefore, "release launches PNG projectile, not damage");
                await Capture("release-fixture");
                playback.Advance(playback.ImpactAt - playback.Time - 0.001);
                Check((int)target.HpBar.Value == hpBefore, "HP remains unchanged until projectile arrives");
                playback.Advance(0.002);
                Check(target.HpBar.Value == 0 && target.Actor!.Pose == "death" && !target.Actor.DisplayAlive, "impact commits death and death clip together");
                Check(host.GetChildren().OfType<BattleFrameEffect>().Count() == 1 && !host.GetChildren().OfType<BattleImpactVisual>().Any(), "PNG impact replaces procedural VFX without double effect");
                await Capture("impact-fixture");
                playback.Advance(100);
                playback.Advance(100);
                Check(playback.Completed && playback.ReleaseCount == 1 && playback.ImpactCount == 1, "large time steps cannot skip or repeat release/impact");
                Check(caster.Position.IsEqualApprox(home) && caster.Pose == "idle" && target.Actor!.Pose == "death", "finish restores actor home without resurrecting dead target");
            }
            host.QueueFree();

            // Living targets play hit; a cancellation cannot commit impact later.
            engine = Engine("gia_cat_luong");
            (host, cells) = View(engine);
            engine.ActCurrentNormal(); action = engine.LastAction!;
            target = cells[action.Impacts[0].Target];
            using (var playback = new BattlePresentationController(host, cells, action, s => Commit(cells, s)))
            {
                playback.Advance(playback.ImpactAt + 0.001);
                Check(target.Actor!.Pose == "hit", "living damaged target enters hit clip");
                playback.Advance(100);
                Check(target.Actor.Pose == "idle" && target.Actor.Recoil == Vector2.Zero, "hit recovery returns to idle and clears recoil");
            }
            host.QueueFree();
            engine = Engine("gia_cat_luong"); (host, cells) = View(engine);
            engine.ActCurrentNormal(); action = engine.LastAction!;
            using (var playback = new BattlePresentationController(host, cells, action, s => Commit(cells, s)))
            {
                playback.Advance(playback.ReleaseAt + 0.001);
                playback.Dispose(); playback.Advance(100);
                Check(playback.ImpactCount == 0 && host.GetChildren().All(n => n is not BattleProjectile || n.IsQueuedForDeletion()), "cancel owns and removes projectiles, no late impact");
            }
            host.QueueFree();

            for (int seed = 0; seed < 100; seed++)
            {
                engine = Engine("gia_cat_luong", seed: seed);
                foreach (var f in engine.EnemyFighters) f.Def.Dodge = 0.85;
                engine.ActCurrentNormal();
                if (engine.LastAction!.Impacts[0].Dodged) break;
            }
            action = engine.LastAction!; (host, cells) = View(engine);
            target = cells[action.Impacts[0].Target];
            hpBefore = (int)target.HpBar.Value;
            using (var playback = new BattlePresentationController(host, cells, action, s => Commit(cells, s)))
            {
                playback.Advance(playback.ImpactAt + 0.001);
                Check(action.Impacts[0].Dodged && target.Actor!.Pose == "idle" && target.HpBar.Value == hpBefore
                    && !host.GetChildren().OfType<BattleFrameEffect>().Any(), "dodge never plays hit or impact VFX");
                playback.Advance(100);
            }
            host.QueueFree();

            // Melee root travel is independent of sprite frame offsets.
            engine = Engine("quan_vu"); (host, cells) = View(engine);
            var melee = cells[engine.PlayerFighters[0]].Actor!;
            home = melee.Position;
            engine.ActCurrentNormal(); action = engine.LastAction!;
            using (var playback = new BattlePresentationController(host, cells, action, s => Commit(cells, s)))
            {
                playback.Advance(0.09);
                Check(melee.Position.DistanceTo(home) > 5, "melee approaches before starting attack frames");
                playback.Advance(0.2);
                Check(melee.Pose == "attack" && playback.ImpactCount == 0, "melee attack uses contact marker after approach");
                playback.Advance(100);
                Check(melee.Position.IsEqualApprox(home), "melee returns to exact home anchor");
            }
            host.QueueFree();

            engine = Engine("gia_cat_luong");
            engine.PlayerFighters[0].Rage = 100;
            (host, cells) = View(engine);
            engine.ActCurrentUltimate(); action = engine.LastAction!;
            var commits = new Dictionary<Fighter, int>();
            using (var playback = new BattlePresentationController(host, cells, action, s => { Commit(cells, s); commits[s.Fighter] = commits.GetValueOrDefault(s.Fighter) + 1; }))
            {
                playback.Advance(100);
                Check(action.Impacts.Count == 3 && action.Impacts.All(i => commits[i.Target] == 1), "AoE commits each enemy instance once");
            }
            host.QueueFree();

            engine = Engine("dieu_thuyen");
            var healer = engine.PlayerFighters[0]; healer.Hp -= 10; healer.Rage = 100;
            (host, cells) = View(engine);
            engine.ActCurrentUltimate(); action = engine.LastAction!;
            using (var playback = new BattlePresentationController(host, cells, action, s => Commit(cells, s)))
            {
                Check(playback.ReleaseAt == playback.ImpactAt, "self-heal has no artificial projectile travel");
                playback.Advance(playback.ImpactAt + 0.001);
                Check(cells[healer].Actor!.Pose != "hit" && cells[healer].HpBar.Value == healer.Hp, "self-heal never interrupts caster with hit");
                playback.Advance(100);
            }
            host.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        catch (Exception ex) { _failed++; GD.PrintErr(ex); }
        finally
        {
            AssetCatalog.UseTestDirectory(null);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        GD.Print(_failed == 0 ? "BATTLE CLIPS: ALL PASS" : $"BATTLE CLIPS: {_failed} FAILURES");
        GetTree().Quit(_failed == 0 ? 0 : 1);
    }
}
