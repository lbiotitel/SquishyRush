using System;
using TMPro;
using UnityEngine;

public class SynergyBonusUIData : BaseUIData
{
    public Sprite UnitIcon { get; set; }
    public string UnitName { get; set; }
    public SynergyBonusSession Session { get; set; }
    public SynergyCounter Counter { get; set; }
    public Action<SynergyData> OnPicked { get; set; }
}

public class SynergyBonusUI : BaseUI
{
    [Header("Root")]
    [SerializeField] private CanvasGroup contentGroup;

    [Header("Button")] 
    [SerializeField] private AnimButton toggleButton;

    [Header("Cell View")]
    [SerializeField] private SynergyBonusCellView[] cellViews;

    [Header("Animator")] 
    [SerializeField] private Animator animator;
    
    [Header("Label")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;

    private SynergyBonusSession session;
    private SynergyCounter counter;
    private Action<SynergyData> onPicked;
    private bool isSelected;
    private bool isHidden;
    protected virtual string DescKey => LocaliztionKeyCode.UI_SYNERGY_BONUS_DESC_TEXT;

    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);

        titleText.text =
            Localization.GetLocaliationText(Localization.LocalizationTable.UI, LocaliztionKeyCode.UI_SYNERGY_BONUS_TITLE_TEXT);
        descText.text =
            Localization.GetLocaliationText(Localization.LocalizationTable.UI,
                DescKey);
        
        toggleButton.OnClicked += ToggleContent;
        for (int i = 0; i < cellViews.Length; i++)
        {
            cellViews[i].SetIndex(i);
            cellViews[i].OnSelected += OnSelected;
            cellViews[i].OnRerollClicked += OnReroll;
        }
    }

    private void OnDestroy()
    {
        toggleButton.OnClicked -= ToggleContent;
        foreach (var cell in cellViews)
        {
            cell.OnSelected -= OnSelected;
            cell.OnRerollClicked -= OnReroll;
        }
    }

    /// <summary>열림 연출 끝 애니메이션 이벤트용. 기존 프리팹 이벤트를 그대로 쓰기 위해 남겨둡니다.</summary>
    public void DisableAnimator() => animator.enabled = false;

    public override void SetInfo(BaseUIData data)
    {
        base.SetInfo(data);
        if (data is not SynergyBonusUIData d) return;

        session = d.Session;
        counter = d.Counter;
        onPicked = d.OnPicked;

        isSelected = false;
        SetHidden(false);

        for (int i = 0; i < cellViews.Length; i++)
            DrawCell(i);
    }

    public override void CloseUI()
    {
        isSelected = false;
        SetHidden(false);
        animator.enabled = true;
        session = null;
        onPicked = null;
        base.CloseUI();
    }

    /// <summary>칸 하나를 세션 상태대로 그립니다. 후보가 없으면 칸을 끕니다.</summary>
    private void DrawCell(int index)
    {
        SynergyBonusCellView cell =cellViews[index];
        SynergyData axis = session.GetSlot(index);
        
        cell.gameObject.SetActive(axis != null);
        if (axis == null) return;
        
        cell.Draw(axis, axis.GetCount(counter));
        cell.SetRerollVisible(session.CanReroll(index));
    }

    private void OnReroll(int index)
    {
        if (isSelected || session == null) return;

        session.Reroll(index);
        DrawCell(index);
        
        for(int i = 0; i <cellViews.Length; i++)
            cellViews[i].SetRerollVisible(session.CanReroll(i));
    }
    
    /// <summary>칸 index로 세션에서 지금 떠 있는 축을 꺼내 넘깁니다.</summary>
    private void OnSelected(int index)
    {
        if (isSelected || session == null) return;

        SynergyData picked = session.GetSlot(index);
        if (picked == null) return;

        isSelected = true;
        Action<SynergyData> callback = onPicked;
        CloseUI();
        callback?.Invoke(picked);
    }

    private void ToggleContent()
    {
        if (isSelected) return;
        SetHidden(!isHidden);
    }

    /// <summary>
    /// 콘텐츠만 숨기고 토글 버튼은 남깁니다.
    /// blocksRaycasts를 끄므로 숨긴 동안 터치가 보드로 내려가 유닛을 선택할 수 있습니다.
    /// </summary>
    private void SetHidden(bool hidden)
    {
        isHidden = hidden;
        contentGroup.alpha = hidden ? 0 : 1;
        contentGroup.blocksRaycasts = !hidden;
        contentGroup.interactable = !hidden;
    }
}
