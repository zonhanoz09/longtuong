using System.Collections.Generic;

namespace LongTuong;

public class SaveData
{
    public int Gold { get; set; } = 1000;
    // Phase 2.25: Bạc (silver) — the common ingame currency. Drinks cost
    // Bạc (150.000/300.000); Vàng stays premium (all-win drinks, etc.).
    public int Silver { get; set; }
    // One-time starter-silver grant for saves created before 2.25.
    public bool SilverSeeded { get; set; }
    public int PlayerLevel { get; set; } = 1;
    public int LastStage { get; set; } = 0;
    public int Wins { get; set; } = 0;
    public List<string> TeamIds { get; set; } = new() { "quan_vu", "truong_phi", "gia_cat_luong" };
    // Phase 2.20: formation layout, slot id -> hero id (only filled slots).
    // TeamIds is derived from this in grid order (FormationService).
    public Dictionary<string, string> Formation { get; set; } = new();
    public Dictionary<string, int> StageStars { get; set; } = new();
    public Dictionary<string, HeroProgress> Heroes { get; set; } = new();
    public List<string> OwnedHeroIds { get; set; } = new();
    // Phase 2.24: souls pooled per tier (keys "Lam"/"Tim"/"Cam").
    // Phase 2.23 stored souls per general (GeneralSouls); 2.24 discards
    // those (no migration) — old saves just start new tier pools at 0.
    public Dictionary<string, int> TierSouls { get; set; } = new();

    public Dictionary<string, Dictionary<string, EquipmentState>> HeroEquipment { get; set; } = new();
    public List<BagEquipmentItem> BagEquipment { get; set; } = new();
    public int StarNodesUnlocked { get; set; } = 0;
    // Phase 2.31a: Cây Rung Tiền — free shakes per day.
    public string MoneyTreeDate { get; set; } = "";
    public int MoneyTreeUsed { get; set; } = 0;
    // Phase 2.32: Đá tẩy luyện (refine stones).
    public int RefineStones { get; set; } = 0;
}

public class BagEquipmentItem
{
    public string SlotId { get; set; } = "";
    public int Level { get; set; } = 0;
    public int RefineIndex { get; set; } = 0;
    // Phase 2.30: equipment sets (mirrors EquipmentState).
    public int Tier { get; set; } = 30;
    public string Quality { get; set; } = "Lam";
    public string SetId { get; set; } = "tan_thu";
    public List<EquipmentSocket> Sockets { get; set; } = new();
    public List<RefineLine> RefineLines { get; set; } = new();
}
