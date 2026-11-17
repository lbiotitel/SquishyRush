using DG.Tweening;
using TMPro;
using UnityEngine;


public class ToastMessageUIData : BaseUIData
{
    public string message;
}


//토스트 메시지의 역할을 하는 클래스입니다.
public class ToastMessageUI : BaseUI
{
    [Header("Text")]
    [SerializeField] private TMP_Text messageText;
    
    [Header("Anim")]
    [SerializeField] private float moveY;
    [SerializeField] private float moveDuration;
    private Vector3 originPos;

    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);
        originPos = transform.position;
    }

    public override void SetInfo(BaseUIData data)
    {
        base.SetInfo(data);
        ToastMessageUIData toastMessageUIData = data as ToastMessageUIData;

        if (toastMessageUIData != null)
        {
            messageText.text =  Localization.GetLocaliationText(Localization.LocalizationTable.UI, toastMessageUIData.message);
        }
    }

    public override void OpenUI()
    {
        base.OpenUI();
        DoAnimation();
    }


    private void DoAnimation()
    {
        transform.DOMoveY(originPos.y + moveY, moveDuration).
            OnComplete(() => CloseUI());
    }


    public override void CloseUI()
    {
        transform.position = originPos;
        base.CloseUI();
    }

}
