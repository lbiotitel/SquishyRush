using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 재능 효과: 후보 상태이상 버프 중 하나를 무작위로 골라 자신에게 부여합니다.
///
///  - 랜덤 박스 : Start OnBattleStart / 후보 [광폭, 신속, 건강 ...] / stack 1
///
/// 시너지의 RandomBuffData와 같은 방식(상태이상 ID 목록 + 하나 선택)입니다.
/// 난수는 전투 시드(Battle.Rng)를 사용합니다.
/// </summary>
[Serializable]
public class RandomStatusPerkEffect : IPerkEffect
{
    [SerializeField] private List<string> statusIds = new();
    [SerializeField] private int stack = 1;
    
    public void Excute(BattleUnit unit, object source)
    {
        if (statusIds.Count == 0)
        {
            Logger.LogWarning($"[Perk] {source} : 랜덤 버프 후보가 비어 있습니다.");
            return;
        }

        BattleManager battle = InGameManagers.Battle;
        
        string pickedId = statusIds[battle.Rng.Next(statusIds.Count)];
        
        if(!GlobalManagers.Resource.TryGetResource<StatusEffectData>(pickedId, out StatusEffectData status))
        {
            Logger.LogWarning($"[Perk] {source} : StatusEffectData '{pickedId}'를 찾지 못했습니다.");
            return;
        }
        
        unit.ApplyStatus(status, unit, stack);
    }

    public void Release(BattleUnit unit, object source)
    {
        
    }
}
