using System.Collections.Generic;
using System.Text;

/// <summary>
/// 한 진영의 전투 슬롯을 훑어 속성별 / 종족별 인원수를 셉니다.
///
/// 진영은 이 클래스가 모릅니다. 아군용 하나, 적군용 하나를 따로 두고
/// 각자 자기 BoardModel을 넘겨 받는 것으로 구분이 끝납니다.
///
/// 규칙
///  - 같은 유닛이 여러 번 배치돼 있어도 각각 1로 셉니다. 고유 유닛 판정이 아닙니다.
///  - 이중 종족은 양쪽에 각각 +1 됩니다. (이끼 골렘 = Plant 1 + Golem 1)
///  - 벤치는 세지 않습니다. GetBattleUnits가 전투 배열만 돌려줍니다.
///  - 생사를 보지 않습니다. 전투 중에 죽었다고 시너지가 빠지면 안 됩니다.
/// </summary>
public class SynergyCounter
{
    private readonly Dictionary<EElementType, int> elementCounts = new();
    private readonly Dictionary<ERace, int> raceCounts = new();
    
    private readonly Dictionary<EElementType,int> forceElementCounts = new();
    private readonly Dictionary<ERace, int> forceRaceCounts = new();
    
    public IReadOnlyDictionary<EElementType, int> ElementCounts => elementCounts;
    public IReadOnlyDictionary<ERace, int> RaceCounts => raceCounts;

    public int GetCount(EElementType element)
    {
        elementCounts.TryGetValue(element, out int count);
        forceElementCounts.TryGetValue(element, out int force);
        return count + force;
    }

    public int GetCount(ERace race)
    {
        raceCounts.TryGetValue(race, out int count);
        forceRaceCounts.TryGetValue(race, out int force);
        return count + force;
    }

    /// <summary>보드를 다시 훑어 카운트를 갱신합니다. 상점에서 배치가 바뀔 때마다 부릅니다.</summary>
    public void Recount(BoardModel board)
    {
        elementCounts.Clear();
        raceCounts.Clear();

        if (board == null) return;

        List<BattleUnit> units = board.GetBattleUnits();
        for (int i = 0; i < units.Count; i++)
        {
            UnitData data = units[i]?.CurrentUnitData;
            if (data == null) continue;

            Increase(elementCounts, data.ElementType);

            IReadOnlyList<ERace> races = data.Races;
            for (int r = 0; r < races.Count; r++)
            {
                if (races[r] == ERace.None) continue;
                Increase(raceCounts, races[r]);
            }
        }
    }

    private static void Increase<TKey>(Dictionary<TKey, int> dict, TKey key)
    {
        dict.TryGetValue(key, out int count);
        dict[key] = count + 1;
    }

    /// <summary>
    /// 3성 보너스로 속성 카운트를 영구히 +1 합니다.
    /// Recount는 elementCounts만 비우므로 이 값은 런이 끝날 때까지 유지됩니다.
    /// </summary>
    public void ForceIncrease(EElementType elementType)
    {
        if (elementType == EElementType.None) return;
        Increase(forceElementCounts, elementType);
    }

    
    /// <summary>
    /// 3성 보너스로 종족 카운트를 영구히 +1 합니다.
    /// 후보 단계에서 이미 걸러지지만, 제외 종족이 들어와도 무시하도록 한 번 더 막습니다.
    /// </summary>
    public void ForceIncrease(ERace race)
    {
        if(race == ERace.None || SynergyBonusRule.IsExcludedRace(race)) return;
        Increase(forceRaceCounts, race);
    }


    public string Dump()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.Append("속성 ");
        if (elementCounts.Count == 0) sb.Append("- ");
        foreach (var pair in elementCounts) sb.Append($"{pair.Key}:{pair.Value} ");

        sb.Append("| 종족 ");
        if (raceCounts.Count == 0) sb.Append("- ");
        foreach (var pair in raceCounts) sb.Append($"{pair.Key}:{pair.Value} ");

        return sb.ToString();
    }
}
