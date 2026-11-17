using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SynergyCellView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private Vector2 toolTipOffset = new(20f, 0f);

    [Header("색")] 
    [SerializeField] private Color onColor = new Color(0.3f, 1f, 0.3f);
    [SerializeField] private Color offColor = new Color(0.55f, 0.55f, 0.55f);
    
    private readonly StringBuilder sb = new(64);
    private readonly SynergyToolTipUIData toolTipData = new();
    private string onHex;
    private string offHex;

    private Camera uiCamera;
    private ScrollRect parentScroll;
    private Canvas rootCanvas;
    private SynergyStatus currentStatus;
    private BaseUI openedToolTip;

    private void Awake()
    {
        // 리치 텍스트용 "#RRGGBB" 문자열은 한 번만 만들어 둠
        onHex = "#" + ColorUtility.ToHtmlStringRGB(onColor);
        offHex = "#" + ColorUtility.ToHtmlStringRGB(offColor);
    }

    public void Init(Camera uiCamera, ScrollRect parentScroll)
    {
        this.uiCamera = uiCamera;
        this.parentScroll = parentScroll;
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
    }

    public void Set(in SynergyStatus status)
    {
        currentStatus = status;
        icon.sprite = status.Tiers[0].DisplayIcon;  

        sb.Clear();
        for (int i = 0; i < status.Tiers.Count; i++)
        {
            if( i > 0) sb.Append("<color=").Append(offHex).Append(">/</color>");

            bool isOn = i == status.ActiveTier - 1;
            sb.Append("<color=").Append(isOn ? onHex : offHex).Append('>')
                .Append(status.Tiers[i].RequiredCount)
                .Append("</color>");
        }
        
        tierText.SetText(sb);   // StringBuilder를 직접 넘기면 string 할당이 생기지 않음
    }

    public void OnPointerDown(PointerEventData e) => OpenToolTip();
    public void OnPointerUp(PointerEventData e) => CloseToolTip();

    public void OnInitializePotentialDrag(PointerEventData e) => parentScroll?.OnInitializePotentialDrag(e);
    public void OnBeginDrag(PointerEventData e) { CloseToolTip(); parentScroll?.OnBeginDrag(e); }
    public void OnDrag(PointerEventData e) => parentScroll?.OnDrag(e);
    public void OnEndDrag(PointerEventData e) => parentScroll?.OnEndDrag(e);

    private void OnDisable() => CloseToolTip();

    private void OpenToolTip()
    {
        var rt = (RectTransform)transform;
        Vector3 rightTop = rt.TransformPoint(rt.rect.xMax, rt.rect.yMax, 0f);
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, rightTop);

        toolTipData.OpenPos = screenPos + toolTipOffset * rootCanvas.scaleFactor;
        toolTipData.Status = currentStatus;
        
        openedToolTip = InGameManagers.UI.PopUpContainer.OpenUI<SynergyToolTipUI>(toolTipData);
    }

    private void CloseToolTip()
    {
        if (openedToolTip == null) return;
        openedToolTip.CloseUI();
        openedToolTip = null;
    }
}
