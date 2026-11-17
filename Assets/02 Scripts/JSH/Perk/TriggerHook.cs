using System;

/// <summary>
/// 트리거 시스템은 모든 유닛의 트리거를 흘려보내므로,
/// 소유 유닛이 일으킨 트리거만 걸러서 콜백을 호출합니다.
/// </summary>
public class TriggerHook : ITriggerListener
{
    private readonly BattleUnit owner;
    private readonly Action callback;
    
    public ETriggerType Trigger { get; }

    public TriggerHook(ETriggerType trigger, BattleUnit owner, Action callback)
    {
        Trigger = trigger;
        this.owner = owner;
        this.callback = callback;
    }

    /// <summary>
    /// 트리거를 일으킨 유닛이 소유자일 때만 콜백을 호출합니다.
    /// </summary>
    public void OnTriggered(BattleUnit triggeredUnit, BattleUnit other)
    {
        if(triggeredUnit == owner) callback?.Invoke();
    }
}
