using System;
using System.Collections.Generic;

/// <summary>
/// 재능 효과: 이 재능을 고른 유닛을 랜덤한 레전드 유닛으로 교체합니다.
///
///  - 럭키 7 : InstantPerkData + 이 효과 하나
///
/// 교체 규칙
///  - 후보 : 보유 여부와 무관하게 전체 레전드 (자기 자신과 같은 유닛은 제외)
///  - 성급 : 0성으로 초기화
///  - 레벨 : 새 레전드의 레벨 (보유 중이면 그 레벨, 아니면 1)
///  - 재능 : 기존 재능은 모두 해제되어 인벤토리로 돌아가고, 새 유닛은 재능 없이 시작
///
/// 기존 유닛 인스턴스를 재활용하지 않고 새로 만들어 칸을 교체합니다.
/// BoardView가 RuntimeId로 뷰를 관리하므로 뷰 교체는 자동으로 처리됩니다.
/// </summary>
[Serializable]
public class TransformToLegendPerkEffect : IOneShotPerkEffect
{
    private readonly List<UnitData> candidateBuffer = new();

    public void Excute(BattleUnit unit, object source)
    {
        RunContext run = InGameManagers.Run;
        BoardModel board = InGameManagers.Board;

        if (run == null || board == null)
        {
            Logger.LogWarning($"[Perk] {source} : 런 정보가 없어 유닛을 교체할 수 없습니다.");
            return;
        }

        if (!board.TryFindSlot(unit, out SlotIndex slot))
        {
            Logger.LogWarning($"[Perk] {source} : {unit.RuntimeId}가 보드에 없어 교체할 수 없습니다.");
            return;
        }

        UnitData legend = PickLegend(run, unit.CurrentUnitData.UnitId);
        if (legend == null)
        {
            Logger.LogWarning($"[Perk] {source} : 교체할 레전드 유닛이 없습니다.");
            return;
        }
        
        // 기존 재능을 해제해서 인벤토리로 돌려줍니다.
        // 럭키7 자신은 소모형이라 목록에 없으므로 돌아오지 않고,
        List<PerkDataBase> returned = new();
        unit.ReleasePerks(returned);
        board.ReturnPerks(returned);

        BattleUnit created = BattleUnitFactory.CreatePlayerUnit(run, legend);
        
        if(created.MaxHp <= 0f)
            Logger.LogError($"[Perk] {legend.UnitId}의 기본 스탯이 없습니다. 보유 유닛 스냅샷에 포함되지 않은 유닛입니다.");

        board.Replace(slot, created);
        
        board.TryMergePlacedUnit(created);
        
        Logger.Log($"[Perk] {source} : {unit.CurrentUnitData.UnitId} → {legend.UnitId} (Lv{created.Level}) 교체");
    }

    public void Release(BattleUnit unit, object source) { }

    /// <summary>
    /// 전체 유닛 데이터에서 레전드만 골라 하나를 뽑습니다. 난수는 런 시드를 씁니다.
    /// </summary>
    private UnitData PickLegend(RunContext run, string excludeUnitId)
    {
        List<UnitData> all = GlobalManagers.Resource.GetResources<UnitData>();
        if (all == null || all.Count == 0) return null;

        candidateBuffer.Clear();
        foreach (UnitData data in all)
        {
            if (data.UnitRarity != ERarity.Legend) continue;
            if (data.UnitId == excludeUnitId) continue;
            candidateBuffer.Add(data);
        }

        if (candidateBuffer.Count == 0) return null;
        return candidateBuffer[run.NextInt(candidateBuffer.Count)];
    }
}
