/// <summary>
/// 공격할 때마다 스택이 하나 쌓입니다. 간식 누적.
///
/// 공속 증가분 자체는 StatModifierEffect와 같은 경로로 처리되므로
/// 여기서는 스택만 올립니다. MaxStack이 상한 역할을 하고,
/// </summary>
public class SnackStackEffect : StatModifierEffect
{
    public override bool OnOwnerAttack(BattleUnit owner, BattleUnit target)
    {
        if (!AddStack(1)) return false;
        
        // 스택이 바뀌면 모디파이어도 다시 계산되어야 합니다.
        OnApply(owner);
        return true;
    }
}
