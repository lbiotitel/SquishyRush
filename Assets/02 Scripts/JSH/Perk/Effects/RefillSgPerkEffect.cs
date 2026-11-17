using System;
using UnityEngine;

/// <summary>
/// 재능 효과: 스킬 게이지(SG)를 최대치의 percent%만큼 충전합니다.
///
///  - 과충전 : Start OnBattleStart / percent 100
///
/// 충전만 하고 즉시 시전하지는 않습니다. 시전은 다음 일반 공격이 적중할 때 나갑니다.
/// (BattleUnit.RefillSG와 같은 규칙. 발동 시점을 공격 타이밍에 고정해 리플레이 재현성을 지킵니다)
/// </summary>
[Serializable]
public class RefillSgPerkEffect : IPerkEffect
{
    [SerializeField] private float percent = 100f;
    
    public void Excute(BattleUnit unit, object source)
    {
        unit.RefillSG(percent);    
    }

    public void Release(BattleUnit unit, object source)
    {
        
    }
}
