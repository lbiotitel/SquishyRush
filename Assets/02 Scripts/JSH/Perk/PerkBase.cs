using System.Collections.Generic;

/// <summary>
/// 유닛이 보유한 런타임 재능의 부모 클래스입니다.
///
/// 유닛은 "지금 무슨 일이 일어났는지"(획득, 스탯 재빌드, 전투 진입/종료, 제거)만 알려주고,
/// 그 시점에 효과를 적용할지 해제할지는 하위 클래스가 결정합니다.
/// 하위 클래스는 필요한 훅만 override 하면 됩니다.
/// </summary>
public abstract class PerkBase
{
    private readonly List<IPerkEffect> effects = new();
    
    public BattleUnit Owner { get; private set; }
    public PerkDataBase Data { get; private set; }
    public string PerkId => Data.PerkId;

    public void Initialize(BattleUnit owner, PerkDataBase data)
    {
        Owner = owner;
        Data = data;
        
        effects.Clear();
        foreach (string effectId in data.EffectIds)
        {
            if(!GlobalManagers.Resource.TryGetResource<PerkEffectData>(effectId, out PerkEffectData effectData))
            {
                Logger.LogWarning($"[Perk] PerkEffectData '{effectId}'를 찾지 못했습니다. ({data.PerkId})");
                continue;
            }
            
            effects.AddRange(effectData.Effects);
        }
        
        OnInitialized();
    }

    #region 생명주기 훅
    
    /// <summary>
    /// Initialize가 끝난 직후 호출됩니다. 하위 클래스의 추가 준비 작업용입니다.
    /// </summary>
    protected virtual void OnInitialized() { }
    
    /// <summary>
    /// 유닛이 이 재능을 획득한 직후 호출됩니다. (선택 또는 합성 상속)
    /// </summary>
    public virtual void OnAcquired() {}
    
    /// <summary>
    /// 유닛의 스탯이 새로 만들어진 직후 호출됩니다. (성급 상승 등)
    /// 이전에 붙인 스탯 모디파이어는 이미 사라진 상태입니다.
    /// </summary>
    public virtual void OnStatRebuilt() {}
    
    /// <summary>
    /// 전투가 시작될 때 호출됩니다. 트리거 시스템이 초기화된 이후입니다.
    /// </summary>
    public virtual void OnBattleEnter() {}
    
    /// <summary>
    /// 전투가 끝날 때 호출됩니다.
    /// </summary>
    public virtual void OnBattleExit() {}
    
    /// <summary>
    /// 유닛이 이 재능을 잃을 때 호출됩니다. (합성으로 흡수될 때 등)
    /// 걸어둔 효과와 등록한 리스너를 모두 정리해야 합니다.
    /// </summary>
    public virtual void OnRemoved() {}

    /// <summary>
    /// 합성으로 흡수된 유닛에게서 이 재능을 물려받았을 때 호출됩니다.
    /// 기본은 직접 획득과 같게 처리하며, 한 번만 실행돼야 하는 재능은 비워둡니다.
    /// </summary>
    public virtual void OnInherited() => OnAcquired();
    #endregion

    #region 효과 실행

    /// <summary>
    /// 캐싱된 모든 효과를 소유 유닛에게 적용합니다.
    /// </summary>
    protected void Execute()
    {
        foreach(IPerkEffect effect in effects)
            effect.Excute(Owner, this);
    }
    
    /// <summary>
    /// 일회성 효과를 제외한 효과만 적용합니다.
    /// 스탯 재빌드나 상속처럼 "이미 한 번 받은 재능을 다시 거는" 경우에 씁니다.
    /// </summary>
    protected void ExecutePersistent()
    {
        foreach (IPerkEffect effect in effects)
            if (effect is not IOneShotPerkEffect) effect.Excute(Owner, this);
    }

    /// <summary>
    /// 캐싱된 모든 효과를 소유 유닛에게서 해제합니다.
    /// </summary>
    protected void Release()
    {
        foreach (IPerkEffect effect in effects)
            effect.Release(Owner, this);
            
    }
    
    #endregion
}
