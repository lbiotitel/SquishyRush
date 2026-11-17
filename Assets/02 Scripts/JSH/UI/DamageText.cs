using DG.Tweening;
using TMPro;
using UnityEngine;



/// <summary>
/// 데미지/회복/Miss 텍스트 한 개.
///
/// 스스로 날아가지 않습니다. 위치는 유닛마다 하나씩 있는 DamageTextStack이 정합니다.
///   - 새 텍스트는 스택 맨 아래(기준 위치)에 뜨고, 먼저 뜬 텍스트들을 한 칸씩 위로 밀어 올립니다.
///   - 수명이 끝나면 제자리에서 사라집니다. 위로 날아가면 윗줄 적의 텍스트 영역을 침범하기 때문입니다.
///   - 스택 최대 높이를 넘어간 오래된 텍스트는 Eject()로 빠르게 사라집니다.
///
/// 풀링 오브젝트라 같은 인스턴스가 다른 유닛에게 재사용될 수 있습니다.
/// 그래서 꺼낼 때마다 SpawnId를 올리고, 스택은 자기가 넣은 SpawnId와 같을 때만 이 텍스트를 건드립니다.
/// </summary>
public class DamageText : MonoBehaviour, IPoolingObject
{
    public GameObject OriginPrefab { get; set; }

    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject criObject;
    [SerializeField] private SpriteRenderer criRenderer;

    [Header("연출")]
    [Tooltip("뜨자마자 이 배율로 커졌다가 원래 크기로 돌아옵니다. 방금 뜬 숫자를 구분하는 용도입니다.")]
    [SerializeField] private float popScale = 1.35f;
    [SerializeField] private float popDuration = 0.1f;

    [Tooltip("다 뜬 뒤 제자리에 머무는 시간 (초)")]
    [SerializeField] private float lifeTime = 1f;
    [Tooltip("수명이 끝난 뒤 사라지는 시간 (초)")]
    [SerializeField] private float fadeDuration = 0.25f;
    [Tooltip("스택 최대 높이를 넘어 밀려난 텍스트가 사라지는 시간 (초). 짧을수록 위쪽이 빨리 정리됩니다.")]
    [SerializeField] private float ejectDuration = 0.12f;

    [Header("크리 배경")] 
    [Tooltip("숫자 박스의 왼쪽 위 모서리에서 Cri 위치까지의 거리 (Label 로컬 단위).\n" +
             "직접 입력하기보다 ⋮ > '크리 위치 저장'으로 현재 배치를 그대로 기록하세요.")]
    [SerializeField] private Vector2 criAnchorOffset;
    
    private const string SORTING_LAYER = "UI_World";
    private const int SORTING_ORDER = 1000;

    // 최신 텍스트를 앞에 그리기 위한 순번.
    private const int ORDER_RANGE = 5000;
    private static int orderCounter;

    private Renderer labelRenderer;
    
    public int SpawnId { get; private set; }
    
    public bool IsEjecting { get; private set; }

    private Vector3 basePos;

    private Vector3 prefabScale = Vector3.one;
    
    private Tween moveTween;
    private Tween fadeTween;
    private Tween criFadeTween;
    
    private void Awake()
    {
        prefabScale = transform.localScale;
        
        if (label == null) label = GetComponent<TMP_Text>();
        labelRenderer = label.GetComponent<Renderer>();

        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++) 
            renderers[i].sortingLayerName = SORTING_LAYER;
    }

    #region 표시

    public void ShowNumber(float amount, Color color, float scale, bool isCri = false)
    {
        Begin(color, scale, isCri);
        label.SetText("{0:0}", amount);
        
        if(isCri) AttachCriToNumber();
    }

    private void AttachCriToNumber()
    {
        label.ForceMeshUpdate();
        Bounds b = label.textBounds;

        Vector3 local = new Vector3(b.min.x + criAnchorOffset.x, b.max.y + criAnchorOffset.y, 0f);
        Vector3 world = label.transform.TransformPoint(local);

        Transform cri = criObject.transform;
        cri.position = new Vector3(world.x, world.y, cri.position.z);
    }
    
    public void ShowMessage(string text, Color color, float scale)
    {
        Begin(color, scale, false);
        label.SetText(text);
    }


    public void Begin(Color color, float scale, bool isCri)
    {
        ResetObject();

        SpawnId++;
        basePos = transform.position;

        ApplyNewestOrder();
        
        criObject.SetActive(isCri);
        label.color = color;

        // 팝: 살짝 크게 시작해서 원래 크기로
        transform.localScale = prefabScale * (scale * popScale);
        transform.DOScale(scale, popDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        // 수명이 끝나면 제자리에서 사라지고 풀로 돌아갑니다.
        fadeTween = label.DOFade(0f, fadeDuration)
            .SetDelay(lifeTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(ReturnObject);

        if(isCri)
             criFadeTween = criRenderer.DOFade(0f, fadeDuration)
            .SetDelay(lifeTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    private void ApplyNewestOrder()
    {
        int order = SORTING_ORDER + (orderCounter % ORDER_RANGE) * 2;
        orderCounter++;

        criRenderer.sortingOrder = order;
        labelRenderer.sortingOrder = order + 1;
    }

    #endregion
    

    #region 스택 배치

    /// <summary>
    /// 스택 기준 위치에서 offsetY만큼 위로 옮깁니다.
    /// duration이 0이면 즉시, 아니면 미끄러지듯 이동합니다.
    /// </summary>
    public void SetStackOffset(float offsetY, float duration)
    {
        moveTween?.Kill();

        Vector3 target = basePos + Vector3.up * offsetY;
        if (duration <= 0f)
        {
            transform.position = target;
            return;
        }

        moveTween = transform.DOMove(target, duration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    /// <summary>
    /// 스택 최대 높이를 넘어 밀려났을 때 부릅니다. 남은 수명과 관계없이 빠르게 사라집니다.
    /// </summary>
    public void Eject()
    {
        if (IsEjecting) return;
        IsEjecting = true;
        
        fadeTween?.Kill();
        criFadeTween?.Kill();

        fadeTween = label.DOFade(0f, ejectDuration)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(ReturnObject);

        if (criObject.activeSelf)
            criFadeTween = criRenderer.DOFade(0f, ejectDuration)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

    }
    
    #endregion

    public void ResetObject()
    {
        transform.DOKill();
        label.DOKill();
        criRenderer.DOKill();
        moveTween = null;
        fadeTween = null;
        criFadeTween = null;

        IsEjecting = false;
        
        label.color = Color.white;
        label.alpha = 1f;
        transform.localScale = Vector3.one;
        
        Color c = criRenderer.color;
        c.a = 1f;
        criRenderer.color = c;
        
        criObject.SetActive(false);
    }
    
    public void ReturnObject()
    {
        ResetObject();
        InGameManagers.Pool.ReturnObject(gameObject);
    }

    private void OnDestroy()
    {
        transform.DOKill();
        label.DOKill();
        if (criRenderer != null) criRenderer.DOKill();
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// 프리팹에서 ⋮ 메뉴로 실행. 지금 Label 텍스트와 Cri 위치를 보고 criAnchorOffset을 계산해 저장합니다.
    ///
    /// 쓰는 법
    ///  1. 크리 배경이 숫자에 보기 좋게 붙어 있는 상태로 둡니다. (정렬이 왼쪽이든 가운데든 상관없음)
    ///  2. 이 메뉴를 실행해 오프셋을 저장합니다.
    ///  3. 그다음 Label을 가운데 정렬 / Pos X 0으로 바꿔도, 런타임에 숫자 왼쪽 위에 같은 모양으로 붙습니다.
    /// </summary>
    [ContextMenu("크리 위치 저장 (현재 배치 기준)")]
    private void CaptureCriOffset()
    {
        if (label == null) label = GetComponent<TMP_Text>();
 
        label.ForceMeshUpdate();
        Bounds b = label.textBounds;
 
        Vector3 criLocal = label.transform.InverseTransformPoint(criObject.transform.position);
 
        UnityEditor.Undo.RecordObject(this, "Capture Cri Offset");
        criAnchorOffset = new Vector2(criLocal.x - b.min.x, criLocal.y - b.max.y);
        UnityEditor.EditorUtility.SetDirty(this);
 
        Logger.Log($"[DamageText] 크리 오프셋 저장 = {criAnchorOffset} (텍스트 '{label.text}' 기준)");
    }
    /// <summary>
    /// 프리팹에서 ⋮ 메뉴로 실행. 숫자 한 줄의 실제 월드 높이를 콘솔에 찍습니다.
    /// 이 값을 BattleUnitView > Text Stack > Text Height에 넣으세요.
    /// </summary>
    [ContextMenu("텍스트 높이 측정 (숫자 기준)")]
    private void MeasureTextHeight()
    {
        if (label == null) label = GetComponent<TMP_Text>();
 
        string prev = label.text;
        label.SetText("0123456789");
        label.ForceMeshUpdate();
 
        // textBounds는 Label 자신의 로컬 좌표라서 Label 트랜스폼의 월드 스케일을 곱합니다.
        // 런타임에도 프리팹 루트 스케일을 유지하므로(prefabScale), 프리팹에서 잰 값이 곧 실제 크기입니다.
        // 표시 배율 1(SCALE_NORMAL) 기준입니다.
        float localHeight = label.textBounds.size.y;
        float labelScale = label.transform.lossyScale.y;
        float height = localHeight * labelScale;
 
        label.SetText(prev);
        label.ForceMeshUpdate();
 
        Logger.Log($"[DamageText] 숫자 높이 = {height:F3} (월드 유닛) " +
                   $"/ Label 로컬 높이 {localHeight:F3} × Label 월드 스케일 {labelScale:F3} " +
                   $"(루트 스케일 {transform.localScale.y:F3})");
    }
#endif
}
