using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//진행사항에 대한 서비스 정의 입니다.
public class ProgressService : IService
{
    private UserServerProgressData userServerProgressData;
    public bool ShowUnLockUI { get; private set; } = false;
    public EStageType UnLockStageType { get; private set; }

    public void Initialize()
    {
        userServerProgressData = GlobalManagers.UserServerData.GetData<UserServerProgressData>();
    }

    public void Release()
    {

    }
    
    
    public void CompleteShowClearUI()
        => ShowUnLockUI = false;
    
    /// <summary>
    /// 스테이지의 클리어 유무를 리턴합니다.
    /// </summary>
    /// <param name="stageType"></param>
    /// <returns></returns>
    public bool GetStageProgress(EStageType stageType)
    {
        userServerProgressData.StageProgressDict.TryGetValue(stageType, out bool isUnLock);
        return isUnLock;
    }


    /// <summary>
    /// 스테이지 진행사항을 업데이트 합니다.
    /// </summary>
    /// <param name="stageType"></param>
    /// <param name="unLock"></param>
    public void SetStageProgress(EStageType stageType, bool unLock)
    {
        ShowUnLockUI = unLock;
        UnLockStageType = stageType;
        userServerProgressData.UpdateStageProgress(stageType, unLock);
    }

    

    /// <summary>
    /// 로비 튜토리얼 클리어 유무를 리턴합니다.
    /// </summary>
    public bool IsClearLobbyTutorial
        => userServerProgressData.IsClearLobbyTutorial;
    
    /// <summary>
    /// 인게임 튜토리얼 클리어 유무를 리턴합니다.
    /// </summary>
    public bool IsClearInGameTutorial
        => userServerProgressData.IsClearInGameTutorial;

    /// <summary>
    /// 로비 튜토리얼을 클리어합니다.
    /// </summary>
    public void ClearLobbyTutorial()
        => userServerProgressData.IsClearLobbyTutorial = true;
    
    /// <summary>
    /// 인게임 튜토리얼을 클리어 합니다.
    /// </summary>
    public void ClearInGameTutorial()
        => userServerProgressData.IsClearInGameTutorial = true;
}
