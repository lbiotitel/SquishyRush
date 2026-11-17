using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;


//user의 프로필이나 자원을 표시하는 UI입니다.
public class UserProfileUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Text")]
    [SerializeField] private TMP_Text userNameText;
    [SerializeField] private TMP_Text userLevelText;
    [SerializeField] private TMP_Text userRPText;
    [SerializeField] private TMP_Text userExpText;
    
    [Header("Slider")]
    [SerializeField] private Slider userExpSlider;

    [Header("Image")] 
    [SerializeField] private Image userProfileImage;
    [SerializeField] private Image tierFrame;
    
    private ProfileService profileService;

    
    private void Start()
    {
        profileService = GlobalManagers.Service.GetService<ProfileService>();
        GlobalManagers.Event?.AddEvent<OnUserProfileDataChangedEventData>(OnUserDataChanged);
        DrawUserData();
    }

    private void OnDestroy()
    {
        GlobalManagers.Event?.RemoveEvent<OnUserProfileDataChangedEventData>(OnUserDataChanged);
    }
    

    //클릭시 상세 프로필 창 표시
    public void OnPointerClick(PointerEventData eventData) 
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        OpenProfileDetailedUI();
    }


    //유저 데이터가 변경되었을 때 UI를 업데이트하는 로직을 작성합니다.
    private void OnUserDataChanged(OnUserProfileDataChangedEventData e)
        => DrawUserData();


    
    /// <summary>
    /// 프로필 상세창 UI를 표시합니다.
    /// </summary>
    private void OpenProfileDetailedUI()
        => GlobalManagers.UI.PopUpContainer.OpenUI<ProfileDetailedUI>();
    
    
    /// <summary>
    /// 유저의 레벨,프레임,이름 유저에 관한 데이터를 그립니다.
    /// </summary>
    private void DrawUserData()
    {
        Sprite image = GlobalManagers.Sprite.GetProfileImage(profileService.UserProfileImageID);
        userProfileImage.sprite = image;
        
        userNameText.text = profileService.UserName;
        userLevelText.text = profileService.UserLevel.ToString();
        userRPText.text = profileService.UserRP.ToString();

        UserLevelData userLevelData = GlobalManagers.Chart.GetUserLevelChartData(profileService.UserLevel);   
        
        int currentExp = profileService.UserExp;
        int requiredExp = userLevelData.RequiredExp;
        
        userExpText.text = $"{currentExp}/{requiredExp}";
        userExpSlider.value = (float)currentExp / (float)requiredExp;

        int rp = profileService.UserRP;
        tierFrame.color = GlobalDefines.TierRP.GetTierColor(rp);
    }

}
