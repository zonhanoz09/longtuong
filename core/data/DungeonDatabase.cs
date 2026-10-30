using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

public enum BattleMode { Campaign, Dungeon, Duel, Trial, Party }
public enum DungeonDifficulty { Normal, Hard }

public class DungeonStageDef
{
    public string Name { get; set; } = "";
    public List<string> EnemyIds { get; set; } = new();
    public string? BossId { get; set; }
}

public class DungeonDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int MinLevel { get; set; }
    public double BaseScale { get; set; }
    /// Set whose fragments this dungeon drops (the set of the NEXT level band).
    public string FragmentSetId { get; set; } = "";
    public List<DungeonStageDef> Stages { get; set; } = new();
}

/// <summary>
/// Phase 2.45: Phó Bản Trang Bị — 5 dungeons by level band, 4 stages each
/// (the last is the boss), Thường and Khó. The main source of set
/// fragments (also: Chiến tích shop, Tổ Đội). All numbers are Pax setup
/// (Huynh tunes freely): Scale = BaseScale + 0.1 × stage index (Khó × 1.5),
/// fragments 1/1/1/3 per stage (Khó × 2), +2.000 Bạc per win.
/// Phase 2.52 balance: BaseScale 4.1/5.5/7.4/9.7/12.2 so a team at the
/// dungeon's level beats the Thường boss in the 2.52 simulation and a team
/// 20 levels below does not (was 2.6/3.8/5.0/6.2/7.4 — far too easy).
/// </summary>
public static class DungeonDatabase
{
    public const int StageCount = 4;
    public const double HardMultiplier = 1.5;

    private static DungeonStageDef S(string name, string enemies, string? boss = null) =>
        new() { Name = name, EnemyIds = enemies.Split(',').ToList(), BossId = boss };

    public static readonly List<DungeonDef> All = new()
    {
        new()
        {
            Id = "co_mo", Name = "Cổ Mộ Hán Trung", MinLevel = 30, BaseScale = 4.1, FragmentSetId = "chu_tuoc",
            Stages =
            {
                S("Mộ Đạo Khăn Vàng", "hc_binh,truong_luong,hc_binh"),
                S("Hầm Phù Thủy", "hc_binh,truong_bao,hc_binh,vu_cat"),
                S("Quan Tài Đá", "hc_binh,truong_luong,truong_bao,vu_cat,hc_binh"),
                S("Đại Hiền Lương Sư", "hc_binh,truong_luong,truong_bao,vu_cat,truong_giac", "truong_giac"),
            },
        },
        new()
        {
            Id = "hung_no", Name = "Bắc Phạt Hung Nô", MinLevel = 50, BaseScale = 5.5, FragmentSetId = "bach_ho",
            Stages =
            {
                S("Thảo Nguyên Tiền Đồn", "hn_ky,ta_hien_vuong,hn_ky"),
                S("Doanh Trại Vu Phu La", "hn_ky,vu_phu_la,hn_ky,hn_ky"),
                S("Vương Đình Tả Hiền", "hn_ky,ta_hien_vuong,vu_phu_la,hn_ky,hn_ky"),
                S("Giải Cứu Thái Văn Cơ", "hn_ky,ta_hien_vuong,vu_phu_la,hn_ky,thien_vu", "thien_vu"),
            },
        },
        new()
        {
            Id = "xich_bich", Name = "Trận Chiến Xích Bích", MinLevel = 70, BaseScale = 7.4, FragmentSetId = "thanh_long",
            Stages =
            {
                S("Thủy Trại Giang Bắc", "thuy_quan,tao_nhan,thuy_quan"),
                S("Liên Hoàn Chiến Thuyền", "tao_binh,truong_lieu,tao_binh,thuy_quan"),
                S("Hỏa Công Đêm Gió Đông", "tao_binh,tao_nhan,hua_chu,thuy_quan,tao_binh"),
                S("Tào Tháo Bại Tẩu", "tao_binh,tao_nhan,truong_lieu,hua_chu,tao_thao", "tao_thao"),
            },
        },
        new()
        {
            Id = "xuyen_thuc", Name = "Quyết Chiến Xuyên Thục", MinLevel = 90, BaseScale = 9.7, FragmentSetId = "huyen_vu",
            Stages =
            {
                S("Cửa Ải Ba Quận", "thuc_binh,nghiem_nhan,thuc_binh"),
                S("Lạc Phượng Pha", "thuc_binh,truong_nhiem,thuc_binh,thuc_binh"),
                S("Mưu Sĩ Phản Phúc", "thuc_binh,nghiem_nhan,truong_nhiem,phap_chinh,thuc_binh"),
                S("Quyết Chiến Hán Trung", "thuc_binh,nghiem_nhan,truong_nhiem,thuc_binh,phap_chinh", "phap_chinh"),
            },
        },
        new()
        {
            Id = "kinh_chau", Name = "Kinh Châu Tranh Đoạt", MinLevel = 100, BaseScale = 12.2, FragmentSetId = "ky_lan",
            Stages =
            {
                S("Giang Lăng Phòng Tuyến", "thuy_quan,luc_ton,thuy_quan"),
                S("Mạch Thành", "thuy_quan,lu_mong,thuy_quan,tao_binh"),
                S("Bạch Y Độ Giang", "thuy_quan,luc_ton,lu_mong,thuy_quan,tao_binh"),
                S("Di Lăng Chi Chiến", "thuy_quan,luc_ton,lu_mong,thuy_quan,quan_vu", "quan_vu"),
            },
        },
    };

    public static DungeonDef Get(string id) => All.First(d => d.Id == id);
}
