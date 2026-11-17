using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투에 등장할 연출 프리팹을 미리 풀에 채워둡니다.
///
/// PoolingSystem은 지연 생성이라, 손대지 않으면 각 스킬이 '처음 나갈 때' Instantiate가 일어납니다.
/// 전투 초반 몇 초 동안 스킬이 하나씩 처음 터지면서 프레임이 계속 튀는 이유입니다.
///
/// 58개를 전부 채우지 않습니다. 이번 전투에 올라온 유닛이 실제로 쓸 수 있는 것만 찾아서 채웁니다.
/// 데이터 체인을 거슬러 올라가면 목록이 나옵니다.
///
///   UnitData.SkillId -> SkillData.EffectIds -> SkillEffectData.visualEffectId
///   UnitData.NormalAttackVfxId 
///
/// 이 클래스는 '무엇을 채울지'만 정합니다. 실제로 꺼내고 돌려주는 일은 IVfxSpawner가 합니다.
/// </summary>
public class VfxPrewarmer
{
    private readonly IVfxSpawner spawner;

    // 같은 전투에서 두 번 데우지 않도록 기억해 둡니다.
    private readonly HashSet<string> warmed = new();

    //재사용을 위해 매번 새로 만들지 않습니다.
    private readonly List<VfxObject> borrowed = new();

    // 상태이상 연출은 유닛과 무관하게 전투당 한 번만 데우면 됩니다.
    // Prewarm이 라운드마다 불려도 GetResources(리스트 할당)를 반복하지 않으려고 따로 기억합니다.
    private bool statusWarmed;
    
    public VfxPrewarmer(IVfxSpawner spawner)
    {
        this.spawner = spawner;
    }

    public void Clear()
    {
        warmed.Clear();
        statusWarmed = false;
    }

    /// <summary>
    /// 유닛들이 쓸 연출을 countPerPrefab개씩 미리 만들어 둡니다.
    ///
    /// 꺼냈다가 바로 돌려주는 방식입니다. PoolingSystem에 "미리 만들어라"는 API가 없어서,
    /// 정상 경로를 그대로 태우는 편이 별도 진입점을 새로 뚫는 것보다 안전합니다.
    /// 화면 밖에서 한 프레임 안에 끝나므로 보이지 않습니다.
    /// </summary>
    public void Prewarm(IEnumerable<BattleUnit> units, int countPerPrefab = 2)
    {
        if (spawner == null || units == null) return;

        foreach (BattleUnit unit in units)
        {
            if (unit?.CurrentUnitData == null) continue;
            WarmSkill(unit.CurrentUnitData.SkillId, countPerPrefab);
            
            // 일반공격 발사체도 데웁니다.
            // 스킬보다 훨씬 자주 나가고, 비행 중에 다음 공격이 나가면 동시에 여러 개가 떠 있습니다.
            // 그래서 스킬의 2배를 채워 둡니다. 모자라면 PoolingSystem이 알아서 늘립니다.
            // 같은 연출을 쓰는 유닛이 여럿이어도 warmed가 중복을 거릅니다.
            WarmVisual(unit.CurrentUnitData.NormalAttackVfxId, countPerPrefab * 2);
        }
        
        WarmStatusEffects(countPerPrefab);
    }

    private void WarmSkill(string skillId, int count)
    {
        if (string.IsNullOrEmpty(skillId)) return;

        if(!GlobalManagers.Resource.TryGetResource(skillId, out SkillData skill))
            return;

        foreach (string effectId in skill.EffectIds)
        {
            if(!GlobalManagers.Resource.TryGetResource(effectId, out SkillEffectData effect))
                continue;

            WarmVisual(effect.visualEffectId, count);
        }
    }

    /// <summary>
    /// 연출이 달린 상태이상을 전부 데웁니다.
    ///
    /// [체인을 따라가지 않는 이유]
    /// 스킬은 UnitData에서 한 줄로 거슬러 올라가지만, 상태이상은 거는 곳이 너무 많습니다.
    /// 스킬 효과, 적중 시 부여(OnHitApply), 재능, 시너지, 유물...
    /// 이걸 다 따라가면 새로운 부여 경로가 생길 때마다 여기를 고쳐야 하고, 빠뜨리면 조용히 프레임이 튑니다.
    /// 연출이 달린 상태이상은 열 개 남짓이라 전부 채워도 비용이 거의 없습니다.
    ///
    /// [StatusVfxView도 이 덕을 봅니다]
    /// StatusVfxView는 자기 PoolingVfxSpawner를 따로 만들지만, 둘 다 같은 PoolingSystem을 감싸고 있고
    /// 풀은 프리팹 기준으로 나뉩니다. 그래서 여기서 데운 것을 지속형 연출도 그대로 꺼내 씁니다.
    ///
    /// [개수]
    /// 틱형과 지속형은 광역기 한 방에 여러 유닛이 동시에 걸리므로 넉넉히(2배),
    /// 부활은 동시에 여럿이 살아날 일이 드물어 기본 개수만 채웁니다.
    /// </summary>
    private void WarmStatusEffects(int count)
    {
        if (statusWarmed) return;
        statusWarmed = true;
        
        List<StatusEffectData> statuses = GlobalManagers.Resource.GetResources<StatusEffectData>();

        for (int i = 0; i < statuses.Count; i++)
        {
            StatusEffectData status = statuses[i];

            switch (status.VfxMode)
            {
                case EStatusVfxMode.OnTick:
                case EStatusVfxMode.WhileActive:
                    WarmVisual(status.VisualEffectId, count * 2);
                    break;
                
                case EStatusVfxMode.OnTrigger:
                    WarmVisual(status.VisualEffectId, count);
                    break;
            }
        }
    }

    private void WarmVisual(string visualEffectId, int count)
    {
        if (string.IsNullOrEmpty(visualEffectId)) return;
        if (!warmed.Add(visualEffectId)) return;

        if (!GlobalManagers.Resource.TryGetResource(visualEffectId, out VisualEffectData data) ||
            !data.IsPlayable) return;


        borrowed.Clear();

        for (int i = 0; i < count; i++)
        {
            VfxObject vfx = spawner.Spawn(data.EffectPrefab, Vector3.zero);
            if (vfx == null) break;

            borrowed.Add(vfx);
        }

        // 바로 반납해야 풀에 '대기 중'으로 쌓입니다.
        for (int i = 0; i < borrowed.Count; i++)
            spawner.Despawn(borrowed[i]);

        borrowed.Clear();
    }
}