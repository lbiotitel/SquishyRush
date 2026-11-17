using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class PlayerPerkSelectionUIData : BaseUIData
{
    public Sprite UnitIcon { get; set; }
    public string UnitName { get; set; }
    public List<PerkDataBase> PlayerPerkDatas { get; set; }
    public bool CanReroll { get; set; }
}

//재능 선택 팝업 UI 입니다.
public class PlayerPerkSelectionUI : BaseUI
{
    [Header("Text")] 
    [SerializeField] private TMP_Text unitNameText;
    
    [Header("Image")] 
    [SerializeField] private Image unitImage;
    
    [Header("Button")]
    [SerializeField] private AnimButton rerollButton;
    
    [Header("PerkCell View")]
    [SerializeField] private PerkCellView[] perkCellViews;
    
    [Header("Animator")]
    [SerializeField] private Animator animator;
    
    
    private bool isSelected = false;
    public event Action OnClickedRerollBtn;
    public event Action<string> OnClickedPerkBtn;
    
    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
        BindEvent();
    }

    private void OnDestroy()
    {
        rerollButton.OnClicked -= Reroll;
        
        foreach (var perkCellView in perkCellViews)
        {
            perkCellView.OnPerkSelected -= OnPerkSelected;
        }
    }
    

    private void BindEvent()
    {
        rerollButton.OnClicked += Reroll;
        foreach (var perkCellView in perkCellViews)
        {
            perkCellView.OnPerkSelected += OnPerkSelected;
        }
    }
    
    
    /// <summary>
    /// 애니메이터를 비활성화 합니다.
    /// </summary>
    public void DisableAnimator()
        => animator.enabled = false;
    
    
    public override void CloseUI()
    {
        isSelected = false;
        animator.enabled = true;
        base.CloseUI();
    }

    
    public override void SetInfo(BaseUIData data)
    {
        base.SetInfo(data);
        
        PlayerPerkSelectionUIData perkData = data as PlayerPerkSelectionUIData;

        if (perkData != null)
        {
            DrawUnitInfo(perkData.UnitIcon, perkData.UnitName);
            DrawPerkCellView(perkData.PlayerPerkDatas);
            rerollButton.gameObject.SetActive(perkData.CanReroll);
        }
    }


    
    /// <summary>
    /// 유닛 정보를 그립니다.
    /// </summary>
    private void DrawUnitInfo(Sprite unitIcon, string unitName)
    {
        unitImage.sprite = unitIcon;
        unitNameText.text = unitName;
    }


    /// <summary>
    /// 재능 cell view를 그립니다.
    /// </summary>
    private void DrawPerkCellView(List<PerkDataBase> perKDatas)
    {
        if (perKDatas == null || perKDatas.Count == 0)
        {
            Logger.LogError("제대로 된 재능 데이터가 UI에 전달되지 않았습니다.");
            return;
        }

        for (int i = 0; i < perkCellViews.Length; i++)
        {
            PerkDataBase perkData = perKDatas[i];
            if(perkData == null)continue;
            
            perkCellViews[i].DrawPerk(perkData.PerkId,perkData.PerkIcon, perkData.PerkRarity, 
                perkData.PerkNameKey, perkData.PerkDescKey);
        }
    }


    
    /// <summary>
    /// 재능을 선택했을 때 호출합니다.
    /// </summary>
    /// <param name="perkId"></param>
    private void OnPerkSelected(string perkId)
    {
        if (isSelected) return;
        isSelected = true;
        Logger.Log($"{perkId}의 재능을 선택했습니다.");

        CloseUI();
        OnClickedPerkBtn?.Invoke(perkId);
    }

    

    /// <summary>
    /// 리롤을 진행합니다.
    /// </summary>
    private void Reroll()
    {
        if (isSelected) return;
        OnClickedRerollBtn?.Invoke();
    }
}
