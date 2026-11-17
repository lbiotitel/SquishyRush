using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;


//유저의 서버 데이터를 괸리하는 매니저 입니다.
public class UserServerDataManager : IManager
{
    private readonly Dictionary<Type, UserServerDataBase> userDataDict = new Dictionary<Type, UserServerDataBase>();
    private bool isInit = false;
    
    public bool Initialize()
    {
        if (isInit) return true;
        isInit = true; 
        RegistData<UserServerProfileData>();
        RegistData<UserServerCurrencyData>();
        RegistData<UserServerEnergyData>();
        RegistData<UserServerAttendanceData>();
        RegistData<UserServerUnitData>();
        RegistData<UserServerArtifactData>();
        RegistData<UserServerDailyQuestData>();
        RegistData<UserServerProgressData>();
        BindEvent();
        
        foreach(var userData in userDataDict.Values)
            userData.Initialize();

        return true;
    }


    private void BindEvent()
    {
        GlobalManagers.Event.AddEvent<OnDataSaveTimeEventData>(OnDataSaveAndLoadTimeEventData);
    }


    /// <summary>
    /// 데이터 모듈을 등록 합니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private void RegistData<T>() where T : UserServerDataBase, new()
    {
        var type = typeof(T);
        if (!userDataDict.ContainsKey(type))
            userDataDict.Add(type, new T());
    }



    /// <summary>
    /// 특정 데이터 모듈을 가지고 옵니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetData<T>()  where T : UserServerDataBase
    { 
        if(userDataDict.TryGetValue(typeof(T), out UserServerDataBase userData))
            return (T)userData;

        return null;
    }

    
    
    
    //일정주기마다 데이터를 저장합니다.
    private void OnDataSaveAndLoadTimeEventData(
        OnDataSaveTimeEventData e)
    {
        SaveUserDataAsync().Forget();
    }
    
    
    /// <summary>
    /// 유저의 데이터를 전부 로드 합니다.
    /// </summary>
    public async UniTask<bool> LoadUserDataAsync()
    {
        foreach (var data in userDataDict)
        {
            bool res = await data.Value.LoadDataAsync();
            if (!res)
            {
                Logger.LogError("초기 데이터 로드 진입에 실패했습니다");
                return false;
            }
        }
        
        return true;
    }



    /// <summary>
    /// 유저의 모든 데이터를 비동기로 저장합니다.
    /// </summary>
    /// <returns></returns>
    public async UniTask<bool> SaveUserDataAsync()
    {
        if (isInit && GlobalManagers.BackEnd != null)
        {
            GlobalManagers.Event.NotifyEvent<OnBeforeApplicationQuit>(new OnBeforeApplicationQuit());

            foreach (var data in userDataDict)
            {
                bool res = await data.Value.SaveDataAsync();
                if (!res)
                {
                    Logger.LogError("유저 데이터 저장 실패");
                    return false;
                }
            }
            Logger.Log("유저 데이터 저장 완료");
            return true;
        }

        return false;
    }


    /// <summary>
    /// 유저의 모든 데이터를 동기로 저장합니다.
    /// </summary>
    public bool SaveUserData()
    {
        if (GlobalManagers.BackEnd != null)
        {
            string activeScene = GlobalManagers.Scene.GetActiveSceneName();
            if (activeScene == nameof(EScene.Title) || activeScene == nameof(EScene.BootStrap)) 
                return false;
                
            GlobalManagers.Event.NotifyEvent<OnBeforeApplicationQuit>(new OnBeforeApplicationQuit());

            foreach (var data in userDataDict)
            {
                bool res = data.Value.SaveData();
                if (!res)
                {
                    Logger.LogError("데이터 동기 저장 실패");
                    return false;
                }
            }
            Logger.Log("유저 데이터 저장 완료");
            return true;
        }
        
        Logger.LogError("백엔드 초기화 하지 않은 상태로 로그인 요청");

        return false;
    }



    public void Release()
    {
        isInit = false;
        userDataDict?.Clear();
        GlobalManagers.Event.RemoveEvent<OnDataSaveTimeEventData>(OnDataSaveAndLoadTimeEventData);
    }

}
