/// <summary>
/// 피격 시 추가 피해를 붙입니다. 감전.
///
/// 기준이 피격자가 아니라 공격자의 공격력입니다.
/// 그래서 StatusEffect 데이터의 StatType은 비어 있고, 여기서 공격자에게서 직접 읽습니다.
/// 스택은 소모되지 않고 지속시간으로만 풀립니다.
/// </summary>
public class ShockEffect : StatusEffect
{
    public float CalcBonus(BattleUnit attacker)
    {
        if (attacker == null) return 0f;

        float percent = RawValue() * Stack;
        return attacker.Stat.GetStat(EStatType.Atk) * percent * 0.01f;
    }
}
