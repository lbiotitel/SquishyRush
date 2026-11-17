using System;
using UnityEngine;

/// <summary>
/// 재능 효과: 보유 골드에 비례해 공격력을 얻습니다.
///
///  - 재산은 힘이다 : Start OnBattleStart / ratio 50 (= 골드 / 2)
///
/// 전투 시작 시점의 골드로 계산하며, 전투가 끝나면 해제되고 다음 전투에서 그때 골드로 다시 계산합니다.
/// 배율(성급·버프)에 불어나지 않도록 모든 배율을 곱한 뒤 더하는 FinalAdd로 적용합니다.
/// </summary>
[Serializable]
public class GoldToAtkPerkEffect : IPerkEffect
{
    [SerializeField] private float ratio = 50f;

    /// <summary>
    /// 현재 골드 × 비율만큼 공격력을 더합니다.
    /// </summary>
    public void Excute(BattleUnit unit, object source)
    {
        RunContext run = InGameManagers.Run;

        float bonus = run.IGCurrency * ratio * 0.01f;
        if (bonus <= 0f) return;
        
        PerkStatHelper.AddRaw(unit, EStatType.Atk, EStatModifireType.FinalAdd, bonus, source);
        Logger.Log($"[Perk] {source} : 골드 {run.IGCurrency} × {ratio}% → {unit.RuntimeId} 공격력 +{bonus:F1}");
    }

    public void Release(BattleUnit unit, object source)
        => PerkStatHelper.RemoveAll(unit, source);

}
