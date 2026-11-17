using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//보상 서비스 로직에 관환 클래스 입니다.
public class RewardService  : IService
{
    private CurrencyService currencyService;
    private EnergyService energyService;
    
    
    public void Initialize()
    {
        currencyService = GlobalManagers.Service.GetService<CurrencyService>();
        energyService = GlobalManagers.Service.GetService<EnergyService>();
    }

    public void Release()
    {
        
    }


    /// <summary>
    /// 리워드 데이터에 해당하는 리워드를 줍니다.
    /// </summary>
    /// <param name="rewardData"></param>
    public void GiveReward(RewardData rewardData)
    {
        if (rewardData.RewardType == ERewardType.Currency)
        {
            GiveCurrency(rewardData);
        }
    }

    
    /// <summary>
    /// 보상 데이터를 롤백합니다.
    /// </summary>
    /// <param name="rewardData"></param>
    public void RollBackReward(RewardData rewardData)
    {
        if (rewardData.RewardType == ERewardType.Currency)
        {
            RollBackCurrency(rewardData);
        }
    }


    private void RollBackCurrency(RewardData rewardData)
    {
        if (rewardData.RewardID == "Currency_001")
        {
            //골드
            currencyService.SpendGold(rewardData.Amount);
        }
        else if (rewardData.RewardID == "Currency_002")
        {
            //젬
            currencyService.SpendGem(rewardData.Amount);
        }else if (rewardData.RewardID == "Currency_003")
        {
            //에너지
            energyService.SpendEnergy(rewardData.Amount);
        }
    }



    /// <summary>
    /// 재화를 보상을 받습니다
    /// </summary>
    /// <param name="rewardData"></param>
    private void GiveCurrency(RewardData rewardData)
    {
        
        if (rewardData.RewardID == "Currency_001")
        {
            //골드
            currencyService.AddGold(rewardData.Amount);
        }
        else if (rewardData.RewardID == "Currency_002")
        {
            //젬
            currencyService.AddGem(rewardData.Amount);
        }else if (rewardData.RewardID == "Currency_003")
        {
            //에너지
            energyService.AddEnergy(rewardData.Amount);
        }
    }
}
