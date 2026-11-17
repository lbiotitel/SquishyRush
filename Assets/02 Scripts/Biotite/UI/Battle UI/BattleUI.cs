using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


//전투 UI에 대한 정의입니다.
public class BattleUI : BaseUI
{
    [Header("Button")]
    [SerializeField] private AnimButton gameStartBtn;
    [SerializeField] private AnimButton settingBtn;
    [SerializeField] private AnimButton calendarBtn;
    [SerializeField] private AnimButton dailyQuestButton;
    [SerializeField] private AnimButton mailButton;


    [Header("Object")] 
    [SerializeField] private GameObject dailyQuestRedObject;
    [SerializeField] private GameObject mailRedDotObject;
    [SerializeField] private StageListUI stageListUI;

    [Header("Text")]
    [SerializeField] private TMP_Text stageNameText;

    private ProgressService progressService;
    private LobbyController lobbyController;


    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);

        progressService = GlobalManagers.Service.GetService<ProgressService>();
        lobbyController = FindAnyObjectByType<LobbyController>();

        BindEvents();
    }
    
    
    
    private void BindEvents()
    {
        
        gameStartBtn.OnClicked += OnClickGameStart;
        settingBtn.OnClicked += OpenSettingUI;
        calendarBtn.OnClicked += OpenDailyRewardUI;
        dailyQuestButton.OnClicked += OnClickedDailyQuestButton;
        mailButton.OnClicked += OpenMailUI;
        
        GlobalManagers.Event?.AddEvent<OnPostMailLoadedEventData>(OnPostMailLoadedEventDataHandler);
        GlobalManagers.Event?.AddEvent<OnDailyQuestStateChangedEventData>(OnDailyQuestStateChangedEventHandler);
    }
    


    private void OnDestroy()
    {
        gameStartBtn.OnClicked -= OnClickGameStart;
        settingBtn.OnClicked -= OpenSettingUI;
        calendarBtn.OnClicked -= OpenDailyRewardUI;
        mailButton.OnClicked -= OpenMailUI;
        dailyQuestButton.OnClicked -= OnClickedDailyQuestButton;
        
        GlobalManagers.Event?.RemoveEvent<OnPostMailLoadedEventData>(OnPostMailLoadedEventDataHandler);
        GlobalManagers.Event?.RemoveEvent<OnDailyQuestStateChangedEventData>(OnDailyQuestStateChangedEventHandler);
    }



    /// <summary>
    /// 퀘스트가 완료 됬을 때 레드 닷을 띄웁니다.
    /// </summary>
    /// <param name="e"></param>
    private void OnDailyQuestStateChangedEventHandler(OnDailyQuestStateChangedEventData e)
    {
        if(e.QuestState == EQuestState.Completed)
            dailyQuestRedObject.SetActive(true);
    }

    /// <summary>
    /// 매일이 왔을 때 레드 닷을 띄웁니다.
    /// </summary>
    /// <param name="e"></param>
    private void OnPostMailLoadedEventDataHandler(OnPostMailLoadedEventData e)
        => mailRedDotObject.SetActive(e.MailCount > 0);


    /// <summary>
    /// 춣석체크 UI를 엽니다.
    /// </summary>
    private void OpenDailyRewardUI()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        GlobalManagers.UI.PopUpContainer.OpenUI<DailyRewardUI>();
    }

    /// <summary>
    /// 옵션창을 엽니다.
    /// </summary>
    private void OpenSettingUI()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX); 
        GlobalManagers.UI.PopUpContainer.OpenUI<SettingUI>();
    }
    
    
    /// <summary>
    /// 매일창을 엽니다.
    /// </summary>
    private void OpenMailUI()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        mailRedDotObject.SetActive(false);
        GlobalManagers.UI.PopUpContainer.OpenUI<MailUI>();
    }

    
    /// <summary>
    /// 일일 퀘스트 창을 엽니다.
    /// </summary>
    private void OnClickedDailyQuestButton()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        dailyQuestRedObject.SetActive(false);
        GlobalManagers.UI.PopUpContainer.OpenUI<DailyQuestUI>();
    }
 



    /// <summary>
    /// 게임을 시작 로직을 호출합니다.
    /// </summary>
    private void OnClickGameStart()
    {
        EStageType stageType = stageListUI.GetCurrentStageType();
        bool isOpen = progressService.GetStageProgress(stageType);
        if (!isOpen) return;
        lobbyController?.GameStart(stageType).Forget();
    }
}
