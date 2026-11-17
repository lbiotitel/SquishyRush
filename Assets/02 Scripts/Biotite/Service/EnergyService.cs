using System;
using UnityEngine;

//유저 에너지 서비스 로직 입니다.
public class EnergyService : IService
{
    private UserServerEnergyData userServerEnergyData;
    
    
    public void Initialize()
    {
        userServerEnergyData = GlobalManagers.UserServerData.GetData<UserServerEnergyData>();
    }

    public void Release()
    {
        
    }
    

    public int UserEnergy => userServerEnergyData.UserEnergy;
    public float EnergyRemainTimeSec => userServerEnergyData.EnergyRemainTimeSec;


    /// <summary>
    /// 에너지를 추가 합니다.
    /// </summary>
    public void AddEnergy(int amount, bool showAnimEffect = true)
    {
        if (amount <= 0) return;
        int prevEnergy = userServerEnergyData.UserEnergy;
        int nextEnergy = Mathf.Clamp(prevEnergy + amount, 0, GlobalDefines.MAX_VALUE.MAX_ENERGY);

        userServerEnergyData.UserEnergy = nextEnergy;
        GlobalManagers.Event.NotifyEvent<OnUserResourceValueChangedEventData>(new OnUserResourceValueChangedEventData
        {
            resourceType = EResourceType.Energy,
            prevValue = prevEnergy,
            currentValue = nextEnergy,
            showAnimEffect = showAnimEffect,
            spriteKey = GlobalDefines.ResourceKey.Sprite.ICON_ENERGY_SPRITE
        });
    }


    /// <summary>
    /// 에너지를 감소시킵니다.
    /// </summary>
    /// <param name="amount"></param>
    public void SpendEnergy(int amount)
    {
        if (amount <= 0) return;
        int prevEnergy = userServerEnergyData.UserEnergy;
        int nextEnergy = Mathf.Clamp(prevEnergy - amount, 0, GlobalDefines.MAX_VALUE.MAX_ENERGY);

        userServerEnergyData.UserEnergy = nextEnergy;
        GlobalManagers.Event.NotifyEvent<OnUserResourceValueChangedEventData>(new OnUserResourceValueChangedEventData
        {
            resourceType = EResourceType.Energy,
            prevValue = prevEnergy,
            currentValue = nextEnergy,
            showAnimEffect = false,
        });
    }


    /// <summary>
    /// 남은 에너지 시간을 세팅 합니다.
    /// </summary>
    /// <param name="time"></param>
    public void SetEnergyRemainTime(float time)
        => userServerEnergyData.EnergyRemainTimeSec = Mathf.Clamp(time, 0, GlobalDefines.GameConstants.ENERGY_REGEN_TIME);
}
