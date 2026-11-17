using System;
using BackEnd;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;



//매니저들을 통합 관리합니다.
public class GlobalManagers : MonoBehaviour
{
    public static GlobalManagers Instance { get; private set; } = null;


    private BackEndManager backEndManager;
    private SceneController sceneController;
    private UserServerDataManager userServerData;
    private UserLocalDataManager userLocalData;
    private UIManager uiManager;
    private ResourceTableManager resourceTableManager;
    private ChartDataManager chartDataManager;
    private EventManager eventManager;
    private AudioManager audioManager;
    private ServiceManager serviceManager;
    private SpriteAtlasManager spriteAtlasManager;

    public static BackEndManager BackEnd => Instance?.backEndManager;
    public static SceneController Scene => Instance?.sceneController;
    public static UserServerDataManager UserServerData => Instance?.userServerData;
    public static  UserLocalDataManager UserLocalData => Instance?.userLocalData;
    public static ChartDataManager Chart => Instance?.chartDataManager;
    public static UIManager UI => Instance?.uiManager;
    public static ResourceTableManager Resource => Instance?.resourceTableManager;
    public static EventManager Event => Instance?.eventManager;
    public static AudioManager Audio => Instance?.audioManager;    
    public static ServiceManager Service => Instance?.serviceManager;
    public static SpriteAtlasManager Sprite => Instance?.spriteAtlasManager;
    
    private DateTime lastPauseTime = DateTime.UtcNow;
    private bool backgroudQuit = false;
    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitManagers();
            PreInit();
        }
        else
        {
            Destroy(gameObject);
        }
    }


    /// <summary>
    /// 게임을 재시작 합니다.
    /// </summary>
    public async UniTask RestartGame()
    {
        await Scene.NewTransition()
            .Unload(nameof(EScene.Lobby))
            .UnLoadResources(nameof(EResourceLabel.Lobby))
            .UnLoadResources(nameof(EResourceLabel.Common))
            .Load(nameof(EScene.ForInit))
            .Perform();

        ReleaseForRestartGame();
        PreInit();         
        
        await Scene.NewTransition()
            .Unload(nameof(EScene.ForInit))
            .LoadResources(nameof(EResourceLabel.Title))
            .LoadResources(nameof(EResourceLabel.Common))
            .Load(nameof(EScene.Title), true)
            .WithOverlay()
            .Perform();
    }


    
    
    /// <summary>
    /// 미리 필요한 매니저들을 할당 합니다.
    /// </summary>
    private void InitManagers()
    {
        eventManager = new EventManager();
        userLocalData = new UserLocalDataManager();
        uiManager = new UIManager();
        resourceTableManager = new ResourceTableManager();
        sceneController = new SceneController();
        audioManager = new AudioManager();
        backEndManager = new BackEndManager();
        chartDataManager = new ChartDataManager();
        userServerData = new UserServerDataManager();
        serviceManager = new ServiceManager();
        spriteAtlasManager = new SpriteAtlasManager();
    }


    //미리 초기화 해야하는 매니저들 초기화
    private void PreInit()
    {
        Application.targetFrameRate = GlobalDefines.GameConstants.FRAME_RATE;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        eventManager.Initialize();
        resourceTableManager.Initialize();
        userLocalData.Initialize();
        sceneController.Initialize();
        uiManager.Initialize();
        audioManager.Initialize();
        spriteAtlasManager.Initialize();
    }
    


    /// <summary>
    ///  게임 재시작을 위해 매니저들을 해제합니다.
    /// </summary>
    private void ReleaseForRestartGame()
    {
        serviceManager.Release();
        userServerData.Release();
        chartDataManager.Release();
        backEndManager.Release();
        spriteAtlasManager.Release();
        audioManager.Release();  
        uiManager.Release();
        resourceTableManager.Release();
        userLocalData.Release();
        eventManager.Release();
    }

    

    //할당된 모든 매니저들을 해제합니다.
    private void Release()
    {
        backEndManager.Release();
        userServerData.Release();
        userLocalData.Release();
        serviceManager.Release();
        spriteAtlasManager.Release();
        uiManager.Release();
        audioManager.Release();
        sceneController.Release();
        resourceTableManager.Release();
        eventManager.Release();
    }

        
    
    //앱이 백그라운드로 이동하거나(홈 버튼, 멀티태스킹 창 진입) 다시 돌아올 때
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            lastPauseTime = DateTime.UtcNow;
            SaveAllUserData();
            return;
        }
        
        if (DateTime.UtcNow - lastPauseTime > TimeSpan.FromMinutes(GlobalDefines.GameConstants.BACKGROUND_TIME))
        {
            backgroudQuit = true;
            Application.Quit(0);
        }
    }
    
    
    
    private void OnApplicationQuit()
    {
        if (backgroudQuit) return;
        SaveAllUserData();
    }
    
    
    
    private void SaveAllUserData()
    {
        if (Backend.IsInitialized)
            userServerData?.SaveUserData();

        userLocalData?.SaveUserData();
    }


    
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Release();
            Instance = null;
        }
    }
}
