using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//게임 옵션 세팅에 대한 서비스 입니다.
public class SettingService : IService
{
    private UserSettingData userSettingData;
    
    public void Initialize()
    {
        userSettingData = GlobalManagers.UserLocalData.UserSetting;
        GlobalManagers.Event.NotifyEvent<OnSettingDataChangedEventData>(new OnSettingDataChangedEventData());
    }


    public void Release()
    {
      
    }


    public bool SpeedUp => userSettingData.SpeedUp;
    public bool BgmOn => userSettingData.BgmOn;
    public bool SfxOn => userSettingData.SfxOn;
    public float BgmVolume => userSettingData.BgmVolume;
    public float SfxVolume => userSettingData.SfxVolume;
    public SystemLanguage Language => userSettingData.Language;



    public void SetBgmVolume(float volume)
    {
        if (!Mathf.Approximately(userSettingData.BgmVolume, volume))
        {
            userSettingData.BgmVolume = volume;
            GlobalManagers.Event.NotifyEvent<OnSettingDataChangedEventData>(new OnSettingDataChangedEventData());
        }
    }

    public void SetSfxVolume(float volume)
    {
        if (!Mathf.Approximately(userSettingData.SfxVolume, volume))
        {
            userSettingData.SfxVolume = volume;
            GlobalManagers.Event.NotifyEvent<OnSettingDataChangedEventData>(new OnSettingDataChangedEventData());
        }
    }
    

    public void SetBgm(bool onOff)
    {
        if (userSettingData.BgmOn != onOff)
        {
            userSettingData.BgmOn = onOff;
            GlobalManagers.Event.NotifyEvent<OnSettingDataChangedEventData>(new OnSettingDataChangedEventData());
        }
    }

    public void SetSfx(bool onOff)
    {
        if (userSettingData.SfxOn != onOff)
        {
            userSettingData.SfxOn = onOff;
            GlobalManagers.Event.NotifyEvent<OnSettingDataChangedEventData>(new OnSettingDataChangedEventData());
        }
    }


    public void SetSpeedUp(bool onOff)
    {
        if (userSettingData.SpeedUp != onOff)
        {
            userSettingData.SpeedUp = onOff;
        }
    }


    public void SetLang(SystemLanguage language)
    {
        if (userSettingData.Language == language) return;
        userSettingData.Language = language;
        Localization.ChangeLanguage(userSettingData.Language);
        GlobalManagers.Event.NotifyEvent<OnSettingDataChangedEventData>(new OnSettingDataChangedEventData { });
    }
}
