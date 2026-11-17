using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VersusUI : MonoBehaviour
{
    [Header("플레이어")]
    [SerializeField] private Image profileImage;
    [SerializeField] private Image tierImage;
    [SerializeField] private TMP_Text userNameText;
    [SerializeField] private TMP_Text rpText;

    [Header("적")]
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private VersusUnitSlot[] slots;
    [SerializeField] private VersusUnitSlot hiddenSlot;
    [SerializeField] private TMP_Text overflowText;

    [Header("연출")]
    [SerializeField] private float popInterval = 0.03f;
    [SerializeField] private float holdAfterLineup = 0.5f;
    
    [SerializeField] private Animator animator;

    private Tween lastPop;
    private float lineupDoneTime = -1f;
    
    public event Action OnCovered;
    public event Action OnAnimationEnd;

    /// <summary>
    /// 첫 프레임(화면이 다 덮인 상태)에서 커버를 채웁니다.
    /// </summary>
    public void ShowCover()
    {
        gameObject.SetActive(true);
        animator.speed = 0f;
    }

    public void Play()
    {
        animator.speed = 1f;
    }
    
    public void CoveredEvent()
    {
        animator.speed = 0f;
        OnCovered?.Invoke();
    }
    
    
    public void PlayVersusSfx()
        =>  GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.VERSUS_SFX);

    public void LineupInEvent()
    {
        lastPop = null;
        lineupDoneTime = -1f;
        float delay = 0f;
        
        foreach (VersusUnitSlot slot in slots)
        {
            if (!slot.gameObject.activeSelf) continue;
            slot.PopIn(delay);
            delay += popInterval;
        }

        if (hiddenSlot.gameObject.activeSelf)
            lastPop = hiddenSlot.PopIn(delay);

        // 마지막 유닛 등장 후 완료 시간 기록
        if (lastPop != null) lastPop.OnComplete(() => lineupDoneTime = Time.unscaledTime);
        else lineupDoneTime = Time.unscaledTime;
    }

    public async UniTask ResumeAsync(CancellationToken ct)
    {
        if (lastPop != null)
            await lastPop.AwaitTween(ct);

        float remain = lineupDoneTime + holdAfterLineup - Time.unscaledTime;
        if (remain > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(remain), DelayType.UnscaledDeltaTime, cancellationToken: ct);

        animator.speed = 1f;
    }
    
    public void AnimationEndEvent()
    {
        OnAnimationEnd?.Invoke();   
    }

    public int SlotCount => slots.Length;

    public void Bind(ProfileService profile, InGameStageDataContainer stage, StageLineup lineup)
    {
        if (profile != null)
        {
            profileImage.sprite = GlobalManagers.Sprite.GetProfileImage(profile.UserProfileImageID);
            tierImage.sprite = GlobalManagers.Sprite.GetTierImage(profile.UserRP);
            userNameText.text = profile.UserName;
            rpText.text = profile.UserRP.ToString();
        }

        //stageNameText.text = stage.StageName;

        for (int i = 0; i < slots.Length; i++)
            slots[i].Set(i < lineup.Visible.Count ? lineup.Visible[i] : null, false);
        
        hiddenSlot.Set(lineup.Hidden, true);
        
        overflowText.gameObject.SetActive(lineup.Overflow > 0);
        overflowText.text = $"+{lineup.Overflow}";
    }
}
