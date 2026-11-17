using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class PerkToolTipUIData : BaseUIData
{
    public Vector3 OpenPos { get; set; } = Vector3.zero;
    public string PerkNameKey { get; set; } = string.Empty;
    public string PerkDescKey { get; set; } = string.Empty;
    public ERarity PerkRarity { get; set; }

    public static PerkToolTipUIData Create(PerkDataBase perk, Vector3 screenPos) => new()
    {
        OpenPos = screenPos,
        PerkRarity = perk.PerkRarity,
        PerkNameKey = perk.PerkNameKey,
        PerkDescKey = perk.PerkDescKey,
    };
}


//재능에 관한 툴팁 입니다.
public class PerkToolTipUI : BaseUI
{
    [Header("Text")] 
    [SerializeField] private TMP_Text perkNameText;
    [SerializeField] private TMP_Text perkDescText;
    [SerializeField] private TMP_Text perkRarityText;
    
    [Header("Layout")]
    [SerializeField] private float screenPadding = 8f;
    
    
    private RectTransform rectTransform;
    private readonly Vector3[] corners = new Vector3[4];
    
    public override void Initialize(Transform anchor)
    {
        rectTransform = transform.GetComponent<RectTransform>();
        float originWidth = rectTransform.rect.width;
        base.Initialize(anchor);
        rectTransform.sizeDelta = new Vector2(originWidth, 0);
    }


    public override void SetInfo(BaseUIData data)
    {
        base.SetInfo(data);
        
        PerkToolTipUIData perkToolTipUIData = data as  PerkToolTipUIData;

        if (perkToolTipUIData == null) return;

        perkRarityText.text = GlobalDefines.Rarity.GetRarityText(perkToolTipUIData.PerkRarity);
        perkRarityText.color = GlobalDefines.Rarity.GetRarityColor(perkToolTipUIData.PerkRarity);
        perkNameText.text = Localization.GetLocaliationText(Localization.LocalizationTable.Perk, perkToolTipUIData.PerkNameKey);
        perkDescText.text = Localization.GetLocaliationText(Localization.LocalizationTable.Perk, perkToolTipUIData.PerkDescKey);
        
        transform.position = perkToolTipUIData.OpenPos;
        ClampToScreen();
    }

    
    
    /// <summary>
    /// ui가 화면 밖으로 안나가게 보정합니다.
    /// </summary>
    private void ClampToScreen()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        rectTransform.GetWorldCorners(corners);
        
        float screenW = Screen.width;
        float screenH = Screen.height;

        Vector3 pos = rectTransform.position;
        
        float minX = corners[0].x;
        float maxX = corners[2].x;
        float minY = corners[0].y;
        float maxY = corners[2].y;

        float offsetX = 0f;
        float offsetY = 0f;

        if (minX < screenPadding)
            offsetX = screenPadding - minX;
        else if (maxX > screenW - screenPadding)
            offsetX = (screenW - screenPadding) - maxX;

        if (minY < screenPadding)
            offsetY = screenPadding - minY;
        else if (maxY > screenH - screenPadding)
            offsetY = (screenH - screenPadding) - maxY;

        pos.x += offsetX;
        pos.y += offsetY;

        rectTransform.position = pos;
    }
}
