using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 스테이지 데이터와 맵을 관리하고, 라운드별 적 보드를 만들어 줍니다.
/// 라운드 번호는 RunContext.RoundIdx(1부터)를 그대로 사용합니다.
/// </summary>
public class StageManager 
{
    private InGameStageDataContainer currentStage;
    private GameObject mapInstance;
    
    public InGameStageDataContainer CurrentStage => currentStage;
    public int FinalRound => currentStage?.FinalRound ?? 0;
    
    public SuddenDeathEnvView EnvView { get; private set; }

    /// <summary>
    /// 스테이지 타입에 맞는 데이터를 찾고 맵을 생성합니다.
    /// InGame 라벨로 로드된 컨테이너를 어드레스(= EStageType 이름, 예: "Forest")로 찾습니다.
    /// </summary>
    public bool Initialize(EStageType stageType, Transform mapRoot)
    {
        if(!GlobalManagers.Resource.TryGetResource(stageType.ToString(), out currentStage)) 
        {
            Logger.LogError($"[Stage] '{stageType}' 스테이지 데이터를 찾을 수 없습니다. 어드레스/InGame 라벨을 확인하세요.");
            return false;
        }

        if (FinalRound == 0)
        {
            Logger.LogError($"[Stage] '{stageType}' 스테이지에 라운드 데이터가 없습니다.");
            return false;
        }
        
        SpawnMap(mapRoot);
        Logger.Log($"[Stage] {currentStage.StageName} 시작 - 총 {FinalRound} 라운드");
        return true;
    }

    public bool IsPerkShopRound(int round) => CurrentStage != null && currentStage.IsPerkShopRound(round);

    #region 맵

    private void SpawnMap(Transform mapRoot)
    {
        ReleaseMap();

        if (currentStage.StageMapPrefab == null)
        {
            Logger.LogWarning($"[Stage] {currentStage.StageName} 맵 프리팹이 비어 있습니다.");
            return;
        }

        mapInstance = Object.Instantiate(currentStage.StageMapPrefab, mapRoot);
        
        EnvView = mapInstance.GetComponentInChildren<SuddenDeathEnvView>();
    }

    private void ReleaseMap()
    {
        if(mapInstance != null) Object.Destroy(mapInstance);
        mapInstance = null;
        EnvView = null;
    }

    public void Release()
    {
        ReleaseMap();
        currentStage = null;
    }

    #endregion

    #region 적 보드

    /// <param name="roundIdx">1부터 시작하는 라운드 번호</param>
    public InGameStageRowData GetRow(int roundIdx)
    {
        if (currentStage?.Rows == null) return null;

        int i = roundIdx - 1;
        if (i < 0 || i >= currentStage.Rows.Count) return null;

        return currentStage.Rows[i];
    }

    public BoardModel BuildEnemyBoard(RunContext ctx, int round, int battleRows, int battleCols)
    {
        BoardModel board = new BoardModel(battleRows, battleCols, 0);

        InGameStageRowData row = GetRow(round);
        if (row == null)
        {
            Logger.LogError($"[Stage] {ctx.RoundIdx}라운드 데이터가 없습니다.");
            return board;
        }

        if (row.IsPerkShop)
        {
            Logger.LogError($"[Stage] {round}라운드는 재능상점 라운드라 적이 없습니다. 호출하는 쪽의 라운드 계산을 확인하세요.");
            return board;
        }

        List<InGameStageRowUnitData> units = row.UnitRowDatas;
        int unitIdx = 0;

        for (int r = 0; r < row.grid.Length; r++)
        {
            bool[] cols = row.grid[r].cols;
            if (cols == null) continue;

            for (int c = 0; c < cols.Length; c++)
            {
                if(!cols[c]) continue;

                if (unitIdx >= units.Count)
                {
                    Logger.LogWarning($"[Stage] {ctx.RoundIdx}라운드: 배치 칸이 유닛 수({units.Count})보다 많습니다.");
                    return board;
                }
                
                InGameStageRowUnitData entry = units[unitIdx++];

                if (r >= battleRows || c >= battleCols)
                {
                    Logger.LogWarning($"[Stage] {ctx.RoundIdx}라운드: ({r},{c})는 보드 범위를 벗어납니다.");
                    continue;
                }
                
                if(!GlobalManagers.Resource.TryGetResource(entry.UnitId, out UnitData data))
                {
                    Logger.LogWarning($"[Stage] {ctx.RoundIdx}라운드: UnitData '{entry.UnitId}'를 찾지 못했습니다.");
                    continue;
                }

                BattleUnit unit = BattleUnitFactory.CreateEnemyUnit(ctx, data, entry.Level, entry.Star);
                board.Place(new SlotIndex(EZone.Battle, board.ToIndex(r, c)), unit);
            }
        }
        
        if (unitIdx < units.Count)
            Logger.LogWarning($"[Stage] {ctx.RoundIdx}라운드: 유닛 {units.Count}개 중 {unitIdx}개만 배치됐습니다. grid를 확인하세요.");
        
        return board;
    }
    
    #endregion

    public StageLineup BulidLineup(int maxVisible)
    {
        var lineup = new StageLineup();
        if (currentStage?.Rows == null) return lineup;

        lineup.Hidden = FindFinaleUnit();

        var seen = new HashSet<string>();
        if (lineup.Hidden != null) seen.Add(lineup.Hidden.UnitId);

        foreach (InGameStageRowData row in currentStage.Rows)
        {
            if (row == null || row.IsPerkShop) continue;

            foreach (InGameStageRowUnitData u in row.UnitRowDatas)
            {
                if(!seen.Add(u.UnitId)) continue;
                if (!GlobalManagers.Resource.TryGetResource(u.UnitId, out UnitData data)) continue;

                if (lineup.Visible.Count < maxVisible) lineup.Visible.Add(data);
                else lineup.Overflow++;
            }
        }

        return lineup;
    }

    /// Boss 라운드가 있으면 그 row, 없으면 마지막 전투 row에서 등급 → 성급이 가장 높은 유닛
    private UnitData FindFinaleUnit()
    {
        InGameStageRowData target = null;
        for (int i = currentStage.Rows.Count - 1; i >= 0; i--)
        {
            InGameStageRowData row = currentStage.Rows[i];
            if (row == null || row.IsPerkShop) continue;

            target ??= row;
            if (row.RoundType == ERoundType.Boss)
            {
                target = row;
                break;
            }
        }
        if(target == null) return null;

        UnitData best = null;
        int bestStar = -1;
        foreach (InGameStageRowUnitData u in target.UnitRowDatas)
        {
            if(!GlobalManagers.Resource.TryGetResource(u.UnitId, out UnitData data)) continue;

            bool better = best == null
                          || data.UnitRarity > best.UnitRarity
                          || (data.UnitRarity == best.UnitRarity && u.Star > bestStar);
            if (better)
            {
                best = data;
                bestStar = u.Star;
            }
        }

        return best;
    }
}

public class StageLineup
{
    public readonly List<UnitData> Visible = new();
    public UnitData Hidden;
    public int Overflow;
}
