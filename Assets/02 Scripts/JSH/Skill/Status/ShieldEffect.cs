using UnityEngine;

/// <summary>
/// 피해를 흡수하는 막. 쉴드.
///
/// 스택 배수가 아니라 절대값으로 듭니다.
/// 이끼 골렘(최대체력 40%)과 어항 금붕어(15%)가 같은 StatusEffect_0002를 쓰는데
/// 시전자마다 금액이 달라서 스택으로는 표현이 안 되기 때문입니다.
/// 겹쳐 걸리면 금액이 더해집니다.
///
/// [미터기] 미터기의 보호막량은 "부여량"입니다. Amount가 늘어나는 순간 그만큼을 부여한 쪽 몫으로 기록합니다.
/// 여러 시전자의 실드가 Amount 하나로 합쳐지므로 소모량은 시전자별로 나눌 수 없어 부여량으로 정했습니다.
/// </summary>
public class ShieldEffect : StatusEffect
{
    public float Amount { get; private set;}

    public override bool IsConsumed => Amount <= 0f;

    /// <summary>
    /// 처음 걸릴 때(그리고 Refresh·Replace로 다시 불릴 때) 금액을 더하고, 더한 만큼 Credit 몫으로 기록합니다.
    /// </summary>
    public override void OnApply(BattleUnit owner)
    {
        // 실드는 시전자 최대체력 기준입니다.
        // 지금은 ApplyStatusEffect가 caster.Stat으로 계산해 OverrideValue로 넘겨주므로
        // CalcAmount 경로를 타지 않습니다. OverrideValue 없이 거는 스킬이 생기면
        // CalcAmount가 대상 최대체력을 읽으니 그때 여기를 고쳐야 합니다.
        float add = OverrideValue ?? CalcAmount(owner);
        if (add <= 0f) return;

        Amount += add;
        RecordGrant(Credit, add);
    }

    /// <summary>
    /// 이미 실드가 있는데 또 걸릴 때. 새 시전자 금액을 더하고 새 시전자(credit) 몫으로 기록합니다.
    /// </summary>
    public override void OnStack(BattleUnit owner, BattleUnit caster, float? overrideValue, in DamageCredit credit)
    {
        float add = overrideValue ?? CalcAmount(owner);
        if (add <= 0f) return;

        Amount += add;
        RecordGrant(credit, add);
    }

    //흡수하고 막지 못한 데미지를 돌려줌
    public float Absorb(float damage)
    {
        if (Amount <= 0f || damage <= 0f) return damage;

        float absorbed = Mathf.Min(Amount, damage);
        Amount -= absorbed;
        return damage - absorbed;
    }

    /// <summary>
    /// 실드 금액에 배율을 곱합니다. (철벽 등)
    /// </summary>
    public void Scale(float multiplier)
    {
        if (multiplier <= 0f || Amount <= 0f) return;
        Amount *= multiplier;
    }

    /// <summary>[미터기] 부여량 기록. 전투 밖(상점)에서는 Battle이 없을 수 있어 null을 확인합니다.</summary>
    private static void RecordGrant(in DamageCredit credit, float add)
        => InGameManagers.Battle?.Stats.RecordShieldGrant(credit, add);
}
