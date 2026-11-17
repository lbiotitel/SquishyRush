using System.Collections.Generic;
 
/// <summary>
/// 스킬 발동 시점을 중개합니다.
///
/// BattleUnit이 OnCast / OnHit / OnAttack / OnDeath / OnKill 을 이미 올려주고 있고,
/// 여기서 받아 해당 TriggerType을 가진 스킬만 발동시킵니다.
///
/// 중개를 거치는 이유는 두 가지입니다.
///  - 재귀 차단. 스킬이 적을 죽이고 그 OnKill이 또 스킬을 부르는 연쇄를 깊이로 끊습니다.
///  - 전투 시작 훑기. OnBattleStart는 누가 올려주는 이벤트가 아니라 전원을 한 번 도는 일입니다.
/// </summary>
public class SkillTriggerSystem
{
    private readonly Dictionary<ETriggerType, List<ITriggerListener>> listeners = new();
    private List<BattleUnit> units;

    private int depth;
    private const int MAX_DEPTH = 5;

    public void Init(List<BattleUnit> allUnits)
    {
        units = allUnits;
        depth = 0;
        listeners.Clear();
    }

    public void AddListener(ITriggerListener listener)
    {
        if (listener == null) return;

        if (!listeners.TryGetValue(listener.Trigger, out var list))
        {
            list = new List<ITriggerListener>();
            listeners[listener.Trigger] = list;
        }
        
        list.Add(listener);
    }

    public void RemoveListener(ITriggerListener listener)
    {
        if (listener == null) return;
        if (listeners.TryGetValue(listener.Trigger, out var list)) list.Remove(listener);
    }
    
    /// <summary>
    /// 트리거 발생. owner 본인의 스킬만 확인합니다.
    /// other는 상대(때린 쪽 또는 죽은 쪽)이며 지금은 참조만 넘깁니다.
    /// </summary>
    public void Raise(ETriggerType type, BattleUnit owner, BattleUnit other = null)
    {
        if (owner == null) return;

        if (depth >= MAX_DEPTH)
        {
            Logger.LogWarning($"[트리거] {type} 연쇄가 {MAX_DEPTH}겹을 넘어 중단했습니다. ({owner.RuntimeId})");
            return;
        }

        depth++;
        
        try
        {
            owner.TryCastByTrigger(type);
            
            // 유닛 소유가 아닌 반응도 같은 깊이 안에서 흘립니다.
            // 여기서 또 트리거가 나면 depth가 막아 줍니다.
            if(listeners.TryGetValue(type, out var list))
                for (int i = 0; i < list.Count; i++) list[i].OnTriggered(owner, other);
        }
        finally
        {
            depth--;
        }
    }

    public void RaiseBattleStart()
    {
        if (units == null) return;

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit u = units[i];
            if (u == null || !u.IsAlive) continue;
            
            Raise(ETriggerType.OnBattleStart, u);
        }
    }
}
