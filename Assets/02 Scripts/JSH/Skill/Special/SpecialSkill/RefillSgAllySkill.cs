using System.Collections.Generic;

/// <summary>
/// SP_0018 - 대상 아군의 SG를 시전자 성급에 맞는 비율만큼 즉시 채웁니다.
///
/// 대상은 SkillEffect 행의 TargetType/AreaType이 정합니다(깃털이: Team / All).
/// 같은 SP_0018 스킬을 가진 아군은 건너뜁니다. 깃털이 2기(또는 깃털이를 복사한 마법서)가
/// 서로를 채워 매 공격 차례마다 스킬이 나가는 연쇄를 막기 위해서입니다.
/// </summary>
public class RefillSgAllySkill : SpecialEffect<RefillSgAllySkillData>
{
    public const string ID = "SP_0018";

    protected override void Execute(BattleUnit caster, List<BattleUnit> targets)
    {
        if (caster == null) return;

        float percent = Data.GetRefillPercent(caster.Star);
        if (percent <= 0) return;

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit t = targets[i];
            if (t == null || !t.IsAlive) continue;
            if (t == caster)
            {
                if (!Data.IncludeCaster) continue; // 자기 자신은 이 옵션으로만 판단
            }
            else if (HasRefillSkill(t)) continue;   // 다른 깃털이끼리 서로 채우는 연쇄만 막음
            
            t.RefillSG(percent);
            Logger.Log($"[SG 충전] {caster.RuntimeId}({caster.Star}성) -> {t.RuntimeId} SG {percent:F0}%");
        }
    }

    private static bool HasRefillSkill(BattleUnit unit)
    {
        SkillData data = unit.SkillData;
        if (data == null) return false;

        var ids = data.EffectIds;
        for (int i = 0; i < ids.Count; i++)
        {
            if (GlobalManagers.Resource.TryGetResource(ids[i], out SkillEffectData e)
                && e.effectType == EEffectType.Special
                && e.specialSkillId == ID)
                return true;
        }

        return false;
    }
}
