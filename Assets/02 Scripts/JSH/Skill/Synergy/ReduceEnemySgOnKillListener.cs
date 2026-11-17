using System.Collections.Generic;

/// <summary>
/// SP_0033 (유령의 장난) ? 아군이 적을 처치하면 적 전체의 SG를 깎습니다.
///
/// 상태이상이 아닌 이유는 대상이 적 전체라서입니다. 상태이상은 걸린 유닛 하나에
/// 대한 것이라, 전역 반응을 표현하려면 결국 밖을 참조해야 합니다.
/// </summary>
public class ReduceEnemySgOnKillListener : ITriggerListener
{
    private readonly BoardModel targetBoard;
    private readonly bool ownerIsAlly;
    private readonly float percent;

    public ETriggerType Trigger => ETriggerType.OnKill;

    public ReduceEnemySgOnKillListener(BoardModel targetBoard, bool ownerIsAlly, float percent)
    {
        this.targetBoard = targetBoard;
        this.ownerIsAlly = ownerIsAlly;
        this.percent = percent;
    }

    public void OnTriggered(BattleUnit owner, BattleUnit other)
    {
        // OnKill은 죽인 쪽이 owner입니다. 우리 편이 죽였을 때만 반응합니다.
        if (owner == null || owner.IsAlly != ownerIsAlly) return;
        if (targetBoard == null) return;

        List<BattleUnit> units = targetBoard.GetBattleUnits();
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] == null || !units[i].IsAlive) continue;

            units[i].ReduceSG(percent);
        }
        
        Logger.Log($"[시너지] 유령의 장난 - 적 전체 SG {percent:F0}% 감소");
    }
}
