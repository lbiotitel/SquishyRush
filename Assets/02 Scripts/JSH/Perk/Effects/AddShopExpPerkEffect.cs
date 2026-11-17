using System;
using UnityEngine;

/// <summary>
/// 재능 효과: 상점 경험치를 획득합니다. OneShotPerk에 붙여 사용합니다.
///
/// 레벨업 판정과 최대 레벨 처리는 RunContext.AddShopExp가 담당합니다.
/// </summary>
[Serializable]
public class AddShopExpPerkEffect : IOneShotPerkEffect
{
    [SerializeField] private int amount = 2;

    /// <summary>
    /// 상점 경험치를 amount만큼 더합니다.
    /// </summary>
    public void Excute(BattleUnit unit, object source)
    {
        RunContext run = InGameManagers.Run;
        
        run.AddShopExp(amount);
        Logger.Log($"[Perk] {source} : 상점 경험치 +{amount} → Lv{run.IGShopLevel} ({run.ShopExp}/{run.ShopExpReq})");
    }

    public void Release(BattleUnit unit, object source)
    {
    }
}
