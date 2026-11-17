using System;
using TMPro;
using UnityEngine;


//에너지 시스템을 관리하는 클래스입니다.
public class EnergySystem : MonoBehaviour
{
    [SerializeField] private TMP_Text energyRegenText;
    private float regenEnergyTime = 0f;
    private bool isCalc = false;

    private EnergyService energyService;
    


    //게임 종료전 시간이 흐르고 남은 자투리 시간을 저장합니다.
    private void SaveRemainTime(OnBeforeApplicationQuit e)
        => energyService.SetEnergyRemainTime(regenEnergyTime);



    private void Start()
    {
        GlobalManagers.Event?.AddEvent<OnBeforeApplicationQuit>(SaveRemainTime);
        energyService = GlobalManagers.Service.GetService<EnergyService>();
        CalcEnergyByLastLoginTime();
    }
    
    private void OnDestroy()
    {
        GlobalManagers.Event?.RemoveEvent<OnBeforeApplicationQuit>(SaveRemainTime);
    }


    /// <summary>
    /// 마지막 로그인 시간을 가지고와서 에너지가 얼마나 차야하는지 계산합니다.
    /// </summary>
    /// 
    private void CalcEnergyByLastLoginTime()
    {
        UserServerProfileData userServerProfileData = GlobalManagers.UserServerData.GetData<UserServerProfileData>();

        int currentEnergy = energyService.UserEnergy;
        int regenMaxEnergy = GlobalDefines.MAX_VALUE.MAX_REGEN_ENERGY;
        float regenInterval = (float)GlobalDefines.GameConstants.ENERGY_REGEN_TIME;
        
        if (currentEnergy >= regenMaxEnergy)
        {
            regenEnergyTime = regenInterval;
            isCalc = true;
            return;
        }

        DateTime lastLogin = userServerProfileData.LastSavedTime;
        float remainTime = energyService.EnergyRemainTimeSec; 
        
        TimeSpan timePassed = DateTime.UtcNow - lastLogin;
        float totalSeconds = Math.Max(0f, (float)timePassed.TotalSeconds);
        
        float totalAccumulatedSeconds = totalSeconds + (regenInterval - remainTime);
        
        int calculatedRecover = (int)(totalAccumulatedSeconds / regenInterval);
        
        int maxRecoverableAmount = regenMaxEnergy - currentEnergy;
        int actualEnergyToAdd = Math.Min(calculatedRecover, maxRecoverableAmount);
        
        if (actualEnergyToAdd > 0)
        {
            energyService.AddEnergy(actualEnergyToAdd, false);
        }
        
        if (energyService.UserEnergy >= regenMaxEnergy)
        {
            regenEnergyTime = regenInterval;
        }
        else
        {
            float progressSeconds = totalAccumulatedSeconds % regenInterval;
            regenEnergyTime = regenInterval - progressSeconds;
        }

        isCalc = true;
    }


    private void Update()
    {
        RegenEnergy();
    }



    /// <summary>
    /// 에너지가 가득 차지 않으면 계속 정해진 시간에 따라 다시 채웁니다.
    /// </summary>
    private void RegenEnergy()
    {
        if (!isCalc) return;
        if (energyService.UserEnergy >= GlobalDefines.MAX_VALUE.MAX_REGEN_ENERGY)
        {
            regenEnergyTime = GlobalDefines.GameConstants.ENERGY_REGEN_TIME;
            energyRegenText.gameObject.SetActive(false);
            return;
        }

        if(!energyRegenText.gameObject.activeSelf)
            energyRegenText.gameObject.SetActive(true);

        regenEnergyTime -= Time.deltaTime;

        float time = Mathf.Max(0, regenEnergyTime);
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);

        energyRegenText.text = $"{minutes:D2}:{seconds:D2}";

        if (regenEnergyTime <= 0f)
        {
            energyService.AddEnergy(1, false);
            regenEnergyTime += GlobalDefines.GameConstants.ENERGY_REGEN_TIME;
            energyRegenText.text = $"{regenEnergyTime}";
        }
    }


}
