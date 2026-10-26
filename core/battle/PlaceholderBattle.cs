namespace LongTuong;

/// <summary>Phase 0 placeholder battle logic: deterministic, no RNG yet (RNG with seed comes in Phase 1).</summary>
public class PlaceholderBattle
{
    public const int PlayerDamage = 25;
    public const int EnemyDamage = 10;

    public int PlayerHp { get; private set; } = 100;
    public int EnemyHp { get; private set; } = 100;
    public int Turn { get; private set; } = 0;
    public bool IsOver => PlayerHp <= 0 || EnemyHp <= 0;
    public bool PlayerWon => EnemyHp <= 0 && PlayerHp > 0;

    public void Attack()
    {
        if (IsOver) return;
        Turn++;
        EnemyHp = System.Math.Max(0, EnemyHp - PlayerDamage);
        if (EnemyHp > 0) PlayerHp = System.Math.Max(0, PlayerHp - EnemyDamage);
    }

    public void Reset()
    {
        PlayerHp = 100;
        EnemyHp = 100;
        Turn = 0;
    }
}
