
using UnityEngine;

/// <summary>
/// 피해 증폭 스탯을 적용하는 단 하나의 자리입니다.
///
/// 규칙:
///   - DmgMultiplier    : 유닛이 '직접' 가하는 피해(기본공격, 스킬)에 곱합니다.
///   - StatusEffectDmg  : 상태이상이 출처인 피해(화상/독/출혈/지옥불)에 곱합니다.
///   - 둘은 겹치지 않습니다. EDamageSource가 어느 쪽인지를 결정합니다.
///
/// 두 스탯 모두 기본값이 1(=100%)인 배수입니다. 0이 아닙니다.
/// 기본값이 0이면 Stat의 PercentAdd 계산이 (0 + 0) * (1 + n) = 0 이 되어
/// 광폭 같은 '피해 +100%' 상태이상이 통째로 무효가 됩니다.
///
/// 계산식이 세 군데(NormalAttack, DamageEffect, SpecialEffect)에 흩어져 있던 것을
/// 여기로 모았습니다. 증폭 규칙이 바뀌면 이 파일만 고칩니다.
/// </summary>
public static class DamageFormula
{
    private static readonly IDamageModifier[] modifiers =
    {
        new ExecuteBonusModifier(),
        new SuddenDeathModifier(),
    };
    
    static DamageFormula()
    {
    {
        System.Array.Sort(modifiers, (a, b) => a.Order.CompareTo(b.Order));
    }}

    /// <summary>
    /// 직접 피해(기본공격, 스킬)의 최종 피해량을 구합니다. 치명타 판정 → 증폭 순서입니다.
    /// DoT·반사는 치명타 없이 Amplify만 거칩니다.
    /// 다중 대상이면 대상마다 따로 부르세요. 판정도 대상마다 따로 굴러갑니다.
    /// </summary>
    public static float Resolve(BattleUnit attacker, BattleUnit target, float amount, EDamageSource source,
        out bool isCrit)
    {
        isCrit = false;
        if (attacker == null || amount <= 0f) return amount;

        if (source.IsDirect())
        {
            isCrit = RollCrit(attacker);
            if (isCrit) amount *= attacker.Stat.GetStat(EStatType.CriDamageMultiplier);
        }

        return Amplify(attacker, target, amount, source);
    }
    
    /// <summary>
    /// 치명타 판정.
    /// rateScale은 치유 치명타처럼 확률 일부만 쓰는 경우용입니다.
    /// </summary>
    public static bool RollCrit(BattleUnit attacker, float rateScale = 1f)
    {
        float rate = attacker.Stat.GetStat(EStatType.CritRate) * rateScale;
        int scaled = Mathf.RoundToInt(rate * (InGameBalance.PERCENT_SCALE / 100f));
        return InGameManagers.Battle.Rng.Next(InGameBalance.PERCENT_SCALE) < scaled;
    }
    
    public static float Amplify(BattleUnit attacker, BattleUnit target, float amount, EDamageSource source)
    {
        if (attacker == null || amount <= 0f) return amount;

        DamageContext ctx = new DamageContext(attacker, target, source);
        for (int i = 0; i < modifiers.Length; i++)
            amount = modifiers[i].Modify(in ctx, amount);
        
        return source == EDamageSource.DoT
            ? amount * StatusDmgOf(attacker)
            : amount * DmgMultiplierOf(attacker);
    }

    public static float StatusDmgOf(BattleUnit unit)
        => unit?.Stat?.GetStat(EStatType.StatusEffectDmg) ?? 1f;
    
    public static float DmgMultiplierOf(BattleUnit unit)
        => unit?.Stat?.GetStat(EStatType.DmgMultiplier) ?? 1f;
}
