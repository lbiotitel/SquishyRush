using System.Collections.Generic;
using UnityEngine;

public class ApplyStatusEffect : SkillEffect, IPendingHitHandler
{
    private StatusEffectData statusData;
    
    protected override void OnInit()
    {
        if(!GlobalManagers.Resource.TryGetResource(skillEffectData.statusId, out statusData))
            Logger.LogWarning($"[ApplyStatus] StatusEffectData '{skillEffectData.statusId}'를 찾지 못했습니다. (effect: {skillEffectData.effectId})");
    }

    protected override void Execute(BattleUnit caster, List<BattleUnit> targets)
    {
        if (statusData == null) return;
        
        // 스택과 덮어쓸 수치는 발사 시점 성급·스탯으로 굳힙니다.
        int stack = skillEffectData.GetStatusStack(caster.Star);

        float? overrideValue = null;
        if (skillEffectData.HasStatusValueOverride)
        {
            float baseStat = caster.Stat.GetStat(skillEffectData.statType);
            overrideValue = skillEffectData.CalcValue(baseStat, caster.Star);
        }

        for (int i = 0; i < targets.Count; i++)
        {
            if (TravelTime > 0f)
            {
                InGameManagers.Battle.Hits.Dispatch(new PendingHit
                {
                    Handler = this,
                    Caster = caster,
                    Target = targets[i],
                    Amount = overrideValue ?? 0f,
                    Stack = stack,
                    Remaining = TravelTime,
                });
                continue;
            }
            
            targets[i].ApplyStatus(statusData, caster, stack, overrideValue);
        }
    }

    // 발사체가 닿은 순간. 덮어쓰기 여부는 데이터가 정하므로 그때만 Amount를 꺼냅니다.
    public void ApplyHit(in PendingHit hit)
    {
        BattleUnit t = hit.Target;
        if (t == null || !t.IsAlive) return;

        float? overrideValue = skillEffectData.HasStatusValueOverride ? hit.Amount : (float?)null;
        t.ApplyStatus(statusData, hit.Caster, hit.Stack, overrideValue);
    }
    
}
