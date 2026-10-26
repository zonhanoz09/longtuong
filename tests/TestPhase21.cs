using Godot;
using System;
using System.IO;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class TestPhase21 : Node
{
    private int _failures;

    public override void _Ready()
    {
        CheckAuth();
        CheckSavePerAccount();
        CheckScenesLoad();
        GD.Print(_failures == 0 ? "TEST PHASE2.1: ALL PASS" : $"TEST PHASE2.1: {_failures} FAILURES");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool cond, string name)
    {
        if (cond) GD.Print($"PASS: {name}");
        else { GD.PrintErr($"FAIL: {name}"); _failures++; }
    }

    private void CheckAuth()
    {
        var path = Path.Combine(OS.GetUserDataDir(), "test_phase21_accounts.json");
        try
        {
            if (File.Exists(path)) File.Delete(path);
            Check(!LocalAuth.Register(path, "ab", "1234", out _), "register rejects short username");
            Check(!LocalAuth.Register(path, "player1", "123", out _), "register rejects short password");
            Check(LocalAuth.Register(path, "player1", "matkhau123", out _), "register succeeds");
            Check(!LocalAuth.Register(path, "player1", "khac123", out _), "register rejects duplicate username");
            Check(!LocalAuth.Login(path, "player1", "sai123"), "login fails with wrong password");
            Check(!LocalAuth.Login(path, "nobody", "matkhau123"), "login fails for unknown user");
            Check(LocalAuth.Login(path, "player1", "matkhau123"), "login succeeds with correct password");
            Check(LocalAuth.Register(path, "player2", "matkhau123", out _), "second user registers");
            var accounts = LocalAuth.LoadAccounts(path);
            var h1 = accounts.First(a => a.Username == "player1").PasswordHash;
            var h2 = accounts.First(a => a.Username == "player2").PasswordHash;
            Check(h1 != h2, "same password => different hashes (per-user salt)");
            var content = File.ReadAllText(path);
            Check(!content.Contains("matkhau123"), "accounts file never contains plaintext password");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private void CheckSavePerAccount()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.CurrentUsername = "";
        Check(gm.SavePath.EndsWith("/save.json"), "guest save path is save.json");
        gm.CurrentUsername = "player1";
        Check(gm.SavePath.EndsWith("/save_player1.json"), "logged-in save path is per-account");
        gm.CurrentUsername = "a/b";
        Check(gm.SavePath.EndsWith("/save_a_b.json"), "username sanitized in save path");
        gm.CurrentUsername = "";
    }

    private void CheckScenesLoad()
    {
        string[] scenes =
        {
            "res://features/auth/scenes/AuthScene.tscn",
            "res://features/hub/scenes/HubScene.tscn",
            "res://features/hero/scenes/HeroListScene.tscn",
            "res://features/bag/scenes/BagScene.tscn",
            "res://features/formation/scenes/FormationScene.tscn",
            "res://features/recruit/scenes/RecruitScene.tscn",
        };
        foreach (var s in scenes)
            Check(ResourceLoader.Load<PackedScene>(s) != null, $"scene loads: {s}");
    }
}
