
/// <summary>
/// 처형: 대상 체력이 낮을 때 공격력의 N%를 더합니다.
///
/// 곱산이 아니라 가산인 이유는 엑셀 서술이 "공격력의 N%만큼 추가 피해"이기 때문입니다.
/// 기준이 최종 피해가 아니라 Atk이라, 치명타나 광폭이 붙어도 기여분은 고정입니다.
/// </summary>
public class ExecuteBonusModifier : IDamageModifier
{
    private const float HP_THRESHOLD = 0.3f;

    public int Order => -10;

    public float Modify(in DamageContext ctx, float amount)
    {
        if (ctx.Source == EDamageSource.DoT) return amount;

        float rate = ctx.Attacker.Stat.GetStat(EStatType.ExecuteBonusRate);
        if (rate <= 0f) return amount;

        if (ctx.Target == null || ctx.Target.HpRatio > HP_THRESHOLD) return amount;

        return amount + ctx.Attacker.Stat.GetStat(EStatType.Atk) * rate * 0.01f;
    }
}
