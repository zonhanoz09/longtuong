using System.Collections.Generic;

namespace LongTuong;

public class SaveData
{
    public int Gold { get; set; } = 1000;
    public int Gems { get; set; } = 0;
    public int PlayerLevel { get; set; } = 1;
    public int LastStage { get; set; } = 0;
    public int Wins { get; set; } = 0;
    public List<string> TeamIds { get; set; } = new() { "quan_vu", "truong_phi", "gia_cat_luong" };
    public Dictionary<string, int> StageStars { get; set; } = new();
    public Dictionary<string, HeroProgress> Heroes { get; set; } = new();
    public List<string> OwnedHeroIds { get; set; } = new();
    public Dictionary<string, int> Shards { get; set; } = new();
    public Dictionary<string, Dictionary<string, EquipmentState>> HeroEquipment { get; set; } = new();
    public int StarNodesUnlocked { get; set; } = 0;
}
