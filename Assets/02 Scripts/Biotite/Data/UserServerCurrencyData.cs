using System;
using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using UnityEngine;

public class UserServerCurrencyData : UserServerDataBase
{
    private int userGold;
    public int UserGold
    {
        get => userGold;
        set
        {
            IsDirty = true;
            userGold = value;
        }
    }

    private int userGem;
    
    public int UserGem
    {
        get => userGem;
        set
        {
            IsDirty = true;
            userGem = value;
        }
    }
    
    
    public override void Initialize()
    {
        tableId = GlobalDefines.DataTableName.USER_CURRENCY_DATA_TABLE;
    }
    

    protected override void ParsingData(JsonData data)
    {
        if (data.Keys.Contains("inDate") && data["inDate"] != null)
            InDate = data["inDate"].ToString();

        // 초기화 시에는 backing field 또는 direct setter를 이용해 로드
        if (data.Keys.Contains("Gold") && data["Gold"] != null)
            userGold = Convert.ToInt32(data["Gold"].ToString());

        if (data.Keys.Contains("Gem") && data["Gem"] != null)
            userGem = Convert.ToInt32(data["Gem"].ToString());
    }
    

    public override Param ToParam()
    {
        Param param = new Param();
        param.Add("Gold", userGold);
        param.Add("Gem", userGem);
        return param;
    }

    public override void SetDefaultData()
    {
        userGold = 0;
        userGem = 0;
    }
}