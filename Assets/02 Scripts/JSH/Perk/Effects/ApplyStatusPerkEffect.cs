using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 재능 상태이상의 대상입니다.
/// </summary>
public enum EPerkStatusTarget
{
    Self = 0,
    AllEnemies = 1,
}

[Serializable]
public class ApplyStatusPerkEffect : IPerkEffect
{
    [Header("상태이상")] 
    [SerializeField] private string statusId;
    [SerializeField] private int stack = 1;

    [Header("대상")] 
    [SerializeField] private EPerkStatusTarget target = EPerkStatusTarget.Self;
    
    [NonSerialized] private StatusEffectData cachedStatus;
    
    public void Excute(BattleUnit unit, object source)
    {
        StatusEffectData status = GetStatus();

        if (status == null) return;

        switch (target)
        {
            case EPerkStatusTarget.Self:
                unit.ApplyStatus(status, unit, stack);
                break;
            case EPerkStatusTarget.AllEnemies:
                ApplyToAllEnemies(unit, status);
                break;
        }
        
        Logger.Log($"[재능] {source} : {target}에게 {statusId} x{stack} 부여");
    }

    public void Release(BattleUnit unit, object source)
    {
        
    }

    private void ApplyToAllEnemies(BattleUnit owner, StatusEffectData status)
    {
        BattleManager battle = InGameManagers.Battle;
        
        if(battle ==null)
        {
            Logger.LogWarning($"[Perk] 전투 중이 아니라 '{statusId}'를 적에게 부여할 수 없습니다.");
            return;
        }

        BoardModel enemyBoard = battle.GetBoard(owner, ETargetType.Enemy, out _);
        List<BattleUnit> enemies = enemyBoard.GetBattleUnits();

        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].ApplyStatus(status, owner, stack);
        }
    }

    private StatusEffectData GetStatus()
    {
        if (cachedStatus != null) return cachedStatus;

        if(!GlobalManagers.Resource.TryGetResource(statusId, out cachedStatus))
            Logger.LogWarning($"[Perk] StatusEffectData '{statusId}'를 찾지 못했습니다.");

        return cachedStatus;
    }
}
