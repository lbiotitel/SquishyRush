
/// <summary>
/// 기본공격 적중 시 맞은 대상에게 OnHitApply에 정의된 상태이상을 겁니다. 독단검, 화상단검 등.
/// 거는 쪽(Caster)은 이 상태의 보유자라서, 걸린 상태이상의 수치는 보유자 스탯 기준이 됩니다.
///
/// 거는 스택 = entry.stack × 이 상태의 Stack.
/// 이 상태가 MaxStack 1이면 항상 entry.stack 그대로입니다.
/// </summary>
public class ApplyStatusOnHitEffect : StatusEffect
{
    private StatusEffectData linked;

    public override void OnApply(BattleUnit owner)
    {
        if (linked != null) return;

        OnHitApplyEntry entry = Data.OnHitApply;
        if (entry == null || string.IsNullOrEmpty(entry.statusId))
        {
            Logger.LogWarning($"[{Data.StatusId}] OnHitApply가 비어 있습니다.");
            return;
        }
        
        if(!GlobalManagers.Resource.TryGetResource(entry.statusId, out linked))
            Logger.LogWarning($"[{Data.StatusId}] 적중 시 부여할 '{entry.statusId}'를 찾지 못했습니다.");
    }

    public override bool OnOwnerAttack(BattleUnit owner, BattleUnit target)
    {
        if (linked == null || target == null || !target.IsAlive) return false;

        OnHitApplyEntry entry = Data.OnHitApply;
        if (!RollChance(entry.chance)) return false;
        
        target.ApplyStatus(linked, owner, entry.stack * Stack);
        return false;
    }

    private static bool RollChance(int chance)
    {
        if (chance >= InGameBalance.PERCENT_SCALE) return true;
        if (chance <= 0) return false;

        return InGameManagers.Battle.Rng.Next(InGameBalance.PERCENT_SCALE) < chance;
    }
}
