using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//유저 에너지 데이터에 대한 정의 입니다.
public class UserServerEnergyData : UserServerDataBase
{
    private int userEnergy;

    public int UserEnergy
    {
        get => userEnergy;
        set
        {
            IsDirty = true;
            userEnergy = value;
        }
    }

    
    private float energyRemainTimeSec;
    //에너지시간이 흐르고 남은 시간입니다.
    public float EnergyRemainTimeSec
    {
        get => energyRemainTimeSec;
        set
        {
            IsDirty = true;
            energyRemainTimeSec = value;
        }
    }


    public override void Initialize()
    {
        tableId = GlobalDefines.DataTableName.USER_ENERGY_DATA_TABLE;
    }
    

    protected override void ParsingData(JsonData data)
    {
        // inDate
        if (data.Keys.Contains("inDate") && data["inDate"] != null)
            InDate = data["inDate"].ToString();

        //  Energy
        if (data.Keys.Contains("Energy") && data["Energy"] != null)
            userEnergy = Convert.ToInt32(data["Energy"].ToString());


        // EnergyRemainTimeSec
        if (data.Keys.Contains("EnergyRemainTimeSec") && data["EnergyRemainTimeSec"] != null)
        {
            if (float.TryParse(data["EnergyRemainTimeSec"].ToString(), out float remainSec))
            {
                energyRemainTimeSec = remainSec;
            }
        }
    }


    public override void SetDefaultData()
    {
        InDate = string.Empty;
        userEnergy = GlobalDefines.MAX_VALUE.MAX_REGEN_ENERGY;
        energyRemainTimeSec = 0;
    }


    public override Param ToParam()
    {
        Param param = new Param();

        param.Add("Energy", userEnergy);
        param.Add("EnergyRemainTimeSec", energyRemainTimeSec);

        return param;
    }
}
