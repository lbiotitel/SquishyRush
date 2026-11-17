using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ArtifactDetailedUIData : BaseUIData
{
    public ArtifactObject ArtifactObject;
}


//아티팩트의 세부 정보를 표시하는 ui 입니다.
public class ArtifactDetailedUI : BaseUI
{
    [Header("Image")]
    [SerializeField] private Image artifactImage;
    [SerializeField] private Image rarityBG;

    [Header("Text")]
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text artifactLevelText;
    [SerializeField] private TMP_Text artifactNameText;
    [SerializeField] private TMP_Text artifactDescText;
    [SerializeField] private TMP_Text artifactSoulStoneText;
    [SerializeField] private TMP_Text requiredGoldText;

    [Header("Button")]
    [SerializeField] private AnimButton upgradeButton;
    [SerializeField] private AnimButton closeButton;


    [Header("Slider")]
    [SerializeField] private Slider artifactSoulStoneSlider;

    [Header("Object")]
    [SerializeField] private GameObject lockObject;
    [SerializeField] private GameObject canUpgradeObject;

    private ArtifactService artifactService;
    private ArtifactObject currentArtifactObject;

    //toast message data
    private ToastMessageUIData upgradeSucceedData;
    private ToastMessageUIData upgradeFailedData;


    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
      
        artifactService = GlobalManagers.Service.GetService<ArtifactService>();

        upgradeSucceedData = new ToastMessageUIData()
        {
            message = LocaliztionKeyCode.UI_LEVEL_UPGRADE_SUCCESSED_TEXT
        };

        upgradeFailedData = new ToastMessageUIData()
        {
            message = LocaliztionKeyCode.UI_NOT_ENOUGH_RESOURCE_TEXT
        };

        BindEvent();
    }


    private void BindEvent()
    {
        upgradeButton.OnClicked += OnClickedUpgradeButton;
        closeButton.OnClicked += CloseUI;
    }


    private void OnDestroy()
    {
        upgradeButton.OnClicked -= OnClickedUpgradeButton;
        closeButton.OnClicked -= CloseUI;
    }

    
    /// <summary>
    /// 아티팩트 업그레이드 버튼을 누를 시 업그레이드 메서드를 호출 합니다/
    /// </summary>
    private void OnClickedUpgradeButton()
        => UpgradeArtifact().Forget();


    public override void SetInfo(BaseUIData data)
    {
        base.SetInfo(data);

        ArtifactDetailedUIData artifactDetailedUIData = data as ArtifactDetailedUIData;

        if (artifactDetailedUIData != null)
        {
            currentArtifactObject = artifactDetailedUIData.ArtifactObject;
        }
    }



    /// <summary>
    /// 아티팩트의 세부 정보를 표시합니다.
    /// </summary>
    private void DrawDetailedUI()
    {
        DrawArtifactBaseInfo();

        bool isOwned = artifactService.IsOwned(currentArtifactObject.ArtifactID);
        if (isOwned)DrawOwnedArtifactInfo();
        else DrawLockedArtifactInfo();
    }
    
    
    
    /// <summary>
    /// 아티팩트의 기본 공통 데이터를 그립니다.
    /// </summary>
    private void DrawArtifactBaseInfo()
    {
        artifactImage.sprite = currentArtifactObject.ArtifactImage;
        artifactNameText.text = currentArtifactObject.GetArtifactName();
        artifactDescText.text = currentArtifactObject.GetArtifacDesc(0);

        rarityText.text = GlobalDefines.Rarity.GetRarityText(currentArtifactObject.ArtifactRarity);

        Color rarityColor = GlobalDefines.Rarity.GetRarityPastelColor(currentArtifactObject.ArtifactRarity);
        rarityText.color = rarityColor;
        rarityBG.color = rarityColor;
    }
    


    /// <summary>
    /// 가지오 있는 아티팩트 데이터 정보를 그립니다.
    /// </summary>
    private void DrawOwnedArtifactInfo()
    {
        ArtifactDTO artifactDto = artifactService.GetUserArtifactData(currentArtifactObject.ArtifactID);
        ArtifactUpgradeData artifactUpgradeData = GlobalManagers.Chart.GetArtifactUpgradeData(artifactDto.Level);

        if (artifactUpgradeData == null) return;

        upgradeButton.gameObject.SetActive(true);
        artifactLevelText.gameObject.SetActive(true);
        lockObject.SetActive(false);

        artifactLevelText.text = $"Lv.{artifactDto.Level}";
        artifactDescText.text = currentArtifactObject.GetArtifacDesc(artifactDto.Level);
        requiredGoldText.text = $"{artifactUpgradeData.RequiredGold}";

        bool isMaxLevel = artifactDto.Level >= GlobalDefines.MAX_VALUE.ARTIFACT_MAX_LEVEL;
        DrawArtifactSoulStoneProgress(artifactDto, artifactUpgradeData, isMaxLevel);

        canUpgradeObject.SetActive(!isMaxLevel && artifactDto.ArtifactStone >= artifactUpgradeData.RequiredSoulStone);
    }
    
    
    
    /// <summary>
    /// 아티팩트를 소유하지 않을때의 상태를 그립니다.
    /// </summary>
    private void DrawLockedArtifactInfo()
    {
        lockObject.SetActive(true);
        canUpgradeObject.SetActive(false);
        upgradeButton.gameObject.SetActive(false);
        artifactLevelText.gameObject.SetActive(false);
    }

    
    

    /// <summary>
    /// 아티팩트 스톤의 보유량을 그립니다.
    /// </summary>
    /// <param name="artifactDto"></param>
    /// <param name="artifactUpgradeData"></param>
    /// <param name="isMaxLevel"></param>
    private void DrawArtifactSoulStoneProgress(ArtifactDTO artifactDto, ArtifactUpgradeData artifactUpgradeData, bool isMaxLevel)
    {
        if (isMaxLevel)
        {
            artifactSoulStoneSlider.value = 1f;
            artifactSoulStoneText.text = "MAX";
            upgradeButton.gameObject.SetActive(false);
        }
        else
        {
            int requiredSoulStone = artifactUpgradeData.RequiredSoulStone;
            artifactSoulStoneSlider.value = (float)artifactDto.ArtifactStone / requiredSoulStone;
            artifactSoulStoneText.text = $"{artifactDto.ArtifactStone}/{requiredSoulStone}";
        }
    }
    

    /// <summary>
    /// 아티팩트를 승급합니다.
    /// </summary>
    private async UniTask UpgradeArtifact()
    {
        Logger.Log("업그레이드!");
        bool res = 
            await Utils.WaitForAwaitMethodWithOverlayUI(artifactService.UpgradeArtifact(currentArtifactObject.ArtifactID));

        if (res)
        {
            DrawDetailedUI();
            GlobalManagers.UI.PopUpContainer.OpenUI<ToastMessageUI>(upgradeSucceedData);
        }
        else
        {
            GlobalManagers.UI.PopUpContainer.OpenUI<ToastMessageUI>(upgradeFailedData);
        }
    }


    public override void OpenUI()
    {
        base.OpenUI();
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        DrawDetailedUI();
    }
}
