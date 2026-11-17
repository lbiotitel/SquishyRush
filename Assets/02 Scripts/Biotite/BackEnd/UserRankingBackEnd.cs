using BackEnd;
using BackEnd.BackndLitJson;
using BackEnd.Leaderboard;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;



//유저의 랭킹 데이터의 로직을 관리합니다.
public class UserRankingBackEnd
{


  /// <summary>
  /// 비동기로 유저의 랭킹 데이터를 업데이트 합니다.
  /// </summary>
  /// <param name="inDate"></param>
  /// <param name="param"></param>
  /// <returns></returns>
    public async UniTask<bool> UpdateRankingDataAsync(string inDate, Param param)
    {
        if (inDate == null || inDate == string.Empty)
        {
            Logger.LogError("유저의 데이터가 존재하지 않습니다.");
            return false;
        }

        var utcs = AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();
        Backend.Leaderboard.User.UpdateMyDataAndRefreshLeaderboard(GlobalDefines.DataTableName.RANKING_DATA_TABLE, GlobalDefines.DataTableName.USER_PROFILE_DATA_TABLE,
               inDate, param, (callback) =>
               {
                   utcs.TrySetResult(callback);
               });

        BackendReturnObject bro = await utcs.Task;

        if (bro.IsSuccess())
        {
            Logger.Log("랭킹 등록 완료");
            return true;
        }

        return false;
    }

  
    /// <summary>
    /// 동기로 유저 랭킹 데이터를 업데이터 합니다.
    /// </summary>
    /// <param name="userData"></param>
    /// <returns></returns>
    public bool UpdateRankingData(string inDate, Param param)
    {
        if (inDate == null || inDate == string.Empty)
        {
            Logger.LogError("유저의 데이터가 존재하지 않습니다.");
            return false;
        }

        BackendReturnObject bro = Backend.Leaderboard.User.UpdateMyDataAndRefreshLeaderboard(GlobalDefines.DataTableName.RANKING_DATA_TABLE, GlobalDefines.DataTableName.USER_PROFILE_DATA_TABLE,inDate, param);

        if (bro.IsSuccess())
        {
            Logger.Log("랭킹 등록 완료");
            return true;
        }

        return false;
    }

    
    
    /// <summary>
    /// 현재 접속한 유저의 랭킹 데이터를 가져옵니다.
    /// </summary>
    /// <returns></returns>
    public async UniTask<(bool isSuccess, UserLeaderboardItem result)> GetUserRankingData()
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendUserLeaderboardReturnObject>.Create();

        Backend.Leaderboard.User.GetMyLeaderboard(GlobalDefines.DataTableName.RANKING_DATA_TABLE,0,(callback) => {
            utcs.TrySetResult(callback);
        });

        BackendUserLeaderboardReturnObject bro = await utcs.Task;
        

        if (bro.IsSuccess())
        {
            return (true,bro.GetUserLeaderboardList()[0]);
        }

        return (false, null);
    }


   /// <summary>
   /// 유저들의 랭크 데이터를 가져옵니다.
   /// </summary>
   /// <param name="count">가져올 데이터의 개 수 입니다.,</param>
   /// <returns></returns>
    public async UniTask<(bool isSuccess,List<UserLeaderboardItem> result)> GetUserRankingData(int count)
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendUserLeaderboardReturnObject>.Create();

        Backend.Leaderboard.User.GetLeaderboard(GlobalDefines.DataTableName.RANKING_DATA_TABLE, count, (callback) => {
            utcs.TrySetResult(callback);
        });

        BackendUserLeaderboardReturnObject bro = await utcs.Task;

        if (bro.IsSuccess())
        { 
            return  (true,bro.GetUserLeaderboardList());
        }

        return (false, null);
    }
}
