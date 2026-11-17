using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 재능 효과: 아군 전투 구역 전체의 공격력을 합산해 일정 비율을 자신의 공격력에 더합니다.
///
///  - 힘의 갈망 : Start OnBattleStart / ratio 50 / includeSelf
///
/// 합산 시점은 전투 시작(OnBattleStart)입니다. 시너지·속성 보너스는 BattlePhase.Enter에서 먼저 걸리므로 포함되고,
/// 다른 유닛의 전투 시작 재능은 유닛 순서에 따라 포함되지 않을 수 있습니다. (결정론이라 결과는 항상 같습니다)
/// </summary>
[Serializable]
public class TeamAtkSumPerkEffect : IPerkEffect
{
    [SerializeField] private float ratio = 50f;
    [SerializeField] private bool includeSelf = false;

    public void Excute(BattleUnit unit, object source)
    {
        BattleManager battle = InGameManagers.Battle;

        BoardModel allyBoard = battle.GetBoard(unit, ETargetType.Team, out _);
        List<BattleUnit> allies = allyBoard.GetBattleUnits();

        float sum = 0f;
        for (int i = 0; i < allies.Count; i++)
        {
            if (!includeSelf && allies[i] == unit) continue;
            
            Stat atk = allies[i].Stat.GetStatObject(EStatType.Atk);
            sum += atk.CalculateWith(m => Equals(m.Source, InGameBalance.STAR_MODIFIER_SOURCE));
            Logger.Log(
                $"[힘의 갈망] {allies[i].RuntimeId} 최종 {atk.Value:F1} / 성급만 {atk.CalculateWith(m => Equals(m.Source, InGameBalance.STAR_MODIFIER_SOURCE)):F1}");
        }

        float bonus = sum * ratio * 0.01f;
        PerkStatHelper.AddRaw(unit, EStatType.Atk, EStatModifireType.FinalAdd, bonus, source);
        
        
    }
    
    public void Release(BattleUnit unit, object source) 
        => PerkStatHelper.RemoveAll(unit, source);


}
