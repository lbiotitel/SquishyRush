using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using System;
using System.Collections.Generic;


/// <summary>
/// 가챠로직에 관한 서비스 클래스 정의입니다.
/// </summary>
public class GachaBackEnd
{
    /// <summary>
    /// 가챠를 정해진 n번 진행합니다.
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public async UniTask<(bool isSuccess,JsonData result)> DoGachaAsync(string gachaTableId ,int n) 
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();
        Backend.Probability.GetProbabilitys(gachaTableId, n, (callback) =>
        {
            utcs.TrySetResult(callback);
        });

        BackendReturnObject bro = await utcs.Task;

        if (!bro.IsSuccess())
        {
            Logger.LogError("가챠 뽑기에 실패 하였습니다.");
            return (false, null);
        }
        
        JsonData json = bro.GetFlattenJSON()["elements"];
        
        return (true,  json);
    }

    
    
    
    /// <summary>
    /// 가챠결과를 트랜잭션에 묶어서 저장합니다.
    /// </summary>
    /// <param name="dataTableId"></param>
    /// <param name="targetParam"></param>
    /// <param name="targetRowInDate"></param>
    /// <param name="currencyTableId"></param>
    /// <param name="currencyParam"></param>
    /// <param name="currencyRowInDate"></param>
    /// <returns></returns>
    public async UniTask<bool> SaveGachaResultsAsync(string dataTableId, Param targetParam, string targetRowInDate ,string currencyTableId, Param currencyParam, string currencyRowInDate)
    {
        List<TransactionValue> transactionList = new List<TransactionValue>();

        string ownerInDate = Backend.UserInDate;
        
        transactionList.Add(TransactionValue.SetUpdateV2(dataTableId, targetRowInDate, ownerInDate , targetParam));
        transactionList.Add(TransactionValue.SetUpdateV2(currencyTableId, currencyRowInDate, ownerInDate,currencyParam));
        var utcs = AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();
        
        Backend.GameData.TransactionWriteV2(transactionList, (callback) =>
        {
            utcs.TrySetResult(callback);
        });

        var bro = await utcs.Task;

        return bro.IsSuccess();
    }
}
