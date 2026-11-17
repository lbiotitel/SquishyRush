using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 재능을 드래그할 때 손가락을 따라다니는 복사본 이미지
/// </summary>
public class PerkDragGhost : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Color tint = new(0.55f, 0.55f, 0.55f, 0.9f);
    [SerializeField] private float returnDuration = 0.2f;

    private RectTransform rect;
    private RectTransform parentRect;
    private Camera uiCamera;
    private Tween returnTween;
    private bool cached;

    /// <summary>
    /// 필요한 컴포넌트를 한 번만 캐싱합니다.
    /// 고스트는 평소 비활성 상태라 Awake가 첫 Show 전까지 호출되지 않으므로, Awake 대신 지연 초기화를 씁니다.
    /// </summary>
    private void Cache()
    {
        if (cached) return;
        cached = true;

        rect = (RectTransform)transform;
        parentRect = rect.parent as RectTransform;
        uiCamera = GetCanvasCamera(rect);
        image.raycastTarget = false;
    }

    /// <summary>
    /// 고스트를 켜고 스프라이트를 복사한 뒤 어둡게 칠해 손가락 위치에 둡니다.
    /// 되돌아가는 중이던 트윈이 있으면 끊고 새 드래그를 시작합니다.
    /// </summary>
    public void Show(Sprite sprite, Vector2 screenPos)
    {
        Cache();
        returnTween?.Kill();

        image.sprite = sprite;
        image.color = tint;
        gameObject.SetActive(true);
        MoveTo(screenPos);
    }

    /// <summary>
    /// 화면 좌표를 부모 로컬 좌표로 바꿔 고스트를 옮깁니다.
    /// Screen Space - Camera 캔버스라 캔버스 카메라를 넘겨야 좌표가 맞습니다.
    /// </summary>
    public void MoveTo(Vector2 screenPos)
    {
        if (!cached) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, screenPos, uiCamera, out Vector2 local))
            rect.localPosition = local;
    }

    
    /// <summary>
    /// 장착에 실패했을 때 고스트를 원래 셀 위치로 되돌린 뒤 끕니다.
    ///
    /// 셀과 고스트는 서로 다른 캔버스에 있을 수 있습니다. Screen Space - Camera에서는 캔버스마다
    /// Plane Distance가 달라 월드 좌표를 그대로 쓰면 어긋나므로, 셀 위치를 화면 좌표로 바꾼 뒤
    /// 고스트 부모의 로컬 좌표로 다시 변환합니다.
    /// </summary>
    public void ReturnTo(RectTransform target, Action onComplete = null)
    {
        if (!cached || target == null)
        {
            Hide();
            onComplete?.Invoke();
            return;
        }
        
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(
            GetCanvasCamera(target), target.position);
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, screen, uiCamera, out Vector2 local);
        
        returnTween?.Kill();
        returnTween = rect.DOLocalMove(local, returnDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                Hide();
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// 고스트를 즉시 끕니다. (장착 성공, 드래그 취소)
    /// </summary>
    public void Hide()
    {
        returnTween?.Kill();
        returnTween = null;
        gameObject.SetActive(false);
    }

    private void OnDestroy() => returnTween?.Kill();

    /// <summary>
    /// 해당 UI가 속한 루트 캔버스의 카메라를 가져옵니다. Overlay 캔버스면 null이 맞는 값입니다.
    /// </summary>
    private static Camera GetCanvasCamera(Transform t)
    {
        Canvas canvas = t.GetComponentInParent<Canvas>();
        if (canvas == null) return null;

        canvas = canvas.rootCanvas;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }


}
