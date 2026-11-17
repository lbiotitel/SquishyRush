using System;
using System.Collections.Generic;
using UnityEngine;



[Serializable]
struct StatPerkData
{
    public EStatType StatType;  
    public EStatModifireType  StatModifireType;
    public float Value;
}

//스탯을 변화시키는 퍽 이펙트입니다.
[Serializable]
public class StatPerkEffect : IPerkEffect
{
    [SerializeField] private List<StatPerkData> statPerkData;
    
    
    public void Excute(BattleUnit unit, object source)
    {
        foreach (StatPerkData s in statPerkData)
        {
            PerkStatHelper.Add(unit, s.StatType, s.StatModifireType, s.Value, source);
        }
    }

    public void Release(BattleUnit unit, object source) => PerkStatHelper.RemoveAll(unit, source);

}
