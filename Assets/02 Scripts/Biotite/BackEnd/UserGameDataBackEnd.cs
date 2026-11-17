using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using Newtonsoft.Json;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using static BackEnd.Quobject.SocketIoClientDotNet.Parser.Parser.Encoder;



//유저의 게임 데이터의 로직을 담당합니다.
public class UserGameDataBackEnd
{

    /// <summary>
    /// 지정된 테이블 id에 게임 데이터를 삽입합니다.
    /// </summary>
    /// <param name="param"></param>
    /// <param name="tableID"></param>
    /// <returns></returns>
    public async UniTask<(bool isSuccess, string inDate)> InsertDataAsync(Param param, string tableID)
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();

        Backend.GameData.Insert(tableID, param, callback =>
        {
            utcs.TrySetResult(callback);
        });

        BackendReturnObject bro = await utcs.Task;

        if (bro.IsSuccess())
        {
            Logger.Log("데이터 추가 성공");
            return (true, bro.GetInDate());
        }

        Logger.LogError($"데이터 추가 실패 {bro.GetMessage()}");

        return (false, bro.GetInDate());
    }

    
    /// <summary>
    /// 지정된 테이블 id의 유저 데이터를 비동기로 로드합니다.
    /// </summary>
    /// <param name="tableID"></param>
    /// <returns></returns>
    public async UniTask<(bool isSuccess, JsonData data)> LoadUserDataAsync(string tableID)
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();

        Backend.GameData.GetMyData(tableID, new Where(), callback =>
        {
            utcs.TrySetResult(callback);
        });

        BackendReturnObject bro = await utcs.Task;

        if (bro.IsSuccess())
        {
            JsonData json = bro.GetFlattenJSON()["rows"];
            if (json.Count > 0)
            {
                Logger.Log($"{tableID} 의 데이터를 로드 했습니다.");
                return (true ,json[0]);
            }
        }
        else
        {
            Logger.LogError($"{tableID} 데이터 로드를 실패했습니다. : {bro.GetMessage()}");
        }
        
        return (false, null);
    }

    
    
   /// <summary>
   /// 지정된 테이블 id 의 데이터를 비동기로 저장합니다.
   /// </summary>
   /// <param name="inDate"></param>
   /// <param name="param"></param>
   /// <param name="tableID"></param>
   /// <returns></returns>
    public async UniTask<bool> SaveUserDataAsync(string inDate, Param param,string tableID)
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();

        Backend.GameData.UpdateV2(tableID, inDate, Backend.UserInDate ,param, callback => { 
            utcs.TrySetResult(callback);
        });

        BackendReturnObject bro = await utcs.Task;

        if (bro.IsSuccess())
        {
            Logger.Log($"{tableID} 에 데이터를 저장했습니다");
            return true;
        }
        
        Logger.Log($"{tableID} 데이터 저장을 실패했습니다");
        return false;
    }

   
  /// <summary>
  /// 지정된 테이블 id 의 데이터를 동기로 저장합니다.
  /// </summary>
  /// <param name="inDate"></param>
  /// <param name="param"></param>
  /// <param name="tableID"></param>
  /// <returns></returns>
    public bool SaveUserData(string inDate, Param param, string tableID)
    {
        BackendReturnObject bro = Backend.GameData.UpdateV2(tableID, inDate, Backend.UserInDate, param);

        if (bro.IsSuccess())
        {
            Logger.Log($"{tableID} 에 데이터를 저장했습니다");
            return true;
        }
        
        Logger.Log($"{tableID} 데이터 저장을 실패했습니다");
        return false;
    }

}
