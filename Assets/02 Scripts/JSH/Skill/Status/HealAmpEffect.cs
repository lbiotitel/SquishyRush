
/// <summary>
/// 받는 회복량을 ModifierValue%(50) 늘립니다. 스택은 남은 사용 횟수이고, 회복 1회당 1 소모합니다. 영약.
/// 증가율은 스택 수와 무관하게 고정입니다.
/// </summary>
public class HealAmpEffect : StatusEffect
{
    public override bool IsConsumed => Stack <= 0;

    public float Consume()
    {
        if (Stack <= 0) return 0f;

        SetStack(Stack - 1);
        return RawValue();
    }
}
