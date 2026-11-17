using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ExpBarAnimSettings
{
    [Tooltip("0->100% 한 바퀴 기준 시간")] 
    public float FullBarDuration = 0.7f;
    public float MinSegmentDuration = 0.15f;
    [Tooltip("레벨업 순간 꽉 찬 바를 보여주는 시간")] 
    public float LevelUpPause = 0.3f;
    [Tooltip("레벨업이 많아도 이 시간 안에 끝나도록 압축")]
    public float MaxTotalDuration = 3f;
    public float PunchScale = 0.35f;
}

/// <summary>
/// 기존 Slider/Text를 받아서 경험치 바 연출만 재생하는 클래스입니다.
/// Slider.maxValue = 필요 경험치, Slider.value = 현재 경험치 를 그대로 넣습니다.
/// </summary>
public class ExpBarAnimator
{
    private readonly Slider slider;
    private readonly TMP_Text levelText;
    private readonly TMP_Text expText;
    private readonly Transform levelBadge;
    private readonly Graphic flashGraphic; // null 허용
    private readonly ExpBarAnimSettings settings;
    private readonly GameObject linkTarget;
    private readonly Vector3 badgeOriginScale;

    private CancellationTokenSource skipCts;

    /// <summary>레벨업 순간마다 호출됩니다. (사운드/파티클 연결용)</summary>
    public event Action<int> OnLevelUp;
    
    public bool IsPlaying { get; private set; }

    public ExpBarAnimator(Slider slider, TMP_Text levelText, TMP_Text expText,
        Transform levelBadge, Graphic flashGraphic, ExpBarAnimSettings settings)
    {
        this.slider = slider;
        this.levelText = levelText;
        this.expText = expText;
        this.levelBadge = levelBadge != null ? levelBadge : levelText.transform;
        this.flashGraphic = flashGraphic;
        this.settings = settings ?? new ExpBarAnimSettings();
        linkTarget = slider.gameObject;
        badgeOriginScale = this.levelBadge.localScale;

        slider.interactable = false;
        slider.wholeNumbers = false; // 소수 값으로 부드럽게 움직이도록
        slider.minValue = 0f;


        if (flashGraphic != null) SetAlpha(flashGraphic, 0f);
    }

    /// <summary>연출 없이 즉시 표시합니다.</summary>
    public void SetImmediate(int level, int exp, int requiredExp)
    {
        SetLevel(level);
        SetBar(exp, requiredExp);
    }
    
    /// <summary>재생 중이면 마지막 상태로 즉시 건너뜁니다.</summary>
    public void RequestSkip()
    {
        if(IsPlaying) skipCts?.Cancel();
    }
    
    public async UniTask PlayAsync(IReadOnlyList<ExpSegment> segments, CancellationToken ct)
    {
        if (segments == null || segments.Count == 0) return;
        
        skipCts?.Dispose();
        skipCts = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, skipCts.Token);

        IsPlaying = true;
        try
        {
            await PlaySegmentsAsync(segments, linked.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // 스킵: 진행 중 트윈은 AwaitTween에서 Kill됨 → 최종 상태로 스냅
            ApplyFinal(segments[segments.Count - 1]);
        }
        finally
        {
            IsPlaying = false;
        }
    }

    private async UniTask PlaySegmentsAsync(IReadOnlyList<ExpSegment> segments, CancellationToken ct)
    {
        float timeScale = CalcTimeScale(segments);

        SetLevel(segments[0].Level);
        
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            bool isLast = i == segments.Count - 1;

            // 구간 시작 상태 (레벨업 직후라면 여기서 0으로 리셋됨)
            SetBar(seg.FromExp, seg.RequiredExp);
            
            if (seg.FillRate > 0f)
            {
                float duration = Mathf.Max(settings.MinSegmentDuration,
                    seg.FillRate * settings.FullBarDuration * timeScale);

                // 중간 구간은 Linear로 쭉, 마지막 구간만 감속해서 멈추는 느낌
                Ease ease = isLast ? Ease.OutCubic : Ease.Linear;

                // 경험치 값 자체를 FromExp → ToExp 로 트윈
                // 매 프레임 바와 숫자 텍스트를 같이 갱신해야 카운트업이 됩니다.
                float exp = seg.FromExp;
                float endExp = seg.ToExp;
                await DOTween.To(() => exp, x =>
                    {
                        exp = x;
                        SetBar(exp, seg.RequiredExp);
                    }, endExp, duration)
                    .SetEase(ease)
                    .SetUpdate(true) // Time.timeScale 영향 안 받게
                    .SetLink(linkTarget)
                    .AwaitTween(ct);
            }

            if (seg.IsLevelUp && !isLast)
            {
                await PlayLevelUpAsnyc(seg.Level + 1, timeScale, ct);
            }
        }
    }

    private async UniTask PlayLevelUpAsnyc(int newLevel, float timeScale, CancellationToken ct)
    {
        OnLevelUp?.Invoke(newLevel);
        SetLevel(newLevel);

        levelBadge.DOKill(true);
        levelBadge.localScale = badgeOriginScale;
        
        const float punchDuration = 0.3f;
        Sequence seq = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(linkTarget)
            .Join(levelBadge.DOPunchScale(Vector3.one * settings.PunchScale, punchDuration, 6, 0.5f));

        if (flashGraphic != null)
        {
            SetAlpha(flashGraphic, 0.8f);
            seq.Join(flashGraphic.DOFade(0f, 0.25f));
        }
        
        // 꽉 찬 상태를 잠깐 보여준 뒤 다음 구간에서 0으로 리셋
        seq.AppendInterval(Mathf.Max(0f, settings.LevelUpPause * timeScale - punchDuration));

        await seq.AwaitTween(ct);
    }

    /// <summary>전체 연출이 MaxTotalDuration을 넘으면 압축 비율을 구합니다.</summary>
    private float CalcTimeScale(IReadOnlyList<ExpSegment> segments)
    {
        float total = 0f;
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            total += seg.FillRate * settings.FullBarDuration;
            if (seg.IsLevelUp && i < segments.Count - 1) total += settings.LevelUpPause;
        }

        return total > settings.MaxTotalDuration ? settings.MaxTotalDuration / total : 1f;
    }

    private void ApplyFinal(ExpSegment last)
    {
        levelBadge.DOKill();
        levelBadge.localScale = badgeOriginScale;
        if (flashGraphic != null)
        {
            flashGraphic.DOKill();
            SetAlpha(flashGraphic, 0f);
        }

        SetLevel(last.Level);
        SetBar(last.ToExp, last.RequiredExp);
    }

    private void SetLevel(int level)
    {
        levelText.SetText("{0:0}", level);
    }
    
    private void SetBar(float exp, int required)
    {
        if (required <= 0)
        {
            // 만렙/차트 없음: 꽉 찬 바
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(1f);
            expText.SetText("MAX");
            return;
        }

        slider.maxValue = required;
        slider.SetValueWithoutNotify(exp);
        // SetText 포맷 오버로드는 매 프레임 string 할당이 없음
        expText.SetText("{0:0}/{1:0}", Mathf.Round(exp), required);
    }

    private static void SetAlpha(Graphic g, float a)
    {
        var c = g.color;
        c.a = a;
        g.color = c;
    }
}
