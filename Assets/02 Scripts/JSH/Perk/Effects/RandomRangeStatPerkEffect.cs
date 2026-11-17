using System;
using UnityEngine;

[Serializable]
public class RandomRangeStatPerkEffect : IPerkEffect
{
    [SerializeField] private EStatType statType = EStatType.DmgMultiplier;
    [SerializeField] private EStatModifireType modifierType = EStatModifireType.PercentAdd;

    [SerializeField] private int min = -50;
    [SerializeField] private int max = 50;
    
    public void Excute(BattleUnit unit, object source)
    {
        BattleManager battle = InGameManagers.Battle;

        int lo = Mathf.Min(min, max);
        int hi = Mathf.Max(min, max);
        int rolled = battle.Rng.Next(lo, hi + 1);
        
        PerkStatHelper.Add(unit, statType,  modifierType, rolled, source);
        PerkStatHelper.ReStoreIfMaxHp(unit, statType);
        
        Logger.Log($"[Perk] {source} : {unit.RuntimeId} {statType} {rolled:+#;-#;0} ({modifierType})");
    }

    public void Release(BattleUnit unit, object source)
        => PerkStatHelper.RemoveAll(unit, source);
}
