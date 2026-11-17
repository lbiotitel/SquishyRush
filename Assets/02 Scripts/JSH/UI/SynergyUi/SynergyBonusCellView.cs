using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 3성 보너스 팝업의 시너지 후보 칸 하나입니다.
/// 칸 자체를 누르면 선택, 아래 리롤 버튼을 누르면 이 칸만 다시 뽑습니다.
/// 리롤 버튼이 자식이라 버튼 클릭은 버튼에서 멈추고 칸의 OnPointerClick까지 올라오지 않습니다.
/// </summary>
public class SynergyBonusCellView : MonoBehaviour, IPointerClickHandler
{
    [Serializable]
    private struct ElementCardColor
    {
        public EElementType element;
        public Color body;
        public Color head;
    }
    
    [Header("Text")]
    [SerializeField] private TMP_Text synergyNameText;
    [SerializeField] private TMP_Text countText;

    [Header("Image")]
    [SerializeField] private Image synergyIcon;
    
    [Header("Button")] 
    [SerializeField] private AnimButton rerollButton;

    [Header("Animation")] 
    [SerializeField] private Transform punchTarget;
    [SerializeField] private float animDuration = 0.2f;
    [SerializeField] private float animScale = 0.1f;

    [Header("Card Color")] 
    [SerializeField] private Image cardBody;
    [SerializeField] private Image cardHead;

    [SerializeField] private ElementCardColor[] elementColors =
    {
        new()
        {
            element = EElementType.DARK, body = new Color32(0xEF, 0xE7, 0xFF, 0xFF),
            head = new Color32(0xB4, 0x9B, 0xFF, 0xFF)
        },
        new()
        {
            element = EElementType.FIRE, body = new Color32(0xFF, 0xE6, 0xDF, 0xFF),
            head = new Color32(0xFF, 0x9A, 0x82, 0xFF)
        },
        new()
        {
            element = EElementType.WATER, body = new Color32(0xE0, 0xEE, 0xFF, 0xFF),
            head = new Color32(0x8C, 0xC0, 0xFF, 0xFF)
        },
        new()
        {
            element = EElementType.LIGHT, body = new Color32(0xFF, 0xF4, 0xC4, 0xFF),
            head = new Color32(0xFF, 0xD3, 0x4D, 0xFF)
        },
        new()
        {
            element = EElementType.EARTH, body = new Color32(0xF5, 0xEA, 0xDB, 0xFF),
            head = new Color32(0xD9, 0xB4, 0x8A, 0xFF)
        },
    };
    [SerializeField] private Color raceBody = new Color32(0xE3, 0xF6, 0xE8, 0xFF);   // 종족 시너지는 민트 하나로
    [SerializeField] private Color raceHead = new Color32(0x93, 0xDB, 0xA8, 0xFF);

    [Header("Count Text")]
    [SerializeField] private Color countFromColor = new Color32(0xA8, 0x92, 0x7C, 0xFF);
    [SerializeField] private Color countToColor = new Color32(0x2E, 0x9A, 0x49, 0xFF);
    [SerializeField] private string countArrow = " → ";
    
    private bool isAnim = false;
    private int index; // 몇 번째 칸인지. 이벤트에 실어 보내 UI가 세션에서 칸을 찾게 합니다.
    private string fromHex, toHex;
    
    /// <summary>칸을 눌러 선택했을 때. 칸 index를 넘깁니다.</summary>
    public event Action<int> OnSelected;

    /// <summary>리롤 버튼을 눌렀을 때. 칸 index를 넘깁니다.</summary>
    public event Action<int> OnRerollClicked;

    private void Awake()
    {
        if(rerollButton != null) rerollButton.OnClicked += HandleReroll;
        fromHex = ColorUtility.ToHtmlStringRGB(countFromColor);
        toHex = ColorUtility.ToHtmlStringRGB(countToColor);
    }

    private void OnDestroy()
    {
        if(rerollButton != null) rerollButton.OnClicked -= HandleReroll;
    }

    public void SetIndex(int index) => this.index = index;

    public void SetRerollVisible(bool visible)
    {
        if(rerollButton != null) rerollButton.gameObject.SetActive(visible);
    }

    /// <summary>
    /// 시너지 후보를 그립니다. axis는 그 축의 1단계 데이터입니다.
    /// </summary>
    public void Draw(SynergyData axis, int currentCount)
    {
        synergyIcon.sprite = axis.DisplayIcon;
        synergyNameText.text = Localization.GetLocaliationText(
            Localization.LocalizationTable.Synergy, axis.SynergyNameKey);
        countText.text = $"<color=#{fromHex}>{currentCount}</color>{countArrow}<color=#{toHex}>{currentCount + 1}</color>";
        
        ApplyCardColor(axis);   
    }

    /// <summary>속성 시너지는 속성 색, 종족 시너지는 민트로 카드를 칠합니다.</summary>
    private void ApplyCardColor(SynergyData axis)
    {
        if (cardBody == null || cardHead == null) return;

        Color body = raceBody, head = raceHead;
        if (axis.IsElementSynergy)
        {
            foreach (ElementCardColor c in elementColors)
            {
                if (c.element != axis.TargetElement) continue;
                body = c.body;
                head = c.head;
                break;
            }
        }

        cardBody.color = body;
        cardHead.color = head;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isAnim) return;
        isAnim = true;

        punchTarget.DOPunchScale(Vector3.one * animScale, animDuration)
            .OnComplete(() =>
            {
                isAnim = false;
                OnSelected?.Invoke(index);
            });
    }

    private void HandleReroll()
    {
        // 선택 연출 도중에 리롤되면 연출이 끝날 때 바뀐 축이 선택되므로 막습니다.
        if (isAnim) return;
        OnRerollClicked?.Invoke(index);
    }

    private void OnDisable()
    {
        // 팝업이 닫히며 트윈이 끊겨도 다음에 열 때 클릭이 막히지 않게 초기화합니다.
        punchTarget.DOKill();
        punchTarget.localScale = Vector3.one;
        isAnim = false;
    }
}
