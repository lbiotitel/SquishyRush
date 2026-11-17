using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using System.Collections.Generic;
using BackEnd.Content;

// 모든 차트 데이터의 공통 인터페이스 (Dictionary 키로 사용할 int Key 제공)
public interface IChartData
{
    public string Key { get; }
}


public interface IDomainConvertible<TDomain>
{
    TDomain ToDomainData();
}


public class ChartBackEnd
{
    /// <summary>
    /// 서버 CDN에서 차트 데이터를 로드합니다.
    /// </summary>
    private async UniTask<Dictionary<string, T>> LoadChartDataAsync<T>(string folderID, string tableID) where T : IChartData
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendContentTableReturnObject>.Create();
        Backend.CDN.Content.Table.Get(folderID, callback => utcs.TrySetResult(callback));

        var bro = await utcs.Task;
        if (!bro.IsSuccess())
        {
            Logger.LogError($"[Chart] 테이블 목록 로드 실패 ({tableID}): {bro}");
            return null;
        }
        
        var contentCallback = Backend.CDN.Content.Get(bro.GetContentTableItemList());
        if (!contentCallback.IsSuccess())
        {
            Logger.LogError($"[Chart] 차트 바이너리 로드 실패 ({tableID}): {contentCallback}");
            return null;
        }
        
        var dic = contentCallback.GetContentDictionarySortByChartId();
        var result = new Dictionary<string, T>();

        foreach (var pair in dic)
        {
            ContentItem item = pair.Value;
            if (item.selectedChartFileId != tableID) continue;

            Logger.Log($"[Chart] 로드 성공 - 차트명: {item.chartName}, ID: {pair.Key}");
            
            foreach (JsonData row in item.contentJson)
            {
                string jsonString = row.ToJson();
                T data = JsonMapper.ToObject<T>(jsonString);

                if (!result.TryAdd(data.Key, data))
                {
                    Logger.LogWarning($"[Chart] 중복된 키 존재: {data.Key} (TableID: {tableID})");
                }
            }
        }

        return result;
    }
    
    
    /// <summary>
    /// 차트데이터를 로드한뒤 도메인 데이터로 변경하여 로드합니다.
    /// </summary>
    /// <param name="folderID"></param>
    /// <param name="tableID"></param>
    /// <typeparam name="TChartDataType"></typeparam>
    /// <typeparam name="TDomain"></typeparam>
    /// <returns></returns>
    public async UniTask<Dictionary<string, TDomain>> LoadDomainChartDataAsync<TChartDataType, TDomain>(string folderID, string tableID) 
        where TChartDataType : IChartData, IDomainConvertible<TDomain>
    {
        var chartData = await LoadChartDataAsync<TChartDataType>(folderID, tableID);
        if (chartData == null) return null;

        var result = new Dictionary<string, TDomain>();
        foreach (var pair in chartData)
        {
            result[pair.Key] = pair.Value.ToDomainData();
        }

        return result;
    }
}