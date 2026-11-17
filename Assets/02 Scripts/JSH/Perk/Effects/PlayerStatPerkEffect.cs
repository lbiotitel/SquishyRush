using System;
using UnityEngine;

/// <summary>
/// 재능 효과: 플레이어 스탯(골드 획득 배율 등)에 모디파이어를 붙입니다.
/// 유닛 스탯이 아니라 런 전체에 적용되는 플레이어 스탯을 건드립니다.
/// </summary>
[Serializable]
public class PlayerStatPerkEffect : IPerkEffect
{
    [SerializeField] private EStatType statType;
    [SerializeField] private EStatModifireType modifireType;
    [SerializeField] private float value;

    public void Excute(BattleUnit unit, object source)
    {
        InGameManagers.PlayerStat.RemoveAllModifier(statType, source);
        InGameManagers.PlayerStat.AddModifier(statType, new StatModifier(value, modifireType, 0, source));
    }

    public void Release(BattleUnit unit, object source)
        => InGameManagers.PlayerStat.RemoveAllModifier(statType, source);
        
    
}
