using System;
using System.Collections;
using System.Collections.Generic;
using BackEnd.Quobject.EngineIoClientDotNet.Modules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public enum EConfirmType
{ 
    OK,
    OK_CANCLE
}

public class ConfirmUIData : BaseUIData
{
    public EConfirmType ConfirmType;
    public string LocalizationTableId = Localization.LocalizationTable.UI;

    public string Title;
    public string Desc;

    public string OkText;
    public string CancelText;

    public Action OkAction;
    public Action CancelAction;
}

//확인 팝업 UI에 대한 정의 입니다.
public class ConfirmUI : BaseUI
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;

    [SerializeField] private TMP_Text okBtnText;
    [SerializeField] private TMP_Text cancelBtnText;
    
    [Header("Button")]
    [SerializeField] private AnimButton okButton;
    [SerializeField] private AnimButton cancelButton;
    
    [Header("RectTransform")]
    [SerializeField] private RectTransform rectTransform;

    
    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
        okButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
    }

    public override void SetInfo(BaseUIData data)
    {
        base.SetInfo(data);

        ConfirmUIData confirmUIData = data as ConfirmUIData;

        if (confirmUIData != null)
        {
            titleText.text = Localization.GetLocaliationText(confirmUIData.LocalizationTableId,confirmUIData.Title);
            descText.text =  Localization.GetLocaliationText(confirmUIData.LocalizationTableId,confirmUIData.Desc);
            okBtnText.text =  Localization.GetLocaliationText(confirmUIData.LocalizationTableId,confirmUIData.OkText);
            cancelBtnText.text = Localization.GetLocaliationText(confirmUIData.LocalizationTableId,confirmUIData.CancelText);
            
            okButton.gameObject.SetActive(true);
            cancelButton.gameObject.SetActive(confirmUIData.ConfirmType == EConfirmType.OK_CANCLE);
            
            descText.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

            okButton.OnClicked += () =>
            {
                Logger.Log("ok 버튼을 눌렸습니다.");
                confirmUIData.OkAction?.Invoke();
                CloseUI();
            };

            cancelButton.OnClicked += () =>
            {
                Logger.Log("cancel 버튼을 눌렸습니다.");
                confirmUIData.CancelAction?.Invoke();
                CloseUI();
            };
        }
    }


    public override void OpenUI()
    {
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.BUTTON_CLICK_SFX);
        base.OpenUI();
    }

    public override void CloseUI()
    {
        okButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        okButton.RemoveAllAction();
        cancelButton.RemoveAllAction();
        
        base.CloseUI();
    }
}
