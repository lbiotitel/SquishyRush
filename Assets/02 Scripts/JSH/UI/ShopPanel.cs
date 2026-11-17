using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class ShopPanel : MonoBehaviour
{
    [Header("슬롯")] 
    [SerializeField] private ShopSlotView[] slotViews;

    [Header("버튼")] 
    [SerializeField] private AnimButton resetButton;
    [SerializeField] private TMP_Text resetCostText;

    [Header("골드")] 
    [SerializeField] private TMP_Text goldText;

    [Header("슬라이드")] 
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private float hiddenY = -700f;
    [SerializeField] private float shownY = 0f;
    [SerializeField] private float slideDuration = 0.35f;

    [Header("숨김")] 
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("재능 인벤토리")] 
    [SerializeField] private AnimButton bagButton;
    [SerializeField] private GameObject unitSlotRoot;
    [SerializeField] private PerkInventoryView inventoryView;

    [Header("상점 경험치 뷰")]
    [SerializeField] private ShopLevelView shopLevelView;
    
    private ShopSystem shop;
    private RunContext ctx;
    private BoardModel board;
    private IShopActions actions;
    private Tween slideTween;
    private ToastMessageUIData toastData;
    private Camera uiCamera;
    private Tween bagPunch;
    private InputController inputController;

    private bool isInventoryOpen;

    private readonly List<RaycastResult> raycastResults = new();
    private PointerEventData pointerData;
    private EventSystem pointerDataOwner;

    public event Action<BattleUnit> OnUnitPreviewOpened;
    public event Action OnUnitPreviewClosed;

    private readonly Dictionary<string, BattleUnit> previewCache = new();
    private bool isPreviewing;
    private int pressedIndex = -1;
    
    public void Init(ShopSystem shop, InputController inputController ,RunContext ctx, BoardModel board, IShopActions actions, PerkInventory inventory, IPerkEquipActions perkActions)
    {
        Canvas canvas = GetComponentInParent<Canvas>(true).rootCanvas;
        uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        this.inputController = inputController;
        this.shop = shop;
        this.ctx = ctx;
        this.board = board;
        this.actions = actions;

        this.inputController.OnPressEvent -= OnPressEventHandler;
        this.inputController.OnPressEvent += OnPressEventHandler;
        shopLevelView.Init(ctx, shop);
        
        toastData = new ToastMessageUIData();
        
        for (int i = 0; i < slotViews.Length; i++)
        {
            slotViews[i].Init();
        }
        
        resetButton.OnClicked -= OnClickReset;
        resetButton.OnClicked += OnClickReset;

        ctx.OnIGCurrencyChanged -= Refresh;
        ctx.OnIGCurrencyChanged += Refresh;

        shop.OnShopRefreshed -= Refresh;
        shop.OnShopRefreshed += Refresh;
        
        board.OnBoardChanged -= Refresh;
        board.OnBoardChanged += Refresh;

        bagButton.OnClicked -= OnClickBag;
        bagButton.OnClicked += OnClickBag;
        
        inventoryView.Init(inventory, perkActions);
        SetInventoryOpen(false);
        SetPositionImmediate(false);
    }

    

    private void OnPressEventHandler(EPressedType type, Vector2 pos)
    {
        switch (type)
        {
            case EPressedType.PressDown:
                pressedIndex = GetSlotIndex(pos);
                if(pressedIndex != -1) slotViews[pressedIndex].PlayPressDown();
                break;
            
            case EPressedType.ShortPress:
                if (pressedIndex != -1) BuyWithPunch(pressedIndex);
                break;
            
            case EPressedType.LongPress:
                if(pressedIndex != -1) OpenPreview(pressedIndex);
                break;
            
            case EPressedType.PressUp:
                if(pressedIndex != -1) slotViews[pressedIndex].PlayRelease();
                pressedIndex = -1;
                ClosePreview();
                break;
        }
    }

    private void BuyWithPunch(int index)
    {
        ShopSlotView view = slotViews[index];
        if (view.IsPunching) return;
            
        view.PlayPunch(() => HandleResult(actions.TryBuyUnit(index)));
    }

    private void OpenPreview(int index)
    {
        UnitData data = shop.Slots[index].Unit;
        if (data == null) return;
        
        slotViews[index].PlayLift();
        OnUnitPreviewOpened?.Invoke(GetPreviewUnit(data));
        isPreviewing = true;
    }

    private void ClosePreview()
    {
        if (!isPreviewing) return;
        isPreviewing = false;
        OnUnitPreviewClosed?.Invoke();
    }

    private BattleUnit GetPreviewUnit(UnitData data)
    {
        if (!previewCache.TryGetValue(data.UnitId, out BattleUnit unit))
        {
            unit = BattleUnitFactory.CreatePlayerUnit(ctx, data);
            previewCache[data.UnitId] = unit;
        }

        return unit;
    }

    /// <summary>
    /// 화면 좌표에서 맨 위에 맞은 UI가 어느 상점 슬롯 안에 있는지 찾습니다. 없거나 빈 슬롯이면 -1.
    /// 인벤토리가 열렸거나, 드래그로 숨겼거나, 팝업이 위에 있으면 슬롯이 안 맞아서 -1이 됩니다.
    /// </summary>
    private int GetSlotIndex(Vector2 pos)
    {
        EventSystem es = EventSystem.current;
        if (es == null) return -1;

        if (pointerData == null || pointerDataOwner != es)
        {
            pointerData = new PointerEventData(es);
            pointerDataOwner = es;
        }

        pointerData.position = pos;
        raycastResults.Clear();
        es.RaycastAll(pointerData, raycastResults);
        if (raycastResults.Count == 0) return -1;

        Transform hit = raycastResults[0].gameObject.transform;
        for (int i = 0; i < slotViews.Length; i++)
        {
            if (hit.IsChildOf(slotViews[i].transform))
                return slotViews[i].HasUnit ? i : -1;
        }
        return -1;
    }
    
    private void OnDestroy()
    {
        slideTween?.Kill();

        if (ctx != null) ctx.OnIGCurrencyChanged -= Refresh;
        if (shop != null) shop.OnShopRefreshed -= Refresh;
        if (board != null) board.OnBoardChanged -= Refresh;
        if (bagButton != null) bagButton.OnClicked -= OnClickBag;
        if(inputController != null)inputController.OnPressEvent -= OnPressEventHandler;
    }

    #region 입력
    
    private void OnClickReset() => HandleResult(actions.TryReroll());
    private void OnClickBag() => SetInventoryOpen(!isInventoryOpen);
    
    private void HandleResult(EShopActionResult result)
    {
        if (result == EShopActionResult.Success) return;

        string key = result.GetMessageKey();
        if (string.IsNullOrEmpty(key)) return;

        toastData.message = key;
        GlobalManagers.UI.PopUpContainer.OpenUI<ToastMessageUI>(toastData);
    }
    
    #endregion

    #region 표시

    public void Refresh()
    {
        if (shop == null || ctx == null || board == null) return;

        int count = Mathf.Min(slotViews.Length, shop.Slots.Count);
        for (int i = 0; i < count; i++)
        {
            ShopSlot slot = shop.Slots[i];

            bool canUpgrade = slot.Unit != null
                              && board.TryFindMergeTarget(slot.Unit.UnitId, 0, out _);

            EShopActionResult buyResult = shop.CanBuy(i, ctx, board);
            slotViews[i].SetSlot(slot, canUpgrade, buyResult);

        }
        
        int rerollCost =  shop.GetRerollCost(ctx);
        resetCostText.text = rerollCost.ToString();
        resetCostText.color = ctx.IGCurrency >= rerollCost ? Color.white : Color.red;

        goldText.text = ctx.IGCurrency.ToString();
    }

    #endregion

    #region 탭 전환

    /// <summary>
    /// 구매 슬롯과 재능 인벤토리를 전환합니다. 가방 버튼이 토글합니다.
    /// 리롤·골드·전투 시작 버튼은 그대로 두고 슬롯 영역만 바꿔 상점 조작 흐름이 끊기지 않게 합니다.
    /// SetActive로 끄므로 드래그 중에 전환되면 인벤토리 뷰의 OnDisable이 드래그를 정리합니다.
    /// </summary>
    private void SetInventoryOpen(bool open)
    {
        isInventoryOpen = open;
        unitSlotRoot.SetActive(!open);
        inventoryView.gameObject.SetActive(open);
    }

    #endregion

    #region 재능 구매 연출

    /// <summary> 가방 버튼의 화면 좌표. 재능 구매 연출의 목적지</summary>
    public Vector2 BagScreenPosition
        => RectTransformUtility.WorldToScreenPoint(uiCamera, bagButton.transform.position);

    /// <summary>
    /// 재능이 가방에 도착했을 때 살짝 튕깁니다.
    /// 연달아 사면 펀치가 겹쳐 크기가 틀어지므로, 이전 펀치를 끝까지 완료시킨 뒤 새로 겁니다.
    /// (BounceUI는 무한 반복 대기 모션이라 한 번만 튕기는 용도로는 맞지 않음)
    /// </summary>
    public void PlayBagReceive()
    {
        bagPunch?.Complete();
        bagPunch = bagButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 6, 0.5f)
            .SetLink(gameObject);
    }

    #endregion
    
    #region 슬라이드

    public void SlideIn() => Slide(true);
    public void SlideOut() => Slide(false);

    private void Slide(bool show)
    {
        slideTween?.Kill();

        if (show)
        {
            gameObject.SetActive(true);
            SetInventoryOpen(false);
            Refresh();
        }

        slideTween = panelRect
            .DOAnchorPosY(show ? shownY : hiddenY, slideDuration)
            .SetEase(show ? Ease.OutCubic : Ease.InCubic)
            .OnComplete(() =>
            {
                if (!show) gameObject.SetActive(false);
            });
    }

    private void SetPositionImmediate(bool show)
    {
        Vector2 pos = panelRect.anchoredPosition;
        pos.y = show ? shownY : hiddenY;
        panelRect.anchoredPosition = pos;
        gameObject.SetActive(show);
    }
    #endregion

    public void SetDragHidden(bool hidden)
    {
        canvasGroup.alpha = hidden ? 0f : 1f;
        canvasGroup.blocksRaycasts = !hidden;
    }
}