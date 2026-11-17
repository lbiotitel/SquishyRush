using DG.Tweening;
using Spine;
using Spine.Unity;
using UnityEngine;
using Animation = Spine.Animation;
using RotateMode = DG.Tweening.RotateMode;
using Sequence = DG.Tweening.Sequence;

public class BattleUnitView : MonoBehaviour
{
    [SerializeField] private UnitStatusUI statusUI;
    
    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private SkeletonAnimation skeletonAnimation;


    [SerializeField] private StatusVfxView statusVfx;
    
    [Header("소환 연출")]
    [SerializeField] private ParticleSystem spawnParticle;

    [Header("피격 연출")] 
    [SerializeField] private Color hitColor = Color.white;
    [SerializeField] private float hitDuration = 0.12f;
    [SerializeField] private float punchStrength = 0.08f;
    [SerializeField] private int punchVibrato = 12;
    [SerializeField] private float punchElasticity = 0f;

    [Header("사망 연출")]
    [SerializeField] private Color deathColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private float deathFadeDuration = 0.25f;
    [SerializeField] private float deathHitStop = 0.08f; // 맞고 멈칫하는 시간
    [SerializeField] private float flyDuration = 0.7f;
    [SerializeField] private Vector2 flyOffset = new(9f, 3f);   // x는 진영 뒤쪽으로 부호 자동. 화면 밖까지 가게 조정
    [SerializeField] private float flyHeightJitter = 2f;   // 유닛마다 궤적 살짝 다르게
    [SerializeField] private float flyJumpPower = 4f;  // 포물선 높이
    [SerializeField] private float flySpinTurns = 2.5f;
    [SerializeField] private float flyScaleMul = 1.3f;
    
    [Header("데미지 텍스트")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private float textOffsetY = 1.2f;
    [Tooltip("이 유닛에 뜨는 텍스트를 쌓는 규칙. 칸 간격, 최대 높이 등")]
    [SerializeField] private DamageTextStack textStack = new();
    
    public BattleUnit Unit {get; private set; }
    public SlotIndex Slot { get; private set; }

    private string boundUnitId;
    private Vector3 bodyBasePos;
    
    private Tweener hitTween;
    private Sequence deathSeq;
    private Tween perkPunch;
    private bool isDeadView;
    private bool isFlipped;
    private bool isDragging;

    private Animation idleAnim;
    private Animation attackAnim;

    private MaterialPropertyBlock mpb;

    private const string LAYER_UNIT = "Unit";
    private const string LAYER_DRAG = "Drag";

    private const string ANIM_IDLE = "Idle";
    private const string ANIM_ATTACK = "Attack";
    
    // Spine/Skeleton Fill 셰이더 프로퍼티
    private static readonly int FillColorId = Shader.PropertyToID("_FillColor");
    private static readonly int FillAlphaId = Shader.PropertyToID("_FillPhase");
    
    // 공격 모션 길이 = 공격 간격 × OCCUPANCY, MIN~MAX로 제한
    private const float ANIM_OCCUPANCY = 0.9f;
    
    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        bodyBasePos = skeletonAnimation.transform.localPosition;
    }

    #region 바인딩

    public void Bind(BattleUnit unit, SlotIndex slot)
    {
        if (Unit != null) Unsubscribe();
        
        Unit = unit;
        Slot = slot;
        Unit.OnAttackPerformed += PlayAttack;
        Unit.OnDied += PlayDeath;
        Unit.OnDamaged += PlayHit;
        Unit.OnDamageTaken += OnDamageTaken;
        Unit.OnMissed += OnMissed;
        Unit.OnHealed += OnHealed;
        Unit.OnStarChanged += OnStarChangedHandler;
        Unit.OnPerkChanged += OnPerkChangedEventHandler;

        string unitId = unit.CurrentUnitData.UnitId;
        if (boundUnitId != unitId)
        {
            PlaySpawnParticle();
            boundUnitId = unitId;
            ApplySkeleton(unit.CurrentUnitData);
        }
        
        ResetVisual();
        statusUI.Bind(unit);
        statusVfx.Bind(unit);
        RefreshBarVisibility();
    }

    private void ApplySkeleton(UnitData unitData)
    {
        if(!GlobalManagers.Resource.TryGetResource(unitData.UnitSpine, out SkeletonDataAsset skeletonData))
        {
            Logger.LogWarning($"[BattleUnitView] 애니메이터 없음 : {unitData.UnitSpine}");
            return;
        }

        skeletonAnimation.skeletonDataAsset = skeletonData;
        skeletonAnimation.Initialize(true);

        SkeletonData data = skeletonData.GetSkeletonData(false);
        idleAnim = data.FindAnimation(ANIM_IDLE);
        attackAnim = data.FindAnimation(ANIM_ATTACK);

        ApplyFlip();
    }


    
    private void Unsubscribe()
    {
        Unit.OnAttackPerformed -= PlayAttack;
        Unit.OnDied -= PlayDeath;
        Unit.OnDamaged -= PlayHit;
        Unit.OnDamageTaken -= OnDamageTaken;
        Unit.OnMissed -= OnMissed;
        Unit.OnHealed -= OnHealed;
        Unit.OnStarChanged -= OnStarChangedHandler;
        Unit.OnPerkChanged -= OnPerkChangedEventHandler;

        statusUI.Unbind();
        statusVfx.UnBind();
    }
    
    #endregion

    #region 외형 제어

    private void OnStarChangedHandler(int star)
    {
        PlaySpawnParticle();
        Logger.LogWarning($"{boundUnitId} {Unit.CurrentUnitData.UnitId} {star}");
    }


    private void OnPerkChangedEventHandler()
    {
        perkPunch?.Complete();
        perkPunch = transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 7)
            .SetLink(gameObject);
    }


    public void PlaySpawnParticle()
        => spawnParticle.Play();

    public void SetDragging(bool dragging)
    {
        isDragging = dragging;
        bodyRenderer.sortingLayerName = dragging ? LAYER_DRAG : LAYER_UNIT;
        RefreshBarVisibility();
    }

    private void RefreshBarVisibility()
        => statusUI.SetBattleBarVisible(Slot.Zone == EZone.Battle && !isDragging);

    public void SetBodyScale(float scale) => 
        skeletonAnimation.transform.localScale = Vector3.one * scale;

    public void SetFlip(bool flipX)
    {
        isFlipped = flipX;
        ApplyFlip();
    }

    private void ApplyFlip()
    {
        if (skeletonAnimation.skeleton == null) return;
        skeletonAnimation.Skeleton.ScaleX = isFlipped ? -1f : 1f;
    }
    
    #endregion

    #region 공격

    public void PlayAttack()
    {
        if (isDeadView || attackAnim == null) return;

        float atkSpeed = Unit.EffectiveAtkSpeed;
        float target = GetAttackAnimDuration(atkSpeed);

        Spine.AnimationState state = skeletonAnimation.AnimationState;
        TrackEntry entry = state.SetAnimation(0, attackAnim, false);
        entry.TimeScale = attackAnim.Duration / target;
        entry.MixDuration = 0f;

        if (idleAnim != null) state.AddAnimation(0, idleAnim, true, 0f);
    }

    private static float GetAttackAnimDuration(float atkSpeed)
    {
        // BattleUnit.TickAttack과 같은 식으로 간격 계산
        float interval = 1f / Mathf.Max(0.1f, atkSpeed);
        return (interval * ANIM_OCCUPANCY);
    }

    #endregion

    #region 피격

    private void PlayHit(EDamageSource source)
    {
        if (isDeadView) return;
        if (!source.IsDirect()) return;
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.HIT_SFX,true);

        EnsureHitTween();
        SetFill(1f);
        hitTween.Restart();
    }

    private void EnsureHitTween()
    {
        if (hitTween != null) return;

        hitTween = skeletonAnimation.transform
            .DOPunchPosition(new Vector3(punchStrength, 0f, 0f), hitDuration, punchVibrato, punchElasticity)
            .SetAutoKill(false)
            .SetRecyclable(false)
            .SetLink(gameObject)
            .Pause()
            .OnComplete(RestoreHitVisual);
    }

    private void RestoreHitVisual()
    {
        if (isDeadView) return;
        SetFill(0f);
        skeletonAnimation.transform.localPosition = bodyBasePos;
    }

    // SkeletonAnimation은 매 프레임 sharedMaterials를 재할당하므로
    // 머티리얼 교체 대신 PropertyBlock으로 값만 바꾼다
    private void SetFill(float phase)
    {
        bodyRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(FillColorId, hitColor);
        mpb.SetFloat(FillAlphaId, phase);
        bodyRenderer.SetPropertyBlock(mpb);
    }
    
    #endregion

    #region 사망

    private void PlayDeath()
    {
        if (isDeadView) return;
        isDeadView = true;
        
        GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.DIE_SFX);
        
        hitTween?.Rewind();
        statusUI.SetBattleBarVisible(false);
        statusUI.HideStars();
        bodyRenderer.sortingLayerName = LAYER_DRAG;

        Transform body = skeletonAnimation.transform;
        body.localPosition = bodyBasePos;

        if (idleAnim != null) skeletonAnimation.AnimationState.SetAnimation(0, idleAnim, false);
        skeletonAnimation.Update(0f);
        skeletonAnimation.timeScale = 0f;

        float dir = isFlipped ? 1f : -1f;

        float jitter = Mathf.Repeat(Unit.RuntimeId * 0.618034f, 1f) - 0.5f;

        Vector3 end = bodyBasePos + new Vector3(flyOffset.x * dir, flyOffset.y + jitter * flyHeightJitter, 0f);
        float spin = -dir * 360f * flySpinTurns;
        
        Skeleton skeleton = skeletonAnimation.Skeleton;
        SetFill(1f);

        deathSeq = DOTween.Sequence()
            .AppendInterval(deathHitStop)
            .AppendCallback(() =>
            {
                SetFill(0f);
                GlobalManagers.Audio.PlaySfx(GlobalDefines.ResourceKey.Audio.DEATH_FLY_SFX, true);
            })
            .Append(body.DOLocalJump(end, flyJumpPower, 1, flyDuration).SetEase(Ease.OutQuad))
            .Join(body.DOLocalRotate(new Vector3(0f, 0f, spin), flyDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear))
            .Join(body.DOScale(body.localScale * flyScaleMul, flyDuration))
            .Insert(deathHitStop + flyDuration * 0.6f,
                DOTween.To(() => skeleton.A, a => skeleton.A = a, 0f, flyDuration * 0.4f))
            .Insert(deathHitStop, DOTween.To(() => 0f, t =>
            {
                skeleton.R = Mathf.Lerp(1f, deathColor.r, t);
                skeleton.G = Mathf.Lerp(1f, deathColor.g, t);
                skeleton.B = Mathf.Lerp(1f, deathColor.b, t);
            }, 1f, deathFadeDuration))
            .OnComplete(() => bodyRenderer.enabled = false)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    #endregion

    #region 초기화

    private void ResetVisual()
    {
        isDeadView = false;

        deathSeq?.Kill();
        deathSeq = null;
        hitTween?.Rewind();

        SetFill(0f);
        skeletonAnimation.transform.localPosition = bodyBasePos;
        skeletonAnimation.transform.localRotation = Quaternion.identity;
        
        bodyRenderer.enabled = true;
        bodyRenderer.sortingLayerName = isDragging ? LAYER_DRAG : LAYER_UNIT;

        skeletonAnimation.timeScale = 1f;
        if(skeletonAnimation.Skeleton != null) skeletonAnimation.Skeleton.SetColor(Color.white);
        if (idleAnim != null) skeletonAnimation.AnimationState.SetAnimation(0, idleAnim, true);
        ApplyFlip();
    }

    #endregion

    #region 데미지 텍스트

    // 이 유닛에 뜨는 텍스트(피해, 감전, 회복, Miss, Block)는 전부 textStack 하나에 쌓입니다.
    // 종류가 달라도 같은 스택을 쓰기 때문에 서로 겹쳐 뜨지 않고, 위아래로 정리됩니다.
    
    private Vector3 TextBasePos => transform.position + Vector3.up * textOffsetY;
    
    private void OnDamageTaken(DamageReport r)
    {

        if (r.IsBlocked)
        {
            SpawnMessage("Block", GlobalDefines.DAMAGE_TEXT.SHIELD, GlobalDefines.DAMAGE_TEXT.SCALE_SUB);
            return;
        }

        float shown = r.Amount + r.ShieldAbsorbed;
        
        if (shown > 0f)
        {
            Color c = r.ResolveColor(out float scale);

            if (r.IsCrit) scale = GlobalDefines.DAMAGE_TEXT.SCALE_NORMAL;
            
            SpawnNumber(shown, c, scale, r.IsCrit);
        }
        
        if (r.ShockAmount > 0f)
            SpawnNumber(r.ShockAmount, GlobalDefines.DAMAGE_TEXT.SHOCK,
                GlobalDefines.DAMAGE_TEXT.SCALE_SUB);
        
    }

    private void OnMissed()
    {
        SpawnMessage("Miss", GlobalDefines.DAMAGE_TEXT.SHIELD, GlobalDefines.DAMAGE_TEXT.SCALE_NORMAL);
    }

    private void OnHealed(float amount, StatusEffectData statusSource, bool isCrit)
    {
        Color c = statusSource != null ? statusSource.TextColor : GlobalDefines.DAMAGE_TEXT.HEAL;
        float scale = statusSource != null ? statusSource.TextScale : GlobalDefines.DAMAGE_TEXT.SCALE_NORMAL;

        SpawnNumber(amount, c, scale, isCrit);
    }

    private void SpawnNumber(float amount, Color color, float scale, bool isCrit = false)
    {
        var text = InGameManagers.Pool.GetObject<DamageText>(damageTextPrefab, TextBasePos, Quaternion.identity);
        if (text == null) return;
        
        text.ShowNumber(amount, color, scale, isCrit);
        textStack.Push(text);
    }

    private void SpawnMessage(string msg, Color color, float scale)
    {
        var text = InGameManagers.Pool.GetObject<DamageText>(damageTextPrefab, TextBasePos, Quaternion.identity);
        if (text == null) return;
        
        text.ShowMessage(msg, color, scale);
        textStack.Push(text);
    }

    
#if UNITY_EDITOR
    /// <summary>
    /// 선택하면 씬 뷰에 텍스트 스택 영역을 노란 박스로 그립니다.
    /// 아래 줄 유닛의 박스 윗변이 윗줄 유닛의 박스 아랫변보다 낮아야 서로 안 겹칩니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (textStack == null) return;

        float h = textStack.TextHeight;
        float bottom = textOffsetY - h * 0.5f;
        float top = textOffsetY + textStack.MaxHeight + h * 0.5f;

        Vector3 center = transform.position + Vector3.up * ((bottom + top) * 0.5f);
        Vector3 size = new Vector3(1.6f, top - bottom, 0f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
#endif
    
    #endregion
   
    private void OnDestroy()
    {
        if (Unit != null) Unsubscribe();
        deathSeq?.Kill();
        hitTween?.Kill();
        hitTween = null;
    }
}
