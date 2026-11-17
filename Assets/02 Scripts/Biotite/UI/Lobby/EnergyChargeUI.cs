using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyChargeUI : BaseUI
{
    [Header("Button")] 
    [SerializeField] private RewardedAdsButton rewardedAdsButton;
    [SerializeField] private AnimButton closeButton;
    
    //Toast Data
    private ToastMessageUIData rewardedAdsFailedMessageData;
    
    //service
    private EnergyService energyService; 
    
    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
        
        energyService = GlobalManagers.Service.GetService<EnergyService>();
        
        rewardedAdsFailedMessageData = new ToastMessageUIData()
        {
            message = LocaliztionKeyCode.UI_REWARDED_ADS_FAILED_TEXT,
        };
        
        BindEvents();
    }

    private void BindEvents()
    {
        rewardedAdsButton.OnAdStateChanged += OnAdsStateChangedHandler;
        closeButton.OnClicked += CloseUI;
    }


    private void OnDestroy()
    {
        rewardedAdsButton.OnAdStateChanged -= OnAdsStateChangedHandler;
        closeButton.OnClicked -= CloseUI;
    }


    /// <summary>
    /// 광고 버튼의 액션을 핸들링 합니다.
    /// </summary>
    /// <param name="state"></param>
    private void OnAdsStateChangedHandler(EAdState state)
    {
        if (state == EAdState.OnLoadFail || state == EAdState.OnShowFail)
        {
            GlobalManagers.UI.PopUpContainer.OpenUI<ToastMessageUI>(rewardedAdsFailedMessageData);
        }
        else if (state == EAdState.OnShowComplete)
        {
            energyService.AddEnergy(GlobalDefines.GameConstants.REFILL_ENERGY_AMOUNT);
        }
    }
    
    

    public override void OpenUI()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        base.OpenUI();
    }
}
