using UnityEngine;

public enum EAttackResult
{
    NoTarget,
    Miss,
    Hit,
}


/// <summary>
/// 일반공격.
///
/// TryExecute 하나였던 것을 두 단계로 나눴습니다.
/// TryPrepare : 발사 시점. 타겟, 명중, 피해량을 전부 계산해서 PendingHit에 담는다.
/// Apply      : 명중 시점. 담긴 값을 대상에게 적용한다.
/// 둘 사이는 PendingHitQueue가 잇습니다. 근접(비행시간 0)이면 둘이 같은 프레임에 연달아 돕니다.
/// </summary>
public static class NormalAttack
{
    /// <summary>
    /// 큐에 넘길 적용 담당.
    /// static 클래스는 인터페이스를 구현할 수 없어서, 안쪽에 상태 없는 인스턴스를 하나만 둡니다.
    /// 상태가 없으니 모든 유닛이 공유해도 안전합니다(ITrajectory 공유와 같은 이유).
    /// </summary>
    private sealed class HitHandler : IPendingHitHandler
    {
        public void ApplyHit(in PendingHit hit) => Apply(hit);
    }
    
    private static readonly IPendingHitHandler handler = new HitHandler();

    /// <summary>
    /// 공격 1회를 '준비'합니다. 데미지는 여기서 넣지 않습니다.
    /// </summary>
    public static EAttackResult TryPerpare(BattleUnit caster, out PendingHit hit)
    {
        hit = default;

        BoardModel board = InGameManagers.Battle.GetBoard(caster, ETargetType.Enemy, out bool boardIsAlly);

        if (!TargetSelector.TrySelect(ETargetSelectType.Front, caster, board, boardIsAlly,
                InGameManagers.Battle.Rng, out SlotIndex anchor))
            return EAttackResult.NoTarget;
        
        BattleUnit target = board.Get(anchor);
        if (target == null || !target.IsAlive) return EAttackResult.NoTarget;

        hit.Handler = handler;
        hit.Caster = caster;
        hit.Target = target;
        hit.Remaining = caster.AttackTravelTime;

        if (!RollHit(caster, target))
        {
            hit.IsMiss = true;
            return EAttackResult.Miss;
        }
        
         
        // 계산만 하고 TakeDamage는 하지 않습니다.
        hit.Amount = CalcDamage(caster, target, out bool isCrit);
        hit.IsCrit = isCrit;

        return EAttackResult.Hit;
    }

    /// <summary>
    /// 발사체가 닿은 순간. PendingHitQueue가 부릅니다.
    /// </summary>
    private static void Apply(in PendingHit hit)
    {
        BattleUnit target = hit.Target;

        // 날아가는 사이에 다른 공격으로 죽었으면 버립니다.
        if (target == null || !target.IsAlive) return;

        if (hit.IsMiss)
        {
            target.ReportMiss();
            return;
        }
        
        target.TakeDamage(hit.Amount, hit.Caster, EDamageSource.Attack, hit.IsCrit);

        // 원래 BattleUnit.TickAttack에 있던 NotifyOwnerAttacked.
        // "적중 시 상태이상 부여(ApplyStatusOnHit)"는 맞는 순간에 들어가야 자연스럽습니다.
        // 시전자가 비행 중 죽었다면 상태이상 컨테이너가 이미 비워져 있어 아무 일도 없습니다.
        hit.Caster?.NotifyAttackHit(target);
    }

    private static bool RollHit(BattleUnit attacker, BattleUnit target)
    {
        float hitRate = attacker.Stat.GetStat(EStatType.Accuracy)
                        - target.Stat.GetStat(EStatType.Evasion);

        if (hitRate >= 100f) return true;

        int scaled = Mathf.RoundToInt(hitRate * (InGameBalance.PERCENT_SCALE) / 100f);
        return InGameManagers.Battle.Rng.Next(InGameBalance.PERCENT_SCALE) < scaled;
    }

    private static float CalcDamage(BattleUnit attacker, BattleUnit target, out bool isCrit)
    {
        float amount = attacker.Stat.GetStat(EStatType.Atk) * InGameBalance.ATK_RATIO * attacker.Stat.GetStat(EStatType.NormalAtkDmg);

        return DamageFormula.Resolve(attacker, target, amount, EDamageSource.Attack, out isCrit);
    }
}
