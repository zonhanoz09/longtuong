using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.40: enemy-only combatants (common soldiers + named bosses).
/// They are NOT in HeroDatabase.Heroes, so they never enter saves, the
/// tavern pool or the Hub hero count. Battles resolve ids through
/// HeroDatabase.GetCombatant (Heroes first, then these).
/// Stats are Pax setup (Huynh tunes freely): soldiers sit near the
/// average playable hero so chapter 1–3 keep their old difficulty;
/// bosses are a bit above a playable hero and also get StageDef.BossScale.
/// </summary>
public static class EnemyDatabase
{
    private static HeroDef E(string id, string name, HeroRole role, int hp, int atk, int def, int mdef, int speed,
        double crit, double dodge, string normal, string ult, double power, UltKind kind, string passive, string desc) => new()
    {
        Id = id, Name = name, Role = role, Hp = hp, Atk = atk, Def = def, MagicDef = mdef, Speed = speed,
        Crit = crit, Dodge = dodge, NormalName = normal, UltName = ult, UltPower = power, UltKind = kind,
        PassiveName = passive, PassiveDesc = desc,
    };

    private const HeroRole V = HeroRole.Vanguard, W = HeroRole.Warrior, S = HeroRole.Strategist, P = HeroRole.Support;
    private const UltKind One = UltKind.SingleDamage, Aoe = UltKind.AoEDamage, Heal = UltKind.Heal;

    public static readonly List<HeroDef> Soldiers = new()
    {
        E("hc_binh", "Hoàng Cân binh", V, 560, 66, 36, 26, 58, 0.08, 0.05, "Giáo Gỗ", "Khăn Vàng Xung Phong", 1.5, One, "Cuồng Tín", "Lính khởi nghĩa đông đảo."),
        E("tl_binh", "Tây Lương binh", W, 520, 74, 32, 24, 70, 0.12, 0.06, "Đao Kỵ", "Thiết Kỵ Đột Kích", 1.6, One, "Kỵ Binh", "Kỵ binh Tây Lương nhanh và hung hãn."),
        E("vien_quan", "Viên quân", V, 580, 68, 40, 30, 60, 0.10, 0.05, "Trường Thương", "Đại Kích Sĩ", 1.5, One, "Tứ Thế Tam Công", "Quân Viên Thiệu đông và chắc."),
        E("tao_binh", "Tào binh", W, 540, 72, 36, 28, 66, 0.12, 0.06, "Hổ Báo Kỵ", "Hổ Báo Xung Trận", 1.6, One, "Tinh Nhuệ", "Quân Tào kỷ luật."),
        E("thuy_quan", "Thủy quân", S, 480, 76, 28, 38, 64, 0.10, 0.06, "Cung Thuyền", "Hỏa Tiễn Liên Châu", 1.2, Aoe, "Thủy Chiến", "Quân thủy bắn tên lửa toàn trận."),
        E("nm_binh", "Nam Man binh", V, 620, 64, 42, 22, 56, 0.08, 0.04, "Đằng Giáp", "Mãnh Thú Xung Kích", 1.5, One, "Đằng Giáp", "Giáp mây dày, kháng đao kiếm."),
        E("thuc_binh", "Thục binh", W, 530, 72, 34, 30, 68, 0.12, 0.06, "Liên Nỏ", "Nguyên Nhung Liên Nỏ", 1.6, One, "Hiểm Địa", "Quân trấn giữ Tây Xuyên."),
        E("hn_ky", "Hung Nô kỵ", W, 540, 78, 30, 22, 78, 0.14, 0.08, "Cung Ngựa", "Thảo Nguyên Kỵ Xạ", 1.7, One, "Du Mục", "Kỵ xạ thảo nguyên rất nhanh."),
    };

    public static readonly List<HeroDef> Bosses = new()
    {
        // Loạn Hoàng Cân / Cổ Mộ Hán Trung.
        E("truong_luong", "Trương Lương", W, 600, 84, 36, 30, 66, 0.15, 0.06, "Hoàng Cân Đao", "Nhân Công Tướng Quân", 1.9, One, "Nhân Công", "Em út Trương Giác."),
        E("truong_bao", "Trương Bảo", S, 520, 88, 30, 42, 70, 0.12, 0.06, "Yêu Thuật", "Địa Công Phong Lôi", 1.3, Aoe, "Địa Công", "Gọi gió gọi sấm khắp trận."),
        E("vu_cat", "Vu Cát", P, 500, 60, 30, 46, 72, 0.10, 0.10, "Phù Thủy", "Thần Tiên Hồi Sinh", 0.30, Heal, "Tiên Nhân", "Đạo sĩ chữa bệnh bằng phù thủy."),
        E("truong_giac", "Trương Giác", S, 640, 92, 34, 48, 72, 0.12, 0.06, "Thái Bình Thuật", "Thiên Công Lôi Phạt", 1.4, Aoe, "Thiên Công", "Đại Hiền Lương Sư, thủ lĩnh Khăn Vàng."),
        // Thảo phạt Đổng Trác.
        E("hoa_hung", "Hoa Hùng", W, 640, 90, 38, 26, 66, 0.18, 0.05, "Đại Đao", "Trảm Tướng Tị Thủy", 2.0, One, "Hãn Tướng", "Mãnh tướng trấn giữ Tị Thủy Quan."),
        E("ly_giac", "Lý Giác", W, 600, 84, 34, 26, 70, 0.15, 0.06, "Tây Lương Đao", "Loạn Trường An", 1.9, One, "Tàn Bạo", "Bộ tướng Đổng Trác."),
        E("quach_di", "Quách Dĩ", V, 660, 76, 44, 30, 62, 0.12, 0.05, "Trường Mâu", "Thiết Kỵ Vây Thành", 1.7, One, "Ngoan Cố", "Bộ tướng Đổng Trác."),
        E("dong_trac", "Đổng Trác", V, 760, 84, 46, 36, 54, 0.12, 0.04, "Bạo Chính", "Thiêu Lạc Dương", 1.3, Aoe, "Gian Hùng Tây Lương", "Quyền thần thao túng triều Hán."),
        // Quan Độ.
        E("nhan_luong", "Nhan Lương", W, 640, 92, 36, 26, 68, 0.18, 0.05, "Hà Bắc Đao", "Hà Bắc Đệ Nhất", 2.0, One, "Dũng Quán Tam Quân", "Danh tướng Hà Bắc."),
        E("van_xu", "Văn Xú", W, 620, 90, 34, 26, 72, 0.18, 0.06, "Thiết Thương", "Diên Tân Tử Chiến", 2.0, One, "Mãnh Tướng", "Danh tướng Hà Bắc."),
        E("vien_thieu", "Viên Thiệu", V, 720, 82, 44, 38, 60, 0.12, 0.05, "Bảo Kiếm", "Tứ Thế Tam Công", 1.3, Aoe, "Minh Chủ", "Minh chủ liên quân Hà Bắc."),
        // Tân Dã / Trường Bản / Xích Bích.
        E("ha_hau_don", "Hạ Hầu Đôn", W, 640, 90, 38, 28, 70, 0.16, 0.05, "Độc Nhãn Thương", "Bạt Thỉ Đạm Tinh", 2.0, One, "Cương Liệt", "Đại tướng thân tín của Tào Tháo."),
        E("hua_chu", "Hứa Chử", V, 780, 80, 50, 30, 56, 0.12, 0.04, "Thiết Chùy", "Hổ Si Khỏa Y", 1.8, One, "Hổ Si", "Cận vệ dũng mãnh của Tào Tháo."),
        E("tao_nhan", "Tào Nhân", V, 740, 78, 52, 36, 58, 0.10, 0.05, "Thiết Thuẫn", "Bát Môn Kim Tỏa", 1.7, One, "Thủ Thành", "Tướng phòng thủ giỏi nhất nhà Tào."),
        E("thai_mao", "Thái Mạo", S, 560, 84, 32, 40, 66, 0.12, 0.06, "Thủy Kỳ", "Kinh Châu Thủy Trận", 1.3, Aoe, "Thủy Đốc", "Đô đốc thủy quân Kinh Châu."),
        E("tao_thao", "Tào Tháo", S, 720, 94, 40, 48, 70, 0.14, 0.06, "Ỷ Thiên Kiếm", "Gian Hùng Loạn Thế", 1.5, Aoe, "Gian Hùng", "Thà ta phụ người thiên hạ."),
        // Tây Xuyên / Nam Man / Hán Trung.
        E("nghiem_nhan", "Nghiêm Nhan", W, 640, 86, 40, 30, 64, 0.16, 0.05, "Lão Đao", "Lão Tướng Ba Quận", 1.9, One, "Bất Khuất", "Lão tướng Ba Quận."),
        E("truong_nhiem", "Trương Nhiệm", W, 620, 90, 36, 30, 72, 0.18, 0.07, "Thương Kích", "Lạc Phượng Pha", 2.0, One, "Trung Liệt", "Danh tướng Tây Xuyên."),
        E("phap_chinh", "Pháp Chính", S, 540, 94, 30, 46, 74, 0.12, 0.06, "Mưu Kế", "Kỳ Mưu Định Quân", 1.4, Aoe, "Kỳ Mưu", "Mưu sĩ nhìn xa trông rộng."),
        E("manh_hoach", "Mạnh Hoạch", V, 800, 82, 48, 30, 56, 0.12, 0.04, "Đại Phủ", "Nam Man Vương Nộ", 1.8, One, "Nam Man Vương", "Vua Nam Man bảy lần bị bắt."),
        E("ha_hau_uyen", "Hạ Hầu Uyên", W, 620, 92, 34, 28, 82, 0.18, 0.08, "Cấp Hành Đao", "Tam Nhật Ngũ Bách", 2.0, One, "Thần Tốc", "Tướng hành quân thần tốc."),
        E("tu_hoang", "Từ Hoảng", W, 660, 88, 40, 30, 66, 0.16, 0.05, "Đại Phủ", "Phá Vây Phàn Thành", 1.9, One, "Chu Á Phu", "Danh tướng kỷ luật nhà Ngụy."),
        // Bắc Phạt Hung Nô.
        E("ta_hien_vuong", "Tả Hiền Vương", W, 640, 90, 36, 26, 76, 0.16, 0.07, "Loan Đao", "Thảo Nguyên Bá Chủ", 1.9, One, "Hiền Vương", "Vương Hung Nô giữ Thái Văn Cơ."),
        E("vu_phu_la", "Vu Phu La", V, 700, 82, 44, 30, 66, 0.12, 0.05, "Lang Nha Bổng", "Thiết Kỵ Hung Nô", 1.8, One, "Thiền Vu Tử", "Hoàng tử Nam Hung Nô."),
        E("thien_vu", "Thiền Vu", S, 680, 92, 36, 44, 70, 0.14, 0.06, "Vương Cung", "Vạn Mã Bôn Đằng", 1.4, Aoe, "Thiền Vu", "Chúa tể thảo nguyên."),
        // Kinh Châu / Di Lăng.
        E("luc_ton", "Lục Tốn", S, 600, 98, 34, 50, 76, 0.14, 0.07, "Thư Kiếm", "Hỏa Thiêu Liên Doanh", 1.5, Aoe, "Nho Tướng", "Đô đốc Đông Ngô, đốt 700 dặm doanh trại."),
        E("lu_mong", "Lữ Mông", W, 660, 92, 40, 34, 70, 0.16, 0.06, "Bạch Y Đao", "Bạch Y Độ Giang", 2.0, One, "Ngô Hạ A Mông", "Mặc áo trắng qua sông đánh úp Kinh Châu."),
    };

    public static readonly List<HeroDef> All = Soldiers.Concat(Bosses).ToList();

    public static HeroDef? Find(string id) => All.FirstOrDefault(e => e.Id == id);
}
