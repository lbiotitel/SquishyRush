using System.Collections.Generic;
using UnityEngine;

public class HealEffect : SkillEffect
{
    private StatusEffectData statusData;
    
    protected override void OnInit()
    {
        if(!string.IsNullOrEmpty(skillEffectData.statusId))
            if(!GlobalManagers.Resource.TryGetResource(skillEffectData.statusId, out statusData))
                Logger.LogWarning($"[Heal] StatusEffectData '{skillEffectData.statusId}'를 찾지 못했습니다. (effect: {skillEffectData.effectId})");
    }

    protected override void Execute(BattleUnit caster, List<BattleUnit> targets)
    {
        float amount = CalcAmount(caster);
        bool isCrit = false;

        float ratio = Mathf.Min(caster.Stat.GetStat(EStatType.HealCrit), 100f);
        if (ratio > 0f)
        {
            isCrit = DamageFormula.RollCrit(caster, ratio * 0.01f);
            if (isCrit) amount *= caster.Stat.GetStat(EStatType.CriDamageMultiplier);
        }
        
        for (int i = 0; i < targets.Count; i++)
        {
            targets[i].Heal(amount, caster, null, isCrit);
            Logger.Log($"[스킬 발동] {caster.RuntimeId} {Localization.GetLocaliationText(Localization.LocalizationTable.UNIT, caster.CurrentUnitData.UnitNameKey)} " +
                       $"-> {targets[i].RuntimeId} {Localization.GetLocaliationText(Localization.LocalizationTable.UNIT, targets[i].CurrentUnitData.UnitNameKey)} " +
                       $": {amount:F1} (남은 HP {targets[i].CurrentHp:F1})");
        }
    }

    protected override void TryApplyStatus(BattleUnit caster, List<BattleUnit> targets)
    {
        if (statusData == null) return;

        int stack = skillEffectData.GetStatusStack(caster.Star);
        for(int i =0; i < targets.Count; i++)
            targets[i].ApplyStatus(statusData, caster, stack);
    }
}
