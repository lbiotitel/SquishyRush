/// <summary>
/// 피해 계산 중간에 끼어드는 규칙 하나.
///
/// 조건이 안 맞으면 amount를 그대로 돌려줍니다. 켜고 끄는 것은
/// 각 수정자가 자기 스탯을 읽어서 판단합니다(스탯이 0이면 통과).
/// </summary>
public interface IDamageModifier 
{
    /// <summary>작은 값부터 적용됩니다. 사이에 끼울 자리를 남기려고 10 단위로 씁니다.</summary>
    int Order { get; }

    float Modify(in DamageContext ctx, float amount);
}
