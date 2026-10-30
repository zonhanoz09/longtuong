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
    // Phase 2.33: gem pouch (gemId -> count).
    public Dictionary<string, int> GemInventory { get; set; } = new();
    // Phase 2.34: forge — learned blueprints + materials.
    public Dictionary<string, bool> Blueprints { get; set; } = new();
    public Dictionary<string, int> Materials { get; set; } = new();
    // Phase 2.39: last battle (stage + seed, so it can be replayed) and
    // the daily sweep counter (Quét ải, reset by local date).
    public BattleRecord? LastBattle { get; set; }
    public string SweepDate { get; set; } = "";
    public int SweepUsed { get; set; } = 0;
    // Phase 2.45: dungeon progress. "<dungeon>:<normal|hard>" -> stages cleared
    // in the current run (0..3, back to 0 after the last stage), and
    // "<dungeon>:<diff>:done" -> 1 once the whole dungeon was ever cleared
    // (Khó opens after Thường is done).
    public Dictionary<string, int> DungeonProgress { get; set; } = new();
    // Phase 2.47: Chiến tích (merit) currency + Ải Vượt Tướng highest floor
    // cleared (kept forever, no daily reset).
    public int Merit { get; set; } = 0;
    public int DuelFloor { get; set; } = 0;
    // Phase 2.49: Thí Luyện Tháp highest floor cleared (kept forever).
    public int TrialFloor { get; set; } = 0;
    // Phase 2.50: Tổ Đội bosses beaten (bossId -> times won). Boss n+1
    // opens once boss n was beaten.
    public Dictionary<string, int> PartyCleared { get; set; } = new();
}

public class BattleRecord
{
    // Phase 2.45: "Campaign" for old saves (the field is missing there).
    public string Mode { get; set; } = "Campaign";
    public string ModeKey { get; set; } = "";
    public int StageId { get; set; }
    public int Seed { get; set; }
    public bool Won { get; set; }
    public int Stars { get; set; }
    public List<string> TeamIds { get; set; } = new();
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
