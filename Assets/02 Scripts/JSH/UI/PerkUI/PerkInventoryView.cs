using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 재능 인벤토리 화면입니다. 페이지 단위로 칸을 그리고, 드래그 장착 흐름을 관리합니다.
/// </summary>
public class PerkInventoryView : MonoBehaviour
{
    [Header("칸")] 
    [SerializeField] private PerkInventoryCellView[] cells;

    [Header("페이지 화살표")] 
    [SerializeField] private AnimButton leftArrow;
    [SerializeField] private AnimButton rightArrow;

    [Header("드래그")] 
    [SerializeField] private PerkDragGhost ghost;
    
    [Header("툴팁")]
    [SerializeField] private Vector2 tooltipOffset = new Vector2(0f, 150f);

    private PerkInventory inventory;
    private IPerkEquipActions actions;
    private BaseUI tooltipUI;
    
    private int page;

    //드래그 상태
    private PerkInventoryCellView dragCell;
    //드래그 시작 시점의 인벤토리 인덱스
    private int dragIndex = -1;
    //툴팁을 연 손가락
    private int pressPointerId = -1;
    //드래그 시작 시점의 재능
    private PerkDataBase dragData;
    // 멀티터치에서 다른 손가락 입력을 걸러내기 위함
    private int dragPointerId;

    private int PageSize => cells.Length;

    #region 초기화 / 해제

    public void Init(PerkInventory inventory, IPerkEquipActions actions)
    {
        this.inventory = inventory;
        this.actions = actions;

        inventory.OnChanged -= Redraw;
        inventory.OnChanged += Redraw;

        foreach (PerkInventoryCellView cell in cells)
        {
            cell.OnDragBegan -= HandleDragBegan;
            cell.OnDragBegan += HandleDragBegan;

            cell.OnDragging -= HandleDragging;
            cell.OnDragging += HandleDragging;
            
            cell.OnDragEnded -= HandleDragEnded;
            cell.OnDragEnded += HandleDragEnded;

            cell.OnPressed -= HandlePressed;
            cell.OnPressed += HandlePressed;

            cell.OnReleased -= HandleReleased;
            cell.OnReleased += HandleReleased;
        }

        leftArrow.OnClicked -= OnClickLeft;
        leftArrow.OnClicked += OnClickLeft;
        rightArrow.OnClicked -= OnClickRight;
        rightArrow.OnClicked += OnClickRight;

        ghost.Hide();
        page = 0;
        Redraw();
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.OnChanged -= Redraw;

        foreach (PerkInventoryCellView cell in cells)
        {
            if (cell == null) continue;
            cell.OnDragBegan -= HandleDragBegan;
            cell.OnDragging -= HandleDragging;
            cell.OnDragEnded -= HandleDragEnded;
            cell.OnPressed -= HandlePressed;
            cell.OnReleased -= HandleReleased;
        }

        if (leftArrow != null) leftArrow.OnClicked -= OnClickLeft;
        if (rightArrow != null) rightArrow.OnClicked -= OnClickRight;
    }

    /// <summary>
    /// 드래그 도중 뷰가 꺼지면(가방 버튼으로 탭 전환, 상점 패널 슬라이드 아웃 등) OnEndDrag가 오지 않을 수 있습니다.
    /// 그대로 두면 고스트가 화면에 남으므로 여기서 드래그를 취소합니다.
    /// </summary>
    private void OnDisable() => CancelDrag();
    
    #endregion

    #region 툴팁
    
    /// <summary>
    /// 드래그를 시작한 위치 위에 툴팁을 띄웁니다.
    /// 드래그하는 동안 따라다니지 않고 제자리에 고정합니다. 따라다니면 재능을 놓을 유닛을 가리기 때문입니다.
    /// </summary>
    private void OpenTooltip(PerkDataBase perk, Vector2 screenPos)
    {
        Vector3 pos = screenPos + tooltipOffset;
        tooltipUI = InGameManagers.UI.PopUpContainer.OpenUI<PerkToolTipUI>(PerkToolTipUIData.Create(perk, pos));
    }

    /// <summary>
    /// 툴팁을 닫습니다. 장착 성공, 실패, 취소 모든 경로에서 호출됩니다.
    /// </summary>
    private void HideTooltip()
    {
        if (tooltipUI == null) return;

        InGameUIManager ui = InGameManagers.UI;
        if(ui != null && ui.PopUpContainer != null)
            ui.PopUpContainer.CloseUI(tooltipUI);
        
        tooltipUI = null;
    }

    #endregion
    
    #region 그리기 / 페이지

    /// <summary>
    /// 현재 페이지의 칸을 다시 그리고 화살표 표시를 갱신합니다.
    /// 목록이 줄어 현재 페이지가 비게 되면 마지막 페이지로 당깁니다.
    /// </summary>
    private void Redraw()
    {
        if (inventory == null) return;

        page = Mathf.Clamp(page, 0, MaxPage);

        for (int i = 0; i < cells.Length; i++)
        {
            int index = page * PageSize + i;

            //범위 밖이면 null -> 빈칸
            cells[i].Draw(index, inventory.Get(index));
        }
        
        leftArrow.gameObject.SetActive(page > 0);
        rightArrow.gameObject.SetActive(page < MaxPage);
    }

    /// <summary>
    /// 마지막 페이지 번호입니다. 비어 있어도 0페이지는 존재합니다.
    /// </summary>
    private int MaxPage => Mathf.Max(0, (inventory.Count - 1) / PageSize);

    private void OnClickLeft()
    {
        if (page <= 0) return;
        page--;
        Redraw();
    }

    private void OnClickRight()
    {
        if (page >= MaxPage) return;
        page++;
        Redraw();
    }
    
    #endregion
    
    #region 드래그 장착

    /// <summary>
    /// 드래그 시작. 이미 다른 드래그가 진행 중이면(두 번째 손가락) 무시합니다.
    /// 인덱스와 재능을 이 시점에 저장해 두고, 드롭할 때 목록이 그사이 바뀌지 않았는지 확인합니다.
    /// </summary>
    private void HandleDragBegan(PerkInventoryCellView cell, PointerEventData e)
    {
        if (dragCell != null) return;
        if (cell.IsEmpty) return;

        dragCell = cell;
        dragIndex = cell.InventoryIndex;
        dragData = cell.Data;
        dragPointerId = e.pointerId;

        ghost.Show(cell.Icon, e.position);
    }

     
    /// <summary>
    /// 드래그 중. 드래그를 시작한 셀과 손가락의 입력만 고스트에 반영합니다.
    /// </summary>
    private void HandleDragging(PerkInventoryCellView cell, PointerEventData e)
    {
        if (!IsCurrentDrag(cell, e)) return;

        ghost.MoveTo(e.position);
    }

    /// <summary>
    /// 드래그 종료. 놓은 위치의 아군 유닛에 장착을 시도합니다.
    /// 성공하면 고스트를 끄고(인벤토리 OnChanged로 칸은 이미 다시 그려짐),
    /// 실패하면 고스트를 원래 칸으로 돌려보냅니다.
    /// </summary>
    private void HandleDragEnded(PerkInventoryCellView cell, PointerEventData e)
    {
        if (!IsCurrentDrag(cell, e)) return;
        HideTooltip();

        // 드래그 도중 인벤토리가 바뀌어 같은 인덱스가 다른 재능을 가리키면 잘못 장착될 수 있으므로 취소합니다.
        if (inventory.Get(dragIndex) != dragData)
        {
            Logger.LogWarning("[PerkInventory] 드래그 중 인벤토리가 바뀌어 장착을 취소합니다.");
            ReturnGhost();
            return;
        }

        EPerkEquipResult result = actions.TryEquipPerkAt(dragIndex, e.position);

        if (result == EPerkEquipResult.Success)
        {
            ghost.Hide();
            ClearDragState();
            return;
        }
        
        ReturnGhost();
    }

    /// <summary>
    /// 누르는 순간 툴팁을 엽니다. 이후 드래그로 이어져도 툴팁은 그대로 유지됩니다.
    /// </summary>
    private void HandlePressed(PerkInventoryCellView cell, PointerEventData e)
    {
        if (dragCell != null || tooltipUI != null) return;

        pressPointerId = e.pointerId;
        OpenTooltip(cell.Data, e.position);
    }

    /// <summary>
    /// 툴팁을 연 손가락을 떼면 닫습니다. 드래그 여부와 상관없이 호출됩니다.
    /// </summary>
    private void HandleReleased(PerkInventoryCellView cell, PointerEventData e)
    {
        if (e.pointerId != pressPointerId) return;

        pressPointerId = -1;
        HideTooltip();
    }

    private bool IsCurrentDrag(PerkInventoryCellView cell, PointerEventData e)
        => dragCell != null && cell == dragCell && e.pointerId == dragPointerId;

    /// <summary>
    /// 고스트를 드래그를 시작한 칸으로 되돌립니다. 인벤토리 원본은 건드리지 않았으므로 되돌릴 모델 상태가 없습니다.
    /// 드래그 상태는 바로 비워 되돌아가는 도중에도 다음 드래그를 시작할 수 있게 합니다.
    /// </summary>
    private void ReturnGhost()
    {
        RectTransform origin = dragCell != null ? dragCell.Rect : null;
        ClearDragState();
        ghost.ReturnTo(origin);
    }

    private void CancelDrag()
    {
        HideTooltip();
        pressPointerId = -1;
        
        if (dragCell == null) return;

        ClearDragState();
        if (ghost != null) ghost.Hide();
    }

    private void ClearDragState()
    {
        dragCell = null;
        dragIndex = -1;
        dragData = null;
    }
    
    #endregion
}
