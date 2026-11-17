using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

//서버에서 로드한 차트 데이터를 관리합니다.
public class ChartDataManager : IManager
{
    private const string CHART_DATA_FOLDER_ID = "3268";
    private const string UnitLevelChartTableID = "257762";
    private const string UserLevelChartTableID = "253391";
    private const string ArtifactUpgardeChartTableID = "255416";
    private const string DailyRewardChartTableID = "257273"; 
    private const string DailyQuestChartTableID = "259348";
    
    private IReadOnlyDictionary<string, UserLevelData> UserLevelUpChartData { get; set; }
    private IReadOnlyDictionary<string, UnitUpgradeData> UnitLevelUpChartData { get; set; }
    private IReadOnlyDictionary<string, ArtifactUpgradeData> ArtifactUpgradeChartData { get; set; }
    private IReadOnlyDictionary<string, RewardData> DailyRewardChartData { get; set; }
    private IReadOnlyDictionary<string, DailyQuestData> DailyQuestChartData { get; set; }
    
    
    public bool IsInitialized { get; private set; } = false;
    
    
    public bool Initialize()
    {
        return true;
    }


    /// <summary>
    /// 차트 데이터를 서버에서 로드 합니다.
    /// </summary>
    /// <returns></returns>
    public async UniTask<bool> LoadChartDataAsync()
    {
        try
        {
            var chartLoader = GlobalManagers.BackEnd.Chart;
            
            (
                UserLevelUpChartData,
                UnitLevelUpChartData,
                ArtifactUpgradeChartData,
                DailyQuestChartData,
                DailyRewardChartData
            ) = await UniTask.WhenAll(
                chartLoader.LoadDomainChartDataAsync<UserLevelChartData, UserLevelData>(CHART_DATA_FOLDER_ID, UserLevelChartTableID),
                chartLoader.LoadDomainChartDataAsync<UnitLevelChartData, UnitUpgradeData>(CHART_DATA_FOLDER_ID, UnitLevelChartTableID),
                chartLoader.LoadDomainChartDataAsync<ArtifactUpgradeChartData, ArtifactUpgradeData>(CHART_DATA_FOLDER_ID, ArtifactUpgardeChartTableID),
                chartLoader.LoadDomainChartDataAsync<DailyQuestChartData, DailyQuestData>(CHART_DATA_FOLDER_ID, DailyQuestChartTableID),
                chartLoader.LoadDomainChartDataAsync<DailyRewardChartData, RewardData>(CHART_DATA_FOLDER_ID, DailyRewardChartTableID)
            );
            
            if (UnitLevelUpChartData == null || 
                UserLevelUpChartData == null || 
                ArtifactUpgradeChartData == null || 
                DailyQuestChartData == null || 
                DailyRewardChartData == null)
            {
                Logger.LogError("하나 이상의 차트 데이터 로드 실패");
                return false;
            }
            
            IsInitialized = true;
            Logger.Log("전역 차트 데이터 로드 완료");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"차트 데이터 로드 중 예외 발생: {ex.Message}");
            return false;
        }
    }
    
    
    /// <summary>
    /// 차트 Dictionary에서 안전하게 데이터를 조회하는 내부 범용 메서드
    /// </summary>
    private TValue GetChartValue<TValue>(IReadOnlyDictionary<string, TValue> dictionary, string key, string dataName = "데이터") where TValue : class
    {
        if (dictionary != null && dictionary.TryGetValue(key, out TValue data))
        {
            return data;
        }

        Logger.LogWarning($"[{dataName}] Key({key})에 해당하는 차트 데이터가 없습니다.");
        return null;
    }

    
    #region Unit

    public UnitUpgradeData GetUnitLevelChartData(int level)
    {
        return GetChartValue(UnitLevelUpChartData, level.ToString(), "유닛 레벨");
    }

    #endregion

    #region User

    public UserLevelData GetUserLevelChartData(int level)
    {
        return GetChartValue(UserLevelUpChartData, level.ToString(), "유저 레벨");
    }
    

    #endregion

    #region Artifact

    public ArtifactUpgradeData GetArtifactUpgradeData(int level)
    {
        return GetChartValue(ArtifactUpgradeChartData, level.ToString(), "아티팩트 승급");
    }
    
    #endregion

    #region Daily Reward & Quest
    
    /// <summary>
    /// 날짜에 해당하는 리워드 데이터를 반환합니다.
    /// </summary>
    /// <param name="day"></param>
    /// <returns></returns>
    public RewardData GetDailyReward(int day)
    {
        return GetChartValue(DailyRewardChartData, day.ToString(), "출석 보상");
    }
    
    
    /// <summary>
    /// 일일 퀘스트 데이터를 전부 불러옵니다.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyDictionary<string, DailyQuestData> GetDailyQuestDatas()
    {
        return DailyQuestChartData;
    }
    #endregion

    public void Release()
    {
        UserLevelUpChartData = null;
        UnitLevelUpChartData = null;
        DailyRewardChartData = null;
        DailyQuestChartData = null;
        ArtifactUpgradeChartData = null;
        IsInitialized = false;
    }
}
