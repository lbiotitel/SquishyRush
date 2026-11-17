using System;
using UnityEngine;

/// <summary>
/// 재능 효과: 최대 체력의 percent%만큼 자신을 회복합니다.
///
///  - 흡혈 : Start OnKill / isRepeat / percent 15
///
/// 시전자를 자신으로 넘기므로 자신의 힐량 증가(HealMultiplier)가 적용됩니다.
/// 회복 수치 표시(OnHealed)와 OnHeal 트리거도 일반 회복과 똑같이 발생합니다.
/// </summary>
[Serializable]
public class HealByMaxHpPerkEffect : IPerkEffect
{
    [SerializeField] private float percent = 15f;
    
    public void Excute(BattleUnit unit, object source)
    {
        float amount = unit.MaxHp * percent * 0.01f;
        unit.Heal(amount, unit);
    }

    public void Release(BattleUnit unit, object source) { }
}
