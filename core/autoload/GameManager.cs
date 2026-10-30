using Godot;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

[GlobalClass]
public partial class GameManager : Node
{
    public const string Version = "0.1.0-phase1";
    public SaveData Save { get; private set; } = new();
    public List<string> SelectedTeamIds { get; set; } = new() { "quan_vu", "truong_phi", "gia_cat_luong" };
    public string CurrentUsername { get; set; } = "";
    public string SelectedHeroId { get; set; } = "";
    public string AccountsPath => DataDir + "/accounts.json";

    // Phase 2.38: test scenes (res://tests/...) get their own data folder so
    // a test account ("nhan", "huynh", ...) can never overwrite a real save.
    // The folder is wiped once per test process, so every run starts clean.
    public static bool IsTestRun =>
        OS.GetCmdlineArgs().Any(a => a.Contains("res://tests/"));
    public static string DataDir =>
        IsTestRun ? OS.GetUserDataDir() + "/tests" : OS.GetUserDataDir();

    public override void _Ready()
    {
        if (!IsTestRun) return;
        if (System.IO.Directory.Exists(DataDir)) System.IO.Directory.Delete(DataDir, true);
        System.IO.Directory.CreateDirectory(DataDir);
    }
    public int CurrentStageId { get; set; } = 1;
    public int CurrentSeed { get; set; }
    public bool LastBattleWon { get; private set; }
    public int LastBattleStars { get; private set; }
    public BattleReward? LastBattleReward { get; private set; }

    // Save tách theo tài khoản (Phase 2.1, vẫn local; server phase sau mới tách thật).
    // Chưa đăng nhập thì dùng save.json cũ để không mất tiến trình Phase 0-2.
    public string SavePath => string.IsNullOrEmpty(CurrentUsername)
        ? DataDir + "/save.json"
        : DataDir + "/save_" + SanitizeUsername(CurrentUsername) + ".json";

    public static string SanitizeUsername(string username) =>
        new string(username.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').ToArray());

    public void Logout()
    {
        HeroDatabase.SetCustomHeroName("");
        CurrentUsername = "";
        SelectedHeroId = ""; // no account => no default hero
        Save = new SaveData();
        SelectedTeamIds = new List<string> { "quan_vu", "truong_phi", "gia_cat_luong" };
        LastBattleWon = false; LastBattleStars = 0; LastBattleReward = null;
    }

    public void SaveGame()
    {
        Save.TeamIds = new List<string>(SelectedTeamIds);
        SaveManager.Save(SavePath, Save);
    }

    public void LoadGame()
    {
        // Phase 2.38 (B6): a logged-in account whose save file is missing
        // gets a fresh seeded save — never the legacy "own every hero"
        // migration that an empty SaveData would trigger.
        if (!string.IsNullOrEmpty(CurrentUsername) && !System.IO.File.Exists(SavePath))
        {
            CreateSeededSave(CurrentUsername);
            return;
        }
        Save = SaveManager.Load(SavePath);
        ProgressionService.EnsureDefaults(Save);
        if (!string.IsNullOrEmpty(CurrentUsername)) HeroDatabase.SetCustomHeroName(CurrentUsername);
        // Phase 2.20: the battle team IS the formation (migrated from TeamIds
        // for old saves, main hero forced into the Trung column).
        FormationService.EnsureFormation(Save);
        SelectedTeamIds = FormationService.FormationToTeam(Save.Formation)
            .Where(id => Save.OwnedHeroIds.Contains(id)).ToList();
        if (SelectedTeamIds.Count == 0 && Save.OwnedHeroIds.Count > 0)
            SelectedTeamIds = new List<string> { Save.OwnedHeroIds[0] };
        Save.TeamIds = new List<string>(SelectedTeamIds);
        // Rule: the default hero is always an owned hero — never a locked one.
        // (Before this fix, LoadGame left the old "quan_vu" default in place,
        // which is locked for new accounts.)
        if (!Save.OwnedHeroIds.Contains(SelectedHeroId))
            SelectedHeroId = SelectedTeamIds.Count > 0 ? SelectedTeamIds[0]
                : Save.OwnedHeroIds.Count > 0 ? Save.OwnedHeroIds[0] : "";
    }

    public void CreateSeededSave(string username)
    {
        CurrentUsername = username;
        Save = SeedService.CreateSeededSave(username);
        FormationService.EnsureFormation(Save);
        SelectedTeamIds = new List<string>(Save.TeamIds);
        SelectedHeroId = Save.TeamIds[0];
        SaveGame();
    }

    public void StartBattle(int stageId, List<string> teamIds, int seed)
    {
        CurrentStageId = stageId;
        SelectedTeamIds = new List<string>(teamIds);
        CurrentSeed = seed;
        LastBattleWon = false;
        LastBattleStars = 0;
        LastBattleReward = null;
    }

    // Phase 2.39: wins give Bạc + EXP + 10% drops (StageRewardService), no
    // Vàng. Drops come from the battle seed, and the battle (stage, seed,
    // team, result) is recorded in the save so it can be replayed.
    public BattleReward? ApplyBattleResult(bool won, int stars)
    {
        LastBattleWon = won;
        LastBattleStars = stars;
        LastBattleReward = null;
        Save.LastBattle = new BattleRecord
        {
            StageId = CurrentStageId, Seed = CurrentSeed, Won = won, Stars = stars,
            TeamIds = new List<string>(SelectedTeamIds),
        };
        if (!won) { SaveGame(); return null; }
        bool firstClear = CurrentStageId > Save.LastStage;
        Save.Wins++;
        if (firstClear) Save.LastStage = CurrentStageId;
        var key = CurrentStageId.ToString();
        if (!Save.StageStars.TryGetValue(key, out var old) || stars > old) Save.StageStars[key] = stars;
        var reward = StageRewardService.Roll(CurrentStageId, StageRewardService.DropRng(CurrentSeed));
        StageRewardService.Apply(Save, SelectedTeamIds, reward);
        LastBattleReward = reward;
        SaveGame();
        return reward;
    }
}
