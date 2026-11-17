using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.U2D;



//유저 프로필 데이터에 대한 서비스 입니다.
public class ProfileService : IService
{
    private UserServerProfileData userServerProfileData;
    private readonly TheBackend.ToolKit.InvalidFilter.FilterManager filterManager = new ();

    public void Initialize()
    {
        if (filterManager.LoadInvalidString())
        {
            Debug.Log("욕설 필터매니저를 불러오는데 성공했습니다.");
        }
        else
        {
            Debug.LogError("욕설 필터 매니저를 불러오는데 실패했습니다.");
        }
        
        userServerProfileData = GlobalManagers.UserServerData.GetData<UserServerProfileData>();
    }

    public void Release()
    {
        
    }

    public string UserName => userServerProfileData.UserName;
    public int UserLevel => userServerProfileData.UserLevel;
    public int UserExp => userServerProfileData.UserExp;
    public int UserRP => userServerProfileData.UserRP;
    public int UserPlayCount => userServerProfileData.UserPlayCount;
    public bool IsNickNameChanged => userServerProfileData.IsNickNameChanged;
    public string UserProfileImageID => userServerProfileData.UserProfileImageID;
    public DateTime LastSavedTime => userServerProfileData.LastSavedTime;



    
    /// <summary>
    /// 레벨을 set 합니다.
    /// </summary>
    /// <param name="level"></param>
    public void SetLevel(int level)
    {
        if (UserLevel >= GlobalDefines.MAX_VALUE.MAX_USER_LEVEL) return;
        userServerProfileData.UserLevel = level;
        GlobalManagers.Event.NotifyEvent<OnUserProfileDataChangedEventData>(new OnUserProfileDataChangedEventData());
    }


    /// <summary>
    /// exp 를 set 합니다.
    /// </summary>
    /// <param name="exp"></param>
    public void SetExp(int exp)
    {
        if (UserLevel >= GlobalDefines.MAX_VALUE.MAX_USER_LEVEL) return;
        
        userServerProfileData.UserExp = exp;
        GlobalManagers.Event.NotifyEvent<OnUserProfileDataChangedEventData>(new OnUserProfileDataChangedEventData());
    }


    

    /// <summary>
    /// RP를 추가합니다.
    /// </summary>
    /// <param name="rp"></param>
    public void AddRp(int rp)
    {
        userServerProfileData.UserRP += rp;
        GlobalManagers.Event.NotifyEvent<OnUserProfileDataChangedEventData>(new OnUserProfileDataChangedEventData());
    }

    
    /// <summary>
    /// 플레이 횟수를 증가시킵니다.
    /// </summary>
    /// <param name="playCount"></param>
    public void AddPlayCount(int playCount)
    {
        userServerProfileData.UserPlayCount += playCount;
        GlobalManagers.Event.NotifyEvent<OnUserProfileDataChangedEventData>(new OnUserProfileDataChangedEventData());
    }

    
    /// <summary>
    /// 닉네임을 설정합니다.
    /// </summary>
    /// <param name="nickName"></param>
    /// <returns></returns>
    public async UniTask<bool> SetName(string nickName)
    {
        if (string.IsNullOrEmpty(nickName) || IsNickNameChanged)
            return false;
    
        bool isValid = Utils.CheckNameIsValidate(nickName)
                    && !filterManager.IsFilteredString(nickName);
        
        bool isDuplicate = await GlobalManagers.BackEnd.Login.CheckNicknameDuplicationAsync(nickName);

        if (!isValid || !isDuplicate) return false;

        string prevName = userServerProfileData.UserName;
        userServerProfileData.UserName = nickName;
        bool res = await GlobalManagers.BackEnd.Login.SetNickNameAsync(nickName);

        if (res)
        {
            userServerProfileData.IsNickNameChanged = true;
            GlobalManagers.Event.NotifyEvent<OnUserProfileDataChangedEventData>(new OnUserProfileDataChangedEventData());
        }
        else
        {
            userServerProfileData.UserName = prevName;   
            return false;
        }
        return true;
    }

    

    
   /// <summary>
   /// 해당하는 프로필 id로 프로필 이미지를 변경합니다.
   /// </summary>
   /// <param name="profileImageID"></param>
   /// <returns></returns>
    public async UniTask<bool> SetProfileImage(string profileImageID)
    {
        string prevProfileImageID = userServerProfileData.UserProfileImageID;
        userServerProfileData.UserProfileImageID = profileImageID;
        
        bool res = await userServerProfileData.SaveDataAsync();

        if (res) GlobalManagers.Event.NotifyEvent<OnUserProfileDataChangedEventData>(new OnUserProfileDataChangedEventData());
        else
        {
            GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new  OnBackEndErrorEventData()
            {
                ErrorType = EBackEndErrorType.RequestFailed
            });
            
            userServerProfileData.UserProfileImageID = prevProfileImageID;
            return false;
        }

        return true;
    }
}
