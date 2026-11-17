using System;
using System.Collections.Generic;
using UnityEngine;

//전투 슬롯과 벤치 슬롯을 관리하는 데이터 모델
public class BoardModel
{
    private BattleUnit[] battle;
    private BattleUnit[] bench;
    
    public int BattleRows { get; }
    public int BattleCols { get; }
    public int BattleCapacity => battle.Length;
    public int BenchCapacity => bench.Length;

    public event Action OnBoardChanged;
    public event Action<BattleUnit> OnUnitMerged; //합성 흐름을 시작하라는 신호. 흐름의 첫 합성에서만 발생. MergeFlow 전용
    public event Action OnMergeHappened; //합성이 한 번 일어날 때마다 발생. 연출과 소리용
    
    // 유닛에게 못 들어간 재능을 알립니다.
    public event Action<IReadOnlyList<PerkDataBase>> OnPerksReturned;

    private readonly List<PerkDataBase> rejectedPerks = new();
    
    public BoardModel(int battleRows, int battleCols, int benchCapacity)
    {
        BattleRows = battleRows;
        BattleCols = battleCols;
        battle = new BattleUnit[battleRows * battleCols];
        bench = new BattleUnit[benchCapacity];
    }

    #region  조회
    
    //구역을 구분하는 부분
    private BattleUnit[] GetArray(EZone zone)
        => zone == EZone.Bench ? bench : battle;

    //구역 범위 인덱스 확인
    public bool IsValid(SlotIndex slot)
    {
        BattleUnit[] arr = GetArray(slot.Zone);
        return slot.Index >= 0 && slot.Index < arr.Length;
    }

    //그 칸의 유닛을 가져오는 부분(없으면 null)
    public BattleUnit Get(SlotIndex slot)
        => IsValid(slot) ? GetArray(slot.Zone)[slot.Index] : null;

    //빈칸인지 확인하는 함수
    public bool IsEmpty(SlotIndex slot) => Get(slot) == null;

    //벤치의 비워있는 슬롯을 가져오는 함수
    public bool TryGetEmptyBenchSlot(out SlotIndex slot)
    {
        for (int i = 0; i < bench.Length; i++)
        {
            if (bench[i] == null)
            {
                slot = new SlotIndex(EZone.Bench, i);
                return true;
            }
        }

        slot = default;
        return false;
    }

    public bool TryFindSlot(BattleUnit unit, out SlotIndex slot)
    {
        for (int i = 0; i < battle.Length; i++)
        {
            if (battle[i] == unit)
            {
                slot = new SlotIndex(EZone.Battle, i);
                return true;
            }
        }

        for (int i = 0; i < bench.Length; i++)
        {
            if (bench[i] == unit)
            {
                slot = new SlotIndex(EZone.Bench, i);
                return true;
            }
        }

        slot = default;
        return false;
    }

    public int ToIndex(int row, int col) => row * BattleCols + col;

    public void ToRowCol(int index, out int row, out int col)
    {
        row = index / BattleCols;
        col = index % BattleCols;
    }
    
    //전투 슬롯에 있는 팀 체력 계산함수
    public int GetTotalMaxHp()
    {
        int sum = 0;
        foreach(var unit in battle)
            if(unit != null) sum += Mathf.RoundToInt(unit.MaxHp);

        return sum;
    }

    public void RestoreHp()
    {
        foreach(var unit in battle)
            unit?.RestoreHp();
    }

    public bool IsAllDead()
    {
        foreach(var unit in battle)
            if (unit != null && unit.IsAlive)
                return false;
        return true;
    }

    //배치된 유닛 가져오는 함수
    public List<BattleUnit> GetBattleUnits()
    {
        List<BattleUnit> result = new List<BattleUnit>();
        for (int i = 0; i < battle.Length; i++)
        {
            if(battle[i] != null) result.Add(battle[i]);
        }
        return result;
    }

    // 전투 구역에 유닛이 하나라도 있는지
    public bool HasBattleUnit()
    {
        foreach(var unit in battle)
            if (unit != null) return true;
        return false;
    }
    
    #endregion
    
    #region 배치 / 이동 / 제거

    //빈칸이면 유닛 배치 함수
    public bool Place(SlotIndex slot, BattleUnit unit)
    {
        if (unit == null || !IsValid(slot)) return false;

        var arr = GetArray(slot.Zone);
        if (arr[slot.Index] != null) return false;

        arr[slot.Index] = unit;
        OnBoardChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 칸의 유닛을 다른 유닛으로 교체합니다. 기존 유닛은 버려집니다.
    /// </summary>
    public bool Replace(SlotIndex slot, BattleUnit unit)
    {
        if (unit == null || !IsValid(slot)) return false;
        
        GetArray(slot.Zone)[slot.Index] = unit;
        OnBoardChanged?.Invoke();
        return true;
    }

    //유닛 슬롯 이동함수(스왑가능)
    public bool Move(SlotIndex from, SlotIndex to)
    {
        if (!IsValid(from) || !IsValid(to)) return false;
        if (GetArray(from.Zone)[from.Index] == null) return false;
        
        var fromArr = GetArray(from.Zone);
        var toArr = GetArray(to.Zone);
        
        (fromArr[from.Index], toArr[to.Index]) = (toArr[to.Index], fromArr[from.Index]);
        
        OnBoardChanged?.Invoke();
        return true;
    }

    //제거된 유닛 반환(판매용)
    public BattleUnit Remove(SlotIndex slot)
    {
        if (!IsValid(slot)) return null;

        var arr = GetArray(slot.Zone);
        BattleUnit removed = arr[slot.Index];
        if (removed == null) return null;
        
        arr[slot.Index] = null;
        OnBoardChanged?.Invoke();
        return removed;
    }

    /// <summary>
    /// 벤치 유닛을 전투 구역의 빈 칸에 0번(0,0)부터 순서대로 옮깁니다.
    /// 여러 마리를 옮겨도 OnBoardChanged는 마지막에 한 번만 발행합니다.
    /// 옮긴 유닛 수를 반환합니다. (0이면 옮길 유닛이 없었음)
    /// </summary>
    public int DeployBenchToBattle()
    {
        int moved = 0;
        int battleIdx = 0;

        for (int b = 0; b < bench.Length; b++)
        {
            if (bench[b] == null) continue;
            
            // 다음 빈 전투 칸 찾기
            while (battleIdx < battle.Length && battle[battleIdx] != null) battleIdx++;
            if (battleIdx >= battle.Length) break; // 전투 구역이 가득 참

            battle[battleIdx] = bench[b];
            bench[b] = null;
            moved++;
        }
        
        if(moved > 0) OnBoardChanged?.Invoke();
        return moved;
    }
    
    #endregion

    #region 합성

    //같은 유닛 Id + 같은 별 합성 함수
    public bool TryFindMergeTarget(string unitId, int star, out SlotIndex result, SlotIndex? exclude = null)
    {
        result = default;
        if (star >= GlobalDefines.MAX_VALUE.IG_MAX_STAR) return false;
        
        return SearchZone(EZone.Battle, unitId, star, exclude, out result)
            || SearchZone(EZone.Bench, unitId, star, exclude, out result);    
    }

    //한 구역 서치하는 함수
    private bool SearchZone(EZone zone, string unitId, int star, SlotIndex? exclude, out SlotIndex result)
    {
        var arr = GetArray(zone);
        for (int i = 0; i < arr.Length; i++)
        {
            var unit = arr[i];
            if (unit == null) continue;
            if (unit.Star != star) continue;
            if (unit.CurrentUnitData.UnitId != unitId) continue;

            var slot = new SlotIndex(zone, i);
            if (exclude.HasValue && exclude.Value.Equals(slot)) continue;

            result = slot;
            return true;
        }

        result = default;
        return false;
    }

    //구매 가능 여부 판정 함수
    public bool CanAcceptNewUnit(string unitId)
        => TryFindMergeTarget(unitId, 0, out _) || TryGetEmptyBenchSlot(out _);

    
    /// <summary>
    /// 유닛을 보드에 추가합니다. 같은 유닛·같은 성급이 있으면 합성 1단계만 수행하고
    /// OnUnitMerged를 발행합니다. 이후 연쇄 합성은 MergeFlow가 TryMergeStep으로 이어갑니다.
    /// </summary>
    public bool AddUnit(BattleUnit unit)
    {
        if (unit == null) return false;

        string unitId = unit.CurrentUnitData.UnitId;

        if (TryFindMergeTarget(unitId, unit.Star, out SlotIndex target))
        {
            BattleUnit survivor = Get(target);
            
            rejectedPerks.Clear();
            BattleUnit.MergePerks(survivor, unit, prior: survivor, rejectedPerks);
            
            survivor.SetStar(survivor.Star + 1);
            NotifyPerksReturned();
            
            OnBoardChanged?.Invoke();
            OnMergeHappened?.Invoke();
            OnUnitMerged?.Invoke(survivor);
            
            return true;
        }

        if (!TryGetEmptyBenchSlot(out SlotIndex empty)) return false;

        bench[empty.Index] = unit;
        OnBoardChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 연쇄 합성을 1단계만 수행합니다. 합성됐으면 살아남은 유닛을 survivor로 반환합니다.
    /// 전투 구역 유닛과 벤치 유닛이 합쳐지면 전투 구역 쪽이 살아남습니다.
    /// 흡수되는 유닛의 재능은 합성 전에 살아남는 유닛으로 넘어갑니다.
    /// </summary>
    public bool TryMergeStep(BattleUnit unit, out BattleUnit survivor)
    {
        survivor = unit;
        if (unit == null || unit.Star >= GlobalDefines.MAX_VALUE.IG_MAX_STAR) return false;
        if (!TryFindSlot(unit, out SlotIndex slot)) return false;
        if(!TryFindMergeTarget(unit.CurrentUnitData.UnitId, unit.Star, out SlotIndex target, slot)) return false;

        BattleUnit absorbed;
        EZone survivorZone, absorbedZone; // 재능 우선순위(필드 우선) 판정용
        
        if (target.Zone == EZone.Battle && slot.Zone == EZone.Bench)
        {
            survivor = Get(target);
            absorbed = unit;
            survivorZone = target.Zone;
            absorbedZone = slot.Zone;
            GetArray(slot.Zone)[slot.Index] = null;
        }
        else
        {
            absorbed = Get(target);
            survivorZone = slot.Zone;
            absorbedZone = target.Zone;
            GetArray(target.Zone)[target.Index] = null;
        }

        // 유닛의 재능을 우선합니다. 둘 다 같은 구역이면 살아남는 쪽이 우선입니다.
        // 지금 규칙상 살아남는 쪽이 항상 필드거나 같은 구역이라 결과는 survivor 우선과 같지만,
        // 살아남는 규칙이 바뀌어도 재능 우선순위가 따로 유지되도록 구역으로 판정합니다.
        BattleUnit prior = (absorbedZone == EZone.Battle && survivorZone != EZone.Battle) ? absorbed : survivor;
        
        rejectedPerks.Clear();
        BattleUnit.MergePerks(survivor, absorbed, prior, rejectedPerks);
        
        survivor.SetStar(survivor.Star + 1);
        NotifyPerksReturned();
        
        OnBoardChanged?.Invoke();
        OnMergeHappened?.Invoke();
        
        return true;
    }

    /// <summary>
    /// 구매가 아닌 방법으로 칸에 새로 놓인 유닛(럭키7 교체 등)이 합성 가능하면 합성합니다.
    /// AddUnit과 같은 규칙입니다. 첫 합성만 여기서 하고 OnUnitMerged를 발행하면,
    /// 이어지는 연쇄 합성(2성 → 3성)은 MergeFlow가 처리합니다.
    /// 유닛이 이미 보드에 있어야 합니다. (TryMergeStep이 유닛의 칸을 찾기 때문)
    /// </summary>
    public void TryMergePlacedUnit(BattleUnit unit)
    {
        if (!TryMergeStep(unit, out BattleUnit survivor)) return;
        
        OnUnitMerged?.Invoke(survivor);
    }

    /// <summary>
    /// 유닛에게 못 들어간 재능을 알립니다.
    /// </summary>
    public void ReturnPerks(IReadOnlyList<PerkDataBase> perks)
    {
        if (perks == null || perks.Count == 0) return;
        OnPerksReturned?.Invoke(perks);
    }

    private void NotifyPerksReturned() => ReturnPerks(rejectedPerks);
    
    #endregion
    
    public void ExpandBench(int amount)
    {
        if (amount <= 0) return;
        Array.Resize(ref bench, bench.Length + amount);
        OnBoardChanged?.Invoke();
    }
}
