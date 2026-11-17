
/// <summary>
/// 날아가는 중인 타격 1건.
///
/// 발사 시점에 계산이 전부 끝난 값만 담습니다.
///   발사 시점 : 타겟 선정, 명중/치명 판정(RNG), 피해량 계산
///   명중 시점 : 이 구조체에 담긴 값을 그대로 대상에게 적용
///
/// 왜 이렇게 나누는가
///   RNG를 발사 순서대로 소비해야 같은 시드에서 같은 결과가 나옵니다(결정론).
///   도착 시점에 굴리면 "누가 먼저 도착했나"에 따라 RNG 순서가 바뀔 수 있습니다.
///
/// 왜 struct인가
///   일반공격마다 하나씩 생깁니다. class면 전투 내내 GC가 쌓입니다.
///   (DamageReport가 struct인 것과 같은 이유)
///
/// 필드가 타입별로 조금씩 남는 건 감수합니다.
/// 타입마다 구조체를 따로 만들면 큐도 타입 수만큼 필요해집니다.
/// </summary>
public struct PendingHit
{
    /// <summary>도착하면 이 타격을 적용할 쪽. NormalAttack / DamageEffect.</summary>
    public IPendingHitHandler Handler;
    
    public BattleUnit Caster;
    public BattleUnit Target;

    /// <summary>Damage: 방어 파이프라인 전 최종 피해 / Heal: 회복량.</summary>
    public float Amount;
    public bool IsCrit;

    /// <summary>일반공격 전용. 빗나가도 발사체는 날아가고, 도착할 때 Miss가 뜹니다.</summary>
    public bool IsMiss;

    /// <summary>상태이상 스택. 시전자 성급으로 발사 시점에 정해 둡니다.</summary>
    public int Stack;

    /// <summary>남은 비행 시간(초).</summary>
    public float Remaining;

    /// <summary>
    /// 큐를 거쳐 나중에 도착했는가. PendingHitQueue.Dispatch가 채웁니다.
    /// DamageEffect가 SkillCastContext에 처치를 기록할지 판단할 때 씁니다.
    /// </summary>
    public bool IsDeferred;
}

/// <summary>
/// 도착한 타격을 실제로 적용하는 쪽.
///
/// 큐는 "언제" 적용할지만 알고 "무엇을" 할지는 모릅니다(SRP).
/// 새 종류의 타격이 생겨도 큐는 고치지 않고 이 인터페이스만 구현하면 됩니다(OCP).
/// </summary>
public interface IPendingHitHandler
{
    void ApplyHit(in PendingHit hit);
}
