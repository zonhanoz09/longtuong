using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>Acceptance for the actual delivered pack, not temporary dummy frames.</summary>
public partial class TestGiaCatLuongAssets : Node
{
    private int _failed;
    private void Check(bool ok, string name)
    {
        if (ok) GD.Print("PASS: " + name);
        else { _failed++; GD.PrintErr("FAIL: " + name); }
    }
    private (Control Host, Dictionary<Fighter,BattleHeroCell> Cells) View(BattleEngine engine)
    {
        var host=new Control(); AddChild(host);
        var cells=new Dictionary<Fighter,BattleHeroCell>();
        int i=0;
        foreach(var fighter in engine.PlayerFighters.Concat(engine.EnemyFighters))
        {
            var cell=new BattleHeroCell {Position=new Vector2(fighter.IsPlayer?100:800,80+100*i++)};
            host.AddChild(cell);cell.Refresh(fighter,false);cells[fighter]=cell;
        }
        return(host,cells);
    }
    private static void Commit(Dictionary<Fighter,BattleHeroCell> cells,FighterSnapshot snapshot)=>cells[snapshot.Fighter].Refresh(snapshot.Fighter,false,snapshot.Hp,snapshot.Rage);
    private static BattleEngine Engine(int hp=420)
    {
        var e=new BattleEngine(new List<string>{"gia_cat_luong"},HeroDatabase.GetStage(1),12345);
        e.PlayerFighters[0].Hp=hp;
        e.PlayerFighters[0].Def.Dodge=0;
        foreach(var enemy in e.EnemyFighters)enemy.Def.Dodge=0;
        return e;
    }
    public override async void _Ready()
    {
        try
        {
            Check(AssetCatalog.Body("gia_cat_luong")!=null&&AssetCatalog.Portrait("gia_cat_luong")!=null,"body and portrait remain available");
            foreach(var name in new[]{"idle","attack","hit","ultimate","death"})
            {
                var clip=AssetCatalog.Clip("gia_cat_luong",name);
                Check(clip?.Frames.Count==8&&clip.Frames.All(f=>f is AtlasTexture),name+" has eight real atlas poses");
                if(clip==null)continue;
                Check(clip.Frames.Cast<AtlasTexture>().Select(f=>f.Region).Distinct().Count()==8,name+" uses distinct source regions");
                Check(clip.Frames.All(f=>f.GetSize()==new Vector2(512,512)),name+" retains normalized 512px virtual canvas");
            }
            foreach(var name in new[]{"normal","ultimate","projectile","cast","aura"})
                Check(AssetCatalog.Effect("gia_cat_luong",name)?.Frames.Count==8,name+" VFX has eight frames");
            Check(AssetCatalog.Effect("gia_cat_luong","aura")?.Loop==true,"ground aura loops independently");
            var gm=GetNode<GameManager>("/root/GameManager");
            gm.CreateSeededSave("GCLFullPack");gm.Save.OwnedHeroIds.Add("gia_cat_luong");gm.SelectedHeroId="gia_cat_luong";
            var hub=ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn").Instantiate<HubScene>();
            AddChild(hub);
            var hubActor=hub.GetNode<BattleActor>("Courtyard/HubCharacter/CharacterActor");
            hub.MoveCharacterTo(new Vector2(100,180));hub.StepCharacter(0.2f);
            Check(hubActor.PoseClip("idle")?.Frames.Count==8&&hubActor.PoseTime>0&&!hubActor.FacesRight,"Hub uses real idle art and faces toward movement");
            Check(!hub.GetNode<ColorRect>("Courtyard/HubCharacter/CharacterBody").Visible,"Hub placeholder is hidden when art exists");
            if(OS.GetCmdlineUserArgs().Contains("--capture"))
            {
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var image=GetViewport().GetTexture().GetImage();
                image.SavePng("/tmp/gia_cat_luong_hub.png");
            }
            hub.QueueFree();
            var engine=Engine();var(host,cells)=View(engine);
            var mage=cells[engine.PlayerFighters[0]].Actor!;
            var home=mage.Position;
            engine.PlayerFighters[0].Rage=100;engine.ActCurrentUltimate();
            using(var playback=new BattlePresentationController(host,cells,engine.LastAction!,s=>Commit(cells,s)))
            {
                playback.Advance(playback.ReleaseAt+0.001);
                Check(mage.Pose=="ultimate"&&mage.FrameIndex==4,"real ultimate releases at extended fan pose");
                Check(host.GetChildren().OfType<BattleProjectile>().Count()==3,"ultimate launches three feather projectiles");
                playback.Advance(playback.ImpactAt-playback.Time+0.001);
                Check(host.GetChildren().OfType<BattleFrameEffect>().Count()>=4,"cast and three real trigram impact sheets are active");
                playback.Advance(100);
                Check(playback.Completed&&mage.Position.IsEqualApprox(home)&&mage.Pose=="idle","ultimate completes and returns to idle/home");
            }
            host.QueueFree();
            engine=Engine();(host,cells)=View(engine);mage=cells[engine.PlayerFighters[0]].Actor!;
            engine.EnemyFighters[0].Rage=100;engine.UseUltimate(engine.EnemyFighters[0]);
            using(var playback=new BattlePresentationController(host,cells,engine.LastAction!,s=>Commit(cells,s)))
            {
                playback.Advance(playback.ImpactAt+0.25);
                Check(mage.Pose=="hit"&&mage.FrameIndex==3,"real hurt sequence advances rather than using static body");
                playback.Advance(100);
                Check(mage.Pose=="idle"&&mage.DisplayAlive,"hurt recovers without changing living state");
            }
            host.QueueFree();
            engine=Engine(1);(host,cells)=View(engine);mage=cells[engine.PlayerFighters[0]].Actor!;
            engine.EnemyFighters[0].Rage=100;engine.UseUltimate(engine.EnemyFighters[0]);
            using(var playback=new BattlePresentationController(host,cells,engine.LastAction!,s=>Commit(cells,s)))
            {
                playback.Advance(playback.ImpactAt+0.25);
                Check(mage.Pose=="death"&&!mage.DisplayAlive,"death starts only at real impact");
                playback.Advance(100);
                Check(mage.Pose=="death"&&mage.FrameIndex==7,"death holds the fallen final pose");
            }
            host.QueueFree();
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        }
        catch(Exception ex){_failed++;GD.PrintErr(ex);}
        GD.Print(_failed==0?"GIA CAT LUONG ASSETS: ALL PASS":$"GIA CAT LUONG ASSETS: {_failed} FAILURES");
        GetTree().Quit(_failed==0?0:1);
    }
}
