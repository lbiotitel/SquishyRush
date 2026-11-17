using System;
using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//유저의 유닛 정보 데이터 입니다.
public class UserServerUnitData : UserServerDataBase
{
    //유저가 소지 하고 있는 유니 정보 입니다.
    private DataDictionary<string, UnitDTO> userUnits;
    public IReadOnlyDictionary<string, UnitDTO> UserUnits => userUnits.Map;



    /// <summary>
    /// 새로운 유닛을 추가합니다.
    /// </summary>
    /// <param name="unitId"></param>
    /// <param name="unitDto"></param>
    public void AddUnit(string unitId, UnitDTO unitDto)
    {
        if (!userUnits.ContainsKey(unitId))
        {
            IsDirty = true;
            userUnits.Add(unitId,  unitDto);
            return;
        }

        Logger.LogError($"{unitId} 이미 등록되있는 유닛입니다.");
    }

    
    /// <summary>
    /// 유닛의 상태를 업데이트 합니다.
    /// </summary>
    /// <param name="unitId"></param>
    /// <param name="newUnitDto"></param>
    public void UpdateUnit(string unitId, UnitDTO newUnitDto)
    {
        if (userUnits.TryGetValue(unitId, out UnitDTO unitDto))
        {
            IsDirty = true;
            userUnits.UpdateData(unitId, newUnitDto);
            return;
        }
        
        Logger.LogError($"{unitId}에 해당하는 유닛이 존재하지 않습니다.");
    }

    
    /// <summary>
    /// unitid에 해당하는 데이터를 삭제합니다.
    /// </summary>
    /// <param name="unitId"></param>
    public void RemoveUnit(string unitId)
    {
        if (userUnits.TryGetValue(unitId, out UnitDTO unitDto))
        {
            IsDirty = true;
            userUnits.Remove(unitId);
            return;
        }
        
        Logger.LogError($"{unitId}에 해당하는 유닛이 존재하지 않습니다.");
    }


    public override void Initialize()
    {
        tableId = GlobalDefines.DataTableName.USER_UNIT_DATA_TABLE;
    }
    


    protected override void ParsingData(JsonData data)
    {
        // inDate
        if (data.Keys.Contains("inDate") && data["inDate"] != null)
            InDate = data["inDate"].ToString();

        // UserUnits
        if (data.Keys.Contains("UserUnits") && data["UserUnits"] != null)
        {
            string jsonString = data["UserUnits"].ToString();
            if (!string.IsNullOrEmpty(jsonString))
            {
                Logger.Log(jsonString);
                userUnits = DataDictionary<string, UnitDTO>.DeserializeObject(jsonString);
            }
        }
    }


    public override void SetDefaultData()
    {
        userUnits = new DataDictionary<string, UnitDTO>();

        foreach (var unitKey in GlobalDefines.UnitConstant.FirstUnit)
        {
            AddUnit(unitKey, new UnitDTO(0, 0) );
        }
    }



    public override Param ToParam()
    {
        Param param = new Param();
        param.Add("UserUnits", userUnits.SerializeObject());
        return param;
    }
    
}
