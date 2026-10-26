using Godot;

namespace LongTuong;

[GlobalClass]
public partial class GameManager : Node
{
    public const string Version = "0.0.1-phase0";
    public SaveData Save { get; private set; } = new();

    public string SavePath => OS.GetUserDataDir() + "/save.json";

    public void SaveGame()
    {
        SaveManager.Save(SavePath, Save);
    }

    public void LoadGame()
    {
        Save = SaveManager.Load(SavePath);
    }

    public void RecordWin()
    {
        Save.Wins++;
        Save.LastStage = System.Math.Max(Save.LastStage, 1);
    }
}
