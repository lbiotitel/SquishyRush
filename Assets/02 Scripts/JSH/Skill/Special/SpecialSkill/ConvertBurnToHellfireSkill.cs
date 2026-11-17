using System.Collections.Generic;
using UnityEngine;


public class ConvertBurnToHellfireSkill : SpecialEffect<ConvertBurnToHellfireSkillData>
{
    private StatusEffectData burnData;

    protected override void OnInit()
    {
        burnData = ResolveStatus(Data.BurnStatusId);
    }

    protected override void Execute(BattleUnit caster, List<BattleUnit> targets)
    {
        if (burnData == null) return;

        float atk = caster.Stat.GetStat(EStatType.Atk);

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit t = targets[i];

            int burn = t.GetStatusStack(burnData);
            if (burn < Data.RequiredBurnStack) continue;

            float amount = atk * Data.HellfireAtkPercent * 0.01f;
            if (Data.ScaleByConsumedStack)
                amount *= Mathf.Max(1, burn / Data.RequiredBurnStack);
            
            t.RemoveStatus(burnData);
            
            DealDamage(caster, t, amount, EDamageSource.DoT);
            
            Logger.Log($"[지옥불] {t.RuntimeId} 화상 {burn}스택 소모 -> {amount:F1} 피해 (남은 HP {t.CurrentHp:F1})");
        }
    }
}
