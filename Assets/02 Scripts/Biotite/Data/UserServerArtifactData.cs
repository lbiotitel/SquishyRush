using System;
using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//유저가 가지고 있는 아티팩테 데이터 입니다.
public class UserServerArtifactData : UserServerDataBase
{
    //유저가 소지하고 있는 아티팩트 정보 입니다.
    private DataDictionary<string, ArtifactDTO> userArtifacts;
    public IReadOnlyDictionary<string, ArtifactDTO> UserArtifacts => userArtifacts.Map;

    
    
    public override void Initialize()
    {
        tableId = GlobalDefines.DataTableName.USER_ARTIFACT_DATA_TABLE;
    }
    
    
    /// <summary>
    /// 아티팩트 데이터를 새로 추가합니다.
    /// </summary>
    /// <param name="artifactId"></param>
    /// <param name="artifact"></param>
    public void AddArtifact(string artifactId, ArtifactDTO artifact)
    {
        if (!userArtifacts.ContainsKey(artifactId))
        {
            IsDirty = true;
            userArtifacts.Add(artifactId, artifact);
            return;
        }
        
        Logger.LogError($"{artifactId} 중복된 아티팩트 입니다.");
    }

    
    /// <summary>
    /// 아티팩트 데이터를 업데이트 합니다.
    /// </summary>
    /// <param name="artifactId"></param>
    /// <param name="newArtifactDto"></param>
    public void UpdateArtifact(string artifactId, ArtifactDTO newArtifactDto)
    {
        if (userArtifacts.TryGetValue(artifactId, out ArtifactDTO artifact))
        {
            IsDirty = true;
            userArtifacts.UpdateData(artifactId,  newArtifactDto);
            return;
        }
        
        Logger.LogError($"{artifactId}에 해당하는 아티패트가 없습니다.");
    }

    
    
    /// <summary>
    /// artifactId에 해당하는 아티팩트 데이터를 삭제합니다.
    /// </summary>
    /// <param name="artifactId"></param>
    public void RemoveArtifact(string artifactId)
    {
        if (userArtifacts.TryGetValue(artifactId, out ArtifactDTO artifact))
        {
            IsDirty = true;
            userArtifacts.Remove(artifactId);
            return;
        }
        
        Logger.LogError($"{artifactId}에 해당하는 아티패트가 없습니다.");
    }
    
    
    protected override void ParsingData(JsonData data)
    {
        // inDate
        if (data.Keys.Contains("inDate") && data["inDate"] != null)
            InDate = data["inDate"].ToString();

        // UserArtifacts
        if (data.Keys.Contains("UserArtifacts") && data["UserArtifacts"] != null)
        {
            string jsonString = data["UserArtifacts"].ToString();
            if (!string.IsNullOrEmpty(jsonString))
            {
                Logger.Log(jsonString);
                userArtifacts = DataDictionary<string, ArtifactDTO>.DeserializeObject(jsonString);
            }
        }
    }

    public override void SetDefaultData()
    {
        userArtifacts = new DataDictionary<string, ArtifactDTO>();
    }
    
    
    public override Param ToParam()
    {
        Param param = new Param();
        param.Add("UserArtifacts", userArtifacts.SerializeObject());
        return param;
    }

}
