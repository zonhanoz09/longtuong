using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

public partial class TestBattleVisuals : Node
{
    private int _failed;
    private void Check(bool value, string message)
    {
        if (!value) { _failed++; GD.PrintErr("FAIL: " + message); }
        else GD.Print("PASS: " + message);
    }

    public override async void _Ready()
    {
        try
        {
            Check(HeroDatabase.Heroes.All(h => BattleArtCatalog.HasHero(h.Id)), "every recruitable hero has an explicit art preset");
            Check(BattleArtCatalog.For(new HeroDef { Id = "unknown", Role = HeroRole.Strategist }, false).Ranged, "unknown strategist retains a usable ranged fallback");
            var engine = new BattleEngine(new List<string> { "quan_vu", "gia_cat_luong", "dieu_thuyen" }, HeroDatabase.GetStage(1), 12345);
            var healer = engine.PlayerFighters[2];
            var wounded = engine.PlayerFighters[0];
            wounded.Hp = wounded.MaxHp - 1;
            healer.Rage = 100;
            engine.UseUltimate(healer);
            var heal = engine.LastAction!;
            Check(heal.Actor == healer && heal.Ultimate && heal.Impacts.Single().Target == wounded, "heal targets exact wounded ally instance");
            Check(heal.Impacts[0].Amount == 1 && heal.Impacts[0].Healing, "heal displays effective HP, including overheal clamp");
            Check(heal.Before.Single(s => s.Fighter == healer).Rage == 100 && heal.After.Single(s => s.Fighter == healer).Rage == 0, "action retains rage before and after");
            var mage = engine.PlayerFighters[1];
            mage.Rage = 100;
            int foes = engine.EnemyFighters.Count(f => f.Alive);
            engine.UseUltimate(mage);
            Check(engine.LastAction!.Impacts.Count == foes, "AoE has one impact per living enemy");
            Check(heal.Before.Single(s => s.Fighter == wounded).Hp == wounded.MaxHp - 1, "previous snapshots remain unchanged after later actions");

            var duplicateStage = new StageDef { Id = 999, Name = "duplicate", EnemyIds = new List<string> { "luu_bi", "luu_bi" } };
            var duplicates = new BattleEngine(new List<string> { "gia_cat_luong" }, duplicateStage, 12);
            duplicates.PlayerFighters[0].Rage = 100;
            duplicates.UseUltimate(duplicates.PlayerFighters[0]);
            Check(duplicates.LastAction!.Impacts.Select(i => i.Target).Distinct().Count() == 2, "same hero definition keeps distinct target identities");

            var gm = GetNode<GameManager>("/root/GameManager");
            gm.CreateSeededSave("VisualTest");
            gm.Save.OwnedHeroIds.AddRange(new[] { "quan_vu", "gia_cat_luong", "dieu_thuyen" });
            FormationService.EnsureFormation(gm.Save);
            FormationService.TryPlace(gm.Save, "quan_vu", "tien_mid", null, out _);
            FormationService.TryPlace(gm.Save, "gia_cat_luong", "hau_top", null, out _);
            FormationService.TryPlace(gm.Save, "dieu_thuyen", "hau_bot", null, out _);
            var team = FormationService.FormationToTeam(gm.Save.Formation);
            gm.SelectedTeamIds = new List<string>(team);
            gm.StartBattle(1, new List<string>(team), 12345);
            var scene = ResourceLoader.Load<PackedScene>("res://features/battle/scenes/BattleScene.tscn").Instantiate<BattleScene>();
            AddChild(scene);
            Check(scene.GetNodeOrNull<BattleArena>("BattleArena") != null, "battle arena is installed");
            var cells = scene.GetNode<GridContainer>("PlayerGrid").GetChildren().OfType<BattleHeroCell>().ToArray();
            Check(cells.All(c => c.Actor != null), "all deployed heroes have procedural fallback art");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var allCells = cells.Concat(scene.GetNode<GridContainer>("EnemyGrid").GetChildren().OfType<BattleHeroCell>()).ToArray();
            Check(allCells.All(c => c.GetGlobalRect().End.Y <= scene.GetNode<Label>("TurnLabel").GlobalPosition.Y), $"hero cells fit above turn label at 720p ({allCells.Max(c => c.GetGlobalRect().End.Y)} <= {scene.GetNode<Label>("TurnLabel").GlobalPosition.Y})");
            var before = cells.Select(c => c.HpBar.Value).ToArray();
            var currentArt = allCells.Single(c => c.Actor!.Current).Actor!;
            var homes = allCells.ToDictionary(c => c.Actor!, c => c.Actor!.Position);
            scene.PlayNextAction();
            Check(scene.IsPresenting && cells.Select(c => c.HpBar.Value).SequenceEqual(before), "HP stays on previous snapshot before impact");
            Check(!currentArt.Art.Ranged || scene.GetChildren().OfType<BattleProjectile>().Any(), "ranged action launches a projectile before impact");
            scene.GetNode<Button>("SpeedButton").EmitSignal(Button.SignalName.Pressed);
            scene.GetNode<Button>("SpeedButton").EmitSignal(Button.SignalName.Pressed);
            await ToSignal(GetTree().CreateTimer(0.27), SceneTreeTimer.SignalName.Timeout);
            Check(!scene.IsPresenting, "x3 completes active animation at updated speed");
            Check(homes.All(p => p.Key.Position.IsEqualApprox(p.Value)), "actors return to exact formation anchors after travel");
            Check(scene.GetChildren().All(n => n is not BattleProjectile || n.IsQueuedForDeletion()), "projectiles finish before the next action");
            Check(scene.GetChildren().All(n => n is not BattleImpactVisual || n.IsQueuedForDeletion()), "effects are cleaned after an action");
            // Optional rendered preview, isolated test account; never touches player saves.
            if (OS.GetCmdlineUserArgs().Contains("--capture"))
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng("/tmp/longtuong-battle-preview.png");
            }
            bool popupDuringAction = false;
            for (int i = 0; i < 450 && scene.GetNodeOrNull<Control>("ResultPopup") == null; i++)
            {
                await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
                if (scene.IsPresenting && scene.GetNodeOrNull<Control>("ResultPopup") != null) popupDuringAction = true;
            }
            Check(scene.GetNodeOrNull<Control>("ResultPopup") != null && !popupDuringAction, "animated battle reaches result only after the last effect");
            scene.QueueFree();
            await ToSignal(scene, Node.SignalName.TreeExited);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            gm.StartBattle(1, new List<string> { "quan_vu" }, 12345);
            scene = ResourceLoader.Load<PackedScene>("res://features/battle/scenes/BattleScene.tscn").Instantiate<BattleScene>();
            AddChild(scene);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var melee = scene.GetNode<GridContainer>("PlayerGrid").GetChildren().OfType<BattleHeroCell>().Single().Actor!;
            var meleeHome = melee.Position;
            scene.PlayNextAction();
            await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
            Check(melee.Position.DistanceTo(meleeHome) > 5, "melee actor travels toward the actual target before impact");
            await ToSignal(GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            Check(!scene.IsPresenting && melee.Position.IsEqualApprox(meleeHome), "melee actor returns to formation after impact");
            scene.PlayNextAction();
            scene.QueueFree();
            await ToSignal(scene, Node.SignalName.TreeExited);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(!GodotObject.IsInstanceValid(scene), "leaving during animation frees the scene");
        }
        catch (Exception e) { _failed++; GD.PrintErr(e); }
        GD.Print(_failed == 0 ? "BATTLE VISUALS: ALL PASS" : $"BATTLE VISUALS: {_failed} FAILURES");
        GetTree().Quit(_failed == 0 ? 0 : 1);
    }
}
