using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LongTuong;

public partial class TestAssetCatalog : Node
{
    private int _failed;
    private void Check(bool ok, string name)
    {
        if (ok) GD.Print("PASS: " + name);
        else { _failed++; GD.PrintErr("FAIL: " + name); }
    }
    private static IEnumerable<T> Find<T>(Node n) where T : Node => n.GetChildren()
        .SelectMany(child => (child is T match ? new[] { match } : Array.Empty<T>()).Concat(Find<T>(child)));

    private async System.Threading.Tasks.Task Capture(string path)
    {
        if (!OS.GetCmdlineUserArgs().Contains("--capture")) return;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        GetViewport().GetTexture().GetImage().SavePng(path);
    }

    public override async void _Ready()
    {
        string root = Path.Combine(GameManager.DataDir, "asset-fixtures-" + Guid.NewGuid().ToString("N"));
        try
        {
            if (OS.HasFeature("editor"))
                Check(Path.GetFullPath(AssetCatalog.LooseDirectory) == Path.GetFullPath(ProjectSettings.GlobalizePath("res://assets")), "source run resolves assets under project, not Godot executable");
            Directory.CreateDirectory(root);
            AssetCatalog.UseTestDirectory(root);
            Check(AssetCatalog.Body("quan_vu") == null && AssetCatalog.Portrait("quan_vu") == null, "missing files keep procedural fallbacks");
            Check(AssetCatalog.Body("../quan_vu") == null && AssetCatalog.Portrait("unknown") == null, "only known combatant IDs resolve paths");
            var folder = Path.Combine(root, "heroes", "quan_vu");
            Directory.CreateDirectory(folder);
            var bodyPath = Path.Combine(folder, "body.png");
            var portraitPath = Path.Combine(folder, "portrait.png");
            using (var image = Image.CreateEmpty(512, 512, false, Image.Format.Rgba8))
            {
                image.Fill(Colors.Transparent);
                image.FillRect(new Rect2I(150, 90, 110, 370), UiTheme.Jade);
                image.FillRect(new Rect2I(260, 160, 100, 300), UiTheme.EnemyRed);
                image.FillRect(new Rect2I(240, 452, 32, 8), UiTheme.Gold);
                Check(image.SavePng(bodyPath) == Error.Ok, "technical body PNG created in temporary directory");
                image.Fill(UiTheme.PlayerBlue);
                image.FillRect(new Rect2I(32, 32, 180, 448), UiTheme.Gold);
                Check(image.SavePng(portraitPath) == Error.Ok, "technical portrait PNG created in temporary directory");
            }
            Check(AssetCatalog.Icon("equipment", "weapon") == null, "missing icon uses text fallback");
            Check(AssetCatalog.Icon("../heroes", "quan_vu") == null
                && AssetCatalog.Icon("equipment", "../weapon") == null, "icon IDs cannot escape their group");
            string iconDir = Path.Combine(root, "icons", "equipment");
            Directory.CreateDirectory(iconDir);
            string iconPath = Path.Combine(iconDir, "weapon.png");
            File.Copy(bodyPath, iconPath);
            var iconHost = new PanelContainer { CustomMinimumSize = new Vector2(64, 64) };
            var iconFallback = new Label { Text = "VK" };
            iconHost.AddChild(iconFallback);
            AddChild(iconHost);
            Check(AssetIcons.ApplyIcon(iconHost, "equipment", "weapon", iconFallback)
                && !iconFallback.Visible, "PNG icon replaces placeholder");
            Check(iconHost.GetCombinedMinimumSize() == new Vector2(64, 64)
                && iconHost.GetNode<TextureRect>("AssetIcon").MouseFilter == Control.MouseFilterEnum.Ignore,
                "icon preserves container size and mouse input");
            File.Delete(iconPath);
            Check(!AssetIcons.ApplyIcon(iconHost, "equipment", "weapon", iconFallback)
                && iconFallback.Visible && !iconHost.GetNode<TextureRect>("AssetIcon").Visible,
                "deleting icon restores placeholder");
            iconHost.QueueFree();
            var body = AssetCatalog.Body("quan_vu");
            var portrait = AssetCatalog.Portrait("quan_vu");
            Check(body?.GetWidth() == 512 && portrait?.GetHeight() == 512 && !File.Exists(bodyPath + ".import"), "new PNGs load without editor import");
            Check(ReferenceEquals(body, AssetCatalog.Body("quan_vu")), "repeated actors share the same texture");
            Check(AssetCatalog.Body("luu_bi") == null, "one hero's assets do not replace another hero");
            var enemyDir = Path.Combine(root, "enemies", "hc_binh");
            Directory.CreateDirectory(enemyDir);
            File.Copy(bodyPath, Path.Combine(enemyDir, "body.png"));
            Check(AssetCatalog.Body("hc_binh") != null, "enemy-only IDs use enemies directory");

            var actor = new BattleActor { Fighter = new Fighter(HeroDatabase.Get("quan_vu"), true), Position = new Vector2(110, 140) };
            AddChild(actor);
            Check(ReferenceEquals(actor.BodyTexture, body), "battle actor uses body PNG");
            var foot = actor.BodyDrawRect.Position + actor.BodyDrawRect.Size * new Vector2(0.5f, 460f/512f);
            Check(foot.IsEqualApprox(Vector2.Zero) && actor.BodyDrawRect.Size.Y == 112, "body canvas scales to actor size with exact foot pivot");
            var enemyActor = new BattleActor { Fighter = new Fighter(HeroDatabase.Get("quan_vu"), false), Position = new Vector2(270, 140) };
            AddChild(enemyActor);
            Check(ReferenceEquals(enemyActor.BodyTexture, body), "opponent reuses body texture with renderer-controlled facing");
            var panel = AssetIcons.MakePortrait("quan_vu", new Vector2(180, 180));
            panel.Position = new Vector2(340, 20);
            AddChild(panel);
            Check(!panel.GetNode<Label>("PortraitCharLabel").Visible && panel.GetNode<TextureRect>("AssetPortrait/Image").Texture == portrait, "portrait PNG hides glyph fallback");
            Check(panel.GetCombinedMinimumSize() == new Vector2(180, 180), "large portrait does not enlarge its UI holder");
            AssetIcons.ApplyPortrait(panel, "luu_bi", panel.GetNode<Label>("PortraitCharLabel"));
            Check(panel.GetNode<Label>("PortraitCharLabel").Visible && !panel.GetNode<Control>("AssetPortrait").Visible, "switch to hero without portrait clears previous image");
            panel.QueueFree();
            actor.QueueFree();
            enemyActor.QueueFree();

            var gm = GetNode<GameManager>("/root/GameManager");
            gm.CreateSeededSave("AssetTest");
            gm.Save.OwnedHeroIds.Add("quan_vu");
            gm.SelectedHeroId = "quan_vu";
            FormationService.TryPlace(gm.Save, "quan_vu", "tien_mid", null, out _);
            var heroList = ResourceLoader.Load<PackedScene>("res://features/hero/scenes/HeroListScene.tscn").Instantiate<HeroListScene>();
            AddChild(heroList);
            var heroHolder = heroList.GetNode<ColorRect>("DetailBox/PortraitRow/DetailPortrait");
            Check(heroHolder.GetNodeOrNull<TextureRect>("AssetPortrait/Image")?.Texture == portrait, "Vo tuong displays the shared portrait");
            await Capture("/tmp/longtuong-asset-hero-ui.png");
            heroList.QueueFree();
            var hub = ResourceLoader.Load<PackedScene>("res://features/hub/scenes/HubScene.tscn").Instantiate<HubScene>();
            AddChild(hub);
            Check(hub.GetNodeOrNull<TextureRect>("TopBar/AvatarPanel/AvatarHBox/AvatarPortrait/AssetPortrait/Image")?.Texture == portrait, "Hub avatar displays the shared portrait");
            await Capture("/tmp/longtuong-asset-hub-ui.png");
            hub.QueueFree();
            var formation = ResourceLoader.Load<PackedScene>("res://features/formation/scenes/FormationScene.tscn").Instantiate<FormationScene>();
            AddChild(formation);
            var slot = Find<FormationSlotButton>(formation).Single(b => b.HeroId == "quan_vu");
            Check(slot.GetNodeOrNull<TextureRect>("AssetPortrait/Image")?.Texture == portrait && slot.GetNode<Label>("AssetPortraitCaption").Text == "Quan Vũ", "formation slot shows portrait and preserves hero name");
            await Capture("/tmp/longtuong-asset-formation-ui.png");
            Check(formation.TryRemoveHero("quan_vu"), "portrait slot still supports formation actions");
            Check(!slot.GetNode<Control>("AssetPortrait").Visible && !slot.GetNode<Label>("AssetPortraitCaption").Visible, "empty formation slot clears old portrait and caption");
            formation.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            File.WriteAllText(bodyPath, "broken PNG replacement");
            Check(ReferenceEquals(body, AssetCatalog.Body("quan_vu")), "broken replacement keeps latest good texture");
            File.Delete(bodyPath);
            Check(AssetCatalog.Body("quan_vu") == null, "deleted file restores fallback on next lookup");
            using (var image = Image.CreateEmpty(256, 256, false, Image.Format.Rgba8))
            {
                image.Fill(UiTheme.Petal);
                image.SavePng(bodyPath);
            }
            Check(AssetCatalog.Body("quan_vu")?.GetWidth() == 256, "same path can be replaced or added back without stale missing-cache");
            if (OS.GetCmdlineUserArgs().Contains("--capture"))
            {
                File.Copy(Path.Combine(enemyDir, "body.png"), bodyPath, true);
                var bg = new ColorRect { Color = UiTheme.Background, Size = new Vector2(1280,720) };
                AddChild(bg);
                var title = new Label { Text = "PNG KỸ THUẬT — KIỂM TRA ĐIỂM CHÂN / LẬT PHE / PORTRAIT", Position = new Vector2(20, 20) };
                UiTheme.StyleBody(title);
                AddChild(title);
                AddChild(new BattleActor { Fighter = new Fighter(HeroDatabase.Get("quan_vu"), true), Position = new Vector2(240,300), Scale = Vector2.One*2 });
                AddChild(new BattleActor { Fighter = new Fighter(HeroDatabase.Get("quan_vu"), false), Position = new Vector2(510,300), Scale = Vector2.One*2 });
                var sample = AssetIcons.MakePortrait("quan_vu", new Vector2(180,180));
                sample.Position = new Vector2(720,120);
                AddChild(sample);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng("/tmp/longtuong-asset-fixtures.png");
            }
        }
        catch (Exception ex) { _failed++; GD.PrintErr(ex); }
        finally
        {
            AssetCatalog.UseTestDirectory(null);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        GD.Print(_failed == 0 ? "ASSET CATALOG: ALL PASS" : $"ASSET CATALOG: {_failed} FAILURES");
        GetTree().Quit(_failed == 0 ? 0 : 1);
    }
}
