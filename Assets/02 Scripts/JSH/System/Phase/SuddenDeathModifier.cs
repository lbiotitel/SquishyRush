
/// <summary>
/// 서든데스 피해 증폭.
///
/// DamageFormula.Amplify를 거치는 직접 피해(기본공격·스킬)에만 곱해집니다.
///  - 상태이상이 출처인 피해(지옥불 등, EDamageSource.DoT)는 증폭하지 않습니다.
///  - 화상·독 틱은 Amplify를 거치지 않으므로 애초에 여기 오지 않습니다.
///  - 환경 피해·반사는 attacker가 null이라 Amplify 초반에 빠집니다.
///
/// 다른 수정자와 달리 유닛 스탯이 아니라 전투 상태(BattleManager.SuddenDeath)를 읽습니다.
/// 서든데스는 유닛 버프가 아니라 전장 규칙이기 때문입니다.
/// </summary>
public class SuddenDeathModifier : IDamageModifier
{
    public int Order => 100;

    public float Modify(in DamageContext ctx, float amount)
    {
        if (ctx.Source == EDamageSource.DoT) return amount;

        return amount * (InGameManagers.Battle?.SuddenDeath.DamageFactor ?? 1f);
    }


}
