using System;
using UnityEngine;

/// <summary>
/// 재능 효과: 골드를 획득합니다. OneShotPerk에 붙여 사용합니다.
///
///  - 용돈 5 / 10 / 15 / 25
///
/// 골드 획득 배율(아티팩트 등)은 적용하지 않고 적힌 값 그대로 줍니다.
[Serializable]
public class AddGoldPerkEffect : IOneShotPerkEffect
{
    [SerializeField] private int amount = 5;

    public void Excute(BattleUnit unit, object source)
    {
        RunContext run = InGameManagers.Run;
        
        
        run.AddGold(amount, applyMultiplier: false);
        Logger.Log($"[Perk] {source} : 골드 +{amount} → {run.IGCurrency}");
    }

    public void Release(BattleUnit unit, object source) { }
    
}
