using System;

/// <summary>
/// 전투 중 특정 트리거에 발동하는 재능입니다.
/// StartTrigger가 발생하면 효과를 적용하고, EndTrigger가 발생하거나 전투가 끝나면 해제합니다.
///
/// 트리거 수신은 시너지와 같은 경로(ITriggerListener + SkillTriggerSystem)를 사용합니다.
/// </summary>
public class TriggerPerk : PerkBase
{
    private TriggerHook startHook;
    private TriggerHook endHook;
    
    // 효과가 현재 걸려 있는지. 같은 트리거가 연속으로 와도 중복 적용되지 않게 막습니다.
    private bool isActive;
    //반복 가능한지 여부
    private bool isRepeat;
    
    /// <summary>
    /// 데이터의 시작/종료 트리거로 리스너를 미리 만들어 둡니다.
    /// 등록은 전투 진입 시점에 합니다.
    /// </summary>
    protected override void OnInitialized()
    {
        TriggerPerkData data = (TriggerPerkData)Data;
        isRepeat = data.IsRepeat;
        
        startHook = new TriggerHook(data.StartTrigger, Owner, OnStart);
        
        if(data.EndTrigger != ETriggerType.None)
            endHook = new TriggerHook(data.EndTrigger, Owner, OnEnd);
    }

    /// <summary>
    /// 트리거 시스템에 리스너를 등록합니다.
    /// SkillTriggerSystem.Init이 리스너를 비우므로 반드시 그 이후에 호출되어야 합니다.
    /// </summary>
    public override void OnBattleEnter()
    {
        SkillTriggerSystem trigger = InGameManagers.Battle.Trigger;
        
        trigger.AddListener(startHook);
        trigger.AddListener(endHook);
    }

    /// <summary>
    /// 전투 종료 시 리스너를 해제하고, 걸려 있던 효과도 정리합니다.
    /// </summary>
    public override void OnBattleExit() => Deactivate();


    /// <summary>
    /// 재능을 잃을 때 리스너와 효과를 모두 정리합니다.
    /// </summary>
    public override void OnRemoved() => Deactivate();


    /// <summary>
    /// 시작 트리거 수신.
    /// 반복 모드면 올 때마다 적용하고, 아니면 걸려 있지 않을 때만 적용합니다.
    /// </summary>
    private void OnStart()
    {
        if (isActive && !isRepeat) return;
        isActive = true;
        Execute();
    }
    
    /// <summary>
    /// 종료 트리거 수신. 효과가 걸려 있을 때만 해제합니다.
    /// </summary>
    private void OnEnd()
    {
        if (!isActive) return;
        isActive = false;
        Release();
    }
    
     
    /// <summary>
    /// 리스너 등록을 해제하고 걸려 있던 효과를 해제합니다.
    /// 씬 종료 등으로 Battle이 이미 없을 수 있어 null을 허용합니다.
    /// </summary>
    private void Deactivate()
    {
        SkillTriggerSystem trigger = InGameManagers.Battle?.Trigger;
        
        trigger?.RemoveListener(startHook);
        trigger?.RemoveListener(endHook);
        
        OnEnd();
    }
}
