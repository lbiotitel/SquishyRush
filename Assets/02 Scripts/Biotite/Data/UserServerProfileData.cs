using BackEnd;
using Cysharp.Threading.Tasks;
using LitJson;
using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


//유저 프로필 데이터 클래스입니다.
public class UserServerProfileData : UserServerDataBase
{
    private int level;

    public int UserLevel
    {
        get => level;
        set
        {
            level = Mathf.Clamp(value, 0, 99);
            IsDirty = true;
        }
    }

    private int userExp;
    public int UserExp
    {
        get => userExp;
        set
        {
            IsDirty = true;
            userExp = value;
        }
    }

    private string userName;

    public string UserName
    {
        get => userName;
        set
        {
            IsDirty = true;
            userName = value;
        }
    }

    private int userRP;

    public int UserRP
    {
        get => userRP;
        set
        {
            IsDirty = true;
            userRP = value;
        }
    }

    private string userProfileImageId;

    public string UserProfileImageID
    {
        get => userProfileImageId;
        set
        {
            IsDirty = true;
            userProfileImageId = value;
        }
    }

    private bool isNickNameChanged;

    public bool IsNickNameChanged
    {
        get => isNickNameChanged;
        set
        {
            IsDirty = true;
            isNickNameChanged = value;
        }
    }

    private int userPlayCount;

    public int UserPlayCount
    {
        get => userPlayCount;
        set
        {
            IsDirty = true;
            userPlayCount = value;
        }
    }
    
    public DateTime LastSavedTime { get; private set; }


    public override void Initialize()
    {
        tableId = GlobalDefines.DataTableName.USER_PROFILE_DATA_TABLE;
    }

    /// <summary>
    /// 랭크 데이터를 비동기로 업데이트 합니다.
    /// </summary>
    /// <returns></returns>
    private async UniTask<bool> UpdateRankAsync()
    {
        Param rankParam = new Param();
        
        rankParam.Add("RP", userRP);
        rankParam.Add("extraData", UserProfileImageID);
        
        bool res = await GlobalManagers.BackEnd.UserRanking.UpdateRankingDataAsync(InDate, rankParam);
        
        if(res)Logger.Log("랭크 데이터를 갱신합니다.");
        else Logger.LogError("랭크 데이터를 갱신하는 데 실패했습니다.");
        return res;
    }
    
    
    /// <summary>
    /// 랭크 데이터를 동기로 업데이트 합니다.
    /// </summary>
    /// <returns></returns>
    private bool UpdateRank()
    {
        Param rankParam = new Param();
        
        rankParam.Add("RP", userRP);
        rankParam.Add("extraData", UserProfileImageID);
        
        bool res = GlobalManagers.BackEnd.UserRanking.UpdateRankingData(InDate, rankParam);
        
        if(res)Logger.Log("랭크 데이터를 갱신합니다.");
        else Logger.LogError("랭크 데이터를 갱신하는 데 실패했습니다.");
        return res;
    }
    


    /// <summary>
    /// 난수로 닉네임을 생성합니다.
    /// </summary>
    private async UniTask<bool> CreateRadomNickName()
    {
        var (isSuccess, candidateName) = await GlobalManagers.BackEnd.Login.CreateRandomNicknameAsync();
        if (isSuccess)
        {
            UserName = candidateName;
            return true;
        }

        return false;
    }


    /// <summary>
    /// 데이터를 로드하고 파싱하여 저장합니다.
    /// </summary>
    public override async UniTask<bool> LoadDataAsync()
    {
        var (isSuccess, data) = 
            await GlobalManagers.BackEnd.UserGame.LoadUserDataAsync(tableId);

        if (!isSuccess)
        {
            bool res = await InsertNewProfileData();
            return res;
        }

        try
        {
            ParsingData(data);
        }
        catch (Exception e)
        {
            Logger.LogError($"유저 프로필 정보 로드에 실패했습니다. {e.Message}");
            return false;
        }
        
        return true;
    }
    
    
    /// <summary>
    /// 새로운 데이터를 만들고 삽입 로드 합니다.
    /// </summary>
    private async UniTask<bool> InsertNewProfileData()
    {
        SetDefaultData();
        bool isSuccess = await CreateRadomNickName() 
                         && await InsertNewData() 
                         && await UpdateRankAsync();
        return isSuccess;
    }


      /// <summary>
      /// 데이어를 비동기로 저장합니다.
      /// </summary>
      /// <returns></returns>
    public override async UniTask<bool> SaveDataAsync()
    {
        LastSavedTime = DateTime.UtcNow;
        Param param = ToParam();
        bool res = await GlobalManagers.BackEnd.UserGame.SaveUserDataAsync(InDate, param, tableId)
                   && await UpdateRankAsync();
        return res;
    }


    /// <summary>
    /// 데이터를 동기로 저장합니다.
    /// </summary>
    /// <returns></returns>
    public override bool SaveData()
    {
        LastSavedTime = DateTime.UtcNow;
        Param param = ToParam();
        bool res = GlobalManagers.BackEnd.UserGame.SaveUserData(InDate, param, tableId)
                   && UpdateRank();
        return res;
    }


    protected override void ParsingData(JsonData data)
    {
        //UserName
        userName = Backend.UserNickName;

        // inDate
        if (data.Keys.Contains("inDate") && data["inDate"] != null)
            InDate = data["inDate"].ToString();

        //Exp
        if(data.Keys.Contains("Exp") && data["Exp"] != null)
            userExp = Convert.ToInt32(data["Exp"].ToString());

        // Level
        if (data.Keys.Contains("Level") && data["Level"] != null)
            level = Convert.ToInt32(data["Level"].ToString());
        
        //IsNickNameChanged
        if(data.Keys.Contains("IsNickNameChanged") && data["IsNickNameChanged"] != null)
            isNickNameChanged = Convert.ToBoolean(data["IsNickNameChanged"].ToString());

        // RP
        if (data.Keys.Contains("RP") && data["RP"] != null)
            userRP = Convert.ToInt32(data["RP"].ToString());
        
        //Profile ID
        if (data.Keys.Contains("UserProfileImageID") && data["UserProfileImageID"] != null)
            userProfileImageId = data["UserProfileImageID"].ToString();
        
        //PlayCount
        if (data.Keys.Contains("UserPlayCount") && data["UserPlayCount"] != null)
            userPlayCount = Convert.ToInt32(data["UserPlayCount"].ToString());

        //LastLogin Time
        if (data.Keys.Contains("LastSavedTime") && data["LastSavedTime"] != null)
        {
            string timeStr = data["LastSavedTime"].ToString();
            if (DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime res))
            {
                LastSavedTime = res;
            }
        }
    }


    /// <summary>
    /// 데이터를 기본값으로 설정합니다.
    /// </summary>
    public override void SetDefaultData()
    {
        InDate = string.Empty;
        userName = string.Empty;
        level = 1;
        userExp = 0;
        userRP = 1000;
        UserPlayCount = 0;
        userProfileImageId = GlobalDefines.GameConstants.DEFAULT_PROFILE_ID;
        IsNickNameChanged = false;
        LastSavedTime = DateTime.UtcNow;
    }


    /// <summary>
    /// 저장하기 위한 파라미터로 변환합니다.
    /// </summary>
    /// <returns></returns>
    public override Param ToParam()
    {
        Param param = new Param();

        param.Add("Name", userName);
        param.Add("RP", userRP);
        param.Add("Level", level);
        param.Add("Exp", userExp);
        param.Add("UserProfileImageID", userProfileImageId);
        param.Add("UserPlayCount", userPlayCount);
        param.Add("LastSavedTime", DateTime.UtcNow);
        param.Add("IsNickNameChanged", isNickNameChanged);

        return param;
    }

}
