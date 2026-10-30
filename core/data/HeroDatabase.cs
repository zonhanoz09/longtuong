using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

public enum HeroRole { Vanguard, Warrior, Strategist, Support }
public enum UltKind { SingleDamage, AoEDamage, Heal }

public class HeroDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public HeroRole Role { get; set; }
    public int Hp { get; set; }
    public int Atk { get; set; }
    // Def = Vật phòng. Phase 2.39: MagicDef = Sách phòng (vs Mưu/Phụ attacks).
    public int Def { get; set; }
    public int MagicDef { get; set; }
    public int Speed { get; set; }
    public double Crit { get; set; }
    public double Dodge { get; set; }
    public double DamageReduction { get; set; }
    // Phase 2.38 (Huynh's combat spec): Chính xác vs Né, Kiên cường vs Bạo,
    // Phá kích vs Đỡ đòn, + Sát thương bạo kích thêm. All 0 by default;
    // sources (gems/refine/sets) come later — Huyền Vũ 4 món gives Block.
    public double Hit { get; set; }
    public double CritResist { get; set; }
    public double Block { get; set; }
    public double Pierce { get; set; }
    public double CritDamage { get; set; }
    public string NormalName { get; set; } = "";
    public string UltName { get; set; } = "";
    public double UltPower { get; set; } = 2.0;
    public UltKind UltKind { get; set; } = UltKind.SingleDamage;
    public string PassiveName { get; set; } = "";
    public string PassiveDesc { get; set; } = "";

    public HeroDef WithScale(double scale)
    {
        return new HeroDef
        {
            Id = Id, Name = Name, Role = Role,
            Hp = (int)Math.Round(Hp * scale),
            Atk = (int)Math.Round(Atk * scale),
            Def = (int)Math.Round(Def * scale),
            MagicDef = (int)Math.Round(MagicDef * scale),
            Speed = Speed, Crit = Crit, Dodge = Dodge, DamageReduction = DamageReduction,
            Hit = Hit, CritResist = CritResist, Block = Block, Pierce = Pierce, CritDamage = CritDamage,
            NormalName = NormalName, UltName = UltName, UltPower = UltPower, UltKind = UltKind,
            PassiveName = PassiveName, PassiveDesc = PassiveDesc,
        };
    }
}

public class StageDef
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<string> EnemyIds { get; set; } = new();
    public double Scale { get; set; } = 1.0;
    // Phase 2.41: chapters have their own stage counts (HeroDatabase.Chapters).
    public int Chapter { get; set; } = 1;
    // Phase 2.39: enemy i stands on DefaultEnemySlots[i] (Tiền giữa first).
    public string EnemySlot(int index) => HeroDatabase.DefaultEnemySlots[index];
    // Phase 2.40: the stage boss (any enemy id) gets Scale x BossScale.
    public string? BossId { get; set; }
    public double BossScale { get; set; } = 1.3;
    public double ScaleFor(string enemyId) => enemyId == BossId ? Scale * BossScale : Scale;
    // Phase 2.50: extra HP-only multiplier for every enemy (Tổ Đội boss HP x8).
    public double EnemyHpMultiplier { get; set; } = 1.0;
}

/// Phase 2.41: a campaign chapter = a contiguous range of global stage ids.
public class ChapterDef
{
    public int Number { get; set; }
    public string Name { get; set; } = "";
    public int FirstStage { get; set; }
    public int LastStage { get; set; }
}

public static class HeroDatabase
{
    // Phase 2.39: enemy formation slots by enemy index — the layout the
    // battle screen already drew in 2.21 (Tiền giữa, Trung trên, Trung
    // dưới), extended with Hậu giữa and Trung giữa for 4–5 enemy stages.
    public static readonly string[] DefaultEnemySlots =
        { "tien_mid", "trung_top", "trung_bot", "hau_mid", "trung_mid" };

    // Phase 2.39: MagicDef (Sách phòng) values are Pax's setup — casters
    // and supports resist strategy best, Lữ Bố worst.
    public static readonly List<HeroDef> Heroes = new()
    {
        // Phase 2.10: custom hero for new accounts. Its Name is NOT fixed here:
        // it is set to the account username on seed/login via SetCustomHeroName.
        // Basic stats, no real skills yet (placeholder ult = plain 1.0x hit).
        // It is intentionally NOT in the tavern pool (see TavernData.TierOfHero).
        new HeroDef { Id = "custom_hero", Name = "Tướng Tự Tạo", Role = HeroRole.Vanguard, Hp = 500, Atk = 60, Def = 30, MagicDef = 25, Speed = 60, Crit = 0.10, Dodge = 0.05, NormalName = "Đánh thường", UltName = "Chưa có tuyệt kỹ", UltPower = 1.0, UltKind = UltKind.SingleDamage, PassiveName = "Chưa có bị động", PassiveDesc = "Tướng tự tạo theo tên đăng nhập — kỹ năng sẽ được cập nhật sau." },
        // Phase 2.9: former default starter, still a normal recruitable hero.
        new HeroDef { Id = "luu_bi", Name = "Lưu Bị", Role = HeroRole.Vanguard, Hp = 620, Atk = 72, Def = 42, MagicDef = 35, Speed = 68, Crit = 0.12, Dodge = 0.08, NormalName = "Song Kiếm", UltName = "Song Cổ Kiếm", UltPower = 1.8, UltKind = UltKind.SingleDamage, PassiveName = "Nhân Nghĩa", PassiveDesc = "Được lòng người: tướng cân bằng, không bị khắc hệ (Tiên phong)." },
        new HeroDef { Id = "quan_vu", Name = "Quan Vũ", Role = HeroRole.Warrior, Hp = 520, Atk = 85, Def = 35, MagicDef = 25, Speed = 70, Crit = 0.25, Dodge = 0.05, NormalName = "Thanh Long Trảm", UltName = "Thanh Long Yển Nguyệt", UltPower = 2.0, UltKind = UltKind.SingleDamage, PassiveName = "Võ Thánh", PassiveDesc = "Tỉ lệ bạo kích +15% so với tướng thường." },
        new HeroDef { Id = "truong_phi", Name = "Trương Phi", Role = HeroRole.Vanguard, Hp = 750, Atk = 60, Def = 55, MagicDef = 35, Speed = 55, Crit = 0.10, Dodge = 0.05, DamageReduction = 0.15, NormalName = "Mâu Đâm", UltName = "Tiếng Thét Trường Bản", UltPower = 1.6, UltKind = UltKind.SingleDamage, PassiveName = "Thiết Bích", PassiveDesc = "Giảm 15% sát thương nhận vào." },
        new HeroDef { Id = "trieu_van", Name = "Triệu Vân", Role = HeroRole.Warrior, Hp = 480, Atk = 78, Def = 30, MagicDef = 25, Speed = 95, Crit = 0.15, Dodge = 0.15, NormalName = "Thương Liên Kích", UltName = "Bạch Long Xuất Động", UltPower = 1.9, UltKind = UltKind.SingleDamage, PassiveName = "Thần Tốc", PassiveDesc = "Tốc độ cao nhất đội, né tránh +10%." },
        new HeroDef { Id = "gia_cat_luong", Name = "Gia Cát Lượng", Role = HeroRole.Strategist, Hp = 420, Atk = 90, Def = 25, MagicDef = 45, Speed = 80, Crit = 0.10, Dodge = 0.05, NormalName = "Quạt Lông", UltName = "Bát Quái Trận", UltPower = 1.4, UltKind = UltKind.AoEDamage, PassiveName = "Bát Quái", PassiveDesc = "Tuyệt kỹ đánh toàn bộ địch." },
        new HeroDef { Id = "dieu_thuyen", Name = "Điêu Thuyền", Role = HeroRole.Support, Hp = 430, Atk = 50, Def = 28, MagicDef = 40, Speed = 75, Crit = 0.10, Dodge = 0.10, NormalName = "Vũ Điệu", UltName = "Khuynh Quốc", UltPower = 0.30, UltKind = UltKind.Heal, PassiveName = "Khuynh Thành", PassiveDesc = "Tuyệt kỹ hồi máu cho đồng đội thấp máu nhất." },
        new HeroDef { Id = "lu_bo", Name = "Lữ Bố", Role = HeroRole.Warrior, Hp = 600, Atk = 105, Def = 20, MagicDef = 15, Speed = 65, Crit = 0.20, Dodge = 0.05, NormalName = "Phương Thiên Kích", UltName = "Vô Song Kích", UltPower = 2.4, UltKind = UltKind.SingleDamage, PassiveName = "Vô Song", PassiveDesc = "Tấn công cao nhất, nhưng phòng thủ thấp." },
        // Phase 2.43: one new tavern hero per campaign chapter (Pax setup,
        // same ballpark as existing heroes of the same tavern tier; only the
        // 3 existing ult kinds). Unlock chapter: TavernData.UnlockChapter.
        new HeroDef { Id = "ton_kien", Name = "Tôn Kiên", Role = HeroRole.Warrior, Hp = 560, Atk = 80, Def = 38, MagicDef = 26, Speed = 68, Crit = 0.18, Dodge = 0.05, NormalName = "Cổ Đĩnh Đao", UltName = "Mãnh Hổ Giang Đông", UltPower = 1.9, UltKind = UltKind.SingleDamage, PassiveName = "Giang Đông Mãnh Hổ", PassiveDesc = "Dũng mãnh xung trận, đòn đánh bạo kích thường xuyên." },
        new HeroDef { Id = "dien_vi", Name = "Điển Vi", Role = HeroRole.Vanguard, Hp = 780, Atk = 62, Def = 54, MagicDef = 32, Speed = 54, Crit = 0.10, Dodge = 0.04, DamageReduction = 0.12, NormalName = "Song Thiết Kích", UltName = "Ác Lai Hộ Chủ", UltPower = 1.6, UltKind = UltKind.SingleDamage, PassiveName = "Cổ Chi Ác Lai", PassiveDesc = "Giảm 12% sát thương nhận vào." },
        new HeroDef { Id = "gia_hu", Name = "Giả Hủ", Role = HeroRole.Strategist, Hp = 430, Atk = 92, Def = 24, MagicDef = 48, Speed = 82, Crit = 0.10, Dodge = 0.06, NormalName = "Độc Kế", UltName = "Ly Gián Liên Hoàn", UltPower = 1.4, UltKind = UltKind.AoEDamage, PassiveName = "Độc Sĩ", PassiveDesc = "Tuyệt kỹ đánh toàn bộ địch." },
        new HeroDef { Id = "truong_lieu", Name = "Trương Liêu", Role = HeroRole.Warrior, Hp = 540, Atk = 86, Def = 34, MagicDef = 26, Speed = 78, Crit = 0.20, Dodge = 0.08, NormalName = "Thiết Kích", UltName = "Tiêu Dao Tân Uy Chấn", UltPower = 2.0, UltKind = UltKind.SingleDamage, PassiveName = "Uy Chấn Tiêu Dao", PassiveDesc = "Tốc độ cao, giỏi đột kích." },
        new HeroDef { Id = "tu_thu", Name = "Từ Thứ", Role = HeroRole.Support, Hp = 450, Atk = 52, Def = 28, MagicDef = 44, Speed = 76, Crit = 0.10, Dodge = 0.10, NormalName = "Binh Thư", UltName = "Thanh Thủy Hồi Xuân", UltPower = 0.32, UltKind = UltKind.Heal, PassiveName = "Hiền Sĩ", PassiveDesc = "Tuyệt kỹ hồi máu cho đồng đội thấp máu nhất." },
        new HeroDef { Id = "chu_du", Name = "Chu Du", Role = HeroRole.Strategist, Hp = 440, Atk = 96, Def = 26, MagicDef = 50, Speed = 84, Crit = 0.12, Dodge = 0.06, NormalName = "Cầm Âm", UltName = "Liên Hoàn Hỏa Công", UltPower = 1.5, UltKind = UltKind.AoEDamage, PassiveName = "Chu Lang", PassiveDesc = "Tuyệt kỹ thiêu đốt toàn bộ địch." },
        new HeroDef { Id = "chuc_dung", Name = "Chúc Dung", Role = HeroRole.Warrior, Hp = 580, Atk = 94, Def = 30, MagicDef = 24, Speed = 72, Crit = 0.22, Dodge = 0.06, NormalName = "Phi Đao", UltName = "Hỏa Diệm Phi Đao", UltPower = 2.1, UltKind = UltKind.SingleDamage, PassiveName = "Hỏa Thần", PassiveDesc = "Phi đao nhanh, sát thương bạo kích cao." },
        new HeroDef { Id = "ma_sieu", Name = "Mã Siêu", Role = HeroRole.Warrior, Hp = 590, Atk = 100, Def = 28, MagicDef = 22, Speed = 90, Crit = 0.22, Dodge = 0.10, NormalName = "Thương Tây Lương", UltName = "Cẩm Mã Siêu Xung Kích", UltPower = 2.3, UltKind = UltKind.SingleDamage, PassiveName = "Cẩm Mã Siêu", PassiveDesc = "Tốc độ và tấn công vượt trội, né tránh tốt." },
    };

    /// Phase 2.43: the 8 original heroes. A legacy save with no OwnedHeroIds
    /// owns exactly these — never the tavern heroes added since.
    public static readonly string[] LegacyHeroIds =
        { "custom_hero", "luu_bi", "quan_vu", "truong_phi", "trieu_van", "gia_cat_luong", "dieu_thuyen", "lu_bo" };

    // Phase 2.41 — campaign: 8 story chapters, each with its own stage
    // count (Pax setup: 6/8/6/10/8/10/12/10 = 70). Stage ids stay global,
    // so LastStage/StageStars of old saves keep meaning "the n-th stage".
    // Rules kept from 2.39 by id: Scale (see ScaleForStage); 3 enemies
    // on ải 1–10, 4 on 11–15, 5 from 16. Enemy list = chapter soldier
    // (Tiền), then the named generals, then the boss (Trung), then more
    // soldiers to fill the count. The boss gets Scale x BossScale.
    private record StageSpec(string Name, string? Boss = null, string Named = "", string? Soldier = null);
    private record ChapterSpec(string Name, string Soldier, StageSpec[] Stages);

    private static StageSpec St(string name, string? boss = null, string named = "", string? soldier = null) => new(name, boss, named, soldier);

    private static readonly ChapterSpec[] Campaign =
    {
        new("Loạn Hoàng Cân", "hc_binh", new[]
        {
            St("Khởi Nghĩa Hoàng Cân"), St("Đào Viên Kết Nghĩa"), St("Trác Quận Phá Tặc"),
            St("Quảng Tông Vây Thành", "truong_luong"), St("Hạ Khúc Dương", "truong_bao"),
            St("Trương Giác Bại Vong", "truong_giac"),
        }),
        new("Thảo Phạt Đổng Trác", "tl_binh", new[]
        {
            St("Liên Quân Thảo Đổng"), St("Tị Thủy Quan", "hoa_hung"), St("Ôn Tửu Trảm Hoa Hùng", "hoa_hung"),
            St("Hổ Lao Tiền Đồn", "ly_giac"), St("Quách Dĩ Cản Đường", "quach_di", "ly_giac"),
            St("Hổ Lao Quan", "lu_bo"), St("Tam Anh Chiến Lữ Bố", "lu_bo", "ly_giac"),
            St("Đại Chiến Hổ Lao", "lu_bo", "quach_di,ly_giac"),
        }),
        new("Trường An & Lạc Dương", "tl_binh", new[]
        {
            St("Lạc Dương Hỏa Thiêu", "ly_giac"), St("Truy Kích Đổng Trác", "quach_di", "ly_giac"),
            St("Huỳnh Dương Phục Binh", "ly_giac", "quach_di"), St("Phượng Nghi Đình", "lu_bo", "quach_di"),
            St("Mi Ổ Tàng Bảo", "lu_bo", "ly_giac,quach_di"), St("Trường An Trừ Đổng", "dong_trac", "lu_bo,ly_giac,quach_di"),
        }),
        new("Quan Độ & Ô Sào", "vien_quan", new[]
        {
            St("Bạch Mã Giải Vây", "nhan_luong"), St("Trảm Nhan Lương", "nhan_luong", "van_xu"), St("Diên Tân Độ", "van_xu"),
            St("Tru Văn Xú", "van_xu"), St("Quan Độ Đối Lũy", "van_xu"), St("Hứa Du Đầu Tào"),
            St("Hỏa Thiêu Ô Sào", "van_xu"), St("Ô Sào Lương Thảo"), St("Thương Đình Truy Kích", "vien_thieu", "nhan_luong,van_xu"),
            St("Viên Thiệu Bại Trận", "vien_thieu", "nhan_luong,van_xu"),
        }),
        new("Tân Dã & Kinh Châu", "tao_binh", new[]
        {
            St("Tân Dã Phòng Thủ"), St("Gò Bác Vọng", "ha_hau_don"), St("Hỏa Thiêu Bác Vọng", "ha_hau_don", "hua_chu"),
            St("Tân Dã Hỏa Công", "tao_nhan"), St("Bỏ Tân Dã", "hua_chu", "ha_hau_don"),
            St("Trường Bản Pha", "hua_chu", "ha_hau_don"), St("Ấu Chúa Vượt Vây", "hua_chu", "tao_nhan,ha_hau_don"),
            St("Đương Dương Kiều", "ha_hau_don", "hua_chu,tao_nhan"),
        }),
        new("Đại Chiến Xích Bích", "thuy_quan", new[]
        {
            St("Thiệt Chiến Quần Nho"), St("Giang Đông Hội Minh", "thai_mao"), St("Quần Anh Hội", "thai_mao"),
            St("Thảo Thuyền Mượn Tên", "tao_nhan", "thai_mao"), St("Khổ Nhục Kế", "thai_mao"),
            St("Liên Hoàn Kế", "hua_chu", "tao_nhan"), St("Mượn Gió Đông", "tao_nhan", "thai_mao"),
            St("Hỏa Thiêu Xích Bích", "ha_hau_don", "hua_chu,tao_nhan"), St("Hoa Dung Đạo", "ha_hau_don", "hua_chu,tao_nhan"),
            St("Xích Bích Đại Chiến", "tao_thao", "hua_chu,tao_nhan,ha_hau_don"),
        }),
        new("Bình Định Tây Xuyên & Thất Cầm Mạnh Hoạch", "thuc_binh", new[]
        {
            St("Tiến Quân Ba Quận", "nghiem_nhan"), St("Nghĩa Thích Nghiêm Nhan", "nghiem_nhan"),
            St("Lạc Phượng Pha", "truong_nhiem"), St("Kim Nhạn Kiều", "truong_nhiem", "nghiem_nhan"),
            St("Lạc Thành", "phap_chinh", "truong_nhiem"), St("Thành Đô Quy Thuận", "phap_chinh", "truong_nhiem,nghiem_nhan"),
            St("Nam Chinh", "manh_hoach", soldier: "nm_binh"), St("Lô Thủy", "manh_hoach", soldier: "nm_binh"),
            St("Nhất Cầm Mạnh Hoạch", "manh_hoach", "gia_cat_luong", "nm_binh"), St("Đằng Giáp Quân", "manh_hoach", "gia_cat_luong", "nm_binh"),
            St("Bàn Xà Cốc", "manh_hoach", "gia_cat_luong", "nm_binh"), St("Thất Cầm Mạnh Hoạch", "manh_hoach", "gia_cat_luong", "nm_binh"),
        }),
        new("Hán Trung Tranh Bá", "tao_binh", new[]
        {
            St("Dương Bình Quan", "ha_hau_uyen"), St("Thiên Đãng Sơn", "ha_hau_uyen"), St("Định Quân Sơn", "ha_hau_uyen", "tu_hoang"),
            St("Hán Thủy", "tu_hoang"), St("Dương Bình Tái Chiến", "tu_hoang", "ha_hau_uyen"), St("Kê Lặc", "tu_hoang", "hua_chu"),
            St("Tà Cốc", "tu_hoang", "tao_nhan,hua_chu"), St("Mỹ Sơn", "ha_hau_don", "hua_chu"),
            St("Hán Trung Xưng Vương", "ha_hau_don", "tu_hoang,hua_chu"), St("Hán Trung Quyết Chiến", "tao_thao", "hua_chu,tu_hoang,ha_hau_don"),
        }),
    };

    public static int EnemyCountForStage(int id) => id <= 10 ? 3 : id <= 15 ? 4 : 5;
    // Phase 2.52 balance: ải 1–20 keep the 2.39 step (+0.09). From ải 20 the
    // Scale is interpolated between chapter-end anchors picked from the 2.52
    // simulation so each chapter end falls near a target team level
    // (ch4 Lv35, ch5 Lv45, ch6 Lv55, ch7 Lv70, ch8 Lv85). Pax setup.
    private static readonly (int Stage, double Scale)[] ScaleAnchors =
        { (20, 2.56), (30, 4.8), (38, 5.7), (48, 6.3), (60, 8.6), (70, 9.5) };

    public static double ScaleForStage(int id)
    {
        if (id <= 20) return Math.Round(0.85 + 0.09 * (id - 1), 2);
        for (int i = 1; i < ScaleAnchors.Length; i++)
        {
            var (s1, v1) = ScaleAnchors[i];
            if (id > s1 && i < ScaleAnchors.Length - 1) continue;
            var (s0, v0) = ScaleAnchors[i - 1];
            return Math.Round(v0 + (v1 - v0) * (id - s0) / (s1 - s0), 2);
        }
        return ScaleAnchors[^1].Scale;
    }

    public static readonly List<ChapterDef> Chapters = BuildChapters();
    public static readonly List<StageDef> Stages = BuildStages();

    private static List<ChapterDef> BuildChapters()
    {
        var list = new List<ChapterDef>();
        int next = 1;
        for (int c = 0; c < Campaign.Length; c++)
        {
            list.Add(new ChapterDef { Number = c + 1, Name = Campaign[c].Name, FirstStage = next, LastStage = next + Campaign[c].Stages.Length - 1 });
            next += Campaign[c].Stages.Length;
        }
        return list;
    }

    private static List<StageDef> BuildStages()
    {
        var list = new List<StageDef>();
        int id = 1;
        for (int c = 0; c < Campaign.Length; c++)
            foreach (var spec in Campaign[c].Stages)
            {
                string soldier = spec.Soldier ?? Campaign[c].Soldier;
                var enemies = new List<string> { soldier };
                enemies.AddRange(spec.Named.Split(',', StringSplitOptions.RemoveEmptyEntries));
                if (spec.Boss != null) enemies.Add(spec.Boss);
                int count = EnemyCountForStage(id);
                while (enemies.Count < count) enemies.Add(soldier);
                list.Add(new StageDef
                {
                    Id = id, Chapter = c + 1, Name = spec.Name, Scale = ScaleForStage(id),
                    EnemyIds = enemies.Take(count).ToList(), BossId = spec.Boss,
                });
                id++;
            }
        return list;
    }

    public static int ChapterCount => Chapters.Count;

    /// Phase 2.41: chapter n is cleared once its last stage is won.
    public static bool ChapterCleared(SaveData save, int chapter) =>
        chapter >= 1 && chapter <= Chapters.Count && save.LastStage >= Chapters[chapter - 1].LastStage;

    public static HeroDef Get(string id) => Heroes.First(h => h.Id == id);

    /// Phase 2.40: battle lookup — playable heroes first, then enemy-only
    /// soldiers/bosses (EnemyDatabase), which never enter the roster.
    public static HeroDef GetCombatant(string id) =>
        Heroes.FirstOrDefault(h => h.Id == id) ?? EnemyDatabase.Find(id)
        ?? throw new ArgumentException($"Unknown combatant id '{id}'");

    /// Phase 2.39: Mưu (Strategist) and Phụ (Support) deal Sách (strategy)
    /// damage, checked against Sách phòng; everyone else deals Vật damage.
    public static bool UsesStrategy(HeroRole role) => role is HeroRole.Strategist or HeroRole.Support;

    /// <summary>Phase 2.10: the custom hero's display name is the username.</summary>
    public static void SetCustomHeroName(string username)
    {
        var custom = Heroes.FirstOrDefault(h => h.Id == "custom_hero");
        if (custom != null) custom.Name = string.IsNullOrEmpty(username) ? "Tướng Tự Tạo" : username;
    }
    // Phase 2.41: ids outside 1..Stages.Count clamp to the nearest stage.
    public static StageDef GetStage(int id) => Stages[Math.Clamp(id, 1, Stages.Count) - 1];
    public static string RoleName(HeroRole role) => role switch
    {
        HeroRole.Vanguard => "Tiên phong",
        HeroRole.Warrior => "Võ",
        HeroRole.Strategist => "Mưu",
        HeroRole.Support => "Phụ",
        _ => "?",
    };

    // Khắc hệ: Võ > Mưu > Phụ > Võ. Tiên phong không khắc ai, cũng không bị khắc.
    public static bool Counters(HeroRole attacker, HeroRole defender)
    {
        return (attacker, defender) switch
        {
            (HeroRole.Warrior, HeroRole.Strategist) => true,
            (HeroRole.Strategist, HeroRole.Support) => true,
            (HeroRole.Support, HeroRole.Warrior) => true,
            _ => false,
        };
    }
}
