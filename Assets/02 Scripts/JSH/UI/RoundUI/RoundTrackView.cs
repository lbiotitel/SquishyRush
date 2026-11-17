using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 상단의 라운드 진행 트랙입니다.
/// 현재 라운드 아이콘을 뷰포트 가운데에 두고 앞뒤 라운드를 보여줍니다.
/// </summary>
public class RoundTrackView : MonoBehaviour
{
    [Serializable]
    private struct RoundIcon
    {
        public ERoundType Type;
        public Sprite Sprite;
    }

    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private RoundSlotView slotPrefab;
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private RoundIcon[] icons;

    private readonly List<RoundSlotView> slots = new();
    private RunContext ctx;
    private InGameStageDataContainer stage;
    private Tween slideTween;

    public void Init(RunContext ctx, InGameStageDataContainer stage)
    {
        this.ctx = ctx;
        this.stage = stage;
        
        BuildSlots();
        RefreshStates();
        
        // 레이아웃 그룹은 보통 프레임 끝에 배치하므로, 위치를 바로 읽으려면 지금 강제로 배치합니다.
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        
        MoveTo(ctx.RoundIdx, animate: false);

        ctx.OnRoundChanged -= OnRoundChanged;
        ctx.OnRoundChanged += OnRoundChanged;
    }
    
    // 라운드 수만큼 슬롯을 만들고 아이콘을 지정합니다. 라운드 종류는 런 도중 바뀌지 않으므로 여기서 한 번만 합니다.
    private void BuildSlots()
    {
        // 씬에 미리 놓아둔 자식이나 이전 초기화에서 만든 슬롯을 정리합니다.
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
        slots.Clear();

        if (stage == null) return;

        for (int round = 1; round <= stage.FinalRound; round++)
        {
            RoundSlotView slot = Instantiate(slotPrefab, content);
            slot.name = $"Round_{round}";
            slot.Set(GetIcon(stage.GetRoundType(round)),ERoundSlotState.Upcoming);
            slots.Add(slot);
        }
    }

    private void OnRoundChanged()
    {
        RefreshStates();
        MoveTo(ctx.RoundIdx, animate: true);
    }

    
    // 지난 라운드 / 현재 라운드 / 앞으로 올 라운드 표시를 맞춥니다. 라운드가 바뀔 때 한 번만 불립니다.
    private void RefreshStates()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            int round = i + 1;
            ERoundSlotState state = round < ctx.RoundIdx ? ERoundSlotState.Cleared :
                round == ctx.RoundIdx ? ERoundSlotState.Current :
                ERoundSlotState.Upcoming;
            
            slots[i].Set(GetIcon(stage.GetRoundType(round)),state);
        }
    }

    private void MoveTo(int round, bool animate)
    {
        if (round < 1 || round > slots.Count) return;

        float targetX = GetCenteredContentX(round);
        
        slideTween?.Kill();
        if (!animate)
        {
            Vector3 p = content.localPosition;
            p.x = targetX;
            content.localPosition = p;
            return;
        }

        slideTween = content.DOLocalMoveX(targetX, slideDuration)
            .SetEase(Ease.OutCubic)
            .SetLink(gameObject);
    }

    /// <summary>
    /// round 슬롯의 중심이 뷰포트 중심에 오도록 하는 Content의 localPosition.x를 구합니다.
    /// </summary>
    private float GetCenteredContentX(int round)
    {
        RectTransform slot = (RectTransform)slots[round - 1].transform;

        float slotCenterInContent = slot.localPosition.x + slot.rect.center.x * slot.localScale.x;
        float viewportCenter = viewport.rect.center.x;

        return viewportCenter - slotCenterInContent;
    }

    private Sprite GetIcon(ERoundType type)
    {
        foreach(RoundIcon icon in icons)
            if (icon.Type == type) return icon.Sprite;
        
        Logger.LogWarning($"[RoundTrack] {type} 아이콘이 지정되지 않았습니다.");
        return null;
    }

    private void OnDestroy()
    {
        slideTween?.Kill();
        if (ctx != null) ctx.OnRoundChanged -= OnRoundChanged;
    }
}
