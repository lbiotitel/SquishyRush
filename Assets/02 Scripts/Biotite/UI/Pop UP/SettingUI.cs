using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingUI : BaseUI
{
    [Header("Text")]
    [SerializeField] private TMP_Text versionText;
    
    [Header("Toggle Switch")]
    [SerializeField] private ToggleSwitch bgmToggleSwitch;
    [SerializeField] private ToggleSwitch sfxToggleSwitch;

    [Header("Button")]
    [SerializeField] private AnimButton setLangButton;
    [SerializeField] private AnimButton closeButton;
    [SerializeField] private AnimButton logOutButton;
    [SerializeField] private AnimButton deleteAccountButton;
    [SerializeField] private AnimButton privacyButton;
    [SerializeField] private AnimButton creditButton;
    
    [Header("Slider")]
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private SettingService settingService;

    private ConfirmUIData logOutConfirmData;
    private ConfirmUIData deleteAccountConfirmUIData;

    private bool isLogOut = false;
    private bool isDeleteAccount = false;
    
    
    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
        
        logOutConfirmData = new ConfirmUIData()
        {
            ConfirmType = EConfirmType.OK_CANCLE,
            Title = LocaliztionKeyCode.UI_LOGOUT_TITLE_TEXT,
            Desc = LocaliztionKeyCode.UI_LOGOUT_DESC_TEXT,
            OkText = LocaliztionKeyCode.UI_OK_TEXT,
            CancelText = LocaliztionKeyCode.UI_CANCEL_TEXT,
            OkAction = () => LogOut().Forget(),
        };

        deleteAccountConfirmUIData = new ConfirmUIData()
        {
            ConfirmType = EConfirmType.OK_CANCLE,
            Title = LocaliztionKeyCode.UI_DELETE_ACCOUNT_TITLE_TEXT,
            Desc = LocaliztionKeyCode.UI__DELETE_ACCOUNT_DESC_TEXT,
            OkText = LocaliztionKeyCode.UI_OK_TEXT,
            CancelText = LocaliztionKeyCode.UI_CANCEL_TEXT,
            OkAction = () => DeleteAccount().Forget(),
        };
        
        settingService = GlobalManagers.Service.GetService<SettingService>();
        versionText.text = Application.version;
        
        BindEvents();
    }

    public override void OpenUI()
    {
        DrawSettingUI();
        base.OpenUI();
    }
    


    private void BindEvents()
    {
        setLangButton.OnClicked += OnClickedSetLangButton;
        logOutButton.OnClicked += OnClickedLogOut;
        closeButton.OnClicked += CloseUI;
        deleteAccountButton.OnClicked += OnClickedDeleteAccount;
        privacyButton.OnClicked += OnClickedPrivacyButton;
        creditButton.OnClicked += OnClickedCreditButton;
        
        bgmToggleSwitch.OnToggleStateChanged += OnBgmToggleChanged;
        sfxToggleSwitch.OnToggleStateChanged += OnSfxToggleChanged;

        bgmVolumeSlider.onValueChanged.AddListener(v => OnBgmVolumeChanged(v));
        sfxVolumeSlider.onValueChanged.AddListener(v => OnSfxVolumeChanged(v));
    }



    private void OnDestroy()
    {
        setLangButton.OnClicked -= OnClickedSetLangButton;
        logOutButton.OnClicked -= OnClickedLogOut;
        closeButton.OnClicked -= CloseUI;
        deleteAccountButton.OnClicked -= OnClickedDeleteAccount;
        privacyButton.OnClicked -= OnClickedPrivacyButton;
        creditButton.OnClicked -= OnClickedCreditButton;

        bgmToggleSwitch.OnToggleStateChanged -= OnBgmToggleChanged;
        sfxToggleSwitch.OnToggleStateChanged -= OnSfxToggleChanged;
        
        bgmVolumeSlider.onValueChanged.RemoveAllListeners();
        sfxVolumeSlider.onValueChanged.RemoveAllListeners();
    }
    

    /// <summary>
    /// setting data를 로드하고 세팅 UI를 그립니다.
    /// </summary>
    private void DrawSettingUI()
    {
        bool isBgmOn = settingService.BgmOn;
        bool isSfxOn = settingService.SfxOn;

        bgmToggleSwitch.SetToggleState(isBgmOn);
        sfxToggleSwitch.SetToggleState(isSfxOn);

        bgmVolumeSlider.value = settingService.BgmVolume;
        sfxVolumeSlider.value = settingService.SfxVolume;
    }
    
    
    /// <summary>
    /// 로그아웃을 수행합니다.
    /// </summary>
    private async UniTask LogOut()
    {
        if (isLogOut || isDeleteAccount) return;
        isLogOut = true;
        #if UNITY_EDITOR
        await UniTask.Yield();

        if (!GlobalManagers.UserServerData.SaveUserData())
        {
            isLogOut = false;
            return;
        }

        bool res = await GlobalManagers.BackEnd.Login.LogOutAsync();
        if (res)
        {
            GlobalManagers.UI.CloseAllUI();
            PlayerPrefs.DeleteKey(TitleController.AUTO_LOGIN_KEY);
            PlayerPrefs.Save();
            GlobalManagers.Instance.RestartGame().Forget();
        }
        else
        {
            isLogOut = false;
            Logger.LogError("Log out 실패");
        }
        #endif
        
#if !UNITY_EDITOR
        if (!GlobalManagers.UserServerData.SaveUserData())
        {
            isLogOut = false;
            return;
        }

        bool res = await GlobalManagers.BackEnd.Login.GoogleLogOutAsync();;
        if(res)
        {
            GlobalManagers.UI.CloseAllUI();
            PlayerPrefs.DeleteKey(TitleController.AUTO_LOGIN_KEY);
            PlayerPrefs.Save();
            GlobalManagers.Instance.RestartGame().Forget();
        }else
        {
            isLogOut = false;
            Logger.LogError("Log out 실패");
        }
#endif
    }

    
    /// <summary>
    /// 계정을 삭제합니다.
    /// </summary>
    private async UniTask DeleteAccount()
    {
        if (isLogOut || isDeleteAccount) return;
        isDeleteAccount = true;
        bool isSuccess = await  GlobalManagers.BackEnd.Login.WithDrawAccount();

        if (!isSuccess)
        {
            isDeleteAccount = false;
            Logger.LogError("회원 탈퇴 실패");
            return;
        }
        
        await GlobalManagers.BackEnd.Login.GoogleLogOutAsync();;
        GlobalManagers.UI.CloseAllUI();
        PlayerPrefs.DeleteKey(TitleController.AUTO_LOGIN_KEY);
        PlayerPrefs.Save();
        GlobalManagers.Instance.RestartGame().Forget();
    }


    /// <summary>
    /// 브금 볼륨을 바꿉니다.
    /// </summary>
    /// <param name="value"></param>
    private void OnBgmVolumeChanged(float value)
    {
        if (isLogOut || isDeleteAccount) return;
        settingService.SetBgmVolume(value);
    }


    /// <summary>
    /// sfx 불륨을 바꿉니다.
    /// </summary>
    /// <param name="value"></param>
    private void OnSfxVolumeChanged(float value)
    {
        if (isLogOut || isDeleteAccount) return;
        settingService.SetSfxVolume(value);
    }

    
    
    /// <summary>
    /// 크레딧을 엽니다.
    /// </summary>
    private void OnClickedCreditButton()
    {
        if (isLogOut || isDeleteAccount) return;
        GlobalManagers.UI.PopUpContainer.OpenUI<CreditUI>();
    }

    
    /// <summary>
    /// 개인정보 처리 URL을 여니다.
    /// </summary>
    private void OnClickedPrivacyButton()
    {
        if (isLogOut || isDeleteAccount) return;
        Application.OpenURL(GlobalDefines.GameConstants.PRIVACY_INFO_URL);
    }

    /// <summary>
    /// 언어 선택 팝업 UI를 출력합니다.
    /// </summary>
    private void OnClickedSetLangButton()
    {
        if (isLogOut || isDeleteAccount) return;
        GlobalManagers.UI.PopUpContainer.OpenUI<SettingLanguageUI>(new BaseUIData());
    }

    private void OnClickedLogOut()
    {
        if (isLogOut || isDeleteAccount) return;
        GlobalManagers.UI.PopUpContainer.OpenUI<ConfirmUI>(logOutConfirmData);
    }


    private void OnClickedDeleteAccount()
    {
        if (isLogOut || isDeleteAccount) return;
        GlobalManagers.UI.PopUpContainer.OpenUI<ConfirmUI>(deleteAccountConfirmUIData);
    }
    

    private void OnBgmToggleChanged(bool isOn)
    {
        if (isLogOut || isDeleteAccount) return;
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.SWITCH_BTN_SFX);
        settingService.SetBgm(isOn);
    }

    private void OnSfxToggleChanged(bool isOn)
    {
        if (isLogOut || isDeleteAccount) return;
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.SWITCH_BTN_SFX);
        settingService.SetSfx(isOn);
    }
}