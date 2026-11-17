using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;



//아티팩트의 노드 UI를 정의 합니다.
public class ArtifactListCellView : MonoBehaviour,IPointerClickHandler
{
    [Header("Image")]
    [SerializeField] private Image artifactImage;
    [SerializeField] private Image rarityBG;

    [Header("Text")]
    [SerializeField] private TMP_Text artifactLevelText;
    [SerializeField] private TMP_Text artifactNameText;
    [SerializeField] private TMP_Text artifactSoulStoneText;

    [Header("Slider")]
    [SerializeField] private Slider artifactSoulStoneSlider;

    [Header("Object")]
    [SerializeField] private GameObject canUpgradeObject;
    [SerializeField] private GameObject lockObject;

    public string ArtifactID => artifactObject.ArtifactID;
    
    private ArtifactService artifactService;
    private ArtifactObject artifactObject;

    public void Initailize(ArtifactObject artifactObject)
    {
        this.artifactObject = artifactObject;
        artifactService = GlobalManagers.Service.GetService<ArtifactService>();
    }


    /// <summary>
    /// 아티팩트데이터를 UI에 그립니다.
    /// </summary>
    public void DrawArtifactData()
    {
        artifactImage.sprite = artifactObject.ArtifactImage;
        artifactNameText.text = artifactObject.GetArtifactName();
        
        Color rarityColor =  GlobalDefines.Rarity.GetRarityPastelColor(artifactObject.ArtifactRarity);
        rarityBG.color = rarityColor;

        bool isOwned = artifactService.IsOwned(artifactObject.ArtifactID);

        if (isOwned)
        {
            ArtifactDTO artifactDTO =artifactService.GetUserArtifactData(artifactObject.ArtifactID);
            ArtifactUpgradeData artifactUpgradeData = GlobalManagers.Chart.GetArtifactUpgradeData(artifactDTO.Level);
            
            int requiredSoulStone = artifactUpgradeData.RequiredSoulStone;

            if (artifactDTO.Level < GlobalDefines.MAX_VALUE.ARTIFACT_MAX_LEVEL)
            {
                artifactLevelText.text = $"Lv.{artifactDTO.Level}";
                artifactSoulStoneSlider.value = (float)artifactDTO.ArtifactStone / (float)requiredSoulStone;
                artifactSoulStoneText.text = $"{artifactDTO.ArtifactStone}/{requiredSoulStone}";

                lockObject.SetActive(false);
                canUpgradeObject.SetActive(artifactDTO.ArtifactStone >= requiredSoulStone);
            }
            else
            {
                artifactSoulStoneSlider.value = 1f;
                artifactSoulStoneText.text = "MAX";
                canUpgradeObject.SetActive(false);
                lockObject.SetActive(false);
            }
        }
        else
        {
            lockObject.SetActive(true);
            canUpgradeObject.SetActive(false);
            artifactLevelText.gameObject.SetActive(false);
        }
    }



    /// <summary>
    /// 클릭했을 때 세부 ui 팝업창을 표시합니다.
    /// </summary>
    /// <param name="eventData"></param>
    /// <exception cref="System.NotImplementedException"></exception>
    public void OnPointerClick(PointerEventData eventData)
    {
        ArtifactDetailedUIData artifactDetailedUIData = new ArtifactDetailedUIData()
        { 
            ArtifactObject = artifactObject
        };

        GlobalManagers.UI.PopUpContainer.OpenUI<ArtifactDetailedUI>(artifactDetailedUIData);
    }


}
