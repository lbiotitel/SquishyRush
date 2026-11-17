using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PerkStatHelper
{
    public static float ToModifierValue(EStatModifireType type, float value)
        => type == EStatModifireType.Add || type == EStatModifireType.FinalAdd ? value : value * 0.01f;
    
    public static void AddRaw(BattleUnit unit, EStatType stat, EStatModifireType type, 
        float value, object source)
        => unit.Stat.AddModifier(stat, new StatModifier(value, type, 0, source));
    
    public static void Add(BattleUnit unit, EStatType stat, EStatModifireType type,
        float dataValue, object source)
        => AddRaw(unit, stat, type, ToModifierValue(type, dataValue), source);
    
    public static void RemoveAll(BattleUnit unit, object source)
        => unit.Stat.RemoveAllModifiersFromSource(source);

    public static void ReStoreIfMaxHp(BattleUnit unit, EStatType stat)
    {
        if(stat == EStatType.MaxHp) unit.RestoreHp();
    }
}
