using System.Collections;
using System.Collections.Generic;
using UnityEngine;




//로컬에 있는 데이터를 저장하는 매니저 입니다.
public class UserLocalDataManager : IManager
{
    private bool isInit = false;

    public UserSettingData UserSetting { get; private set; }

    
    public bool Initialize()
    {
        if (isInit) return true;   // 재init 때 다시 로드하지 않음
        isInit = true;

        UserSetting = new UserSettingData();
        UserSetting.LoadData();
        return true;
    }

    
    public void SaveUserData()
    {
        UserSetting.SaveData();
    }
    
    
    public void Release()
    {
        if(!isInit) return;
        isInit = false;
        UserSetting.SaveData();
    }
}
