using TMPro;
using UnityEngine;
using UnityEngine.UI;


//게임 안쪽의 튜토리얼 입니다.
public class InGameTutorialUI : BaseUI
{
    [Header("Button")] 
    [SerializeField] private AnimButton baseInfoButton;
    [SerializeField] private AnimButton unitInfoButton;
    [SerializeField] private AnimButton synergyInfoButton;
    [SerializeField] private AnimButton statusEffectInfoButton;
    [SerializeField] private AnimButton perkInfoButton;
    
    [SerializeField] private AnimButton closeButton;

    [Header("Container")] 
    [SerializeField] private GameObject baseInfoContainer;
    [SerializeField] private GameObject  unitInfoContainer;
    [SerializeField] private GameObject synergyInfoContainer;
    [SerializeField] private GameObject statusEffectInfoContainer;
    [SerializeField] private GameObject perkInfoContainer;


    [Header("Text")] [SerializeField] private TMP_Text titleText;

    [Header("Scroll View")] 
    [SerializeField] private ScrollRect scrollRect;
    
    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
        BindEvents();
    }


    public override void OpenUI()
    {
        OnClickedBaseInfoButton();
        base.OpenUI();
    }


    private void BindEvents()
    {
        baseInfoButton.OnClicked += OnClickedBaseInfoButton;
        unitInfoButton.OnClicked += OnClickedUnitInfoButton;
        statusEffectInfoButton.OnClicked += OnClickedStatusEffectInfoButton;
        perkInfoButton.OnClicked += OnClickedPerkInfoButton;
        synergyInfoButton.OnClicked += OnClickedSynergyInfoButton;
        
        closeButton.OnClicked += CloseUI;
    }


    private void OnDestroy()
    {
        baseInfoButton.OnClicked -= OnClickedBaseInfoButton;
        unitInfoButton.OnClicked -= OnClickedUnitInfoButton;
        statusEffectInfoButton.OnClicked -= OnClickedStatusEffectInfoButton;
        perkInfoButton.OnClicked -= OnClickedPerkInfoButton;
        synergyInfoButton.OnClicked -= OnClickedSynergyInfoButton;
        
        closeButton.OnClicked -= CloseUI;
    }

    
    /// <summary>
    /// 기본 정보 컨테이너를 엽니다.
    /// </summary>
    private void OnClickedBaseInfoButton()
    {
        DisableAllContainer();
        titleText.text = "기본 정보";
        baseInfoContainer.SetActive(true);
        scrollRect.verticalNormalizedPosition = 1f;
    }


    private void OnClickedSynergyInfoButton()
    {
        DisableAllContainer();
        titleText.text = "시너지";
        synergyInfoContainer.SetActive(true);
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void OnClickedPerkInfoButton()
    {
        DisableAllContainer();
        titleText.text = "재능";
        perkInfoContainer.SetActive(true);
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void OnClickedStatusEffectInfoButton()
    {
        DisableAllContainer();
        titleText.text = "상태 효과";
        statusEffectInfoContainer.SetActive(true);
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void OnClickedUnitInfoButton()
    {
        DisableAllContainer();
        titleText.text = "유닛";
        unitInfoContainer.SetActive(true);
        scrollRect.verticalNormalizedPosition = 1f;
    }


    
    /// <summary>
    /// 모든 컨테이너들을 닫습니다.
    /// </summary>
    private void DisableAllContainer()
    {
        baseInfoContainer.SetActive(false);
        unitInfoContainer.SetActive(false);
        synergyInfoContainer.SetActive(false);
        statusEffectInfoContainer.SetActive(false);
        perkInfoContainer.SetActive(false);
    }
}
