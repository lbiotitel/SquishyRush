using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIIdleMotion : MonoBehaviour
{
    public enum Mode {Twinkle, Bob, Sparkle }

    [SerializeField] private Mode mode = Mode.Twinkle;
    [SerializeField] private float delay = 0f;

    [Header("별")] 
    [SerializeField] private float twinkleScale = 1.15f;
    [SerializeField] private float twinkleAngle = 8f;
    [SerializeField] private float twinkleDuration = 0.9f;
    
    [Header("아이콘")]
    [SerializeField] private float bobHeight = 1.4f;
    [SerializeField] private float bobDuration = 1.1f;

    [Header("반짝이")] 
    [SerializeField] private float sparkleMinAlpha = 0.25f;
    [SerializeField] private float sparkleMinScale = 0.6f;
    [SerializeField] private float sparkleDuration = 0.8f;

    private RectTransform rt;
    private Graphic graphic;
    private float baseAlpha = 1f;
    private Vector2 basePos;
    private Vector3 baseScale;
    private Quaternion baseRot;
    private Sequence seq;

    private void Awake()
    {
        rt = (RectTransform)transform;
        basePos = rt.anchoredPosition;
        baseScale = rt.localScale;
        baseRot = rt.localRotation;
        graphic = GetComponent<Graphic>();
        if (graphic != null) baseAlpha = graphic.color.a;
    }

    private void OnEnable()
    {
        seq?.Kill();
        ResetPose();

        seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
            .SetDelay(delay).SetLoops(-1, LoopType.Yoyo);

        if (mode == Mode.Twinkle)
        {
            seq.Append(rt.DOScale(baseScale * twinkleScale, twinkleDuration).SetEase(Ease.InOutSine));
            seq.Join(rt.DOLocalRotate(new Vector3(0, 0, twinkleAngle), twinkleDuration).SetEase(Ease.InOutSine));
        }
        else if (mode == Mode.Bob)
        {
            seq.Append(rt.DOAnchorPosY(basePos.y + bobHeight, bobDuration).SetEase(Ease.InOutSine));
        }
        else
        {
            rt.localScale = baseScale * sparkleMinScale;
            SetAlpha(baseAlpha * sparkleMinAlpha);
            seq.Append(rt.DOScale(baseScale, sparkleDuration).SetEase(Ease.InOutSine));
            if (graphic != null)
                seq.Join(graphic.DOFade(baseAlpha, sparkleDuration).SetEase(Ease.InOutSine));
        }
    }

    private void OnDisable()
    {
        seq?.Kill();
        ResetPose();
    }

    private void ResetPose()
    {
        rt.anchoredPosition = basePos;
        rt.localScale = baseScale;
        rt.localRotation = baseRot;
        SetAlpha(baseAlpha);
    }

    private void SetAlpha(float a)
    {
        if (graphic == null) return;
        Color c = graphic.color;
        c.a = a; graphic.color = c;

    }
}
