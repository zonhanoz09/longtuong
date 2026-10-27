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
    public string AccountsPath => OS.GetUserDataDir() + "/accounts.json";
    public int CurrentStageId { get; set; } = 1;
    public int CurrentSeed { get; set; }
    public bool LastBattleWon { get; private set; }
    public int LastBattleStars { get; private set; }
    public int LastBattleReward { get; private set; }

    // Save tách theo tài khoản (Phase 2.1, vẫn local; server phase sau mới tách thật).
    // Chưa đăng nhập thì dùng save.json cũ để không mất tiến trình Phase 0-2.
    public string SavePath => string.IsNullOrEmpty(CurrentUsername)
        ? OS.GetUserDataDir() + "/save.json"
        : OS.GetUserDataDir() + "/save_" + SanitizeUsername(CurrentUsername) + ".json";

    public static string SanitizeUsername(string username) =>
        new string(username.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').ToArray());

    public void Logout()
    {
        HeroDatabase.SetCustomHeroName("");
        CurrentUsername = "";
        SelectedHeroId = ""; // no account => no default hero
        Save = new SaveData();
        SelectedTeamIds = new List<string> { "quan_vu", "truong_phi", "gia_cat_luong" };
        LastBattleWon = false; LastBattleStars = 0; LastBattleReward = 0;
    }

    public void SaveGame()
    {
        Save.TeamIds = new List<string>(SelectedTeamIds);
        SaveManager.Save(SavePath, Save);
    }

    public void LoadGame()
    {
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

    public void RecordWin()
    {
        Save.Wins++;
        Save.LastStage = System.Math.Max(Save.LastStage, 1);
    }

    public void StartBattle(int stageId, List<string> teamIds, int seed)
    {
        CurrentStageId = stageId;
        SelectedTeamIds = new List<string>(teamIds);
        CurrentSeed = seed;
        LastBattleWon = false;
        LastBattleStars = 0;
        LastBattleReward = 0;
    }

    public void ApplyBattleResult(bool won, int stars, int rewardGold)
    {
        LastBattleWon = won;
        LastBattleStars = stars;
        LastBattleReward = rewardGold;
        if (!won) return;
        bool firstClear = CurrentStageId > Save.LastStage;
        Save.Wins++;
        if (firstClear) Save.LastStage = CurrentStageId;
        var key = CurrentStageId.ToString();
        if (!Save.StageStars.TryGetValue(key, out var old) || stars > old) Save.StageStars[key] = stars;
        Save.Gold += rewardGold;
        Save.Gems += 5 + (firstClear ? 20 : 0);
        foreach (var heroId in SelectedTeamIds)
            ProgressionService.AddExp(Save, heroId, 40 + CurrentStageId * 10);
        SaveGame();
    }
}
