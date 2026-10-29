using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.33: khảm ngọc (gem socketing — opens the "Đục lỗ" tab).
/// Huynh's spec gem types, mapped to engine-supported stats only:
/// Tốc độ / Công / HP / Vật phòng / Né tránh / Bạo kích.
/// (Pháp phòng, Đỡ đòn, Phá kích, Trúng need engine stats — noted for
/// the engine phase.) 3 gems of level N merge into 1 of level N+1
/// (max 10). Sockets per item by quality: Lam 1 / Tím 2 / Cam 3 / Đỏ 4;
/// the first is open, the rest unlock with Bạc.
/// Gem sources (Pax setup): seed 6 Lv1 gems + buy random Lv1 for
/// 2.000 Vàng in the gem pouch; drops/shops arrive with later phases.
/// </summary>
public class GemDef
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string StatId { get; set; } = "";
    public double PerLevel { get; set; }
}

public static class GemService
{
    public const int MaxGemLevel = 10;
    public const int BuyGemGold = 2000;

    public static readonly List<GemDef> Defs = new()
    {
        new() { Type = "speed", Name = "Tốc độ", StatId = "speed", PerLevel = 3 },
        new() { Type = "atk", Name = "Công", StatId = "atk", PerLevel = 10 },
        new() { Type = "hp", Name = "HP", StatId = "hp", PerLevel = 150 },
        new() { Type = "def", Name = "Vật phòng", StatId = "def", PerLevel = 8 },
        new() { Type = "dodge", Name = "Né tránh", StatId = "dodge", PerLevel = 0.01 },
        new() { Type = "crit", Name = "Bạo kích", StatId = "crit", PerLevel = 0.02 },
    };

    public static GemDef? GetDef(string type) => Defs.Find(d => d.Type == type);

    public static string MakeGemId(string type, int level) => $"{type}_{level}";

    public static (string Type, int Level) ParseGemId(string gemId)
    {
        int cut = gemId.LastIndexOf('_');
        if (cut < 0) return ("", 0);
        return int.TryParse(gemId.Substring(cut + 1), out int lv)
            ? (gemId.Substring(0, cut), lv) : ("", 0);
    }

    public static string DescribeGem(string gemId)
    {
        var (type, level) = ParseGemId(gemId);
        var def = GetDef(type);
        if (def == null) return gemId;
        string val = def.StatId is "dodge" or "crit" ? $"+{def.PerLevel * level:P0}" : $"+{def.PerLevel * level:0}";
        return $"Ngọc {def.Name} Lv{level} ({val} {def.Name})";
    }

    public static int SocketsForQuality(string quality) => quality switch
    {
        "Lam" => 1, "Tim" => 2, "Cam" => 3, "Do" => 4, _ => 1,
    };

    /// First socket open; unlocking socket i (0-based) costs 10.000 x 2^i Bạc.
    public static int UnlockCostSilver(int socketIndex) => 10000 * (1 << socketIndex);

    /// Fills an item's sockets to its quality count (idempotent).
    public static void EnsureSockets(EquipmentState eq)
    {
        eq.Sockets ??= new List<EquipmentSocket>();
        int want = SocketsForQuality(eq.Quality);
        while (eq.Sockets.Count < want)
            eq.Sockets.Add(new EquipmentSocket { Unlocked = eq.Sockets.Count == 0 });
    }

    public static void EnsureSockets(BagEquipmentItem item)
    {
        item.Sockets ??= new List<EquipmentSocket>();
        int want = SocketsForQuality(item.Quality);
        while (item.Sockets.Count < want)
            item.Sockets.Add(new EquipmentSocket { Unlocked = item.Sockets.Count == 0 });
    }

    public static int GemCount(SaveData save, string gemId)
    {
        ProgressionService.EnsureDefaults(save);
        return save.GemInventory.TryGetValue(gemId, out int n) ? n : 0;
    }

    public static void AddGems(SaveData save, string gemId, int count)
    {
        ProgressionService.EnsureDefaults(save);
        save.GemInventory[gemId] = GemCount(save, gemId) + count;
        if (save.GemInventory[gemId] <= 0) save.GemInventory.Remove(gemId);
    }

    public static bool TryUnlockSocket(SaveData save, string heroId, string slotId, int socketIndex)
    {
        ProgressionService.EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        if (eq.IsEmpty) return false;
        EnsureSockets(eq);
        if (socketIndex < 0 || socketIndex >= eq.Sockets.Count) return false;
        if (eq.Sockets[socketIndex].Unlocked) return false;
        int cost = UnlockCostSilver(socketIndex);
        if (save.Silver < cost) return false;
        save.Silver -= cost;
        eq.Sockets[socketIndex].Unlocked = true;
        return true;
    }

    public static bool TrySocket(SaveData save, string heroId, string slotId, int socketIndex, string gemId)
    {
        ProgressionService.EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        if (eq.IsEmpty) return false;
        EnsureSockets(eq);
        if (socketIndex < 0 || socketIndex >= eq.Sockets.Count) return false;
        var sock = eq.Sockets[socketIndex];
        if (!sock.Unlocked || !string.IsNullOrEmpty(sock.GemId)) return false;
        if (GetDef(ParseGemId(gemId).Type) == null) return false;
        if (GemCount(save, gemId) <= 0) return false;
        AddGems(save, gemId, -1);
        sock.GemId = gemId;
        return true;
    }

    public static bool TryUnsocket(SaveData save, string heroId, string slotId, int socketIndex)
    {
        ProgressionService.EnsureDefaults(save);
        var eq = save.HeroEquipment[heroId][slotId];
        if (eq.IsEmpty) return false;
        EnsureSockets(eq);
        if (socketIndex < 0 || socketIndex >= eq.Sockets.Count) return false;
        var sock = eq.Sockets[socketIndex];
        if (string.IsNullOrEmpty(sock.GemId)) return false;
        AddGems(save, sock.GemId, 1);
        sock.GemId = "";
        return true;
    }

    public static bool TryMerge(SaveData save, string gemType, int level)
    {
        ProgressionService.EnsureDefaults(save);
        if (GetDef(gemType) == null || level < 1 || level >= MaxGemLevel) return false;
        string from = MakeGemId(gemType, level);
        if (GemCount(save, from) < 3) return false;
        AddGems(save, from, -3);
        AddGems(save, MakeGemId(gemType, level + 1), 1);
        return true;
    }

    public static bool TryBuyGem(SaveData save, Random rng)
    {
        ProgressionService.EnsureDefaults(save);
        if (save.Gold < BuyGemGold) return false;
        save.Gold -= BuyGemGold;
        var def = Defs[rng.Next(Defs.Count)];
        AddGems(save, MakeGemId(def.Type, 1), 1);
        return true;
    }

    /// Applies socketed gems to the effective hero (called by GetEffectiveHero).
    public static void ApplyGems(HeroDef def, EquipmentState eq, ref double atkMul, ref double hpMul)
    {
        if (eq.IsEmpty) return;
        foreach (var sock in eq.Sockets)
        {
            if (!sock.Unlocked || string.IsNullOrEmpty(sock.GemId)) continue;
            var (type, level) = ParseGemId(sock.GemId);
            var gdef = GetDef(type);
            if (gdef == null || level < 1) continue;
            double v = gdef.PerLevel * level;
            switch (gdef.StatId)
            {
                case "speed": def.Speed += (int)v; break;
                case "atk": def.Atk += (int)v; break;
                case "hp": def.Hp += (int)v; break;
                case "def": def.Def += (int)v; break;
                case "dodge": def.Dodge += v; break;
                case "crit": def.Crit += v; break;
                case "atk_pct": atkMul *= 1 + v; break;
                case "hp_pct": hpMul *= 1 + v; break;
            }
        }
    }
}
