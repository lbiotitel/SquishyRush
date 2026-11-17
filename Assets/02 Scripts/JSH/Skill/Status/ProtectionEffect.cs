/// <summary>
/// 피해를 통째로 무시합니다. 보호.
/// 지속 피해도 막고, 막을 때마다 스택을 1 소모합니다.
/// 스택이 곧 남은 무시 횟수라 ModifierValue는 쓰지 않습니다.
/// </summary>
public class ProtectionEffect : StatusEffect
{
    public override bool IsConsumed => Stack <= 0;

    //남은 횟수가 있으면 1 소모하고 true. 호출부는 피해를 0으로 만듦.
    public bool TryConsume()
    {
        if (Stack <= 0) return false;

        SetStack(Stack - 1);
        return true;
    }
}
