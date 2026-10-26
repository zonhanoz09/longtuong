namespace LongTuong;

public class Fighter
{
    public HeroDef Def { get; }
    public bool IsPlayer { get; }
    public int Hp { get; set; }
    public int MaxHp { get; }
    public int Rage { get; set; }

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
