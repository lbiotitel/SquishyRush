using System.Collections.Generic;

/// <summary>
/// SP_0003 — 시전자를 즉시 사망 처리합니다. OnDeath 스킬이 있으면 이어서 발동합니다.
///
/// 수치가 하나도 없는 스페셜이라 SO 애셋을 만들지 않습니다.
/// RequiresData를 false로 두면 SkillEffect가 데이터 조회를 건너뜁니다.
/// 필드가 0개인 빈 ScriptableObject를 만들어 두는 것보다 정직합니다.
/// 나중에 수치가 생기면 SpecialEffect(TData)로 갈아타면 됩니다.
/// </summary>
public class SelfDestructSkill : SpecialEffect
{
    internal override bool RequiresData => false;

    protected override void Execute(BattleUnit caster, List<BattleUnit> targets)
    {
        if (caster == null || !caster.IsAlive) return;
        
        Logger.Log($"[자폭] {caster.RuntimeId} 자신을 사망 처리합니다.");
        
        // 이 한 줄이 OnDeath 트리거를 올립니다.
        // 자폭 스킬 자체가 OnDeath 트리거였다면 자기 자신을 다시 부르게 되는데,
        // SkillTriggerSystem의 MAX_DEPTH가 연쇄를 끊어주므로 무한 재귀는 나지 않습니다.
        caster.Kill();
    }
}
