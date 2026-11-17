using System;
using System.Collections.Generic;

/// <summary>
/// 한 진영의 시너지를 세고, 판정하고, 효과를 붙였다 뗍니다.
///
/// 진영마다 인스턴스를 하나씩 둡니다. 이 클래스는 자기가 아군인지 모릅니다.
/// Apply에 넘어온 ownBoard가 자기 진영이고 enemyBoard가 상대입니다.
///
/// 수명
///   상점 중 : Recount만 돌아 UI 프리뷰를 갱신합니다. 효과는 안 붙습니다.
///   전투 진입: Apply로 모디파이어와 상태이상을 붙입니다.
///   전투 종료: Clear로 뗍니다. ResetForNext보다 먼저 불러야 합니다.
/// </summary>
public class SynergySystem
{
    private readonly SynergyCounter counter = new();
    private readonly List<ActiveSynergy> active = new();

    //Apply가 모디파이어를 붙인 유닛들. Clear에서 되짚어 뗍니다.
    private readonly List<BattleUnit> touched = new();

    //대상 수집용 재사용 버퍼. 매 효과마다 새로 만들지 않습니다.
    private readonly List<BattleUnit> targetBuffer = new();
    private readonly List<BattleUnit> contributorBuffer = new();

    private readonly List<ITriggerListener> listeners = new();

    private SynergyResolver resolver;
    
    public SynergyCounter Counter => counter;
    public IReadOnlyList<ActiveSynergy> Active => active;
    public bool IsApplied { get; private set; }

    /// <summary>Recount가 끝날 때마다 알림. UI가 구독해 목록을 갱신합니다.</summary>
    public event Action OnRecounted;
    
    public void Init(SynergyResolver resolver)
    {
        this.resolver = resolver;
    }

    /// <summary>
    /// 보드를 다시 세고 켜진 시너지를 갱신합니다. 효과는 건드리지 않습니다.
    /// 상점에서 배치가 바뀔 때마다 부릅니다.
    /// </summary>
    public void Recount(BoardModel board)
    {
        counter.Recount(board);
        resolver?.Resolve(counter, active);
        OnRecounted?.Invoke();
    }

    public void AddPermanentBonus(SynergyData axis, BoardModel board)
    {
        if (axis == null) return;
        
        if(axis.IsElementSynergy) counter.ForceIncrease(axis.TargetElement);
        else counter.ForceIncrease(axis.TargetRace);
        
        Recount(board);
        //Logger.Log($"[3성 보너스] {axis.SynergyId} 영구 +1 → {counter.Dump()}");
    }

    /// <summary>
    /// UI용 축별 상태. UI가 Resolver를 직접 알 필요 없도록 여기서 전달합니다.
    /// </summary>
    public void GetStatus(List<SynergyStatus> result) => resolver?.CollectStatus(counter, result);

    public bool Apply(BoardModel ownBoard, BoardModel enemyBoard)
    {
        if (IsApplied)
        {
            Logger.LogWarning("[시너지] 이미 적용된 상태에서 Apply가 다시 불렸습니다. 무시합니다.");
            return false;
        }
        
        Recount(ownBoard);

        bool maxHpChanged = false;
        bool ownerIsAlly = GetBoardSide(ownBoard);

        for (int i = 0; i < active.Count; i++)
        {
            SynergyData data = active[i].Data;
            CollectContributors(ownBoard, data, contributorBuffer);

            // 시전자가 필요한 효과(상태이상 피해 증폭 등)를 위해 제공 유닛 하나를 대표로 씁니다.
            BattleUnit caster = contributorBuffer.Count > 0 ? contributorBuffer[0] : null;

            List<string> effectIds = data.EffectIds;
            for (int e = 0; e < effectIds.Count; e++)
            {
                if(!GlobalManagers.Resource.TryGetResource(effectIds[e], out SkillEffectData effect))
                {
                    Logger.LogWarning($"[시너지] '{data.SynergyId}'의 이펙트 '{effectIds[e]}'를 찾지 못했습니다.");
                    continue;
                }

                if (ApplyEffect(effect, data, caster, ownBoard, enemyBoard, ownerIsAlly)) maxHpChanged = true;
            }
        }

        IsApplied = true;
        //Logger.Log($"[시너지] 적용 - {resolver?.Dump(active)}");
        return maxHpChanged;
    }

    /// <summary>
    /// 붙인 모디파이어를 전부 뗍니다.
    /// 상태이상은 ResetForNext의 ClearAll이 치우므로 여기서 건드리지 않습니다.
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < touched.Count; i++)
            touched[i]?.Stat?.RemoveAllModifiersFromSource(this);
        
        touched.Clear();
        listeners.Clear();
        IsApplied = false;
    }

    #region 효과적용

    /// <summary>최대체력을 건드렸으면 true.</summary>
    private bool ApplyEffect(SkillEffectData effectData, SynergyData data, BattleUnit caster,
        BoardModel ownBoard, BoardModel enemyBoard, bool ownerIsAlly)
    {
        CollectTargets(effectData, data, ownBoard, enemyBoard, targetBuffer);
        if (targetBuffer.Count == 0) return false;

        switch (effectData.effectType)
        {
            case EEffectType.StatModify:
                return ApplyStatModify(effectData, targetBuffer);
            
            case EEffectType.ApplyStatus:
                ApplyStatus(effectData, data, caster, targetBuffer, ownerIsAlly);
                return false;
            
            case EEffectType.Special:
                ApplySynergySpecial(effectData, data, ownBoard, enemyBoard, ownerIsAlly);
                return false;
            
            default:
                Logger.LogWarning($"[시너지] '{data.SynergyId}'에 지원하지 않는 EffectType {effectData.effectType}이 있습니다.");
                return false;
        }
    }

    private bool ApplyStatModify(SkillEffectData effectData, List<BattleUnit> targets)
    {
        float raw = RawModifierValue(effectData);
        if (raw == 0f) return false;

        StatModifier Make() => effectData.modifierType == EValueModifierType.Flat
            ? new StatModifier(raw, EStatModifireType.Add, 0, this)
            : new StatModifier(raw * 0.01f, EStatModifireType.PercentAdd, 1, this);

        for (int i = 0; i < targets.Count; i++)
        {
            BattleUnit unit = targets[i];
            if (unit?.Stat == null) continue;

            Stat stat = unit.Stat.GetStatObject(effectData.statType);
            if (stat == null) continue;
            
            stat.AddModifier(Make());
            
            if(!touched.Contains(unit)) touched.Add(unit);
        }

        return effectData.statType == EStatType.MaxHp;
    }

    /// <summary>
    /// 시너지가 상태이상을 겁니다(화염 결속 6단계 시작 화상 등).
    ///
    /// [미터기] caster는 지금처럼 대표 유닛(contributor[0])으로 둡니다.
    /// 화상 피해 배율(StatusDmgOf)이 이 유닛 스탯에서 나오기 때문에 계산은 바뀌지 않습니다.
    /// 대신 영수증(credit)을 시너지로 붙여서 기록은 시너지 행으로 갑니다.
    /// </summary>
    private void ApplyStatus(SkillEffectData effectData, SynergyData data, BattleUnit caster, 
        List<BattleUnit> targets, bool ownerIsAlly)
    {
        if (string.IsNullOrEmpty(effectData.statusId)) return;
        
        if(!GlobalManagers.Resource.TryGetResource(effectData.statusId, out StatusEffectData statusData))
        {
            Logger.LogWarning($"[시너지] StatusEffectData '{effectData.statusId}'를 찾지 못했습니다.");
            return;
        }

        int stack = effectData.GetStatusStack(0);
        float? overrideValue = effectData.HasStatusValueOverride
            ? RawModifierValue(effectData)
            : (float?)null;

        DamageCredit credit = DamageCredit.FromSynergy(data, ownerIsAlly);
        
        for (int i = 0; i < targets.Count; i++)
            targets[i]?.ApplyStatus(statusData, caster, stack, overrideValue);
    }

    /// <summary>
    /// 전투 시작에 한 번 도는 스페셜을 실행합니다.
    ///
    /// 트리거형(부활, 처치 시 반응)은 여기로 오지 않습니다. 그쪽은 사건을 기다려야 해서
    /// SkillTriggerSystem에 따로 붙습니다. 레지스트리에 없으면 조용히 넘어갑니다.
    ///
    /// [미터기] 드래곤 리스너에 SynergyData를 넘겨 피해가 시너지 몫으로 기록되게 합니다.
    /// </summary>
    private void ApplySynergySpecial(SkillEffectData effectData, SynergyData data, BoardModel ownBoard, 
        BoardModel enemyBoard, bool ownerIsAlly)
    {
        string specialId = effectData.specialSkillId;
        if (string.IsNullOrEmpty(specialId)) return;
        
        if(!GlobalManagers.Resource.TryGetResource(specialId, out SpecialSkillData specialData))
        {
            Logger.LogWarning($"[시너지] SpecialSkillData '{specialId}'를 찾지 못했습니다.");
            return;
        }

        if (specialData is ReduceEnemySgOnKillData sgData)
        {
            ReduceEnemySgOnKillListener listener =
                new ReduceEnemySgOnKillListener(enemyBoard, ownerIsAlly, sgData.Percent);
            
            InGameManagers.Battle.Trigger.AddListener(listener);
            listeners.Add(listener);
            return;
        }

        if (specialData is DamageOnAllyHealData healData)
        {
            DamageOnAllyHealListener listener =
                new DamageOnAllyHealListener(enemyBoard, ownerIsAlly, healData.Damage, data);
            
            InGameManagers.Battle.Trigger.AddListener(listener);
            listeners.Add(listener);
            return;
        }
        
        //전투 시작 시 한 번 도는 형태
        ISynergySpecial special = SynergySpecialRegistry.Create(specialId);
        if (special == null)
        {
            Logger.Log($"[시너지] '{data.SynergyId}' Special '{specialId}' 미구현 - 건너뜁니다.");
            return;
        }
        
        special.BindData(specialData);
        special.Apply(targetBuffer, this);
        
        for(int i = 0; i< targetBuffer.Count; i++)
            if(!touched.Contains(targetBuffer[i])) touched.Add(targetBuffer[i]);
    }

    private static bool GetBoardSide(BoardModel board)
    {
        List<BattleUnit> units = board.GetBattleUnits();
        return units.Count > 0 && units[0].IsAlly;
    }
    
    #endregion

    #region 대상 수집

    /// <summary>
    /// 시너지에는 시전자가 없어 앵커 기하(S_Cross, Column 등)를 쓸 수 없습니다.
    /// 실제로 쓰이는 조합은 셋뿐입니다.
    ///   All             : 해당 진영 전체
    ///   Single + Self   : 이 시너지를 제공한 유닛들 (거석의 보호막, 사신의 손길)
    ///   Single + Random : 무작위 1명
    /// </summary>
    private void CollectTargets(SkillEffectData effectData, SynergyData data,
        BoardModel ownBoard, BoardModel enemyBoard, List<BattleUnit> result)
    {
        result.Clear();

        BoardModel board = effectData.targetType == ETargetType.Enemy ? enemyBoard : ownBoard;
        if (board == null) return;

        List<BattleUnit> units = board.GetBattleUnits();

        if (effectData.areaType == EAreaType.All)
        {
            for (int i = 0; i < units.Count; i++)
                if (units[i] != null && units[i].IsAlive) result.Add(units[i]);
            return;
        }

        if (effectData.targetSelectType == ETargetSelectType.Self)
        {
            // 시너지의 "자신"은 이 시너지를 제공한 유닛입니다.
            CollectContributors(board, data, result);
            return;
        }

        BattleUnit picked = PickRandomAlive(units);
        if (picked != null) result.Add(picked);
    }

    /// <summary>이 시너지의 축(속성 또는 종족)을 가진 유닛들.</summary>
    private static void CollectContributors(BoardModel board, SynergyData data, List<BattleUnit> result)
    {
        result.Clear();
        if (board == null || data == null) return;

        List<BattleUnit> units = board.GetBattleUnits();
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            UnitData unitData = unit?.CurrentUnitData;
            if (unitData == null || !unit.IsAlive) continue;

            if(data.Matches(unitData)) result.Add(unit);
        }
    }

    /// <summary>
    /// 전투 난수는 BattleManager의 시드 난수를 씁니다.
    /// UnityEngine.Random을 쓰면 같은 시드로도 결과가 재현되지 않습니다.
    /// </summary>
    private static BattleUnit PickRandomAlive(List<BattleUnit> units)
    {
        int aliveCount = 0;
        for (int i = 0; i < units.Count;i++)
            if (units[i] != null && units[i].IsAlive)
                aliveCount++;

        if (aliveCount == 0) return null;

        var rng = InGameManagers.Battle?.Rng;
        int pick = rng != null ? rng.Next(aliveCount) : 0;

        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] == null || !units[i].IsAlive) continue;
            if (pick-- == 0) return units[i];
        }

        return null;
    }
    
    /// <summary>
    /// 시너지에는 성 개념이 없어 항상 0번 값을 씁니다.
    /// SkillEffectData.CalcValue는 StatPercent를 기준스탯에 곱해 버리므로 쓰지 않습니다.
    /// 여기서는 모디파이어 원시 수치가 필요합니다.
    /// </summary>
    private static float RawModifierValue(SkillEffectData effectData)
    {
        if (effectData.modifierValue == null || effectData.modifierValue.Length == 0) return 0f;
        return effectData.modifierValue[0];
    }
    
    #endregion
}
