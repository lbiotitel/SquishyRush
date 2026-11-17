using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


//일일퀘스트의 cell view 입니다.
public class DailyQuestCellView : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text dailyQuestProgressText;
    [SerializeField] private TMP_Text rewardAmountText;

    [Header("Image")] 
    [SerializeField] private Image rewardImage;

    [Header("Button")] 
    [SerializeField] private AnimButton claimButton;

    [Header("Slider")] 
    [SerializeField] private Slider dailyQuestSlider;

    [Header("Object")]
    [SerializeField] private GameObject claimBlockObject;
    
    
    public event Action<string> OnClaimClicked;

    private DailyQuestService dailyQuestService;

    private string questID;
    private DailyQuestData dailyQuestData;

    public void Initialize(string questID, DailyQuestData dailyQuestData)
    {
        dailyQuestService = GlobalManagers.Service.GetService<DailyQuestService>();
        
        this.questID = questID;
        this.dailyQuestData = dailyQuestData;
        DrawQuestCellView();

        BindEvent();
    }


    private void BindEvent()
    {
        claimButton.OnClicked += OnClickedClaimButton;
        GlobalManagers.Event?.AddEvent<OnDailyQuestStateChangedEventData>(OnDailyQuestStateChangedEventHandler);
    }


    private void OnDestroy()
    {
        claimButton.OnClicked -= OnClickedClaimButton;
        GlobalManagers.Event?.RemoveEvent<OnDailyQuestStateChangedEventData>(OnDailyQuestStateChangedEventHandler);
    }

    
    /// <summary>
    /// 현재 퀘스트의 상태가 변하면 다시 ui를 그립니다.
    /// </summary>
    private void OnDailyQuestStateChangedEventHandler(OnDailyQuestStateChangedEventData e)
    {
        if (e.QuestID == questID)
        {
            DrawQuestCellView();
        }
    }


    /// <summary>
    /// 현재 퀘스트 상태를 그립니다.
    /// </summary>
    public void DrawQuestCellView()
    {
        UserDailyQuestDTO dailyQuestDto = dailyQuestService.GetDailyQuestDto(questID);

        titleText.text = $"{Localization.GetLocaliationText(Localization.LocalizationTable.Quest, dailyQuestData.QuestTitleKey)}";
        dailyQuestSlider.value = (float)dailyQuestDto.Progress / (float)dailyQuestData.Goal;
        dailyQuestProgressText.text = $"{dailyQuestDto.Progress}/{dailyQuestData.Goal}";

        if (dailyQuestData.RewardData != null)
        {
            if (GlobalManagers.Resource.TryGetResource<Sprite>($"{dailyQuestData.RewardData.RewardID}.sprite", out Sprite rewardSprite))
            {
                rewardImage.sprite = rewardSprite;
                rewardAmountText.text = $"{dailyQuestData.RewardData.Amount}";
            }
        }
        
        claimBlockObject.SetActive(dailyQuestDto.QuestState != EQuestState.Completed);
    }
    
    
    
    /// <summary>
    /// 받기 버튼을 클릭했을때 현재 questid의 상태를 확인하고 이벤트를 발생시킵니다.
    /// </summary>
    private void OnClickedClaimButton()
    {
        UserDailyQuestDTO dailyQuestDto = dailyQuestService.GetDailyQuestDto(questID);
        if (dailyQuestDto.QuestState == EQuestState.Completed)
        {
            OnClaimClicked?.Invoke(questID);
        }
    }
}
