using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


//게임 내부에 있는 세팅 ui 입니다.
public class InGameSettingUI : BaseUI
{
    [Header("Toggle Switch")]
    [SerializeField] private ToggleSwitch bgmToggleSwitch;
    [SerializeField] private ToggleSwitch sfxToggleSwitch;

    [Header("Button")]
    [SerializeField] private AnimButton exitButton;
    [SerializeField] private AnimButton closeButton;
    
    [Header("Slider")]
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private SettingService settingService;
    private ConfirmUIData exitConfirmData;
    private bool goLobby = false;

    
    
    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
        
        exitConfirmData = new ConfirmUIData()
        {
            ConfirmType = EConfirmType.OK_CANCLE,
            Title = LocaliztionKeyCode.UI_IG_GAME_EXIT_TITLE_TEXT,
            Desc = LocaliztionKeyCode.UI_IG_GAME_EXIT_DESC_TEXT,
            OkText = LocaliztionKeyCode.UI_OK_TEXT,
            CancelText = LocaliztionKeyCode.UI_CANCEL_TEXT,
            OkAction = GoToLobby,
        };
        
        settingService = GlobalManagers.Service.GetService<SettingService>();
        BindEvents();
    }

    public override void OpenUI()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        DrawSettingUI();
        base.OpenUI();
    }


    public override void CloseUI()
    {
        if(goLobby) return;
        base.CloseUI();
    }


    private void BindEvents()
    {
        exitButton.OnClicked += OnClickedExitButton;
        closeButton.OnClicked += CloseUI;

        bgmToggleSwitch.OnToggleStateChanged += OnBgmToggleChanged;
        sfxToggleSwitch.OnToggleStateChanged += OnSfxToggleChanged;
        
        bgmVolumeSlider.onValueChanged.AddListener(v => OnBgmVolumeChanged(v));
        sfxVolumeSlider.onValueChanged.AddListener(v => OnSfxVolumeChanged(v));
    }

    
    private void OnDestroy()
    {
        exitButton.OnClicked -= OnClickedExitButton;
        closeButton.OnClicked -= CloseUI;

        bgmToggleSwitch.OnToggleStateChanged -= OnBgmToggleChanged;
        sfxToggleSwitch.OnToggleStateChanged -= OnSfxToggleChanged;
        
        bgmVolumeSlider.onValueChanged.RemoveAllListeners();
        sfxVolumeSlider.onValueChanged.RemoveAllListeners();
    }

    
    /// <summary>
    /// 나가기 버튼을 누를 시 확인 팝업을 엽니다.
    /// </summary>
    private void OnClickedExitButton()
    {
        if(goLobby) return;
        InGameManagers.UI.PopUpContainer.OpenUI<ConfirmUI>(exitConfirmData);
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
    /// 로비로 이동합니다.
    /// </summary>
    private void GoToLobby()
    {
        if (goLobby) return;
        goLobby = true;
        GlobalManagers.Scene.NewTransition()
            .Unload(nameof(EScene.InGame))
            .UnLoadResources(nameof(EScene.InGame))
            .Load(nameof(EScene.Lobby), true)
            .LoadResources(nameof(EScene.Lobby))
            .WithOverlay()
            .Perform().Forget();
    }



    private void OnBgmVolumeChanged(float value)
    {
        if(goLobby) return;
        settingService.SetBgmVolume(value);
    }


    private void OnSfxVolumeChanged(float value)
    {
        if(goLobby) return;
        settingService.SetSfxVolume(value);
    }


    private void OnBgmToggleChanged(bool isOn)
    {
        if(goLobby) return;
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.SWITCH_BTN_SFX);
        settingService.SetBgm(isOn);
    }
    

    private void OnSfxToggleChanged(bool isOn)
    {
        if(goLobby) return;
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.SWITCH_BTN_SFX);
        settingService.SetSfx(isOn);
    }
 }
