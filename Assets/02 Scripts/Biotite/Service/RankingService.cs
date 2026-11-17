using System;
using System.Collections.Generic;
using BackEnd.Leaderboard;
using BackEnd.Quobject.EngineIoClientDotNet.Modules;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class RankingService : IService
{
    private UserRankingBackEnd userRankingBackEnd;

    private UserRankData myRankData;
    private List<UserRankData> userRankDatas;

    public UserRankData MyRankData => myRankData;
    public IReadOnlyList<UserRankData> UserRankDatas => userRankDatas;
    
    public void Initialize()
    {
        userRankingBackEnd = GlobalManagers.BackEnd.UserRanking;
        LoadRankDatas().Forget();
        BindEvents();
    }


    private void BindEvents()
    {
        GlobalManagers.Event.AddEvent<OnDataSaveTimeEventData>(OnDataSaveAndLoadTimeEventDataHandler);
    }

    public void Release()
    {
        GlobalManagers.Event.RemoveEvent<OnDataSaveTimeEventData>(OnDataSaveAndLoadTimeEventDataHandler);
    }


    private void OnDataSaveAndLoadTimeEventDataHandler(OnDataSaveTimeEventData data)
        => LoadRankDatas().Forget();


    /// <summary>
    /// 랭킹데이터를 로드하여 캐싱합니다.
    /// </summary>
    private async UniTask LoadRankDatas()
    {
        var ((isMySuccess, myData), (isUserSuccess, userDatas)) = await UniTask.WhenAll(
            GetMyRank(),
            GetUserRankData(GlobalDefines.GameConstants.RANK_COUNT));

        if (isMySuccess)
            myRankData = myData;

        if (isUserSuccess)
            userRankDatas = userDatas;
    }

    /// <summary>
    /// 자신의 랭킹을 가져옵니다. 
    /// </summary>
    private async UniTask<(bool isSuccess, UserRankData data)> GetMyRank()
    {
        try
        {
            var (isSuccess, data) = await userRankingBackEnd.GetUserRankingData();

            if (!isSuccess)
            {
                Debug.LogWarning("[RankingService] 내 랭킹 데이터를 불러오지 못했습니다.");
                
                GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new OnBackEndErrorEventData()
                {
                    ErrorType = EBackEndErrorType.RequestFailed
                });
                
                return (false, new UserRankData()); 
            }

            UserRankData userRankData = new UserRankData();
            
            int.TryParse(data.rank, out userRankData.UserRank);
            int.TryParse(data.score, out userRankData.UserRp);
            userRankData.UserName = string.IsNullOrEmpty(data.nickname) ? "이름 없음" : data.nickname;
            
            if (data.extraData != null) 
                userRankData.ProfileImageId = data.extraData.ToString();
            
            return (true ,userRankData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RankingService] GetMyRank 에러 발생: {ex.Message}");
            return (false ,new UserRankData());
        }
    }
    

    /// <summary>
    /// 전체 랭킹 목록을 가져옵니다. 
    /// </summary>
    private async UniTask<(bool isSuccess, List<UserRankData> datas)> GetUserRankData(int count)
    {
        try
        {
            var (isSuccess, leaderBoardList) = await userRankingBackEnd.GetUserRankingData(count);

            if (!isSuccess)
            {
                Debug.LogWarning("[RankingService] 랭킹 리스트가 비어있거나 불러오지 못했습니다.");
                
                GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new OnBackEndErrorEventData()
                {
                    ErrorType = EBackEndErrorType.RequestFailed
                });
                
                return (false, null);
            }

            List<UserRankData> userRankList = new List<UserRankData>();
            
            foreach (var data in leaderBoardList)
            {
                UserRankData userRankData = new UserRankData();
                int.TryParse(data.rank, out userRankData.UserRank);
                int.TryParse(data.score, out userRankData.UserRp);
                userRankData.UserName = string.IsNullOrEmpty(data.nickname) ? "이름 없음" : data.nickname;

                if (data.extraData != null) 
                    userRankData.ProfileImageId = data.extraData.ToString();
                
                userRankList.Add(userRankData);
            }
        
            return (true, userRankList);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RankingService] GetUserRankData 에러 발생: {ex.Message}");
            return (false, null);
        }
    }
}