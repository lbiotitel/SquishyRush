using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class TitleConfirmUI : MonoBehaviour
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
    

    public void SetInfo(BaseUIData data)
    {
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


    public void OpenUI()
    {
        gameObject.SetActive(true);
    }

    
    private void CloseUI()
    {
        okButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        okButton.RemoveAllAction();
        cancelButton.RemoveAllAction();
        gameObject.SetActive(false);
    }
}
