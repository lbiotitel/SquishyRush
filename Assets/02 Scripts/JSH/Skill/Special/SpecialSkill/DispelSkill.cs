using System.Collections.Generic;

/// <summary>
/// SP_0016 ? 대상의 디버프를 전부 해제합니다.
/// 사용 유닛이 늘어나면 EEffectType.Dispel로 올립니다.
/// </summary>
public class DispelSkill : SpecialEffect
{
    internal override bool RequiresData => false;

    protected override void Execute(BattleUnit caster, List<BattleUnit> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            targets[i].ClearDebuffs();
            Logger.Log($"[디스펠] {caster.RuntimeId} -> {targets[i].RuntimeId}");
        }
    }
}
