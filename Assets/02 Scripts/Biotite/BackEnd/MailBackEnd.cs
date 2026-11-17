using System.Collections;
using System.Collections.Generic;
using BackEnd;
using LitJson;
using Cysharp.Threading.Tasks;
using UnityEngine;


// 매일 과 서버에 대한 데이터 로직 입니다.
public class MailBackEnd
{
    /// <summary>
    /// 현재 가져올 수 있는 모든 매일을 가져옵니다.
    /// </summary>
    /// <returns></returns>
    public async UniTask<JsonData> GetMailListAsync(int count)
    {
        var utcs = AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();
        
        Backend.UPost.GetPostList(PostType.Admin, count, callback =>
        {
            utcs.TrySetResult(callback);
        });

        var bro = await utcs.Task;

        if (bro.IsSuccess())
        {
            JsonData json = bro.GetReturnValuetoJSON()["postList"];
            return json;
        }
        
        return null;
    }
    
    
    /// <summary>
    /// inDate에 해당하는 메일을 받습니다.
    /// </summary>
    /// <param name="inDate"></param>
    /// <returns></returns>
    public async UniTask<bool> RecieveMailAsync(string inDate)
    {
        var utcs =  AutoResetUniTaskCompletionSource<BackendReturnObject>.Create();    
        Backend.UPost.ReceivePostItem(PostType.Admin, inDate ,callback =>
        {
            utcs.TrySetResult(callback);
        });
        
        var bro = await utcs.Task;
        return bro.IsSuccess();
    }
}
