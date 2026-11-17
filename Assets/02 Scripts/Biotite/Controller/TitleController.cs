using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using BackEnd;
using TMPro;
using UnityEngine;


public class TitleController : MonoBehaviour
{
    public const string AUTO_LOGIN_KEY = "AutoLogin";

    [Header("Button")]
    [SerializeField] private AnimButton googleLoginButton;
    [SerializeField] private AnimButton testLoginButton;
    [SerializeField] private AnimButton exitButton;

    [Header("Destroy Object For Next Scene")]
    [SerializeField] private GameObject destroyForNextScene;
    
    [Header("Text")]
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private TMP_Text loadingText;

    private BackEndManager BackEnd => GlobalManagers.BackEnd;
    private ChartDataManager ChartData => GlobalManagers.Chart;
    private UserServerDataManager UserData => GlobalManagers.UserServerData;
    private ServiceManager Service => GlobalManagers.Service;

    private bool isBusy = false;
    private CancellationTokenSource cts;

    private ConfirmUIData backendInitFailedData;


    private void Awake()
    {
        versionText.text = $"ver{Application.version}";

        googleLoginButton.gameObject.SetActive(false);
        testLoginButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);
        loadingText.gameObject.SetActive(false);

        backendInitFailedData = new ConfirmUIData()
        {
            LocalizationTableId = Localization.LocalizationTable.Error,
            Title = LocaliztionKeyCode.UI_BACKEND_INIT_ERROR_TITLE_TEXT,
            Desc = LocaliztionKeyCode.UI_BACKEND_INIT_ERROR_DESC_TEXT,
            OkText =  LocaliztionKeyCode.UI_ERROR_OK_TEXT,
            ConfirmType = EConfirmType.OK,
            OkAction = () => Application.Quit(0),
        };

    }


    private void Start()
    {
        cts = new CancellationTokenSource();
        GlobalManagers.Audio.PlayBgm(GlobalDefines.ResourceKey.Audio.TITLE_BGM);

        googleLoginButton.OnClicked += OnClickedGoogleLoginButton;
        testLoginButton.OnClicked += OnClickedTestLoginButton;
        exitButton.OnClicked += OnClickedExitButton;

        bool isInit = BackEnd.Initialize();

        if (!isInit)
        {
            GlobalManagers.UI.PopUpContainer.OpenUI<ConfirmUI>(backendInitFailedData);
            return;
        }

        if (!CanAutoLogin())
        {
            ShowLoginButtons();
            return;
        }

        StartAutoLoginAsync(cts.Token).Forget();
    }


    /// <summary>
    /// 이전에 로그인에 성공한 적이 있을 때만 자동 로그인을 시도합니다. (에디터는 항상 불가)
    /// </summary>
    private bool CanAutoLogin()
    {
#if UNITY_EDITOR
        return false;
#else
        return PlayerPrefs.GetInt(AUTO_LOGIN_KEY, 0) == 1;
#endif
    }

    /// <summary>
    /// 로그인에 성공했음을 기록합니다. 다음 실행부터 자동 로그인이 시도됩니다.
    /// </summary>
    private void SaveAutoLogin()
    {
        PlayerPrefs.SetInt(AUTO_LOGIN_KEY, 1);
        PlayerPrefs.Save();
    }


    /// <summary>
    /// 구글 로그인을 먼저 시도하고, 실패하면 로그인 버튼을 보여줍니다.
    /// </summary>
    /// <param name="token"></param>
    private async UniTask StartAutoLoginAsync(CancellationToken token)
    {
        if (isBusy) return;
        isBusy = true;

        try
        {
            SetStatus(LocaliztionKeyCode.UI_LOADING_LOGIN);

            bool success = await BackEnd.Login.GoogleLoginAsync();
            token.ThrowIfCancellationRequested();

            if (!success)
            {
                OnLoginFailed();
                return;
            }

            await LoadAndInitProgress(token);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Logger.Log($"자동 로그인 중 예외 발생: {e}");
            await GlobalManagers.BackEnd.Login.GoogleLogOutAsync();
            OnLoginFailed();
        }
        finally
        {
            isBusy = false;
        }
    }


    /// <summary>
    /// 구글 로그인을 수행합니다. 
    /// </summary>
    private void OnClickedGoogleLoginButton()
        => LoginAsync(() => BackEnd.Login.GoogleLoginAsync(), true, cts.Token).Forget();

    /// <summary>
    /// 테스트 로그인을 수행합니다. 
    /// </summary>
    private void OnClickedTestLoginButton()
        => LoginAsync(TestLoginAsync, false, cts.Token).Forget();


    /// <summary>
    /// 나가기 버튼누를시 게임을 종료합니다.
    /// </summary>
    private void OnClickedExitButton()
        => Application.Quit(0);

    /// <summary>
    /// 테스트 로그인을 수행합니다.
    /// </summary>
    private async UniTask<bool> TestLoginAsync()
    {
        var bro = await BackEnd.Login.SignInAsync();
        if (bro.IsSuccess()) return true;

        var b = await BackEnd.Login.LogInAsync();
        return b.IsSuccess();
    }


    /// <summary>
    /// 로그인을 수행합니다.
    /// </summary>
    /// <param name="loginFunc"></param>
    /// <param name="rememberLogin">성공 시 다음 실행부터 자동 로그인할지 여부</param>
    /// <param name="token"></param>
    private async UniTask LoginAsync(Func<UniTask<bool>> loginFunc, bool rememberLogin, CancellationToken token)
    {
        if (isBusy) return;
        isBusy = true;

        try
        {
            HideLoginButtons();
            SetStatus(LocaliztionKeyCode.UI_LOADING_LOGIN);

            bool success = await loginFunc();
            token.ThrowIfCancellationRequested();

            if (!success)
            {
                await GlobalManagers.BackEnd.Login.GoogleLogOutAsync();
                OnLoginFailed();
                return;
            }

            if (rememberLogin)
            {
                SaveAutoLogin();
            }

            await LoadAndInitProgress(token);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Logger.Log($"로그인 중 예외 발생: {e}");
            await GlobalManagers.BackEnd.Login.GoogleLogOutAsync();
            OnLoginFailed();
        }
        finally
        {
            isBusy = false;
        }
    }


    /// <summary>
    /// 로그인 후 데이터를 전부 로드합니다.
    /// </summary>
    /// <param name="token"></param>
    private async UniTask LoadAndInitProgress(CancellationToken token)
    {
        BackEnd.SetNotificationHandler();

        SetStatus(LocaliztionKeyCode.UI_LOADING_CHART);
        ChartData.Initialize();
        if (!await ChartData.LoadChartDataAsync())
        {
            GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new OnBackEndErrorEventData()
            {
                ErrorType = EBackEndErrorType.ChartDataLoadFailed
            });
            return;
        }
        token.ThrowIfCancellationRequested();

        SetStatus(LocaliztionKeyCode.UI_LOADING_USER_DATA);
        UserData.Initialize();
        if (!await UserData.LoadUserDataAsync())
        {
            GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new OnBackEndErrorEventData()
            {
                ErrorType = EBackEndErrorType.UserDataLoadFailed
            });
            return;
        }
        token.ThrowIfCancellationRequested();

        Service.Initialize();
        GoLobbyScene();
    }


    /// <summary>
    /// 로그인 실패 시 버튼을 먼저 복구한 뒤 안내 팝업을 띄웁니다.
    /// </summary>
    private void OnLoginFailed()
    {
        Logger.Log("OnLoginFailed 진입");
        ShowLoginButtons();
        GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new OnBackEndErrorEventData()
        {
            ErrorType = EBackEndErrorType.LoginFailed
        });
    }

    

    /// <summary>
    /// 로딩 텍스트의 상태를 갱신합니다.
    /// </summary>
    /// <param name="key"></param>
    private void SetStatus(string key)
    {
        loadingText.gameObject.SetActive(true);
        loadingText.text = Localization.GetLocaliationText(Localization.LocalizationTable.UI, key);
    }


    /// <summary>
    /// 로그인 버튼을 보여줍니다.
    /// </summary>
    private void ShowLoginButtons()
    {
        loadingText.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(true);
#if UNITY_EDITOR
        googleLoginButton.gameObject.SetActive(false);
        testLoginButton.gameObject.SetActive(true);
#else
        googleLoginButton.gameObject.SetActive(true);
        testLoginButton.gameObject.SetActive(Debug.isDebugBuild);
#endif
    }

    /// <summary>
    /// 로그인 버튼을 숨깁니다.
    /// </summary>
    private void HideLoginButtons()
    {
        googleLoginButton.gameObject.SetActive(false);
        testLoginButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);
    }


    /// <summary>
    /// 로비 씬으로 진입합니다.
    /// </summary>
    private void GoLobbyScene()
    {
        CancelUniTask();

        if (destroyForNextScene != null)
        {
            Destroy(destroyForNextScene);
        }

        GlobalManagers.Scene.NewTransition()
            .Load(nameof(EScene.Lobby), true)
            .Unload(nameof(EScene.Title))
            .LoadResources(nameof(EResourceLabel.Lobby))
            .WithOverlay()
            .Perform().Forget();
    }

    /// <summary>
    /// Unitask를 캔슬합니다.
    /// </summary>
    private void CancelUniTask()
    {
        if (cts == null) return;
        cts.Cancel();
        cts.Dispose();
        cts = null;
    }


    private void OnDestroy()
    {
        googleLoginButton.OnClicked -= OnClickedGoogleLoginButton;
        testLoginButton.OnClicked -= OnClickedTestLoginButton;
        exitButton.OnClicked -= OnClickedExitButton;
        CancelUniTask();
    }
}