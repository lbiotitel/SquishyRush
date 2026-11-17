using System;
using UnityEngine;

/// <summary>
/// 재능 효과: 현재 걸려 있는 실드 금액을 늘립니다.
///
///  - 철벽 : Start OnCast / isRepeat / increase 30 (= 1.3배)
///
/// OnCast는 스킬이 먼저 나간 뒤 재능이 반응하므로, 이번 스킬로 받은 실드까지 함께 늘어납니다.
/// 실드가 없으면 아무 일도 일어나지 않습니다.
/// </summary>
[Serializable]
public class ScaleShieldPerkEffect : IPerkEffect
{
    [SerializeField] private float increase = 30f;

    /// <summary>
    /// 걸려 있는 모든 실드에 (1 + 증가율)을 곱합니다.
    /// </summary>
    public void Excute(BattleUnit unit, object source)
    {
        float before = unit.TotalShield;
        if (before <= 0f) return;
        
        unit.ScaleShield(1f + increase * 0.01f);
        Logger.Log($"[Perk] {source} : {unit.RuntimeId} 실드 {before:F1} → {unit.TotalShield:F1}");
    }

    public void Release(BattleUnit unit, object source)
    {
        
    }
}
