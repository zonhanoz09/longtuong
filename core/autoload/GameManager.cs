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
    public int CurrentStageId { get; set; } = 1;
    public int CurrentSeed { get; set; }
    public bool LastBattleWon { get; private set; }
    public int LastBattleStars { get; private set; }
    public int LastBattleReward { get; private set; }

    public string SavePath => OS.GetUserDataDir() + "/save.json";

    public void SaveGame()
    {
        Save.TeamIds = new List<string>(SelectedTeamIds);
        SaveManager.Save(SavePath, Save);
    }

    public void LoadGame()
    {
        Save = SaveManager.Load(SavePath);
        if (Save.TeamIds != null && Save.TeamIds.Count == 3) SelectedTeamIds = new List<string>(Save.TeamIds);
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
        Save.Wins++;
        if (CurrentStageId > Save.LastStage) Save.LastStage = CurrentStageId;
        var key = CurrentStageId.ToString();
        if (!Save.StageStars.TryGetValue(key, out var old) || stars > old) Save.StageStars[key] = stars;
        Save.Gold += rewardGold;
        SaveGame();
    }
}
