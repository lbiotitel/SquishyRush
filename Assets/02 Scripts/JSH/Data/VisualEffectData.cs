using UnityEngine;

[CreateAssetMenu(menuName = "Data/VisualEffect")]
public class VisualEffectData : ScriptableObject
{
    [Header("식별")]
    [SerializeField] private string visualEffectId;

    [Header("재생 방식")] 
    [SerializeField] private EMoveType moveType = EMoveType.Impact;

    [Header("리소스")] 

    [SerializeField] private GameObject effectPrefab;

    [Header("사운드")] 
    [SerializeField] private string castSfxKey;

    [Header("수치")] 
    [Tooltip("스폰 지점으로부터의 추가 오프셋")]
    [SerializeField] private Vector2 spawnOffset;
    [Tooltip("타겟 지점으로부터의 추가 오프셋")] 
    [SerializeField] private Vector2 targetOffset;
    
    [Tooltip("월드 유닛/초. 이동하지 않는 타입에서는 쓰이지 않습니다.")]
    [SerializeField] private float travelTime;
    
    [Tooltip("포물선 정점 높이. Bezier일 때만 쓰입니다.")]
    [SerializeField] private float arcHeight;
    
    public string VisualEffectId => visualEffectId;
    public EMoveType MoveType => moveType;
    public GameObject EffectPrefab => effectPrefab;
    public string CastSfxKey => castSfxKey;
    public bool IsPlayable => effectPrefab != null;
    
    public Vector2 SpawnOffset => spawnOffset;
    public Vector2 TargetOffset => targetOffset;
    public float TravelTime => travelTime;
    public float ArcHeight => arcHeight;
    
#if UNITY_EDITOR
    /// <summary>엑셀 임포터 전용. 런타임에서 호출하지 마세요.</summary>
    public void Editor_SetFromImport(
        string visualEffectId, EMoveType moveType,
        GameObject effectPrefab,
        Vector2 spawnOffset, float moveSpeed, float arcHeight)
    {
        this.visualEffectId = visualEffectId;
        this.moveType = moveType;
 
        // 못 찾았을 때 기존 참조를 지우지 않습니다.
        // 손으로 꽂아둔 프리팹이 키 오타 한 번에 날아가면 복구가 번거롭습니다.
        // (StatusEffectData가 아이콘을 다루는 방식과 같습니다)
        if (effectPrefab != null) this.effectPrefab = effectPrefab;
        this.spawnOffset = spawnOffset;
        this.travelTime = moveSpeed;
        this.arcHeight = arcHeight;
    }
#endif
}
