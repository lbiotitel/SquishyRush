using System;
using System.Collections;
using System.Collections.Generic;
using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using UnityEngine;

public class UserServerProgressData : UserServerDataBase
{
    private DataDictionary<EStageType, bool> stageProgressDict;
    public IReadOnlyDictionary<EStageType, bool> StageProgressDict => stageProgressDict.Map;


    private bool isClearLobbyTutorial;

    public bool IsClearLobbyTutorial
    {
        get => isClearLobbyTutorial;
        set
        {
            if (isClearLobbyTutorial != value)
            {
                IsDirty = true;   
                isClearLobbyTutorial = value;
            }
        }
    }
    
    private bool isClearInGameTutorial;

    public bool IsClearInGameTutorial
    {
        get => isClearInGameTutorial;
        set 
        {
            if (isClearInGameTutorial != value)
            {
                IsDirty = true;
                isClearInGameTutorial = value;
            }
        }
    }


    public override void Initialize()
    {
        tableId = GlobalDefines.DataTableName.USER_PROGRESS_DATA_TABLE;
    }


    /// <summary>
    /// 새로운 스테이지 진행사항을 저장합니다.
    /// </summary>
    /// <param name="stageType"></param>
    /// <param name="isClear"></param>
    public void AddStageProgress(EStageType stageType, bool open)
    {
        if (!stageProgressDict.ContainsKey(stageType))
        {
            IsDirty = true;
            stageProgressDict.Add(stageType, open);
            return;
        }
        
        Logger.LogError($"{stageType} 중복된 스테이지 입니다.");
    }

    
    /// <summary>
    /// 스테이지 진행사항을 업데이트 합니다.
    /// </summary>
    /// <param name="stageType"></param>
    /// <param name="isClear"></param>
    public void UpdateStageProgress(EStageType stageType, bool open)
    {
        if (stageProgressDict.TryGetValue(stageType, out bool o))
        {
            IsDirty = true;
            stageProgressDict.UpdateData(stageType,  open);
            return;
        }
        
        Logger.LogError($"{stageType}에 해당하는 스테이지 데이터가 없습니다.");
    }

    
    
    /// <summary>
    /// artifactId에 해당하는 아티팩트 데이터를 삭제합니다.
    /// </summary>
    /// <param name="artifactId"></param>
    public void RemoveStageProgress(EStageType stageType)
    {
        if (stageProgressDict.TryGetValue(stageType, out bool clear))
        {
            IsDirty = true;
            stageProgressDict.Remove(stageType);
            return;
        }
        
        Logger.LogError($"{stageType}에 해당하는 스테이지 데이터가 없습니다.");
    }
    

    protected override void ParsingData(JsonData data)
    {
        // inDate
        if (data.Keys.Contains("inDate") && data["inDate"] != null)
            InDate = data["inDate"].ToString();
            
        //IsClearLobbyTutorial
        if(data.Keys.Contains("IsClearLobbyTutorial") && data["IsClearLobbyTutorial"] != null)
            isClearLobbyTutorial = Convert.ToBoolean(data["IsClearLobbyTutorial"].ToString());
            
        //IsClearInGameTutorial
        if(data.Keys.Contains("isClearInGameTutorial") && data["isClearInGameTutorial"] != null)
            isClearInGameTutorial = Convert.ToBoolean(data["isClearInGameTutorial"].ToString());
            
        // StageProgress 
        if (data.Keys.Contains("StageProgressDict") && data["StageProgressDict"] != null)
        {
            string jsonString = data["StageProgressDict"].ToString();
            if (!string.IsNullOrEmpty(jsonString))
            {
                Logger.Log(jsonString);
                stageProgressDict = DataDictionary<EStageType, bool>.DeserializeObject(jsonString);
            }
        }
    }


    /// <summary>
    /// 데이터를 기본값으로 설정합니다.
    /// </summary>
    public override void SetDefaultData()
    {
        InDate = string.Empty;
        isClearLobbyTutorial = false;
        isClearInGameTutorial = false;

        stageProgressDict = new DataDictionary<EStageType, bool>();
        for (int i = (int)EStageType.Forest; i <= (int)EStageType.DemonCastle; i++)
            AddStageProgress((EStageType)i, false);

        UpdateStageProgress(EStageType.Forest, true);
    }


    /// <summary>
    /// 저장하기 위한 파라미터로 변환합니다.
    /// </summary>
    /// <returns></returns>
    public override Param ToParam()
    {
        Param param = new Param();
        
        param.Add("IsClearLobbyTutorial", isClearLobbyTutorial);
        param.Add("isClearInGameTutorial", isClearInGameTutorial);
        param.Add("StageProgressDict", stageProgressDict.SerializeObject());

        return param;
    }
}
