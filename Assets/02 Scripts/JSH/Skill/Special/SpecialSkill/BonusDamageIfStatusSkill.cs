using System.Collections.Generic;

public class BonusDamageIfStatusSkill : SpecialEffect<BonusDamageIfStatusSkillData>
{
    private StatusEffectData requiredStatus;

    protected override void OnInit()
    {
        requiredStatus = ResolveStatus(Data.RequiredStatusId);
    }

    protected override void Execute(BattleUnit caster, List<BattleUnit> targets)
    {
        if (requiredStatus == null) return;

        float atk = caster.Stat.GetStat(EStatType.Atk);

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit t = targets[i];

            int stack = t.GetStatusStack(requiredStatus);
            if (stack <= 0) continue;

            float amount = atk * Data.BonusAtkPercent * 0.01f;
            if (Data.ScaleByStatusStack) amount *= stack;
            
            DealDamage(caster, t, amount, EDamageSource.Skill);
            
            Logger.Log($"[추가피해] {caster.RuntimeId} -> {t.RuntimeId} : {amount:F1} ({requiredStatus.StatusId} x{stack})");
        }
    }
}
