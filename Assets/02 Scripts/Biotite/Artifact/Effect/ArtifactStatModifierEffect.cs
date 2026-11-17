using System.Collections;
using System.Collections.Generic;
using UnityEngine;




//스탯을 변경하는 아티팩트 데이터 효과 입니다.
public class ArtifactStatModifierEffect : IArtifactEffect
{
    
    private EStatType statType;
    private EStatModifireType  statModifierType;
    private float value;
    
    
    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="statType"></param>
    /// <param name="statModifierType"></param>
    /// <param name="value"></param>
    public ArtifactStatModifierEffect(EStatType statType, EStatModifireType statModifierType, float value)
    {
        this.statType = statType;
        this.statModifierType = statModifierType;
        this.value = value;
    }
    

    
    /// <summary>
    /// 효과를 발동 합니다,
    /// </summary>
    public void ApplyEffect()
    {
        if(statModifierType != EStatModifireType.Add)
            Logger.LogWarning($"[아티팩트] {statType}의 모디파이어 타입이 {statModifierType}입니다. PlayerStat은 Add로만 합산하므로 Add로 처리합니다.");
        
        //플레이어 스탯 매니저에 스탯 추가
        Logger.Log($"{statType}애 {statModifierType} {value}의 모디파이어를 추가하였습니다.");
        StatModifier statModifier = new StatModifier(value, statModifierType, 0, this);
        InGameManagers.PlayerStat.AddModifier(statType, statModifier);
    }
}
