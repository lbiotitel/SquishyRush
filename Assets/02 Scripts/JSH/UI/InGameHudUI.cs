using System;
using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 인게임 상단 HUD입니다. 스테이지명 + 라운드("Forest 5"), 라이프, 라운드 트랙을 표시합니다.
/// 스테이지 정보는 StageManager가 아니라 데이터(InGameStageDataContainer)만 받습니다.
/// 런 도중 스테이지가 바뀌지 않으니 초기화 때 한 번 읽으면 충분합니다.
/// </summary>
public class InGameHudUI : MonoBehaviour
{
    [Serializable]
    private struct LifeSlot
    {
        public RectTransform Bg;
        public GameObject Icon;
    }

    [Serializable]
    private struct StageBadge
    {
        public string StageName;
        public Sprite Sprite;
    }
    
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private LifeSlot[] lifes;
    [SerializeField] private RoundTrackView roundTrack;

    [Header("맵 아이콘")]
    [SerializeField] private Image stageBadgeImage;
    [SerializeField] private StageBadge[] stageBadges;
    [SerializeField] private Sprite defaultStageBadge;

    [Header("라운드 글자")]
    [SerializeField] private string perkShopText = "Shop";
    [SerializeField] private Color totalColor = new Color32(0x7A, 0x66, 0x56, 0xFF);
    [SerializeField, Range(30, 100)] private int totalSizePercent = 70;
    
    [Header("라이프 감소 연출")]
    [SerializeField] private float lifeShakeDuration = 0.4f;
    [SerializeField] private float lifeShakeStrength = 15f;
    [SerializeField] private int lifeShakeVibrato = 6;
    [SerializeField] private float lifeVanishDuration = 0.25f;
    
    private InGameStageDataContainer stage;
    private RunContext ctx;

    // 라운드가 바뀔 때마다 색 변환을 다시 하지 않도록 한 번만 만들어 둡니다
    private string roundFormat = "{0}";
    private string totalSuffix = string.Empty;

    private int shownLife = -1;
    private Sequence[] lifeTweens;

    public void Init(RunContext ctx, InGameStageDataContainer stage)
    {
        this.ctx = ctx;
        this.stage = stage;
        
        lifeTweens = new Sequence[lifes.Length];
        shownLife = -1;

        roundFormat = Localization.GetLocaliationText(Localization.LocalizationTable.UI,
            LocaliztionKeyCode.UI_ROUND_TEXT);
        if (string.IsNullOrEmpty(roundFormat)) roundFormat = "{0}";
        totalSuffix = BuildTotalSuffix(stage);
        ApplyStageBadge(stage);

        ctx.OnUserLifeChanged -= RefreshLife;
        ctx.OnUserLifeChanged += RefreshLife;

        ctx.OnRoundChanged -= RefreshRound;
        ctx.OnRoundChanged += RefreshRound;
        
        if(roundTrack != null) roundTrack.Init(ctx, stage);
        
        Refresh();
    }

    private string BuildTotalSuffix(InGameStageDataContainer stage)
    {
        if(stage == null) return string.Empty;

        string hex = ColorUtility.ToHtmlStringRGB(totalColor);
        return $" <size={totalSizePercent}%><color=#{hex}>/ {stage.TotalDisplayRound}</color></size>";
    }

    private void ApplyStageBadge(InGameStageDataContainer stage)
    {
        if (stageBadgeImage == null) return;

        Sprite sprite = defaultStageBadge;
        if (stage != null && stageBadges != null)
        {
            for (int i = 0; i < stageBadges.Length; i++)
            {
                if (stageBadges[i].StageName != stage.StageName) continue;
                sprite = stageBadges[i].Sprite;
                break;
            }
        }

        stageBadgeImage.sprite = sprite;
        stageBadgeImage.enabled = sprite != null;
    }

    public void Refresh()
    {
        RefreshLife(ctx.UserLife);
        RefreshRound();
    }

    /// <summary>
    /// 라이프가 줄면 잃은 칸만 흔들고 사라지게 하고, 처음 표시하거나 늘어나면 연출 없이 바로 맞춥니다.
    /// </summary>
    private void RefreshLife(int life)
    {
        if (shownLife < 0 || life >= shownLife)
        {
            for (int i = 0; i < lifes.Length; i++)
                ResetLifeIcon(i, i < life);
            
            shownLife = life;
            return;
        }

        for (int i = life; i < shownLife && i < lifes.Length; i++)
            PlayLoseLife(i);

        shownLife = life;
    }

    /// <summary>
    /// 좌우로 흔든 뒤 작아지며 사라집니다. 끝나면 꺼두고 위치·크기를 원래대로 돌려놓아
    /// 나중에 라이프가 회복되면 그대로 다시 켤 수 있게 합니다.
    /// </summary>
    private void PlayLoseLife(int index)
    {
        lifeTweens[index]?.Kill(complete: true);

        LifeSlot slot = lifes[index];
        RectTransform iconRt = (RectTransform)slot.Icon.transform;

        Vector2 basePos = iconRt.anchoredPosition;
        
        slot.Icon.SetActive(true);
        iconRt.localScale = Vector3.one;

        lifeTweens[index] = DOTween.Sequence()
            .Append(CreateSideShake(slot.Bg, basePos))
            .Append(iconRt.DOScale(0f, lifeVanishDuration).SetEase(Ease.InBack))
                .OnComplete(() =>
                {
                    slot.Icon.SetActive(false);
                    slot.Bg.anchoredPosition = basePos;
                    iconRt.localScale = Vector3.one;
                })
                .SetUpdate(true)
                .SetLink(slot.Bg.gameObject);
    }

    private Sequence CreateSideShake(RectTransform target, Vector2 basePos)
    {
        Sequence seq = DOTween.Sequence();

        int count = Mathf.Max(1, lifeShakeVibrato);
        float step = lifeShakeDuration / (count + 1);

        for (int i = 0; i < count; i++)
        {
            float dir = i % 2 == 0 ? -1f : 1f;
            float decay = 1f - (float)i / count;
            float x = basePos.x + dir * lifeShakeStrength * decay;

            seq.Append(target.DOAnchorPosX(x, step).SetEase(Ease.InOutSine));
        }

        seq.Append(target.DOAnchorPosX(basePos.x, step).SetEase(Ease.OutSine));
        return seq;
    }

    private void ResetLifeIcon(int index, bool active)
    {
        lifeTweens[index]?.Kill(complete: true);
        lifeTweens[index] = null;
        lifes[index].Icon.SetActive(active);
    }

    private void RefreshRound()
    {
        if (roundText == null) return;

        int round = ctx.RoundIdx;

        if (stage != null && stage.IsPerkShopRound(round))
        {
            roundText.text = perkShopText;
            return;
        }

        int display = stage != null ? stage.GetDisplayRound(round) : round;
        roundText.text = string.Format(roundFormat, display) + totalSuffix;
    }
    
    private void OnDestroy()
    {
        if (lifeTweens != null)
            foreach (Sequence s in lifeTweens) s?.Kill();
        
        if (ctx != null)
        {
            ctx.OnUserLifeChanged -= RefreshLife;
            ctx.OnRoundChanged -= RefreshRound;
        }
    }
}
