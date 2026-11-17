using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3성 보너스 규칙. 후보 풀 생성과 SynergyCounter가 같은 기준을 쓰도록 한 곳에 둡니다.
/// </summary>
public static class SynergyBonusRule
{
    /// <summary>보너스 후보에서 빠지는 종족인지. 골렘/죽음/드래곤은 +1 대상이 아닙니다.</summary>
    public static bool IsExcludedRace(ERace race)
        => race == ERace.Golem || race == ERace.Death || race == ERace.Dragon;

    /// <summary>
    /// 보너스를 주는 성급인지.
    /// </summary>
    public static bool IsBonusStar(int star) => star == GlobalDefines.MAX_VALUE.IG_MAX_STAR;
}

/// <summary>
/// 3성 보너스 선택 한 번(팝업 한 번)의 상태입니다.
///
/// 규칙
///  - 처음 3칸은 서로 겹치지 않게 뽑습니다.
///  - 각 칸은 리롤을 딱 1번 할 수 있습니다.
///  - 리롤하면 이번 선택에서 한 번이라도 나온 축(지금 떠 있는 다른 칸 포함)은 다시 안 나옵니다.
///  - 팝업이 닫히면 세션을 버립니다. 다음 3성 때는 새 세션이라 제외 목록도 비어 있습니다.
/// </summary>
public class SynergyBonusSession
{
    public const int SLOT_COUNT = 3;

    // 후보 풀. 축마다 1단계 SynergyData 하나씩 들어 있습니다. (아이콘/이름 표시 + 축 식별용)
    private readonly IReadOnlyList<SynergyData> pool;

    // 이번 세션에서 한 번이라도 화면에 나온 축. 뽑기 제외 기준입니다.
    private readonly HashSet<SynergyData> shown = new();

    private readonly SynergyData[] slots = new SynergyData[SLOT_COUNT];
    private readonly bool[] rerolled = new bool[SLOT_COUNT];

    // 뽑기용 재사용 버퍼
    private readonly List<SynergyData> drawBuffer = new();
    
    public SynergyBonusSession(IReadOnlyList<SynergyData> pool)
    {
        this.pool = pool;
        for (int i = 0; i < SLOT_COUNT; i++)
            slots[i] = Draw();
    }

    /// <summary>칸에 떠 있는 축. 풀이 3개보다 적으면 null일 수 있습니다.</summary>
    public SynergyData GetSlot(int index) => slots[index];

    /// <summary>이 칸이 아직 리롤 안 했고, 안 나온 후보가 남아 있으면 true.</summary>
    public bool CanReroll(int index)
        => slots[index] != null && !rerolled[index] && pool.Count > shown.Count;

    /// <summary>칸을 다시 뽑습니다. 리롤할 수 없으면 기존 값을 그대로 돌려줍니다.</summary>
    public SynergyData Reroll(int index)
    {
        if (!CanReroll(index)) return slots[index];

        rerolled[index] = true;
        slots[index] = Draw();
        return slots[index];
    }

    /// <summary>아직 안 나온 후보 중 하나를 뽑고 shown에 등록합니다.</summary>
    private SynergyData Draw()
    {
        drawBuffer.Clear();
        for (int i = 0; i < pool.Count; i++)
            if(!shown.Contains(pool[i]))
                drawBuffer.Add(pool[i]);

        if (drawBuffer.Count == 0) return null;

        SynergyData pick = drawBuffer[Random.Range(0, drawBuffer.Count)];
        shown.Add(pick);
        return pick;
    }
}
