using System;
using System.Collections;
using System.Collections.Generic;
using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using Spine;
using UnityEngine;


//유저 서버 데이터의 추상 클래스 입니다.
public abstract class UserServerDataBase 
{
    public string InDate { get; protected set; }
    protected bool IsDirty { get; set; } = false;
    protected string tableId = string.Empty;



    /// <summary>
    /// 새로운 데이터를  추가합니다.
    /// </summary>
    /// <returns></returns>
    protected virtual async UniTask<bool> InsertNewData()
    {
        Param param = ToParam();
        var (isSuccess, inDate) = await GlobalManagers.BackEnd.UserGame.InsertDataAsync(param, tableId);
        if (isSuccess)
        {
            InDate = inDate;
            return true;
        }
        
        Logger.LogError($"{GetType()} 의 데이터를 새로 추가하는 것에 실패했습니다");
        return false;
    }

    
    /// <summary>
    /// 서버에서 데이터를 로드 합니다.
    /// </summary>
    /// <returns></returns>
    public virtual async UniTask<bool> LoadDataAsync()
    {
        var (isSuccess, data) = await GlobalManagers.BackEnd.UserGame.LoadUserDataAsync(tableId);

        if (!isSuccess)
        {
            SetDefaultData();
            bool res = await InsertNewData();
            return res;
        }

        try
        {
            ParsingData(data);
        }
        catch (Exception e)
        {
            Logger.LogError($"{tableId} 정보를 저장하는 데 실패했습니다. {e.Message}");
            return false;
        }
        
        return true;
    }

    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public virtual bool SaveData()
    {
        if (!IsDirty)
        {
            Logger.LogWarning($"{tableId} 에 바뀐 내용이 없어 저장하지 않습니다.");
            return true;
        }
        
        Param param = ToParam();
        bool res = GlobalManagers.BackEnd.UserGame.SaveUserData(InDate, param, tableId);
        if (res) IsDirty = false;
        return res;
    }

    
    public virtual async UniTask<bool> SaveDataAsync()
    {
        if (!IsDirty)
        {
            Logger.LogWarning($"{tableId} 에 바뀐 내용이 없어 저장하지 않습니다.");
            return true;
        }
        
        Param param = ToParam();
        bool res = await 
            GlobalManagers.BackEnd.UserGame.SaveUserDataAsync(InDate, param, tableId);
        if (res) IsDirty = false;
        return res;
    }

    
    
    public abstract void Initialize();
    protected abstract void ParsingData(JsonData data);
    public abstract Param ToParam();
    public abstract void SetDefaultData();
}
