using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;


//로비를 컨트롤하는 클래스 입니다.
public class LobbyController : MonoBehaviour
{
    [SerializeField] private GameObject lobbyUI;
    private bool isGameStart = false;
    
    private MailService mailService;
    private AttendanceService attendanceService;
    private EnergyService energyService;
    private ProfileService profileService;
    private BackEndTimer backEndTimer;
    private ProgressService progressService;
    

    private void Awake()
    {
        energyService = GlobalManagers.Service.GetService<EnergyService>();
        attendanceService = GlobalManagers.Service.GetService<AttendanceService>();
        mailService = GlobalManagers.Service.GetService<MailService>();
        profileService = GlobalManagers.Service.GetService<ProfileService>();
        progressService = GlobalManagers.Service.GetService<ProgressService>();
        backEndTimer = GlobalManagers.BackEnd.BackEndTimer;
    }

    private void Start()
    {
        BindEvents();
        SetLanguage();
        PlayLobbyBGM();
        CheckAttendance();
        GetMails();
        CheckTutorial();
        backEndTimer.StartTimer();
    }
    

    private void OnDestroy()
    {
        GlobalManagers.Event?.RemoveEvent<OnSceneChangedEventData>(OnSceneChangedEventHandler);
        backEndTimer.StopTimer();
    }


    private void BindEvents()
    {
        GlobalManagers.Event?.AddEvent<OnSceneChangedEventData>(OnSceneChangedEventHandler);
    }


    private void OnSceneChangedEventHandler(OnSceneChangedEventData e)
    {
        if (e.SceneName != nameof(EScene.Lobby)) return;
        
        GetMails();
        PlayLobbyBGM();
        CheckAttendance();
        backEndTimer.StartTimer();
        lobbyUI.SetActive(true);
        isGameStart = false;
    }
    
    
    
    /// <summary>
    /// 튜토리얼 클리어 뮤무를 체크합니다. 클리어 하지 않았으면 가이드 북을 엽니다.
    /// </summary>
    private void CheckTutorial()
    {
        if(!progressService.IsClearLobbyTutorial)
        {
            progressService.ClearLobbyTutorial();
            GlobalManagers.UI.PopUpContainer.OpenUI<LobbyTutorialUI>();
        }
    }




    /// <summary>
    /// 초반에 매일을 처음 비동기로 로드합니다.
    /// </summary>
    private void GetMails()
        => mailService.UpdateMailListAsync(GlobalDefines.GameConstants.MAIL_REQUEST_COUNT).Forget();

    
    private void PlayLobbyBGM()
        => GlobalManagers.Audio.PlayBgm(GlobalDefines.ResourceKey.Audio.LOBBY_BGM);

    private void StopLobbyBGM()
        => GlobalManagers.Audio.StopBGM();
    
    

    //현재 저장된 언어를 세팅합니다.
    private void SetLanguage()
    {
        SettingService settingService = GlobalManagers.Service.GetService<SettingService>();
        SystemLanguage language = settingService.Language;
        settingService.SetLang(language);
    }

    /// <summary>
    /// 출석체크가 됬는지 확인하고 출석체크가 안되있으면 출석체크 ui를 표시합니다.
    /// </summary>
    private void CheckAttendance()
    {
        if (!attendanceService.IsAttendanceCheckedToday)
        {
            GlobalManagers.UI.PopUpContainer.OpenUI<DailyRewardUI>(new BaseUIData { });
        }
    }
    

    /// <summary>
    /// 게임을 시작합니다.
    /// </summary>
    /// <returns></returns>
    public async UniTaskVoid GameStart(EStageType stageType)
    {

        if (!CanStartGame())
            return;
        
        isGameStart = true;
        energyService.SpendEnergy(1);
        profileService.AddPlayCount(1);

        bool isSuccess = await Utils.WaitForAwaitMethodWithOverlayUI(GlobalManagers.UserServerData.SaveUserDataAsync());

        if (!isSuccess)
        {
            GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new OnBackEndErrorEventData()
            {
                ErrorType = EBackEndErrorType.RequestFailed
            });

            energyService.AddEnergy(1, false);
            profileService.AddPlayCount(-1);
            isGameStart = false;
            return;
        }

        DisableLobby();
        backEndTimer.StopTimer();
        
        GlobalManagers.Event.NotifyEvent<OnDoDailyQuestEventData>(new OnDoDailyQuestEventData()
        {
            QuestType = EQuestType.GameStart
        });
        
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.GAME_START_SFX);

        InitializeGameDataPack(stageType);
        
        GlobalManagers.Scene.NewTransition()
            .Load(nameof(EScene.InGame), true)
            .LoadResources(nameof(EResourceLabel.InGame))
            .UnLoadResources(nameof(EResourceLabel.Lobby))
            .WithOverlay()
            .Perform().Forget();
        
    }

    
    /// <summary>
    /// 게임을 시작할 수 있는 지 체크 합니다.
    /// </summary>
    /// <returns></returns>
    private bool CanStartGame()
    {
        if (GlobalManagers.Scene.IsLoading || isGameStart) 
            return false;
        
        int currentEnergy = energyService.UserEnergy;
        if (currentEnergy <= 0)
        {
            GlobalManagers.UI.PopUpContainer.OpenUI<EnergyChargeUI>();
            return false;
        }

        return true;
    }


    /// <summary>
    /// 로비를 정리합니다.
    /// </summary>
    private void DisableLobby()
    {
        StopLobbyBGM();
        lobbyUI.SetActive(false);
        GlobalManagers.UI.CloseAllUI();
    }


    /// <summary>
    /// 인게임에 보낼 게임 데이터 팩을 초기화 합니다.
    /// </summary>
    private void InitializeGameDataPack(EStageType stageType)
    {
        UnitService unitService = GlobalManagers.Service.GetService<UnitService>();
        ArtifactService artifactService = GlobalManagers.Service.GetService<ArtifactService>(); 
        
        GameDataPack.StageType = stageType;
        
        List<ArtifactDataEntry> artifactDataEntries = new  List<ArtifactDataEntry>();
        var artifactDatas = artifactService.GetUserArtifacts();

        foreach (var artifactData in artifactDatas)
        {
            ArtifactDataEntry artifactDataEntry = new ArtifactDataEntry(artifactData.Key, artifactData.Value.Level);
            artifactDataEntries.Add(artifactDataEntry);
        }
        
        GameDataPack.ArtifactDatas = artifactDataEntries;
        
        List<UnitDataEntry> unitDataEntries = new List<UnitDataEntry>();
        var unitDatas = unitService.GetUserUnits();

        foreach (var unitData in unitDatas)
        {
            UnitDataEntry unitDataEntry = new UnitDataEntry(unitData.Key, unitData.Value.Level);
            unitDataEntries.Add(unitDataEntry);
        }
        
        GameDataPack.UnitDatas = unitDataEntries;
    }

}
