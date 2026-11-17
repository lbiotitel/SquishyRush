using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BoardView : MonoBehaviour, IVfxAnchorResolver
{
    [Header("슬롯 앵커")] 
    [SerializeField] private Transform[] battleAnchors;
    [SerializeField] private Transform[] benchAnchors;

    [Header("적 슬롯 앵커")]
    [SerializeField] private Transform[] enemyAnchors;

    [Header("유닛 뷰")] 
    [SerializeField] private BattleUnitView unitViewPrefab;
    [SerializeField] private Transform unitRoot;

    [Header("스냅")]
    [SerializeField] private float snapRadius = 1.5f;
    
    [Header("드래그")]
    [SerializeField] private Camera boardCamera;

    [Header("스케일")] 
    [SerializeField] private float battleScale = 1.5f;
    [SerializeField] private float benchScale = 2f;
    
    private BoardModel board;
    private BoardModel enemyBoard;
    private IBoardActions actions;
    [SerializeField] SellZoneView sellZoneView;

    private readonly Dictionary<int, BattleUnitView> views = new();
    private readonly Dictionary<int, BattleUnitView> enemyViews = new();
    private readonly List<int> removeBuffer = new();
    
    private BattleUnitView dragView;
    private SlotIndex dragFrom;

    private float benchY;
    private float battleFrontY;
    
    // 상세 보기 대상이 정해지거나 풀렸을 때 알리기만 함. 무엇을 할지는 구독자가 결정
    public event Action<BattleUnit> OnUnitSelected;
    public event Action OnUnitDeselected;
    public event Action<bool> OnUnitDragChanged;
    
    private BattleUnit selectedUnit;

    private bool enemyVisible = true;
    private bool inputEnabled = true;

    private const float TAP_MAX_MOVE_PX = 20f;
    private Vector2 pressPos;
    private bool pressOnUI;
    
    private int lastMergeSfxFrame = -1;
    
    public void Init(BoardModel board, IBoardActions actions)
    {
        this.board = board;
        this.actions = actions;

        board.OnBoardChanged -= Refresh;
        board.OnBoardChanged += Refresh;
        
        board.OnMergeHappened -= OnMergeHappened;
        board.OnMergeHappened += OnMergeHappened;

        benchY = benchAnchors[0].position.y;
        battleFrontY = battleAnchors[0].position.y;

        for (int i = 1; i < battleAnchors.Length; i++)
            battleFrontY = Mathf.Min(battleFrontY, battleAnchors[i].position.y);
        
        //Logger.Log($"[BoardView] Init - 전투 {board.BattleCapacity}칸 / 벤치 {board.BenchCapacity} 칸");
        Refresh();
    }
    
    private void OnMergeHappened()
    {
        if (lastMergeSfxFrame == Time.frameCount) return;
        lastMergeSfxFrame = Time.frameCount;
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.MERGED_SFX);

    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (!enabled)
        {
            CancelDrag();
            Deselect();
        }
    }

    private void Update()
    {
        if (actions == null || !inputEnabled) return;

        if (dragView != null && actions.CurrentPhase != EInGamePhase.Shop) CancelDrag();
        
        switch (actions.CurrentPhase)
        {
            case EInGamePhase.Shop: UpdateShopInput(); break;
            case EInGamePhase.Battle: UpdateBattleInput(); break;
        }
    }

    private void UpdateShopInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pressPos = Input.mousePosition;
            pressOnUI = IsPointerOverUI();
            if (!pressOnUI && actions.CanEditBoard) BeginDrag();
        }
        else if (Input.GetMouseButton(0)) UpdateDrag();
        else if (Input.GetMouseButtonUp(0))
        {
            if (dragView != null) EndDrag();
            else HandleShopTap();
        }
    }

    private void UpdateBattleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pressPos = Input.mousePosition;
            return;
        }

        if (!Input.GetMouseButtonUp(0)) return;

        if (Vector2.Distance(pressPos, Input.mousePosition) > TAP_MAX_MOVE_PX) return;
        if (IsPointerOverUI()) return;

        BattleUnit unit = PickUnit();
        if(unit == null || unit == selectedUnit) Deselect();
        else Select(unit);
    }

    private void CancelDrag()
    {
        if (dragView == null) return;
        
        dragView.SetDragging(false);
        dragView = null;
        sellZoneView.HideUI();
        OnUnitDragChanged?.Invoke(false);
        Deselect();
        Refresh();
    }

    #region 선택

    private void Select(BattleUnit unit)
    {
        selectedUnit = unit;
        OnUnitSelected?.Invoke(unit);
    }

    private void Deselect()
    {
        if (selectedUnit == null) return;
        selectedUnit = null;
        OnUnitDeselected?.Invoke();
    }

    // 포인터 위치에서 가장 가까운 유닛 (아군 + 적)
    private BattleUnit PickUnit(bool includeAllies = true)
    {
        if (!TryGetPointerWorld(out Vector3 pos)) return null;

        BattleUnit best = null;
        float bestSqr = snapRadius * snapRadius;

        if(includeAllies)
            foreach (var v in views.Values) Consider(v);
        foreach (var v in enemyViews.Values) Consider(v);
        return best;

        void Consider(BattleUnitView v)
        {
            if (v == null || !v.gameObject.activeSelf) return;
            float sqr = (v.transform.position - pos).sqrMagnitude;
            if (sqr >= bestSqr) return;
            bestSqr = sqr;
            best = v.Unit;
        }
    }

    // 모바일은 인자 없는 IsPointerOverGameObject()가 터치를 못 잡으므로 fingerId를 넘김
    private static bool IsPointerOverUI()
    {
        var es = EventSystem.current;
        if (es == null) return false;
        return Input.touchCount > 0
            ? es.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
            : es.IsPointerOverGameObject();
    }
    
    #endregion

    #region 스케일 조정

    private float GetScaleByY(float y)
    {
        float t = Mathf.InverseLerp(battleFrontY, benchY, y);
        return Mathf.Lerp(battleScale, benchScale, t);
    }

    #endregion

    #region 드래그

    // 상점 단계에서 드래그가 시작되지 않은 탭 처리
    private void HandleShopTap()
    {
        if (pressOnUI) return;
        if (Vector2.Distance(pressPos, Input.mousePosition) > TAP_MAX_MOVE_PX) return;

        // 평소엔 아군은 드래그로 정보를 보므로 탭은 적만 집습니다.
        // 보너스 선택 중엔 드래그가 막혀 있으니 탭으로 아군도 집어 정보를 볼 수 있게 합니다. 
        BattleUnit unit = PickUnit(includeAllies: !actions.CanEditBoard);
        if(unit == null || unit == selectedUnit) Deselect();
        else Select(unit);
    }

    private bool TryGetPointerWorld(out Vector3 worldPos)
        => TryGetPointerWorld(Input.mousePosition, out worldPos);
    
    private bool TryGetPointerWorld(Vector2 screenPos, out Vector3 worldPos)
    {
        worldPos = default;
        Ray ray = boardCamera.ScreenPointToRay(screenPos);

        if (Mathf.Approximately(ray.direction.z, 0f)) return false;
        
        float t = -ray.origin.z / ray.direction.z;
        if (t <= 0f) return false;
        
        worldPos = ray.GetPoint(t);
        return true;
    }

    private void BeginDrag()
    {
        if (!TryGetPointerWorld(out Vector3 worldPos)) return;
        if (!TryFindNearestSlot(worldPos, out SlotIndex slot)) return;
        
        BattleUnit unit = board.Get(slot);
        if (unit == null) return;
        if (!views.TryGetValue(unit.RuntimeId, out dragView)) return;

        dragFrom = slot;

        int price = InGameBalance.GetSellPrice(unit);
        sellZoneView.ShowUI(price);
        OnUnitDragChanged?.Invoke(true);
        
        dragView.SetDragging(true);
        Select(unit);
    }

    private void UpdateDrag()
    {
        if (dragView == null) return;
        if (!TryGetPointerWorld(out Vector3 worldPos)) return;
        
        dragView.transform.position = worldPos;
        dragView.SetBodyScale(GetScaleByY(worldPos.y));

        bool overSell = sellZoneView.Contains(Input.mousePosition, boardCamera);
        sellZoneView.SetHover(overSell);
        
        if(!overSell) dragView.SetBodyScale((GetScaleByY(worldPos.y)));
    }

    private void EndDrag()
    {
        if (dragView == null) return;
        
        bool overSell = sellZoneView.Contains(Input.mousePosition, boardCamera);
        
        dragView.SetDragging(false);
        dragView = null;
        sellZoneView.HideUI();
        OnUnitDragChanged?.Invoke(false);
        Deselect();

        if (overSell)
        {
            actions.TrySellUnit(dragFrom);
            Refresh();
            return;
        }

        if (TryGetPointerWorld(out Vector3 worldPos) && TryFindNearestSlot(worldPos, out SlotIndex to)
                                                     && !to.Equals(dragFrom))
        {
            actions.TryMoveUnit(dragFrom, to);
        }
        
        Refresh();
    }
    
    #endregion

    #region 재능 드롭 판정

    /// <summary>
    /// 화면 좌표에 있는 아군 유닛을 찾습니다.
    /// 재능 인벤토리에서 드래그한 재능을 놓을 대상 판정용
    /// </summary>
    public BattleUnit TryPickAllyUnit(Vector2 screenPos)
    {
        if (board == null) return null;
        if(!TryGetPointerWorld(screenPos, out Vector3 worldPos)) return null;
        if (!TryFindNearestSlot(worldPos, out SlotIndex slot)) return null;

        return board.Get(slot);
    }
    
    /// <summary>
    /// 화면 좌표를 보드 평면(z = 0)의 월드 좌표로 바꿉니다.
    /// UI 위치(가방 버튼)를 연출 목적지로 쓸 때 필요합니다. 내부 드래그 판정과 같은 계산을 재사용합니다.
    /// </summary>
    public bool TryScreenToBoardWorld(Vector2 screenPos, out Vector3 worldPos)
        => TryGetPointerWorld(screenPos, out worldPos);
    

    #endregion

    #region 앵커 조회

    private Transform[] GetAnchors(EZone zone)
        => zone == EZone.Bench ? benchAnchors : battleAnchors;

    public Transform GetAnchor(SlotIndex slot)
    {
        var arr = GetAnchors(slot.Zone);
        return (slot.Index >= 0 && slot.Index < arr.Length) ? arr[slot.Index] : null;
    }

    public bool TryFindNearestSlot(Vector3 worldPos, out SlotIndex slot)
    {
        slot = default;
        float best = snapRadius * snapRadius;
        bool found = false;

        for (int z = 0; z < 2; z++)
        {
            EZone zone = (EZone)z;
            var arr = GetAnchors(zone);

            for (int i = 0; i < arr.Length; i++)
            {
                float sqr = (arr[i].position - worldPos).sqrMagnitude;
                if (sqr >= best) continue;

                best = sqr;
                slot = new SlotIndex(zone, i);
                found = true;
            }

        }

        return found;
    }

    #endregion

    #region 갱신
 
    public void Refresh()
    {
        if (board == null) return;
        
        SyncZone(EZone.Battle, board.BattleCapacity);
        SyncZone(EZone.Bench, board.BenchCapacity);
        RemoveStaleViews();
        
        //임시. BoardView 만들면 지운다
        // string log = "Bench: ";
        // for (int i = 0; i < board.BenchCapacity; i++)
        // {
        //     var u = board.Get(new SlotIndex(EZone.Bench, i));
        //     log += u == null ? "[ - ] " : $"[#{u.RuntimeId} {u.Star}성] ";
        // }
        // Logger.Log(log);
    }

    private void SyncZone(EZone zone, int capacity)
    {
        var arr = GetAnchors(zone);
        int count = Mathf.Min(capacity, arr.Length);

        for (int i = 0; i < count; i++)
        {
            var slot = new SlotIndex(zone, i);
            BattleUnit unit = board.Get(slot);
            if (unit == null) continue;

            if (!views.TryGetValue(unit.RuntimeId, out var view))
            {
                view = Instantiate(unitViewPrefab, unitRoot);
                views.Add(unit.RuntimeId, view);
            }
            
            view.Bind(unit, slot);
            if (view == dragView) continue;
            
            view.transform.position = arr[i].position;
            view.SetBodyScale(GetScaleByY(arr[i].position.y));
        }
    }

    private void RemoveStaleViews()
    {
        removeBuffer.Clear();

        foreach (var pair in views)
        {
            if(pair.Value == null) { removeBuffer.Add(pair.Key);
                continue;
            }
            
            BattleUnit unit = board.Get(pair.Value.Slot);
            if (unit != null && unit.RuntimeId == pair.Key) continue;
            removeBuffer.Add(pair.Key);
        }

        for (int i = 0; i < removeBuffer.Count; i++)
        {
            int id = removeBuffer[i];
            if(views[id] != null) Destroy(views[id].gameObject);
            views.Remove(id);
        }
    }

    public void ClearDeadViews()
    {
        ClearDeadFrom(views);
        ClearDeadFrom(enemyViews);
    }

    private void ClearDeadFrom(Dictionary<int, BattleUnitView> dict)
    {
        removeBuffer.Clear();
        foreach(var pair in dict)
            if (pair.Value == null || pair.Value.Unit == null || !pair.Value.Unit.IsAlive)
                removeBuffer.Add(pair.Key);

        foreach (int id in removeBuffer)
        {
            if(dict[id] != null) Destroy(dict[id].gameObject);
            dict.Remove(id);
        }
    }
    
    #endregion

    #region 적 보드 배치

    public void ShowEnemyBoard(BoardModel enemy)
    {
        enemyBoard = enemy;

        for (int i = 0; i < enemyAnchors.Length && i < enemy.BattleCapacity; i++)
        {
            BattleUnit unit = enemy.Get(new SlotIndex(EZone.Battle, i));
            if (unit == null) continue;

            var view = Instantiate(unitViewPrefab, unitRoot);
            view.Bind(unit, new SlotIndex(EZone.Battle, i));
            view.transform.position = enemyAnchors[i].position;
            view.SetBodyScale(battleScale);
            view.SetFlip(true);
            view.gameObject.SetActive(enemyVisible);
            
            enemyViews.Add(unit.RuntimeId, view);
        }
    }

    public void ClearEnemyBoard()
    {
        Deselect();
        
        foreach(var view in enemyViews.Values)
            if(view != null) Destroy(view.gameObject);
     
        enemyViews.Clear();
        enemyBoard = null;
    }

     
    /// <summary>
    /// 적 보드를 숨기거나 보여줍니다.
    /// 뷰를 지우지 않고 끄기만 하므로 다시 보여줄 때 새로 만들 필요가 없습니다.
    /// 숨길 때 선택 중인 유닛이 적이면 상세 UI가 남지 않도록 선택을 해제합니다.
    /// </summary>
    public void SetEnemyBoardVisible(bool visible)
    {
        enemyVisible = visible;
        
        if(!visible && selectedUnit != null && enemyViews.ContainsKey(selectedUnit.RuntimeId))
            Deselect();

        foreach (var view in enemyViews.Values)
            if (view != null) view.gameObject.SetActive(visible);
    }
    
    #endregion

    #region 연출

    [Header("연출 중심")]
    [SerializeField] private Transform teamCenterAnchor;
    [SerializeField] private Transform enemyCenterAnchor;

    public bool TryGetUnitPosition(BattleUnit unit, out Vector3 position)
    {
        position = Vector3.zero;
        if (unit == null) return false;

        if (views.TryGetValue(unit.RuntimeId, out var view) && view != null)
        {
            position = view.transform.position;
            return true;
        }

        if (enemyViews.TryGetValue(unit.RuntimeId, out var enemyView) && enemyView != null)
        {
            position = enemyView.transform.position;
            return true;
        }

        return false;
    }

    public bool TryGetBoardCenter(bool isEnemyBoard, out Vector3 position)
    {
        Transform anchor = isEnemyBoard ? enemyCenterAnchor : teamCenterAnchor;

        if (anchor == null)
        {
            Logger.LogWarning($"[BoardView] {(isEnemyBoard ? "적" : "아군")} 연출 중심 앵커가 비어 있습니다.");
            position = Vector3.zero;
            return false;
        }

        position = anchor.position;
        return true;
    }

    #endregion
    
    private void OnDestroy()
    {
        if (board != null)
        {
            board.OnBoardChanged -= Refresh;
            board.OnMergeHappened -= OnMergeHappened;
        }
        
    }
}
