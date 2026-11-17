using System;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



//내 프로필 창을 정의합니다.
public class ProfileDetailedUI : BaseUI
{
    [Header("Image")]
    [SerializeField] private Image profileImage;
    [SerializeField] private Image tierFrameImage;
    [SerializeField] private Image tierImage;


    [Header("Text")]
    [SerializeField] private TMP_Text userLevelText;
    [SerializeField] private TMP_Text userExpText;    
    [SerializeField] private TMP_Text userNameText;
    [SerializeField] private TMP_Text userRpText;
    [SerializeField] private TMP_Text userPlayCountText;
    [SerializeField] private TMP_Text userRankingText;

    [Header("Button")]
    [SerializeField] private AnimButton imageChangeButton;
    [SerializeField] private AnimButton nameEditButton;
    [SerializeField] private AnimButton closeButton;

    [Header("Slider")]
    [SerializeField] private Slider expSlider;

    //service
    private ProfileService profileService;
    private RankingService rankingService;

    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);

        profileService = GlobalManagers.Service.GetService<ProfileService>();
        rankingService = GlobalManagers.Service.GetService<RankingService>();
        DrawProfileDetailedUI();
        BindEvents();
    }


    private void BindEvents()
    {
        GlobalManagers.Event?.AddEvent<OnUserProfileDataChangedEventData>(OnUserProfileDataChangedEventHandler);
        
        imageChangeButton.OnClicked += OnClickedImageChangeBtn;
        closeButton.OnClicked += OnClickedCloseBtn;
        nameEditButton.OnClicked += OnClickedNameEditBtn;
    }


    private void OnDestroy()
    {
        GlobalManagers.Event?.RemoveEvent<OnUserProfileDataChangedEventData>(OnUserProfileDataChangedEventHandler);
        
        imageChangeButton.OnClicked -= OnClickedImageChangeBtn;
        closeButton.OnClicked -= OnClickedCloseBtn;
        nameEditButton.OnClicked -= OnClickedNameEditBtn;
    }


    private void OnClickedCloseBtn()
        => CloseUI();

    private void OnClickedNameEditBtn()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        GlobalManagers.UI.PopUpContainer.OpenUI<NickNamePopUP>();
    }

    private void OnClickedImageChangeBtn()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        GlobalManagers.UI.PopUpContainer.OpenUI<ProfileImageChangeUI>();
    }
    
    private void OnUserProfileDataChangedEventHandler(OnUserProfileDataChangedEventData e)
        => DrawProfileDetailedUI();


    /// <summary>
    /// 유저의 프로필 정보를 그립니다.
    /// </summary>
    private void DrawProfileDetailedUI()
    {
        userNameText.text = profileService.UserName;
        userRpText.text = $"{profileService.UserRP}";
        userPlayCountText.text = $"{profileService.UserPlayCount}";

        Sprite image = GlobalManagers.Sprite.GetProfileImage(profileService.UserProfileImageID);
        profileImage.sprite = image;
        tierImage.sprite = GlobalManagers.Sprite.GetTierImage(profileService.UserRP);
        tierFrameImage.color = GlobalDefines.TierRP.GetTierColor(profileService.UserRP);
        
        userLevelText.text = $"{profileService.UserLevel}";
        UserLevelData userLevelData = GlobalManagers.Chart.GetUserLevelChartData(profileService.UserLevel);
        
        int requiredExp = userLevelData.RequiredExp;
        userExpText.text = $"{profileService.UserExp}/{requiredExp}";
        expSlider.value = (float)profileService.UserExp / (float)requiredExp;
        
        nameEditButton.gameObject.SetActive(!profileService.IsNickNameChanged);
        
        UserRankData myRankData = rankingService.MyRankData;
        userRankingText.text = $"{myRankData.UserRank}";
    }
}
        
