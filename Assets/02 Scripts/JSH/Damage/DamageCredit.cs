using System.Collections.Generic;

/// <summary>
/// [미터기] 피해·실드가 "누구 몫인가"를 적는 영수증입니다.
///
/// 전투 계산은 이 값을 읽지 않습니다. attacker / caster는 지금처럼 전투 판정(감전, 가시, OnKill,
/// lastAttacker, 상태이상 피해 배율)에만 쓰고, 미터기 기록만 이 값을 봅니다.
/// 둘을 나눈 이유: 가시·드래곤 피해에 attacker를 채우면 처치 판정과 OnHit 트리거가 바뀌어
/// 전투 결과 자체가 달라지기 때문입니다.
///
/// 세 가지 중 하나입니다.
///   Unit    : 유닛 몫. 비워 두면(default) 호출부의 attacker / caster로 채웁니다.
///   Synergy : 시너지 몫. 화염 결속 시작 화상, 드래곤 고정 피해.
///   Split   : 여러 주인에게 비율로 나눕니다. 화상 스택을 여러 유닛이 쌓은 경우.
///
/// struct인 이유: 공격마다 만들어지므로 클래스면 GC가 쌓입니다. Split의 리스트는
/// DotEffect가 들고 있는 것을 그대로 빌려 쓰고, 기록 즉시 읽고 버리므로 복사하지 않습니다.
/// </summary>
public readonly struct DamageCredit
{
    public readonly BattleUnit Unit;
    public readonly SynergyData Synergy;

    /// <summary>시너지 몫일 때 그 시너지가 아군 진영 것인가. 미터기는 아군 시너지만 행으로 만듭니다.</summary>
    public readonly bool SynergyIsAlly;

    public readonly IReadOnlyList<DamageCredit> SplitCredits;
    public readonly IReadOnlyList<float> SplitWeights;

    public bool IsEmpty => Unit == null && Synergy == null && SplitCredits == null;
    public bool IsSynergy => Synergy != null;
    public bool IsSplit => SplitCredits != null;

    private DamageCredit(BattleUnit unit, SynergyData synergy, bool synergyIsAlly,
        IReadOnlyList<DamageCredit> splitCredits, IReadOnlyList<float> splitWeights)
    {
        Unit = unit;
        Synergy = synergy;
        SynergyIsAlly = synergyIsAlly;
        SplitCredits = splitCredits;
        SplitWeights = splitWeights;
    }

    /// <summary>유닛 한 명의 몫. unit이 null이면 빈 영수증이 됩니다(서든데스처럼 주인 없는 피해).</summary>
    public static DamageCredit FromUnit(BattleUnit unit)
        => new DamageCredit(unit, null, false, null, null);

    /// <summary>시너지 몫. isAlly는 그 시너지를 가진 진영이 아군인지입니다.</summary>
    public static DamageCredit FromSynergy(SynergyData synergy, bool isAlly)
        => new DamageCredit(null, synergy, isAlly, null, null);

    /// <summary>
    /// 여러 주인에게 weights 비율로 나눕니다. 두 리스트는 같은 길이여야 합니다.
    /// 리스트를 복사하지 않으므로, 넘긴 쪽은 기록이 끝날 때까지(같은 호출 안) 리스트를 바꾸면 안 됩니다.
    /// </summary>
    public static DamageCredit FromSplit(IReadOnlyList<DamageCredit> credits, IReadOnlyList<float> weights)
        => new DamageCredit(null, null, false, credits, weights);

    /// <summary>
    /// 비어 있으면 fallback 유닛 몫으로 바꿔 돌려줍니다.
    /// "따로 지정하지 않았으면 공격자(시전자) 몫"이라는 기본 규칙을 한 곳에 둔 것입니다.
    /// </summary>
    public DamageCredit OrUnit(BattleUnit fallback) => IsEmpty ? FromUnit(fallback) : this;
}
