using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 연출 프리팹에 붙이는 컴포넌트. 파티클 수명을 자기가 책임집니다.
///
/// IPoolingObject의 ResetObject / ReturnObject가 "오브젝트가 자기 상태를 스스로 되돌린다"는
/// 설계라, 파티클을 다루는 코드도 바깥이 아니라 여기 있는 게 맞습니다.
/// 재생기는 "틀어" / "끝났니?" 만 묻고 내부 사정은 모릅니다.
///
///  스프라이트 발사체 지원.
///  파티클 = 수명이 있는 것(꼬리, 잔광). IsAlive 판단에 들어감.
///  스프라이트 = 수명이 없는 것(발사체 머리). 재생기가 HideHead로 직접 끔.
///  스프라이트만 있는 프리팹은 발사체(Straight/Bezier) 전용입니다.
///  Impact로 쓰면 IsAlive가 즉시 false라 한 프레임 만에 반납되어 안 보입니다.
/// </summary>
[DisallowMultipleComponent]
public class VfxObject : MonoBehaviour, IPoolingObject
{
    public GameObject OriginPrefab { get; set; }
    public float TailLifetime { get; private set; }
    
    public const float MAX_LIFETIME = 3f;

    [Header("렌더 순서")] 
    [SerializeField] private string sortingLayer = "Unit";
    [SerializeField] private int sortingOrder = 100;
    [SerializeField] private bool overrideSoring = true;

    [Header("수명")] 
    [SerializeField] private bool forceNonLooping = true;

    [Header("방향")]
    [Tooltip("프리팹이 그려진 기본 방향 보정(도).\n" +
             "오른쪽을 보면 0 / 위를 보면 -90 / 왼쪽이면 180 / 아래면 90.\n" +
             "발사체가 반대로 날아가 보이면 여기를 조정하세요.")]
    [SerializeField] private float facingOffset = 0f;
    
    [Header("도착")]
    [Tooltip("체크하면 닿는 순간 파티클을 즉시 지웁니다.\n끄면 방출만 멈추고 남은 입자가 수명대로 사라집니다.")]
    [SerializeField] private bool clearOnArrive = true;
    
    [Tooltip("clearOnArrive가 켜져 있어도 남겨둘 파티클(꼬리, 연기 등). 비워두면 전부 지웁니다.")]
    [SerializeField] private ParticleSystem[] keepOnArrive;
    
    private ParticleSystem[] systems;

    private SpriteRenderer[] heads;
    
    private Vector3 baseScale;
    private Transform[] localChildren;
    private Vector3[] localChildBase;
    
    public float FacingOffset => facingOffset;

    private void Awake()
    {
        baseScale = transform.localScale;
        systems = GetComponentsInChildren<ParticleSystem>(true);
        heads = GetComponentsInChildren<SpriteRenderer>(true);
        
        if(systems.Length == 0 && heads.Length == 0)
            Logger.LogWarning($"[VfxObject] {name} 에 ParticleSystem이 없습니다.");

        ApplySorting();
        ApplyNonLooping();
        CacheTailLifetime();
        CacheLocalChildren();
    }

    /// <summary>
    /// 정렬을 코드에서 강제합니다.
    ///
    /// 프리팹 58개를 손으로 맞추는 것보다 규칙을 한 곳에 두는 편이 안전합니다.
    /// "연출은 항상 유닛 위"라는 규칙이 바뀌면 여기만 고치면 됩니다.
    /// </summary>
    private void ApplySorting()
    {
        if (!overrideSoring) return;

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null) continue;

            var renderer = systems[i].GetComponent<ParticleSystemRenderer>();
            if (renderer == null) continue;
            
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = sortingOrder;
        }

        for (int i = 0; i < heads.Length; i++)
        {
            if (heads[i] == null) continue;

            heads[i].sortingLayerName = sortingLayer;
            heads[i].sortingOrder = sortingOrder;
        }
    }

    /// <summary>
    /// 발사체가 대상에 닿은 순간. ProjectileVfxPlayer가 부릅니다.
    /// 머리(스프라이트)는 끄고, 파티클은 이 프리팹 설정대로 지우거나 남깁니다.
    /// </summary>
    public void Arrive()
    {
        HideHead();

        if (systems == null) return;

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (ps == null) continue;
            
            bool keep = !clearOnArrive || Array.IndexOf(keepOnArrive, ps) >= 0;
            
            // withChildren = false: systems에 자식까지 이미 다 들어 있으므로 하나씩 처리합니다.
            // true로 두면 머리를 지울 때 그 밑에 붙은 꼬리까지 같이 지워집니다.
            ps.Stop(false , keep ? ParticleSystemStopBehavior.StopEmitting : ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    /// <summary>
    /// Looping을 전부 끔. 자식 파티클까지 포함
    /// </summary>
    private void ApplyNonLooping()
    {
        if (!forceNonLooping) return;
        
        for(int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null) continue;

            var main = systems[i].main;
            if (!main.loop) continue;

            main.loop = false;
        }
    }

    /// <summary>
    /// 꼬리 파티클의 최대 수명을 한 번만 계산해 둡니다.
    /// 수명이 범위(0.6~0.7)면 최댓값을 씁니다. 마지막 입자까지 다 사라진 뒤 반납해야 끊기지 않기 때문입니다.
    /// </summary>
    private void CacheTailLifetime()
    {
        TailLifetime = 0f;
        if (keepOnArrive == null) return;

        foreach (ParticleSystem ps in keepOnArrive)
        {
            if (ps == null) continue;
            TailLifetime = Mathf.Max(TailLifetime, GetMaxLifetime(ps.main.startLifetime));
        }
    }

    // 파티클 수명 설정 방식(상수 / 범위 / 커브)마다 최댓값을 읽는 곳이 달라서 분기합니다.
    private static float GetMaxLifetime(ParticleSystem.MinMaxCurve curve)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant: return curve.constant;
            case ParticleSystemCurveMode.TwoConstants: return curve.constantMax;
            default: return curve.curveMultiplier;
        }
    }

    public void ResetObject() => ResetObject(1f);

    /// <summary>
    /// 풀에서 꺼낸 직후 부릅니다.
    ///
    /// Clear가 핵심입니다. SetActive(true)만 하면 이전 재생의 잔여 파티클이 그대로 남아
    /// 두 번째 사용부터 잔상이 붙습니다. 풀링에서 가장 흔한 사고입니다.
    /// </summary>
    public void ResetObject(float scale)
    {
        ApplyScale(scale);
        // 지난 사용에서 HideHead로 꺼둔 머리를 다시 켭니다.
        // 이걸 빼먹으면 두 번째 발사부터 투명한 발사체가 날아갑니다(풀링의 전형적인 사고).
        // systems null 체크보다 앞에 둔 이유: 스프라이트만 있는 프리팹도 복원되어야 하므로.
        SetHeadVisible(true);
        
        if (systems == null) return;

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null) continue;
            
            systems[i].Clear(true);
            systems[i].Play(true);
        }
    }

    /// 파티클 모드는 프리팹 그대로 둡니다.
    /// Local 모드 자식만 부모 배율을 못 받으므로 따로 기억해 두었다가 같이 곱합니다.
    private void CacheLocalChildren()
    {
        var list = new List<Transform>();
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (ps == null || ps.transform == transform) continue;
            if (ps.main.scalingMode != ParticleSystemScalingMode.Local) continue;
            list.Add(ps.transform);
        }
        
        localChildren = list.ToArray();
        localChildBase = new Vector3[localChildren.Length];
        for (int i = 0; i < localChildren.Length; i++)
            localChildBase[i] = localChildren[i].localScale;
    }

    private void ApplyScale(float scale)
    {
        transform.localScale = baseScale * scale;
        for (int i = 0; i < localChildren.Length; i++)
            localChildren[i].localScale = localChildBase[i] * scale;
    }

    /// <summary>
    /// 발사체가 닿은 순간 머리를 끕니다. 파티클(꼬리)은 건드리지 않습니다.
    /// </summary>
    public void HideHead() => SetHeadVisible(false);

    private void SetHeadVisible(bool visible)
    {
        if (heads == null) return;
        
        for(int i = 0; i < heads.Length; i++)
            if (heads[i] != null) heads[i].enabled = visible;
    }

    /// <summary>방출을 멈춥니다. 이미 나간 파티클은 자기 수명대로 사라집니다.</summary>
    public void StopEmitting()
    {
        if (systems == null) return;

        for (int i = 0; i < systems.Length; i++)
        {
            if(systems[i] == null) continue;
            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    //즉시 전부 지움. 잔여 파티클도 남기지 않음.
    public void StopAndClear()
    {
        if (systems == null) return;

        for (int i = 0; i <systems.Length; i++)
        {
            if(systems[i] == null) continue;
            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    /// <summary>
    /// 파티클이 하나라도 살아 있으면 true.
    /// 스프라이트는 수명 개념이 없어서 여기 넣지 않습니다.
    /// 넣으면 스프라이트가 켜져 있는 동안 영원히 "살아 있음"이 되어 LINGER_LIMIT까지 기다립니다.
    /// </summary>
    public bool IsAlive
    {
        get
        {
            if (systems == null) return false;

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null) continue;
                if (systems[i].IsAlive(true)) return true;
            }

            return false;
        }
    }

    public void ReturnObject()
    {
        StopAndClear();
        InGameManagers.Pool.ReturnObject(gameObject);
    }
}
