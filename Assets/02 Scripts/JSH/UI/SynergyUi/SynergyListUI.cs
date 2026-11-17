using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아군 시너지 목록. 스크롤로 visibleCount개까지 보이고 나머지는 내려서 봄.
/// 정렬: 켜진 것 → 높은 단계 → 인원 많은 순 → ID순.
/// SynergySystem.OnRecounted를 구독해 배치가 바뀔 때마다 갱신.
/// </summary>
public class SynergyListUI : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private SynergyCellView cellPrefab;

    [Header("크기")] 
    [SerializeField] private int visibleCount = 5;
    [SerializeField] private float cellHeight = 50f;
    [SerializeField] private float spacing = 4f;

    private SynergySystem synergy;
    private Camera uiCamera;

    private readonly List<SynergyStatus> statusBuffer = new();
    private readonly List<SynergyCellView> cells = new();

    // 정렬 비교 함수를 static 델리게이트로 캐싱 → Sort 호출마다 할당 없음
    private static readonly Comparison<SynergyStatus> SortOrder = Compare;

    public void Init(SynergySystem synergy)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        
        this.synergy = synergy;

        synergy.OnRecounted -= Refresh;
        synergy.OnRecounted += Refresh;

        ApplyLayout();
        Refresh();
    }

    private void ApplyLayout()
    {
        var layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout != null) layout.spacing = spacing;

        float height = visibleCount * cellHeight + (visibleCount - 1) * spacing;
        var rt = (RectTransform)scrollRect.transform;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private void Refresh()
    {
        if (synergy == null) return;
        
        synergy.GetStatus(statusBuffer);
        statusBuffer.Sort(SortOrder);

        while (cells.Count < statusBuffer.Count)
        {
            var cell = Instantiate(cellPrefab, content);
            ((RectTransform)cell.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cellHeight);
            cell.Init(uiCamera, scrollRect);
            cells.Add(cell);
        }

        for (int i = 0; i < cells.Count; i++)
        {
            bool used = i < statusBuffer.Count;
            cells[i].gameObject.SetActive(used);
            if(used) cells[i].Set(statusBuffer[i]);
        }
        
        // 5개 이하면 스크롤 자체를 막고 맨 위로 (넘칠 때만 스크롤 가능)
        bool overflow = statusBuffer.Count > visibleCount;
        scrollRect.vertical = overflow;
        if (!overflow) scrollRect.verticalNormalizedPosition = 1f;
    }

    private static int Compare(SynergyStatus a, SynergyStatus b)
    {
        if (a.IsActive != b.IsActive) return a.IsActive ? -1 : 1; // 켜진 것 먼저

        int byTier = b.ActiveTier.CompareTo(a.ActiveTier); //높은 단계 먼저
        if (byTier != 0) return byTier;

        int byCount = b.Count.CompareTo(a.Count); //인원 많은 순
        if (byCount != 0) return byCount;

        // 여기까지 같으면 ID순으로 고정. List.Sort는 순서가 불안정해서
        // 이게 없으면 배치할 때마다 같은 조건의 줄끼리 위치가 뒤바뀌어 깜빡여 보임
        return string.CompareOrdinal(a.Tiers[0].SynergyId, b.Tiers[0].SynergyId);
    }

    private void OnDestroy()
    {
        if(synergy != null) synergy.OnRecounted -= Refresh;
    }
}
