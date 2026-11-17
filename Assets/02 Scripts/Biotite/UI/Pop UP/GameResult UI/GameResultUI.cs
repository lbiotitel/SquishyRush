using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


//게임 결과 UI에 넘길 데이터 정의입니다.
public class GameResultUIData : BaseUIData
{
    public bool IsClear { get; set; }
    public int RewardGem { get; set; }
    public int RewardRp { get; set; }
    public int RewardExp { get; set; }
    public int RewardGold { get; set; }
    public int Round { get; set; }

    // 경험치 연출 시작점
    public int PrevLevel { get; set; }
    public int PrevExp { get; set; }
    
    public string UserProfileId { get; set; }
    public string UserName { get; set; }
}

//게임이 끝난 후 보상에 처리에 대한 UI 입니다.
public class GameResultUI : BaseUI
{
    [Header("Button")] 
    [SerializeField] private AnimButton claimButton;
    [SerializeField] private RewardedAdsButton rewardedAdsButton;

    [Header("Image")] 
    [SerializeField] private Image profileImage;
    [SerializeField] private Image tierImage;

    [Header("Text")]
    [SerializeField] private TMP_Text gameResultText;
    [SerializeField] private TMP_Text userNameText;
    [SerializeField] private TMP_Text userLevelText;
    [SerializeField] private TMP_Text userExpText;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text rewardLabel;
    
    //reward
    [SerializeField] private TMP_Text rewardGemText;
    [SerializeField] private TMP_Text rewardExpText;
    [SerializeField] private TMP_Text rewardGoldText;
    [SerializeField] private TMP_Text rewardRpText;

    
    [Header("Slider")] 
    [SerializeField] private Slider userExpSlider;
    [SerializeField] private RectTransform userLevelBadge;
    [SerializeField] private Graphic userExpFlash;
    [SerializeField] private ExpBarAnimSettings expBarSettings = new ExpBarAnimSettings();

    private ExpBarAnimator expBarAnimator;
    private GameResultUIData resultData;
    private CancellationTokenSource openCts;

    private bool isClaimed = false;
    
    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);

        rewardLabel.text = Localization.GetLocaliationText(Localization.LocalizationTable.UI,
            LocaliztionKeyCode.UI_REWARD_TEXT); 

        expBarAnimator = new ExpBarAnimator(userExpSlider,
            userLevelText, userExpText, userLevelBadge, userExpFlash, expBarSettings);
        expBarAnimator.OnLevelUp += OnUserLevelUp;
        
        BindEvent();
    }
    
    
    private void BindEvent()
    {
        claimButton.OnClicked += OnClickedClaimButton;
    }
    

    private void OnDestroy()
    {
        claimButton.OnClicked -= OnClickedClaimButton;
        if (expBarAnimator != null) expBarAnimator.OnLevelUp -= OnUserLevelUp;
        CancelOpenToken();
    }



    public override void OpenUI()
    {
        base.OpenUI();
        
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.RESULT_SFX);

        if (resultData != null)
        {
            ResetOpenToken();
            PlayExpAnimationAsync(resultData, openCts.Token).Forget();
        }
    }

    public override void CloseUI()
    {
        CancelOpenToken();
        base.CloseUI();
    }


    public override void SetInfo(BaseUIData data)
    {
        base.SetInfo(data);
        
        resultData = data as GameResultUIData;

        if (resultData != null)
        {
            DrawGameResult(resultData);
            DrawUserInfo(resultData);
        }
    }
    
    
    /// <summary>
    /// 게임의 결과와 보상 데이터를 출력합니다.
    /// </summary>
    /// <param name="gameResultUIData"></param>
    private void DrawGameResult(GameResultUIData gameResultUIData)
    {
        string resultKey = gameResultUIData.IsClear ? LocaliztionKeyCode.UI_WIN : LocaliztionKeyCode.UI_LOSE;
        gameResultText.text = Localization.GetLocaliationText(Localization.LocalizationTable.UI, resultKey);

        rewardGemText.text = $"{gameResultUIData.RewardGem}";
        rewardExpText.text = $"{gameResultUIData.RewardExp}";
        rewardGoldText.text = $"{gameResultUIData.RewardGold}";
        rewardRpText.text = $"{gameResultUIData.RewardRp}";
        roundText.text = string.Format(
            Localization.GetLocaliationText(Localization.LocalizationTable.UI, LocaliztionKeyCode.UI_ROUND_TEXT),
            gameResultUIData.Round);
    }

    /// <summary>
    /// 이전 레벨/경험치에서 보상 경험치만큼 슬라이더를 채웁니다.
    /// </summary>
    private async UniTaskVoid PlayExpAnimationAsync(GameResultUIData data, CancellationToken ct)
    {
        var segments = ExpSegmentBuilder.Bulid(
            data.PrevLevel,
            data.PrevExp,
            data.RewardExp,
            GetRequiredExp,
            GlobalDefines.MAX_VALUE.MAX_USER_LEVEL);
        
        // 시작 상태를 먼저 찍어둬서 팝업 오픈 애니메이션 동안 이전 값이 보이게
        expBarAnimator.SetImmediate(data.PrevLevel, data.PrevExp, segments[0].RequiredExp);

        try
        {
            // 팝업 오픈 연출이 끝난 뒤 시작
            await UniTask.Delay(TimeSpan.FromSeconds(0.3f), ignoreTimeScale: true, cancellationToken: ct);
            await expBarAnimator.PlayAsync(segments, ct);
        }
        catch (OperationCanceledException)
        {
            // UI가 닫히거나 파괴됨
        }
    }

    /// <summary>
    /// 레벨별 필요 경험치를 뒤끝 차트(UserLevel, 253391)에서 가져옵니다.
    /// 차트에 해당 레벨 키가 없으면 0을 반환 → ExpSegmentBuilder가 만렙 취급하고 종료합니다.
    /// </summary>
    private static int GetRequiredExp(int level)
    {
#if UNITY_EDITOR
        if (!GlobalManagers.Chart.IsInitialized)
            return 1000 + level * 200;
#endif
        var levelData = GlobalManagers.Chart.GetUserLevelChartData(level);
        return levelData?.RequiredExp ?? 0;
    }

    private void OnUserLevelUp(int newLevel)
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.USER_LEVEL_UP_SFX);
    }


    
    //로비로 돌아갑니다.
    private void OnClickedClaimButton()
    {
        if (isClaimed) return;
        isClaimed = true;
        
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        
        GlobalManagers.Scene.NewTransition()  
            .Unload(nameof(EScene.InGame))
            .LoadResources(nameof(EResourceLabel.Lobby))
            .UnLoadResources(nameof(EResourceLabel.InGame))
            .WithOverlay()
            .Perform().Forget();
    }


    /// <summary>
    /// 유저의 데이터를 표시합니다.
    /// </summary>
    private void DrawUserInfo(GameResultUIData data)
    {
        userNameText.text = data.UserName ?? string.Empty;
        profileImage.sprite = GlobalManagers.Sprite.GetProfileImage(data.UserProfileId);
    }
    

    private void ResetOpenToken()
    {
        CancelOpenToken();
        openCts = new CancellationTokenSource();
    }

    private void CancelOpenToken()
    {
        if (openCts == null) return;
        openCts.Cancel();
        openCts.Dispose();
        openCts = null;
    }
}
