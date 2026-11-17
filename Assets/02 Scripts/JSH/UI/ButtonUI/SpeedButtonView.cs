using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 배속 버튼의 "보이는 부분"만 담당합니다. (클릭·Time.timeScale 처리는 기존 InGameController 그대로)
///  - OFF: 크림색, 가만히
///  - ON : 연두색 + 앞 화살표가 먼저 쭉 → 뒤 화살표가 따라붙는 '꿈틀' 반복, 몸통도 살짝 말랑
/// 연출은 기본적으로 Time.timeScale 영향을 받지 않습니다(배속 켜도 꿈틀 속도 일정).
/// </summary>
public class SpeedButtonView : MonoBehaviour
{
    [Header("파츠")]
    [SerializeField] private RectTransform body;
    [SerializeField] private Image bgImage;
    [SerializeField] private RectTransform triBack;
    [SerializeField] private RectTransform triFront;

    [Header("색")] 
    [SerializeField] private Color offColor = new Color32(0xFF, 0xF4, 0xDC, 0xFF);
    [SerializeField] private Color onColor = new Color32(0xB6, 0xF0, 0xA0, 0xFF);

    [Header("연출")]
    [SerializeField] private float step = 7f;  // 화살표가 앞으로 나가는 거리(px)
    [SerializeField] private Vector3 arrowSquash = new Vector3(1.2f, 0.85f, 1f); // 나갈 때 화살표 모양 (가로로 늘고 세로로 눌림)
    [SerializeField] private float goDuration = 0.12f; // 나가는 시간
    [SerializeField] private float returnDuration = 0.2f;// 돌아오는 시간 (OutBack이라 살짝 튕김)
    [SerializeField] private float followDelay = 0.07f;// 뒤 화살표가 따라붙는 시간차
    [SerializeField] private float restInterval = 0.2f;// 한 번 꿈틀하고 쉬는 시간
    
    [SerializeField] private Vector3 bodySquash = new Vector3(1.05f, 0.95f, 1f); // 꿈틀할 때 몸통 모양
    [SerializeField] private float bodyReturnDuration = 0.25f;
    [SerializeField] private float onPunch = 0.15f; // 켜는 순간 톡 튀는 크기
    [SerializeField] private float onPunchDuration = 0.3f;
    
    [Header("기타")]
    [SerializeField] private float colorDuration = 0.15f;
    [SerializeField] private float stopDuration = 0.15f;
    [SerializeField] private bool ignoreTimeScale = true;

    private Sequence loop;
    private Tween colorTween, punchTween;
    private Vector2 backBase, frontBase;
    private bool isOn;

    private void Awake()
    {
        backBase = triBack.anchoredPosition;
        frontBase = triFront.anchoredPosition;
        if(bgImage != null) bgImage.color = offColor;
    }

    private void OnEnable()
    {
        if (isOn) StartWiggle();
    }

    private void OnDisable()
    {
        loop?.Kill(); 
        colorTween?.Kill();
        punchTween?.Kill();

        ResetPose();
        if(bgImage != null)  bgImage.color = isOn ? onColor : offColor;
    }

    /// <summary>InGameController.ChangeGameSpeed 에서 호출</summary>
    public void SetOn(bool on)
    {
        bool changed = isOn != on;
        isOn = on;

        if (bgImage != null)
        {
            colorTween?.Kill();
            colorTween = bgImage.DOColor(on ? onColor : offColor, colorDuration)
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject);
        }

        if (!isActiveAndEnabled) return;

        if (on)
        {
            if (changed && body != null)
            {
                punchTween?.Complete();
                punchTween = body.DOPunchScale(Vector3.one * onPunch, onPunchDuration, 5, 0.6f)
                    .SetUpdate(ignoreTimeScale)
                    .SetLink(gameObject)
                    .OnComplete(StartWiggle);
            }
            else StartWiggle();
        }
        else
        {
            StopWiggle();
        }

    }


    private void StartWiggle()
    {
        if (!isOn) return;
        loop?.Kill();
        ResetPose();

        Vector3 squash = arrowSquash;
        float go = goDuration, back = returnDuration;

        loop = DOTween.Sequence().SetUpdate(ignoreTimeScale).SetLink(gameObject).SetLoops(-1);
        
        // 앞 화살표: 쭉 나갔다가 탱 돌아옴
        loop.Insert(0f, triFront.DOAnchorPosX(frontBase.x + step, go).SetEase(Ease.OutQuad));
        loop.Insert(0f, triFront.DOScale(squash, go).SetEase(Ease.OutQuad));
        loop.Insert(go, triFront.DOAnchorPosX(frontBase.x, back).SetEase(Ease.OutBack));
        loop.Insert(go, triFront.DOScale(Vector3.one, back).SetEase(Ease.OutBack));
        
        // 뒤 화살표: 살짝 늦게 따라붙음 → 애벌레처럼 꿈틀
        loop.Insert(followDelay, triBack.DOAnchorPosX(backBase.x + step, go).SetEase(Ease.OutQuad));
        loop.Insert(followDelay, triBack.DOScale(squash, go).SetEase(Ease.OutQuad));
        loop.Insert(followDelay + go, triBack.DOAnchorPosX(backBase.x, back).SetEase(Ease.OutBack));
        loop.Insert(followDelay + go, triBack.DOScale(Vector3.one, back).SetEase(Ease.OutBack));

        if (body != null)
        {
            loop.Insert(0f, body.DOScale(bodySquash, go).SetEase(Ease.OutQuad));
            loop.Insert(go, body.DOScale(Vector3.one, bodyReturnDuration).SetEase(Ease.OutBack));
        }

        loop.AppendInterval(restInterval);
    }

    private void StopWiggle()
    {
        loop?.Kill();
        punchTween?.Kill();
        triFront.DOAnchorPos(frontBase, stopDuration).SetEase(Ease.OutQuad).SetUpdate(ignoreTimeScale).SetLink(gameObject);
        triBack.DOAnchorPos(backBase, stopDuration).SetEase(Ease.OutQuad).SetUpdate(ignoreTimeScale).SetLink(gameObject);
        triFront.DOScale(1f, stopDuration).SetUpdate(ignoreTimeScale).SetLink(gameObject);
        triBack.DOScale(1f, stopDuration).SetUpdate(ignoreTimeScale).SetLink(gameObject);
        if (body != null) body.DOScale(1f, stopDuration).SetUpdate(ignoreTimeScale).SetLink(gameObject);
    }

    private void ResetPose()
    {
        triFront.DOKill(); triBack.DOKill();
        triFront.anchoredPosition = frontBase; triFront.localScale = Vector3.one;
        triBack.anchoredPosition = backBase; triBack.localScale = Vector3.one;
        if (body != null) { body.DOKill(); body.localScale = Vector3.one; }
    }
}
