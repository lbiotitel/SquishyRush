using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PerkShopSlot : MonoBehaviour
{
    [Header("Sprite Renderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Float Animation")]
    [SerializeField] private float floatHeight = 0.15f;
    [SerializeField] private float floatDuration = 1.2f;
    [SerializeField] private float openPopDuration = 0.25f;

    [Header("Text")]
    [SerializeField] private TMP_Text priceText;

    public bool IsSell { get; private set; } = false;
    public int Index { get; private set; }
    public Vector3 IconPosition => spriteRenderer.transform.position;

    private Collider2D clickCollider;
    private Transform animationTransform;              
    private Vector3 originLocalPos;
    private Vector3 originScale;
    private Tween floatTween;
    private Tween popTween;

    
    public void Initialize(int index)
    {
        Index = index;
        clickCollider = GetComponent<Collider2D>();

        animationTransform = spriteRenderer.transform;
        originLocalPos = animationTransform.localPosition;  
        originScale = animationTransform.localScale;

        CloseShopSlot();
    }

    /// <summary>
    /// 상점 슬롯을 열고 애니메이션을 재생합니다.
    /// </summary>
    public void OpenShopSlot(Sprite sprite, int price)
    {
        KillTweens();

        IsSell = false;
        gameObject.SetActive(true);
        spriteRenderer.sprite = sprite;
        priceText.text = $"{price}G";

        animationTransform.localPosition = originLocalPos;
        animationTransform.localScale = Vector3.zero;

        popTween = animationTransform.DOScale(originScale, openPopDuration)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject)
            .OnComplete(StartFloat);
    }

    private void StartFloat()
    {
        floatTween = animationTransform
            .DOLocalMoveY(originLocalPos.y + floatHeight, floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetDelay(Random.Range(0f, 0.4f))
            .SetLink(gameObject);
    }

    /// <summary>
    /// 상점 슬롯을 닫습니다.
    /// </summary>
    public void CloseShopSlot()
    {
        KillTweens();
        animationTransform.localPosition = originLocalPos;   
        animationTransform.localScale = originScale;
        gameObject.SetActive(false);
    }
    
    
    /// <summary>
    /// 받은위치랑 오버랩 되는 지 확인
    /// </summary>
    /// <param name="worldPos"></param>
    /// <returns></returns>
    public bool Contains(Vector2 worldPos)
        => clickCollider != null && clickCollider.OverlapPoint(worldPos);
    
    
    /// <summary>
    /// 판매시 비활성화
    /// </summary>
    public void Sell()
    {
        IsSell = true;
        CloseShopSlot();
    }
    
    /// <summary>
    /// 리셋
    /// </summary>
    public void Reset()
    {
        IsSell = false;
    }
    
    
    /// <summary>
    /// 애니메이션 정지
    /// </summary>
    private void KillTweens()
    {
        floatTween?.Kill();
        popTween?.Kill();
        floatTween = null;
        popTween = null;
    }

    private void OnDestroy() => KillTweens();
}