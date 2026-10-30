namespace LongTuong;

public class Fighter
{
    public HeroDef Def { get; }
    public bool IsPlayer { get; }
    public int Hp { get; set; }
    public int MaxHp { get; }
    public int Rage { get; set; }
    // Phase 2.20: formation position. Phase 2.39: enemies have positions
    // too (from the stage's enemy slots), with the same bonuses.
    // Bonuses are computed, never written back into the shared HeroDef.
    public FormationPosition Position { get; set; } = FormationPosition.None;

    // Trung quân: +20% sức mạnh (ATK). Tiền quân: +20% thủ (Vật + Sách phòng).
    public int EffectiveAtk => Position == FormationPosition.Middle
        ? (int)System.Math.Round(Def.Atk * 1.20) : Def.Atk;
    public int EffectiveDef => Position == FormationPosition.Front
        ? (int)System.Math.Round(Def.Def * 1.20) : Def.Def;
    public int EffectiveMagicDef => Position == FormationPosition.Front
        ? (int)System.Math.Round(Def.MagicDef * 1.20) : Def.MagicDef;

    public Fighter(HeroDef def, bool isPlayer)
    {
        Def = def;
        IsPlayer = isPlayer;
        MaxHp = def.Hp;
        Hp = MaxHp;
        Rage = 0;
    }

    public bool Alive => Hp > 0;
    public string Name => Def.Name;
}
