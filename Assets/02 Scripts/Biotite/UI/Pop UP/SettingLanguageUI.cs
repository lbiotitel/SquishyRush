using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



//언어 설정 팝업 ui 입니다.
public class SettingLanguageUI : BaseUI
{
    [Header("Button")]
    [SerializeField] private AnimButton closeButton;

    [Header("Transform")]
    [SerializeField] private Transform content;
    
    private GameObject languageButtonPrefab;
    private List<AnimButton> languageButtons;

    //service
    private SettingService settingService;
    

    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);

        settingService = GlobalManagers.Service.GetService<SettingService>();

        if (!GlobalManagers.Resource.TryGetResource<GameObject>(GlobalDefines.ResourceKey.UI.LANGUAGE_BUTTON_PREFAB,
                out GameObject prefab))
        {
            Logger.LogError($"{GlobalDefines.ResourceKey.UI.LANGUAGE_BUTTON_PREFAB}에 해당하는 프리팹이 없습니다.");
            return;
        }

        languageButtonPrefab = prefab;
        CreateLanguageButtons();

        closeButton.OnClicked += CloseUI;
    }

    
    /// <summary>
    /// 언어의 목록을 가지고 와 언어 버튼을 생성합니다.
    /// </summary>
    private void CreateLanguageButtons()
    {
        List<SystemLanguage> languages = Localization.GetLanguages();

        for (int i = 0; i < languages.Count; i++)
        {
            GameObject go = Instantiate(languageButtonPrefab, content);
            AnimButton animButton = go.GetComponent<AnimButton>();
            TMP_Text langText = animButton.GetComponentInChildren<TMP_Text>();
            langText.text = languages[i].ToString();

            SystemLanguage targetLang = languages[i];
            animButton.OnClicked += () => SetLanguage(targetLang);
        }
    }



    /// <summary>
    /// 언어를 세팅 합니다.
    /// </summary>
    private void SetLanguage(SystemLanguage language)
        => settingService.SetLang(language);
}
