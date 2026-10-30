using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>
/// Phase 2.50: a rough "lực chiến" number, only used to size the Tổ Đội
/// bot teams to the player's team (Pax setup, not shown as a balance tool):
/// (Hp × 0.2 + Atk + Def + MagicDef + Speed × 0.5) × (1 + Crit + Dodge).
/// </summary>
public static class PowerService
{
    public static double HeroPower(HeroDef h) =>
        (h.Hp * 0.2 + h.Atk + h.Def + h.MagicDef + h.Speed * 0.5) * (1 + h.Crit + h.Dodge);

    public static double TeamPower(IEnumerable<HeroDef> team) => team.Sum(HeroPower);
}
