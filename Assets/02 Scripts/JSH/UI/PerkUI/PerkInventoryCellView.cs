using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 재능 인벤토리의 한 칸. 재능 아이콘을 그리고 드래그 입력을 뷰에 전달
/// </summary>
public class PerkInventoryCellView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("표시")] 
    [Tooltip("아이콘/등급 배경 묶음. 빈 칸이면 끔")]
    [SerializeField] private GameObject content;
    [SerializeField] private Image perkImage;
    [SerializeField] private Image rarityBg;

    /// <summary>
    /// 인벤토리 전체 기준 인덱스
    /// </summary>
    public int InventoryIndex { get; private set; } = -1;
    public PerkDataBase Data {get; private set;}
    public bool IsEmpty => Data == null;
    public Sprite Icon => Data != null ? Data.PerkIcon : null;
    public RectTransform Rect => (RectTransform)transform;

    public event Action<PerkInventoryCellView, PointerEventData> OnDragBegan;
    public event Action<PerkInventoryCellView, PointerEventData> OnDragging;
    public event Action<PerkInventoryCellView, PointerEventData> OnDragEnded;
    public event Action<PerkInventoryCellView, PointerEventData> OnPressed;
    public event Action<PerkInventoryCellView, PointerEventData> OnReleased;

    /// <summary>
    /// 인덱스와 재능 데이터로 칸을 그립니다. data가 null이면 빈 칸으로 표시합니다.
    /// 인덱스는 빈 칸이어도 저장합니다. (디버깅 시 몇 번 칸인지 확인용)
    /// </summary>
    public void Draw(int inventoryIndex, PerkDataBase data)
    {
        InventoryIndex = inventoryIndex;
        Data = data;

        bool hasData = data != null;
        content.SetActive(hasData);
        if (!hasData) return;

        perkImage.sprite = data.PerkIcon;
        rarityBg.color = GlobalDefines.Rarity.GetRarityPastelColor(data.PerkRarity);
    }

    #region 드래그 입력
    
    // 빈 칸에서 시작한 드래그는 알리지 않습니다.
    // 이후 OnDrag/OnEndDrag는 EventSystem이 계속 보내지만, 뷰가 "드래그 중인 셀"이 아니면 무시합니다.
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsEmpty) return;
        OnDragBegan?.Invoke(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
        => OnDragging?.Invoke(this, eventData);
    
    public void OnEndDrag(PointerEventData eventData)
        => OnDragEnded?.Invoke(this, eventData);

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsEmpty) return;
        OnPressed?.Invoke(this, eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
        => OnReleased?.Invoke(this, eventData);

    #endregion
}
