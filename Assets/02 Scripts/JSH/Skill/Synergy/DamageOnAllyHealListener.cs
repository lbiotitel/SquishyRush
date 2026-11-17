using System.Collections.Generic;

/// <summary>
/// SP_0032 (드래곤) ? 아군이 회복하면 적 전체에 고정피해를 줍니다.
///
/// 재생 틱도 회복이라 초당 여러 번 발동합니다. 엑셀 설명이 "재생 포함"이라 의도된 동작이며,
/// 재생 유닛과 같이 쓰면 상당히 강합니다. 밸런스를 볼 때 이 점을 기억하세요.
///
/// [미터기] 피해에 드래곤 시너지 영수증을 붙입니다. attacker와 source(Skill)는 그대로라
/// 전투 판정은 바뀌지 않고, 미터기에서는 [시너지] 드래곤 행의 흰색으로 들어갑니다.
/// 드래곤이 몇 마리든 한 줄로 합산됩니다.
/// </summary>
public class DamageOnAllyHealListener : ITriggerListener
{
    private readonly BoardModel targetBoard;
    private readonly bool ownerIsAlly;
    private readonly float damage;
    private readonly DamageCredit credit;
    
    public ETriggerType Trigger => ETriggerType.OnHeal;

    public DamageOnAllyHealListener(BoardModel targetBoard, bool ownerIsAlly, float damage, SynergyData synergy)
    {
        this.targetBoard = targetBoard;
        this.ownerIsAlly = ownerIsAlly;
        this.damage = damage;
        credit = DamageCredit.FromSynergy(synergy, ownerIsAlly);
    }

    public void OnTriggered(BattleUnit owner, BattleUnit other)
    {
        // owner는 회복받은 유닛입니다. 이 시너지를 가진 진영일 때만 반응합니다.
        if (owner == null || owner.IsAlly != ownerIsAlly) return;
        if (targetBoard == null) return;

        List<BattleUnit> units = targetBoard.GetBattleUnits();
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] == null || !units[i].IsAlive) continue;

            units[i].TakeDamage(damage, null, EDamageSource.Skill, false, null, credit);
        }
        
        //Logger.Log($"[시너지] 드래곤 - 적 전체에 고정피해 {damage:F0}");
    }
}
