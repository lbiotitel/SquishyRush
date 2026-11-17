using System.Collections.Generic;

public class BonusDamageByOwnShieldSkill : SpecialEffect<BonusDamageByOwnShieldSkillData>
{
    protected override void Execute(BattleUnit caster, List<BattleUnit> targets)
    {
        float shield = caster.TotalShield;
        if (shield <= 0f) return;

        float amount = shield * Data.ShieldPercent * 0.01f;
        if (amount <= 0f) return;
        
        DealDamage(caster, targets, amount, EDamageSource.Skill);
        
        Logger.Log($"[곰토리] {caster.RuntimeId} 실드 {shield:F1} -> 추가 피해 {amount:F1} x{targets.Count}");
    }
}
