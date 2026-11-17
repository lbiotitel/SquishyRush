using System.Collections.Generic;

public class GrantThornsByTeamShieldSkill : SpecialEffect<GrantThornsByTeamShieldSkillData>
{
    private StatusEffectData thornsData;

    protected override void OnInit()
    {
        thornsData = ResolveStatus(Data.ThornsStatusId);
    }

    protected override void Execute(BattleUnit caster, List<BattleUnit> targets)
    {
        if (thornsData == null) return;

        if (Data.GrantToAllAllies)
        {
            if (!AnyHasShield(targets)) return;
            
            for(int i = 0; i < targets.Count; i++)
                targets[i].ApplyStatus(thornsData, caster, Data.ThornsStack);
            
            Logger.Log($"[곰쿠키] 아군에 실드 보유 -> 아군 {targets.Count}명에게 가시 {Data.ThornsStack}스택");
            return;
        }

        int granted = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            if(targets[i].TotalShield <= 0f) continue;
            
            targets[i].ApplyStatus(thornsData, caster, Data.ThornsStack);
            granted++;
        }
        
        if(granted > 0)
            Logger.Log($"[곰쿠키] 실드 보유 아군 {granted}명에게 가시 {Data.ThornsStack}스택");
    }

    private static bool AnyHasShield(List<BattleUnit> targets)
    {
        for(int i = 0; i < targets.Count; i++)
            if (targets[i].TotalShield > 0f) return true;
        
        return false;
    }
}
