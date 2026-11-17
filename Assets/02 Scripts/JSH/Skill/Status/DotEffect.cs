using System.Collections.Generic;

/// <summary>
/// 틱마다 피해를 주는 상태이상. 화상·독·출혈·지옥불.
/// 넷의 차이는 전부 데이터에 있습니다(기준 스탯, 틱 간격, 지속, 최대 스택).
///
/// 시전자 스탯 기준(출혈 Atk 300%, 지옥불 500%)은 부여 시점에 굳힙니다.
/// 시전자가 죽으면 Die()가 그 유닛의 버프를 전부 걷어내기 때문에,
/// 굳히지 않으면 "걸 때는 강했는데 터질 때는 약한" 일이 생깁니다.
///
/// 대상 최대체력 기준(화상 0.5%)은 굳히지 않습니다.
/// 대상이 건강이나 광폭으로 최대체력이 바뀌면 피해도 따라가는 게 맞습니다.
/// 
/// [미터기] 스택마다 "누가 쌓았는가"(stackCredit)를 stackDmg와 같은 칸에 함께 둡니다.
/// 틱 피해는 지금처럼 TakeDamage 한 번으로 넣고(보호·실드 판정이 한 번이어야 하므로),
/// 기록만 스택별 배율 비율로 나눕니다. 한 틱 = 기본량 × Σ배율 이라
/// i번째 스택 몫 = 틱 피해 × stackDmg[i] / Σ배율 이 정확히 맞습니다.
/// </summary>
public class DotEffect : StatusEffect
{
    private float frozenAmount;
    
    // 스택별로 건 유닛의 상태이상 피해 배율
    private readonly List<float> stackDmg = new();
    
    // 스택별 몫. stackDmg와 항상 같은 길이, 같은 순서입니다.
    private readonly List<DamageCredit> stackCredits = new();
    
    /// <summary>
    /// 처음 걸릴 때, 그리고 교체(Replace)·갱신(Refresh)으로 다시 불릴 때.
    /// 모든 스택을 지금 시전자 배율과 지금 몫(Credit)으로 다시 채웁니다.
    /// </summary>
    public override void OnApply(BattleUnit owner)
    {
        // 중첩·교체로 다시 불리면 새 시전자 기준으로 갱신됩니다.
        if (!IsOwnerBased) frozenAmount = CalcAmount(owner);

        float m = DamageFormula.StatusDmgOf(Caster);
        stackDmg.Clear();
        stackCredits.Clear();
        for(int i = 0; i < Stack; i++)
        {
            stackDmg.Add(m);
            stackCredits.Add(Credit);
        }
    }

    /// <summary>
    /// 같은 상태가 다시 걸려 스택이 늘 때.
    /// 늘어난 만큼 새 시전자 배율과 새 몫(credit)을 붙입니다.
    /// 상한(화상 5)이면 가장 약한 스택 하나를 더 강한 것으로 바꾸고, 그 칸의 몫도 같이 바꿉니다.
    /// </summary>
    public override void OnStack(BattleUnit owner, BattleUnit caster, float? overrideValue, in DamageCredit credit)
    {
        float m = DamageFormula.StatusDmgOf(caster);
        int added = Stack - stackDmg.Count;

        if (added > 0)
        {
            for(int i = 0; i < added; i++)
            {
                stackDmg.Add(m);
                stackCredits.Add(credit);
            }
            return;
        }

        // 상한(화상 5): 가장 약한 스택 하나를 더 강한 것으로 교체
        int min = 0;
        for (int i = 1; i < stackDmg.Count; i++)
            if (stackDmg[i] < stackDmg[min]) min = i;
        
        if (m > stackDmg[min])
        {
            stackDmg[min] = m;
            stackCredits[min] = credit;
        }
    }

    /// <summary>
    /// 틱 피해를 넣습니다. 계산은 기존과 같고, TakeDamage에 Split 영수증만 붙여 보냅니다.
    /// 출처는 DoT라 미터기에서 흰색(고정)입니다.
    /// </summary>
    public override void OnTick(BattleUnit owner)
    {
        if (!owner.IsAlive) return;

        float per = IsOwnerBased ? CalcAmount(owner) : frozenAmount;
        float mult = 0f;
        for (int i = 0; i < stackDmg.Count; i++) mult += stackDmg[i];
        
        float amount = per * mult;
        if (amount <= 0f) return;
        
        owner.TakeDamage(amount, Caster, EDamageSource.DoT, false, Data,
            DamageCredit.FromSplit(stackCredits, stackDmg));
        //Logger.Log($"[도트] {owner.RuntimeId} <- {Data.StatusId} x{Stack} : {amount:F1} (남은 HP {owner.CurrentHp:F1})");
    }
}
