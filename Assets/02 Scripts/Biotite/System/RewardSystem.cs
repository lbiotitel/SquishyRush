using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;


public struct RewardContainer
{
    public int RewardGem;
    public int RewardGold;
    public int RewardExp;
    public int RewardRp;
    public int Round;
}


public struct StageClearReward
{
    public int GoldGem;   
    public int ExpRp;    

    public StageClearReward(int goldGem, int expRp)
    {
        GoldGem = goldGem;
        ExpRp = expRp;
    }
}

//게임 결과에 따른 보상을 지급하는 시스템 입니다.
public class RewardSystem
{
    private ProfileService profileService;
    private CurrencyService currencyService;
    private ProgressService progressService;
    
    private RewardContainer currentReward;

    private int prevLevel;
    private int prevExp;
    
    private const int MaxRound = 35;
    private const float RoundCurveExponent = 1.5f;

    private bool isClear;

    //나중에 인게임 밸런스로 옮기기
    private readonly Dictionary<EStageType, StageClearReward> clearRewards =
        new Dictionary<EStageType, StageClearReward>()
    {
        { EStageType.Forest,      new StageClearReward(500, 25)  },
        { EStageType.Ocean,       new StageClearReward(750, 50)  },
        { EStageType.Hell,        new StageClearReward(1000, 75) },
        { EStageType.Temple,      new StageClearReward(1250, 100) },
        { EStageType.Night,       new StageClearReward(1500, 125) },
        { EStageType.DemonCastle, new StageClearReward(2000, 150) },
    };


    public void Initialize()
    {
        profileService = GlobalManagers.Service?.GetService<ProfileService>();
        currencyService = GlobalManagers.Service?.GetService<CurrencyService>();
        progressService = GlobalManagers.Service?.GetService<ProgressService>();
    }


    /// <summary>
    /// 라운드에 대한 진행 비율을 계산합니다.
    /// </summary>
    /// <param name="round"></param>
    /// <returns></returns>
    private float GetProgressRatio(int round)
    {
        float t = Mathf.Clamp01((float)round / MaxRound);
        return Mathf.Pow(t, RoundCurveExponent);
    }


    /// <summary>
    /// 리워드를 계산합니다.
    /// </summary>
    /// <param name="round"></param>
    /// <param name="stageType"></param>
    /// <returns></returns>
    private RewardContainer CalculateReward(int round, EStageType stageType)
    {
        StageClearReward clear = clearRewards[stageType];
        float ratio = GetProgressRatio(round);

        int goldGem = Mathf.RoundToInt(clear.GoldGem * ratio);
        int expRp = Mathf.RoundToInt(clear.ExpRp * ratio);

        RewardContainer rewardContainer = new RewardContainer
        {
            RewardGold = goldGem,
            RewardGem = goldGem,
            RewardExp = expRp,
            RewardRp = expRp,
            Round = round
        };

        currentReward = rewardContainer;

        return rewardContainer;
    }


    /// <summary>
    /// 보상을 줍니다.
    /// </summary>
    public async UniTaskVoid GiveReward(int round, bool isClear, EStageType stageType)
    {
        this.isClear = isClear;
        RewardContainer rewardContainer = CalculateReward(round, stageType);

        prevLevel = profileService?.UserLevel ?? 0;
        prevExp = profileService?.UserExp ?? 0;
        int prevRp = profileService?.UserRP ?? 0;
        
        
        profileService?.SetExp(rewardContainer.RewardExp);

        int nextLevel = prevLevel;
        int totalExp = prevExp + rewardContainer.RewardExp;

        var userLevelChartData = GlobalManagers.Chart.GetUserLevelChartData(nextLevel);

        while (totalExp >= userLevelChartData.RequiredExp)
        {
            if (nextLevel >= GlobalDefines.MAX_VALUE.MAX_USER_LEVEL) break;

            nextLevel++;
            totalExp -= userLevelChartData.RequiredExp;
            userLevelChartData = GlobalManagers.Chart.GetUserLevelChartData(nextLevel);
        }
        
        profileService?.SetLevel(nextLevel);
        profileService?.SetExp(totalExp);
        profileService?.AddRp(rewardContainer.RewardRp);
        currencyService?.AddGold(rewardContainer.RewardGold, false);
        currencyService?.AddGem(rewardContainer.RewardGem, false);

        if (isClear)
        {
            int stageIndex = (int)stageType;
            EStageType nextStage = (EStageType)(Mathf.Clamp(++stageIndex,0,(int)EStageType.Last - 1));
            bool isUnlock = progressService.GetStageProgress(nextStage);
            
            if (!isUnlock)
            {
                progressService.SetStageProgress(nextStage, true);     
            }
        }

        bool isSuccess = await Utils.WaitForAwaitMethodWithOverlayUI(GlobalManagers.UserServerData.SaveUserDataAsync());

        if (!isSuccess)
        {
            GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new OnBackEndErrorEventData()
            {
                ErrorType = EBackEndErrorType.RequestFailed
            });
            
            
            if (isClear)
            {
                int stageIndex = (int)stageType;
                EStageType nextStage = (EStageType)(Mathf.Clamp(++stageIndex,0,(int)EStageType.Last - 1));
                bool isUnlock = progressService.GetStageProgress(nextStage);
            
                if (!isUnlock)
                {
                    progressService.SetStageProgress(nextStage, false);     
                }
            }
            
            profileService?.SetExp(prevExp);
            profileService?.SetLevel(prevLevel);
            profileService?.AddRp(-rewardContainer.RewardRp);
            currencyService?.SpendGold(rewardContainer.RewardGold);
            currencyService?.SpendGem(rewardContainer.RewardGem);
        }
    }


    /// <summary>
    /// 결과 보상 ui를 표시합니다.
    /// </summary>
    public void OpenRewardUI()
    {
        GameResultUIData gameResultUIData = new GameResultUIData()
        {
            IsClear = isClear,
            RewardGem = currentReward.RewardGem,
            RewardGold = currentReward.RewardGold,
            RewardRp = currentReward.RewardRp,
            RewardExp = currentReward.RewardExp,
            Round = currentReward.Round,
            PrevLevel = prevLevel,
            PrevExp = prevExp,
            UserName = profileService?.UserName ?? "Test",
            UserProfileId = profileService?.UserProfileImageID ?? "Profile_Image_1"
        };

        InGameManagers.UI.PopUpContainer.OpenUI<GameResultUI>(gameResultUIData);
    }

}