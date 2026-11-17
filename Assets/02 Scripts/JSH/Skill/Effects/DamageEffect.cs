using System.Collections.Generic;

public class DamageEffect : SkillEffect, IPendingHitHandler
{
    private StatusEffectData statusData;
    
    
    protected override void OnInit()
    {
        if(!string.IsNullOrEmpty(skillEffectData.statusId))
            if(!GlobalManagers.Resource.TryGetResource<StatusEffectData>(skillEffectData.statusId, out statusData))
                Logger.LogWarning($"[Damage] StatusEffectData '{skillEffectData.statusId}'를 찾지 못했습니다. (effect: {skillEffectData.effectId})");
    }

    protected override void Execute(BattleUnit caster, List<BattleUnit> targets)
    {
        float raw = CalcAmount(caster);
       
        // 지연일 때 상태이상 스택도 발사 시점 성급으로 고정해 둡니다.
        int stack = statusData != null ? skillEffectData.GetStatusStack(caster.Star) : 0;
        
        foreach (BattleUnit t in targets)
        {
            // 계산(치명타 RNG 포함)은 즉발이든 발사체든 항상 여기, 발사 시점에 합니다.
            float amount = DamageFormula.Resolve(caster, t, raw, EDamageSource.Skill, out bool isCrit);

            // 발사체면 적용만 뒤로 미룹니다.
            if (TravelTime > 0f)
            {
                InGameManagers.Battle.Hits.Dispatch(new PendingHit
                {
                    Handler = this,
                    Caster = caster,
                    Target = t,
                    Amount = amount,
                    IsCrit = isCrit,
                    Stack = stack,
                    Remaining = TravelTime,
                });
                continue;
            }
            
            t.TakeDamage(amount, caster, EDamageSource.Skill, isCrit);
            
            // 사망 처리(Die)는 다음 Tick의 CheckDeath에서 돕니다.
            // 같은 발동 안의 뒤 이펙트가 처치 여부를 알 수 있도록 여기서 기록해 둡니다.
            if(!t.IsAlive) context?.ReportKill(t);
            Logger.Log($"[스킬 발동] {Localization.GetLocaliationText(Localization.LocalizationTable.UNIT, caster.CurrentUnitData.UnitNameKey)} " +
                       $"-> {Localization.GetLocaliationText(Localization.LocalizationTable.UNIT, t.CurrentUnitData.UnitNameKey)} " +
                       $": {amount:F1} (남은 HP {t.CurrentHp:F1})");
        }
    }

    protected override void TryApplyStatus(BattleUnit caster, List<BattleUnit> targets)
    {
        if (statusData == null) return;

        // 발사체면 상태이상도 닿을 때 ApplyHit에서 넣습니다.
        // 여기서 넣으면 발사체가 날아가는 동안 대상에 화상 아이콘이 먼저 뜹니다.
        if (TravelTime > 0f) return;
        
        int stack = skillEffectData.GetStatusStack(caster.Star);
        for (int i = 0; i < targets.Count; i++)
            targets[i].ApplyStatus(statusData, caster, stack);
    }

    
    /// <summary>
    /// 발사체가 닿은 순간. PendingHitQueue가 부릅니다.
    /// 계산은 Execute에서 끝났으므로 여기선 hit에 담긴 값만 씁니다.
    /// </summary>
    public void ApplyHit(in PendingHit hit)
    {
        BattleUnit t = hit.Target;

        if (t == null || !t.IsAlive) return;
        
        t.TakeDamage(hit.Amount, hit.Caster, EDamageSource.Skill, hit.IsCrit);
        
        // context.ReportKill은 하지 않습니다.
        //   이 시점엔 시전이 이미 끝났고, context는 다음 시전용으로 재사용되기 때문입니다.
        //   (OnKill 트리거는 TakeDamage -> Die에서 따로 나가므로 영향 없습니다.)
 
        Logger.Log($"[스킬 적중] {Localization.GetLocaliationText(Localization.LocalizationTable.UNIT, hit.Caster.CurrentUnitData.UnitNameKey)} " +
                   $"-> {Localization.GetLocaliationText(Localization.LocalizationTable.UNIT, t.CurrentUnitData.UnitNameKey)} " +
                   $": {hit.Amount:F1} (남은 HP {t.CurrentHp:F1})");
 
        if (statusData != null && t.IsAlive)
            t.ApplyStatus(statusData, hit.Caster, hit.Stack);
    }
}
