using System.Collections.Generic;

/// <summary>
/// SP_0017 ? 전투 시작 시 아군 스킬 하나를 무작위로 골라, 이번 전투 동안 자기 스킬로 씁니다.
///
/// 교체된 스킬은 원래 트리거대로 발동합니다(OnCast면 SG가 찰 때, OnDeath면 죽을 때).
/// 예외는 OnBattleStart 스킬입니다. 그 시점이 바로 지금이라 즉시 한 번 쏩니다.
///
/// 수치는 카피한 쪽의 성급·스탯을 따릅니다.
/// 카피 스킬 보유자는 후보에서 뺍니다. 서로 베끼는 연쇄를 막기 위해서입니다.
/// 수치가 없어 SO를 만들지 않습니다.
/// </summary>
public class CopyAllySkillSkill : SpecialEffect
{
    public const string ID = "SP_0017";

    internal override bool RequiresData => false;

    private readonly List<BattleUnit> candidates = new();

    protected override void Execute(BattleUnit caster, List<BattleUnit> targets)
    {
        if (caster == null || !caster.IsAlive) return;

        BoardModel board = InGameManagers.Battle.GetBoard(caster, ETargetType.Team, out _);
        List<BattleUnit> allies = board.GetBattleUnits();
        
        candidates.Clear();
        for (int i = 0; i < allies.Count; i++)
        {
            BattleUnit u = allies[i];
            if (u == caster || !u.IsAlive) continue;
            if (u.SkillData == null || IsCopySkill(u.SkillData)) continue;
            
            candidates.Add(u);
        }

        if (candidates.Count == 0)
        {
            Logger.Log($"[카피] {caster.RuntimeId} 복사할 아군 스킬이 없습니다.");
            return;
        }

        SkillData copied = candidates[InGameManagers.Battle.Rng.Next(candidates.Count)].SkillData;

        Skill replaced = caster.ReplaceSkillForBattle(copied.SkillID);
        if (replaced == null) return;
        
        Logger.Log($"[카피] {caster.RuntimeId} -> {copied.SkillID} ({copied.TriggerType})");

        // Trigger.Raise를 쓰지 않는 이유: 시너지 같은 OnBattleStart 리스너가 이 유닛에 대해 한 번 더 돕니다.
        if (copied.TriggerType == ETriggerType.OnBattleStart)
        {
            replaced.TryCast();
            Logger.Log($"[카피] {caster.RuntimeId} 즉시 발동 완료\n{caster.DebugStatus()}");
        }

    }

    private static bool IsCopySkill(SkillData data)
    {
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
