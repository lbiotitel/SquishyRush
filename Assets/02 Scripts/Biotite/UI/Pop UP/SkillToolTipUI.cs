using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillToolTipUIData : BaseUIData
{
    public Vector3 OpenPos { get; set; } = Vector3.zero;
    public SkillData Skill { get; set; }
    public UnitStat Stat { get; set; }
    public int Star { get; set; } = -1;
}

// 스킬 툴팁에 관한 정의 입니다.
public class SkillToolTipUI : BaseUI
{
    [Header("Text")]
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillDescText;

    [Header("Layout")]
    [SerializeField] private float screenPadding = 8f;       
    
    private RectTransform rectTransform; 

    
    public override void Initialize(Transform anchor)
    {
        rectTransform = transform.GetComponent<RectTransform>();
        float originWidth = rectTransform.rect.width;
        base.Initialize(anchor);
        rectTransform.sizeDelta = new Vector2(originWidth, 0); 
    }


    private void Update()
    {
        if(Input.GetMouseButton(0))
            CloseUI();
    }


    public override void SetInfo(BaseUIData data)
    {
        base.SetInfo(data);

        SkillToolTipUIData skillToolTipUIData = data as SkillToolTipUIData;

        if (skillToolTipUIData == null || skillToolTipUIData.Skill == null) return;

        SkillData skill = skillToolTipUIData.Skill;
        
        skillNameText.text =
            Localization.GetLocaliationText(Localization.LocalizationTable.Skill, skill.SkillNameKey);
        
        string template =  Localization.GetLocaliationText(Localization.LocalizationTable.Skill, skill.SkillDescKey);
        skillDescText.text = SkillDescFormatter.Build(template, skillToolTipUIData.Skill, skillToolTipUIData.Stat,
            skillToolTipUIData.Star);
           
        
        transform.position = skillToolTipUIData.OpenPos;
        ClampToScreen();
        
    }
    
    
    
    /// <summary>
    /// ui가 화면 밖으로 안나가게 보정합니다.
    /// </summary>
    private void ClampToScreen()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;
        
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        
        Vector3[] corners = new Vector3[4];
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