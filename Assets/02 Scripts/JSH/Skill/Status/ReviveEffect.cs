/// <summary>
/// 사망 시 한 번 되살아납니다. 부활.
///
/// 스택이 남은 부활 횟수이고, 회복 비율은 ModifierValue입니다.
/// 
/// 소모 시점은 BattleUnit.Die입니다. 여러 개가 걸려 있을 때 어느 것을 먼저 쓸지는
/// 효과 하나가 알 수 없어 UnitStatContainer가 정합니다.
/// </summary>
public class ReviveEffect : StatusEffect
{
    /// <summary>되살아날 때 회복할 최대체력 비율(%).</summary>
    public float HpPercent => OverrideValue ?? RawValue();

    public override bool IsConsumed => Stack <= 0;

    /// <summary>남은 횟수가 있으면 1 소모하고 true. 호출부가 체력을 되돌립니다.</summary>
    public bool TryConsume()
    {
        if (Stack <= 0) return false;

        SetStack(Stack - 1);
        return true;
    }
}
