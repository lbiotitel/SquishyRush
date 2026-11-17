using System;
using System.Collections.Generic;

public abstract class SkillEffect
{
    protected SkillEffectData skillEffectData;
    
    /// <summary>이번 발동의 공유 정보. Run이 넣어줍니다.</summary>
    protected SkillCastContext context;
    private readonly List<BattleUnit> targets = new();

    private readonly OnVfxRequestedEventData vfxEvent = new();

    // Special은 여기 없습니다. ID로 클래스를 찾는 일은 SpecialEffectRegistry가 합니다.
    private static readonly Dictionary<EEffectType, Func<SkillEffect>> effectTypeDict = new()
    {
        { EEffectType.Damage, () => new DamageEffect() },
        { EEffectType.Heal, () => new HealEffect() },
        { EEffectType.ApplyStatus, () => new ApplyStatusEffect() },
    };

    /// <summary>
    /// 대상이 0명이면 Execute를 건너뛸지.
    /// 일반 이펙트는 대상이 없으면 할 일이 없지만, 스페셜은 그렇지 않을 수 있어 훅으로 뺍니다.
    /// </summary>
    protected virtual bool RequireTargets => true;
    
    /// 발사체 연출이면 비행 시간(초), 아니면 0. 연출과 같은 값이라 도착 프레임이 맞습니다.
    protected float TravelTime { get; private set; }
    
    public static SkillEffect Init(string skillEffectID)
    {
        if(!GlobalManagers.Resource.TryGetResource(skillEffectID, out SkillEffectData data))
            return null;

        SkillEffect effect = Create(data);
        if (effect == null) return null;
        
        effect.skillEffectData = data;
        effect.TravelTime = ResolveTravelTime(data.visualEffectId);
        effect.OnInit();
        return effect;
    }

    private static SkillEffect Create(SkillEffectData data)
    {
        if (data.effectType == EEffectType.Special)
            return CreateSpecial(data.specialSkillId);

        if (!effectTypeDict.TryGetValue(data.effectType, out var factory))
        {
            Logger.LogWarning($"'{data.effectId}'의 EffectType {data.effectType}에 대응하는 클래스가 없습니다.");
            return null;
        }

        return factory();
    }

    /// <summary>
    /// 스페셜은 두 조각을 합쳐서 만듭니다.
    ///   - 무엇을 하는가 : SpecialEffectRegistry가 ID로 실행 클래스를 만든다
    ///   - 얼마나 하는가 : SpecialSkillData(SO)가 수치를 들고 있다
    /// 둘 중 하나라도 없으면 이 이펙트는 만들지 않습니다.
    /// </summary>
    private static SkillEffect CreateSpecial(string specialSkillId)
    {
        SpecialEffect effect = SpecialEffectRegistry.Create(specialSkillId);
        if (effect == null) return null;

        if (!effect.RequiresData) return effect;

        if (!GlobalManagers.Resource.TryGetResource(specialSkillId, out SpecialSkillData data))
        {

            Logger.LogWarning(
                $"SpecialSkillData '{specialSkillId}'를 찾지 못했습니다. " +
                $"애셋 파일명과 Addressable 등록을 확인하세요.");
            return null;
        }
        Logger.Log($"[Special] '{specialSkillId}' 조회 결과: {(data == null ? "null" : data.GetType().Name)}");
        
        effect.BindData(data);
        
        if (effect.RequiresData && !effect.HasData)
        {
            Logger.LogWarning($"'{specialSkillId}' 데이터 바인딩 실패. 이 이펙트를 건너뜁니다.");
            return null;
        }
        
        return effect;
    }

    // 무엇이 발사체인지는 VisualEffectPreset만 판단합니다.
    private static float ResolveTravelTime(string vfxId)
    {
        if (string.IsNullOrEmpty(vfxId)) return 0f;
        if (!GlobalManagers.Resource.TryGetResource(vfxId, out VisualEffectData vfx)) return 0f;
        return VisualEffectPreset.IsProjectile(vfx.MoveType) ? vfx.TravelTime : 0f;
    }
    
    protected virtual void OnInit() { }

    /// <summary>
    /// 라운드가 넘어갈 때 이펙트가 들고 있던 런타임 상태를 되돌립니다.
    /// 발동 횟수를 세는 스페셜이 여기를 씁니다. Skill.ResetForNext가 전파합니다.
    /// </summary>
    public virtual void ResetForNext() { }
    
    /// <summary>
    /// 대상을 정하고 효과를 실행합니다. Skill만 부릅니다.
    /// 알고리즘의 뼈대는 여기서 고정하고, 무엇을 하는지는 파생이 채웁니다(Template Method).
    /// </summary>
    public void Run(BattleUnit caster, SkillCastContext ctx)
    {
        context = ctx;
        
        targets.Clear();
        ResolveTargets(caster);

        if (targets.Count == 0 && RequireTargets) return;

        PublishVfx(caster);
        
        Execute(caster, targets);
        TryApplyStatus(caster, targets);
    }

    private void PublishVfx(BattleUnit caster)
    {
        if (string.IsNullOrEmpty(skillEffectData.visualEffectId)) return;

        vfxEvent.VisualEffectId = skillEffectData.visualEffectId;
        vfxEvent.Caster = caster;
        vfxEvent.Targets = targets;
        vfxEvent.Scale = VisualEffectPreset.GetStarScale(caster.Star);
        
        GlobalManagers.Event.NotifyEvent(vfxEvent);
    }
    
    public void ResolveTargets(BattleUnit caster)
    {
        BoardModel board = InGameManagers.Battle.GetBoard(caster, skillEffectData.targetType, out bool boardIsAlly);

        if (skillEffectData.areaType == EAreaType.All)
        {
            AreaResolver.Resolve(EAreaType.All, default, false, board, targets);
            return;
        }

        if (!TargetSelector.TrySelect(skillEffectData.targetSelectType, caster, board, boardIsAlly,
                InGameManagers.Battle.Rng, out SlotIndex anchor))
            return;
        
        AreaResolver.Resolve(skillEffectData.areaType, anchor, boardIsAlly, board, targets);
    }
    
    protected abstract void Execute(BattleUnit caster, List<BattleUnit> targets);
    
    protected virtual void TryApplyStatus(BattleUnit caster, List<BattleUnit> targets) { }

    protected float CalcAmount(BattleUnit caster)
    {
        float baseStat = caster.Stat.GetStat(skillEffectData.statType);
        return skillEffectData.CalcValue(baseStat, caster.Star);
    }
}
