using System.Collections.Generic;

public class RefillSgOnKillSkill : SpecialEffect<RefillSgOnKillSkillData>
{
    protected override void Execute(BattleUnit caster, List<BattleUnit> targets)
    {
        if (context == null || !context.HasKill) return;
        
        caster.RefillSG(Data.SgRefillPercent);
        
        Logger.Log($"[사신] {caster.RuntimeId} 처치 {context.KillCount}건 -> SG {Data.SgRefillPercent:F0}% 충전");
    }
}
