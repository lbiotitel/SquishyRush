using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class UnitListUI : BaseUI
{
    [Header("ScrollRect")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Transform")]
    [SerializeField] private Transform ownedUnitTr;
    [SerializeField] private Transform notOwnedUnitTr;

    private List<UnitData> unitDatas;
    private GameObject unitListNodeUIPrefab;
    private List<UnitListCellView> unitCellVeiws = new List<UnitListCellView>();

    private RectTransform rectTransform;
    private RectTransform ownedUnitRectTr;
    private RectTransform notOwnedUnitRectTr;

    private UnitService unitService;

    private CancellationToken destroyToken;   

    private bool isCreated = false;
    private bool isCreateFailed = false;
    private bool isDrawing = false;


    public override void Initialize(Transform anchor)
    {
        base.Initialize(anchor);

        destroyToken = this.GetCancellationTokenOnDestroy();

        rectTransform = transform.GetComponent<RectTransform>();
        ownedUnitRectTr = ownedUnitTr.GetComponent<RectTransform>();
        notOwnedUnitRectTr = notOwnedUnitTr.GetComponent<RectTransform>();

        if (!GlobalManagers.Resource.TryGetResource<GameObject>(GlobalDefines.ResourceKey.UI.UNIT_LIST_NODE_CELL_VIEW_PREFAB,
                out GameObject prefab))
        {
            Logger.LogError($"{GlobalDefines.ResourceKey.UI.UNIT_LIST_NODE_CELL_VIEW_PREFAB}에 해당하는 프리팹이 없습니다.");
            isCreateFailed = true;   
            return;
        }

        unitListNodeUIPrefab = prefab;
        unitService = GlobalManagers.Service.GetService<UnitService>();

        BindEvents();
        BindData();

        CreateUnitCellViewAsync(destroyToken).Forget();
    }


    private void BindEvents()
    {
        GlobalManagers.Event.AddEvent<OnUnitUpgradeEventData>(OnUnitUpgradeEventHandler);
    }


    /// <summary>
    /// 데이터를 가지고와서 정렬 후 캐싱합니다.
    /// </summary>
    private void BindData()
    {
        var data = GlobalManagers.Resource.GetResources<UnitData>();
        if (data == null)
        {
            Logger.LogError("유닛 데이터를 가져오지 못했습니다.");
            unitDatas = new List<UnitData>();
            return;
        }

        unitDatas = data
            .OrderBy(d => d.UnitRarity)
            .ThenBy(d => d.ElementType)
            .ToList();
    }


    private void OnDestroy()
    {
        GlobalManagers.Event?.RemoveEvent<OnUnitUpgradeEventData>(OnUnitUpgradeEventHandler);
    }


    /// <summary>
    /// 유닛이 업그레이드 됬을 때 특정 유닛 만 다시 그립니다.
    /// </summary>
    private void OnUnitUpgradeEventHandler(OnUnitUpgradeEventData e)
    {
        if (!isCreated) return;

        foreach (var cellView in unitCellVeiws)
        {
            if (cellView.CurrentUnitData.UnitId == e.UnitID)
            {
                cellView.DrawUnitInfo();
                return;
            }
        }
    }


    /// <summary>
    /// 유닛 cellview들을 프레임을 끊어서 생성해서 풀링 합니다.
    /// 성공이든 실패든 isCreated 또는 isCreateFailed 중 하나는 반드시 세팅됩니다.
    /// </summary>
    private async UniTask CreateUnitCellViewAsync(CancellationToken token)
    {
        const int itemsPerFrame = 4;

        try
        {
            for (int i = 0; i < unitDatas.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                GameObject go = Instantiate(unitListNodeUIPrefab, ownedUnitTr);

                if (go.TryGetComponent<UnitListCellView>(out var unitListCellView))
                {
                    unitCellVeiws.Add(unitListCellView);
                }
                else
                {
                    Logger.LogError("유닛 셀 프리팹에 UnitListCellView가 없습니다.");
                    isCreateFailed = true;
                    return;
                }

                if ((i + 1) % itemsPerFrame == 0)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);  
                }
            }

            isCreated = true;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Logger.LogError($"유닛 셀 생성 중 예외 발생: {e}");
            isCreateFailed = true;
        }
    }


    public override void OpenUI()
    {
        base.OpenUI();
        DrawUnitListAsync(destroyToken).Forget();
    }


    /// <summary>
    /// 유닛 리스트들을 비동기로 그립니다.
    /// </summary>
    private async UniTask DrawUnitListAsync(CancellationToken token)
    {
        if (isCreateFailed) return;
        if (isDrawing) return;   
        isDrawing = true;

        try
        {
            if (!isCreated)
            {
                await Utils.WaitForAwaitMethodWithOverlayUI(
                    UniTask.WaitUntil(() => isCreated || isCreateFailed, cancellationToken: token));
                if (isCreateFailed) return;
            }

            DrawUnitList();
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Logger.LogError($"유닛 리스트 그리기 중 예외 발생: {e}");
        }
        finally
        {
            isDrawing = false;
        }
    }


    private void DrawUnitList()
    {
        int count = Mathf.Min(unitDatas.Count, unitCellVeiws.Count);

        for (int i = 0; i < count; i++)
        {
            UnitData unitData = unitDatas[i];
            bool isOwned = unitService.IsOwned(unitData.UnitId);

            Transform targetParent = isOwned ? ownedUnitTr : notOwnedUnitTr;

            unitCellVeiws[i].Initialize(unitData);

            if (unitCellVeiws[i].transform.parent != targetParent)
            {
                unitCellVeiws[i].transform.SetParent(targetParent);
            }

            unitCellVeiws[i].transform.SetAsLastSibling();
            unitCellVeiws[i].OpenUI();
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(ownedUnitRectTr);
        LayoutRebuilder.ForceRebuildLayoutImmediate(notOwnedUnitRectTr);

        scrollRect.verticalNormalizedPosition = 1f;
    }
}