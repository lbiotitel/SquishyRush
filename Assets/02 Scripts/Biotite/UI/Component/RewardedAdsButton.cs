using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;

public enum EAdState
{
    OnLoadSuccess,
    OnLoadFail,
    OnShowFail,
    OnShowComplete,
}

[RequireComponent(typeof(AnimButton))]
public class RewardedAdsButton : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    private const string ANROID_AD_UNIT_ID = "Rewarded_Android";

    private AnimButton adButton;
    private string adUnitId = null; 
    private bool isAdLoaded = false; 
    
    public event Action<EAdState> OnAdStateChanged;
    
    void Awake()
    {   
      adUnitId = ANROID_AD_UNIT_ID;
      adButton = GetComponent<AnimButton>();
      adButton.OnClicked += ShowRewardedAd;
    }

    private void Start()
    {
      LoadRewardedAd();
    }

    private void OnDestroy()
    {
      if (adButton != null)
      {
          adButton.OnClicked -= ShowRewardedAd;
      }
    }

    
    /// <summary>
    /// 광고를 표시합니다.
    /// </summary>
    private void ShowRewardedAd()
    {
      if (isAdLoaded)
      {
          isAdLoaded = false; 
          Advertisement.Show(adUnitId, this);
      }
      else
      {
          Logger.LogWarning("[UnityAds] 광고가 아직 로드되지 않았습니다. 다시 로드를 시도합니다.");
          LoadRewardedAd(); 
      }
    }

    
    /// <summary>
    /// 광고를 로드 합니다.
    /// </summary>
    private void LoadRewardedAd()
    {
      Advertisement.Load(adUnitId, this);
    }
    

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
      isAdLoaded = false;
      Debug.LogError($"[UnityAds] 로드 실패: {error} - {message}");
      OnAdStateChanged?.Invoke(EAdState.OnLoadFail);
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
      if (placementId.Equals(this.adUnitId))
      {
          isAdLoaded = true;
          OnAdStateChanged?.Invoke(EAdState.OnLoadSuccess);
      }
    }

    public void OnUnityAdsShowStart(string adUnitId) { }
    public void OnUnityAdsShowClick(string adUnitId) { }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
      isAdLoaded = false;
      Debug.LogError($"[UnityAds] 재생 실패: {error} - {message}");
      OnAdStateChanged?.Invoke(EAdState.OnShowFail);
      
      LoadRewardedAd();
    }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
      if (adUnitId.Equals(this.adUnitId) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
      {
          OnAdStateChanged?.Invoke(EAdState.OnShowComplete);
      }
      
      LoadRewardedAd();
    }
}