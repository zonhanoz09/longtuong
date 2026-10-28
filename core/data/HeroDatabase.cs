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
    public int Def { get; set; }
    public int Speed { get; set; }
    public double Crit { get; set; }
    public double Dodge { get; set; }
    public double DamageReduction { get; set; }
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
            Speed = Speed, Crit = Crit, Dodge = Dodge, DamageReduction = DamageReduction,
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
    public int RewardGold => 100 + Id * 50;
}

public static class HeroDatabase
{
    public static readonly List<HeroDef> Heroes = new()
    {
        // Phase 2.10: custom hero for new accounts. Its Name is NOT fixed here:
        // it is set to the account username on seed/login via SetCustomHeroName.
        // Basic stats, no real skills yet (placeholder ult = plain 1.0x hit).
        // It is intentionally NOT in the tavern pool (see TavernData.TierOfHero).
        new HeroDef { Id = "custom_hero", Name = "Tướng Tự Tạo", Role = HeroRole.Vanguard, Hp = 500, Atk = 60, Def = 30, Speed = 60, Crit = 0.10, Dodge = 0.05, NormalName = "Đánh thường", UltName = "Chưa có tuyệt kỹ", UltPower = 1.0, UltKind = UltKind.SingleDamage, PassiveName = "Chưa có bị động", PassiveDesc = "Tướng tự tạo theo tên đăng nhập — kỹ năng sẽ được cập nhật sau." },
        // Phase 2.9: former default starter, still a normal recruitable hero.
        new HeroDef { Id = "luu_bi", Name = "Lưu Bị", Role = HeroRole.Vanguard, Hp = 620, Atk = 72, Def = 42, Speed = 68, Crit = 0.12, Dodge = 0.08, NormalName = "Song Kiếm", UltName = "Song Cổ Kiếm", UltPower = 1.8, UltKind = UltKind.SingleDamage, PassiveName = "Nhân Nghĩa", PassiveDesc = "Được lòng người: tướng cân bằng, không bị khắc hệ (Tiên phong)." },
        new HeroDef { Id = "quan_vu", Name = "Quan Vũ", Role = HeroRole.Warrior, Hp = 520, Atk = 85, Def = 35, Speed = 70, Crit = 0.25, Dodge = 0.05, NormalName = "Thanh Long Trảm", UltName = "Thanh Long Yển Nguyệt", UltPower = 2.0, UltKind = UltKind.SingleDamage, PassiveName = "Võ Thánh", PassiveDesc = "Tỉ lệ bạo kích +15% so với tướng thường." },
        new HeroDef { Id = "truong_phi", Name = "Trương Phi", Role = HeroRole.Vanguard, Hp = 750, Atk = 60, Def = 55, Speed = 55, Crit = 0.10, Dodge = 0.05, DamageReduction = 0.15, NormalName = "Mâu Đâm", UltName = "Tiếng Thét Trường Bản", UltPower = 1.6, UltKind = UltKind.SingleDamage, PassiveName = "Thiết Bích", PassiveDesc = "Giảm 15% sát thương nhận vào." },
        new HeroDef { Id = "trieu_van", Name = "Triệu Vân", Role = HeroRole.Warrior, Hp = 480, Atk = 78, Def = 30, Speed = 95, Crit = 0.15, Dodge = 0.15, NormalName = "Thương Liên Kích", UltName = "Bạch Long Xuất Động", UltPower = 1.9, UltKind = UltKind.SingleDamage, PassiveName = "Thần Tốc", PassiveDesc = "Tốc độ cao nhất đội, né tránh +10%." },
        new HeroDef { Id = "gia_cat_luong", Name = "Gia Cát Lượng", Role = HeroRole.Strategist, Hp = 420, Atk = 90, Def = 25, Speed = 80, Crit = 0.10, Dodge = 0.05, NormalName = "Quạt Lông", UltName = "Bát Quái Trận", UltPower = 1.4, UltKind = UltKind.AoEDamage, PassiveName = "Bát Quái", PassiveDesc = "Tuyệt kỹ đánh toàn bộ địch." },
        new HeroDef { Id = "dieu_thuyen", Name = "Điêu Thuyền", Role = HeroRole.Support, Hp = 430, Atk = 50, Def = 28, Speed = 75, Crit = 0.10, Dodge = 0.10, NormalName = "Vũ Điệu", UltName = "Khuynh Quốc", UltPower = 0.30, UltKind = UltKind.Heal, PassiveName = "Khuynh Thành", PassiveDesc = "Tuyệt kỹ hồi máu cho đồng đội thấp máu nhất." },
        new HeroDef { Id = "lu_bo", Name = "Lữ Bố", Role = HeroRole.Warrior, Hp = 600, Atk = 105, Def = 20, Speed = 65, Crit = 0.20, Dodge = 0.05, NormalName = "Phương Thiên Kích", UltName = "Vô Song Kích", UltPower = 2.4, UltKind = UltKind.SingleDamage, PassiveName = "Vô Song", PassiveDesc = "Tấn công cao nhất, nhưng phòng thủ thấp." },
    };

    public static readonly List<StageDef> Stages = new()
    {
        new StageDef { Id = 1, Name = "Khởi Nghĩa Hoàng Cân", EnemyIds = new() { "dieu_thuyen", "truong_phi", "trieu_van" }, Scale = 0.85 },
        new StageDef { Id = 2, Name = "Trận Hổ Lao Quan", EnemyIds = new() { "dieu_thuyen", "truong_phi", "quan_vu" }, Scale = 0.94 },
        new StageDef { Id = 3, Name = "Tam Anh Chiến Lữ Bố", EnemyIds = new() { "gia_cat_luong", "truong_phi", "trieu_van" }, Scale = 1.03 },
        new StageDef { Id = 4, Name = "Liên Minh Thảo Đổng", EnemyIds = new() { "quan_vu", "truong_phi", "gia_cat_luong" }, Scale = 1.12 },
        new StageDef { Id = 5, Name = "Đổng Trác Loạn Chính", EnemyIds = new() { "quan_vu", "lu_bo", "dieu_thuyen" }, Scale = 1.21 },
        new StageDef { Id = 6, Name = "Quan Độ Đối Đầu", EnemyIds = new() { "lu_bo", "quan_vu", "truong_phi" }, Scale = 1.30 },
        new StageDef { Id = 7, Name = "Xích Bích Đại Chiến", EnemyIds = new() { "lu_bo", "quan_vu", "gia_cat_luong" }, Scale = 1.39 },
        new StageDef { Id = 8, Name = "Tam Phân Thiên Hạ", EnemyIds = new() { "lu_bo", "gia_cat_luong", "trieu_van" }, Scale = 1.48 },
        new StageDef { Id = 9, Name = "Bạch Đế Thành", EnemyIds = new() { "lu_bo", "quan_vu", "gia_cat_luong" }, Scale = 1.57 },
        new StageDef { Id = 10, Name = "Thống Nhất Thiên Hạ", EnemyIds = new() { "lu_bo", "quan_vu", "trieu_van" }, Scale = 1.66 },
    };

    public static HeroDef Get(string id) => Heroes.First(h => h.Id == id);

    /// <summary>Phase 2.10: the custom hero's display name is the username.</summary>
    public static void SetCustomHeroName(string username)
    {
        var custom = Heroes.FirstOrDefault(h => h.Id == "custom_hero");
        if (custom != null) custom.Name = string.IsNullOrEmpty(username) ? "Tướng Tự Tạo" : username;
    }
    public static StageDef GetStage(int id) => Stages.First(s => s.Id == id);
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
