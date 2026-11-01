using System.Collections.Generic;

namespace LongTuong;

// Fighter references identify instances, including repeated enemy definitions.
public sealed record FighterSnapshot(Fighter Fighter, int Hp, int Rage);
public sealed record BattleImpact(Fighter Target, int Amount, bool Healing, bool Dodged, bool Critical, bool Blocked);
public sealed record BattleAction(Fighter Actor, bool Ultimate,
    IReadOnlyList<FighterSnapshot> Before, IReadOnlyList<FighterSnapshot> After,
    IReadOnlyList<BattleImpact> Impacts);
