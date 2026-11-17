
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AloneOnBoardStatPerkEffect : IPerkEffect
{
    [SerializeField] private List<StatPerkData> stats = new();
    
    /// <summary>
    /// 아군 전투 구역 인원을 확인하고, 혼자면 스탯을 적용합니다.
    /// </summary>
    public void Excute(BattleUnit unit, object source)
    {
        BattleManager battle = InGameManagers.Battle;

        BoardModel allyBoard = battle.GetBoard(unit, ETargetType.Team, out _);
        int count = allyBoard.GetBattleUnits().Count;

        if (count != 1)
        {
            Logger.Log($"[Perk] {source} : 아군 {count}명이라 발동하지 않습니다.");
            return;
        }

        foreach (StatPerkData s in stats)
        {
            PerkStatHelper.Add(unit, s.StatType, s.StatModifireType, s.Value, source);
            PerkStatHelper.ReStoreIfMaxHp(unit, s.StatType);
        }
    }

    /// <summary>
    /// 이번 전투에 붙인 스탯을 제거합니다.
    /// </summary>
    public void Release(BattleUnit unit, object source)
        => PerkStatHelper.RemoveAll(unit, source);


}
