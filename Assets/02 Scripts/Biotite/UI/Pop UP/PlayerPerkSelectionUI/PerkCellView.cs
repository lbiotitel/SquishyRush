using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


//재능UI 에 대한 cellview 입니다.
public class PerkCellView : MonoBehaviour, IPointerClickHandler
{
    [Header("Text")] 
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text perkNameText;
    [SerializeField] private TMP_Text perkDescText;
    
    [Header("Image")]
    [SerializeField] private Image perkImage;
    [SerializeField] private Image rarityBg;

    [Header("Animation")] 
    [SerializeField] private float animDuration;
    [SerializeField] private float animScale;
    
    private bool isAnim = false;

    private string perkId;
    public event Action<string> OnPerkSelected;
    
    
    
    /// <summary>
    /// 재능 cellview를 그립니다.
    /// </summary>
    /// <param name="perkIcon"></param>
    /// <param name="perkRarity"></param>
    /// <param name="perkNameKey"></param>
    /// <param name="perkNameDesc"></param>
    public void DrawPerk(string perkId,Sprite perkIcon, ERarity perkRarity, string perkNameKey, string perkNameDesc)
    {
        this.perkId = perkId;
        perkImage.sprite = perkIcon;
        rarityText.text = GlobalDefines.Rarity.GetRarityText(perkRarity);
        
        Color rarityColor =  GlobalDefines.Rarity.GetRarityPastelColor(perkRarity);
        rarityText.color = rarityColor;
        rarityBg.color = rarityColor;
        
        perkNameText.text = Localization.GetLocaliationText(Localization.LocalizationTable.Perk, perkNameKey) ;
        perkDescText.text = Localization.GetLocaliationText(Localization.LocalizationTable.Perk, perkNameDesc) ;
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (isAnim) return;
        isAnim = true;
        transform.DOPunchScale(Vector3.one * animScale, animDuration)
            .OnComplete(() =>
            {
                isAnim = false; 
                OnPerkSelected?.Invoke(perkId);
            });
    }
}
