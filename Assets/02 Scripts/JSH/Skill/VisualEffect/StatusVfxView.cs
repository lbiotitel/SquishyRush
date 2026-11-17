using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상태이상이 "걸려 있는 동안" 유닛 위에 켜 두는 연출. 스턴·바인드·동상·감전·실명.
///
/// 틱형(화상·독·출혈·재생)은 '터지는 사건'이라 로직이 OnVfxRequestedEventData로 발행하지만,
/// 이쪽은 '지금 걸려 있다'는 상태의 표시라서 로직이 아무것도 발행하지 않습니다.
/// UnitStatusUI가 아이콘을 맞추는 방식과 똑같이 OnStatusChanged를 듣고,
/// 현재 상태 목록과 비교해서 켤 것은 켜고 끌 것은 끕니다.
///
/// 그래서 부여·중첩·만료·디스펠·사망 ClearAll·부활 ClearDebuffs가 전부 이 한 경로로 처리됩니다.
/// 상태이상이 풀리는 경로가 새로 생겨도 OnChanged만 울리면 여기는 손댈 필요가 없습니다.
///
/// [자식으로 붙이지 않는 이유]
/// BoardView는 유닛 뷰를 Destroy로 지웁니다. 이펙트가 자식으로 붙어 있으면
/// 풀 소속 오브젝트가 뷰와 같이 파괴되어, 풀에 죽은 참조가 남고 다음 Spawn에서 터집니다.
/// 대신 LateUpdate에서 위치만 따라갑니다.
/// 덤으로 SetFlip(스켈레톤 ScaleX), SetBodyScale, 피격 펀치에도 끌려가지 않습니다.
///
/// [프리팹 조건]
/// 이 경로로 쓰는 프리팹은 VfxObject의 forceNonLooping을 꺼야 합니다.
/// 기본값(켜짐)이면 한 번 재생하고 사라집니다. 그런 경우 콘솔에 한 번 경고를 띄웁니다.
/// </summary>

public class StatusVfxView : MonoBehaviour
{
    [Tooltip("이펙트가 따라갈 기준점. 비우면 이 오브젝트입니다.\n" +
             "피격 펀치에 같이 흔들리게 하고 싶으면 스켈레톤 Transform을 넣으세요.")]
    [SerializeField] private Transform anchor;

    [Tooltip("상태가 풀린 뒤 남은 입자가 사라지길 기다리는 최대 시간(초).\n" +
             "넘기면 남은 입자가 있어도 강제로 반납합니다.")]
    [SerializeField] private float fadeOutLimit = 0.5f;

    /// <summary>켜져 있는 이펙트 하나. 상태이상 정의(StatusEffectData) 하나당 최대 하나만 켭니다.</summary>
    private class Entry
    {
        public StatusEffectData Status;
        public VfxObject Vfx;
        public Vector3 Offset;

        // 상태가 풀려서 방출을 멈췄고, 남은 입자가 사라지길 기다리는 중
        public bool Fading;
        public float FadeElapsed;
    }

    private readonly List<Entry> entries = new();

    // 매번 새로 만들지 않고 재사용합니다.
    private readonly List<StatusEffectData> statusBuffer = new();

    private BattleUnit unit;
    private IVfxSpawner spawner;
    private bool dirty;

    private void Awake()
    {
        if (anchor == null) anchor = transform;
    }

    #region 바인딩

    public void Bind(BattleUnit unit)
    {
        UnBind();
        if (unit == null) return;

        this.unit = unit;

        // Pool은 전투마다 새로 만들어질 수 있어서 바인딩할 때마다 다시 잡습니다.
        // 정적으로 들고 있으면 이전 전투의 파괴된 풀을 가리키게 됩니다.
        spawner = InGameManagers.Pool != null ? new PoolingVfxSpawner(InGameManagers.Pool) : null;
        unit.OnStatusChanged += MarkDirty;

        // 이미 상태이상이 걸린 유닛에 붙는 경우(재바인딩, PushSnapshot 이전)를 위해 즉시 한 번 맞춥니다.
        Refresh();
    }

    public void UnBind()
    {
        if (unit != null) unit.OnStatusChanged -= MarkDirty;
        
        // 다른 유닛으로 다시 바인딩되거나 뷰가 지워질 때는 페이드 없이 즉시 반납합니다.
        // 남겨두면 이전 유닛의 스턴이 새 유닛 머리 위에서 사라지는 장면이 나옵니다.
        DespawnAllImmediate();

        unit = null;
        spawner = null;
        dirty = false;
    }

    private void OnEnable()
    {
        // 꺼져 있던 동안 상태가 바뀌었을 수 있으니 다시 맞춥니다.
        if (unit != null) dirty = true;
    }

    private void OnDisable()
    {
        // 씬이 내려가는 중에는 풀도 같이 파괴되는 중이라 반납하면 안 됩니다.
        // 그냥 목록만 비우고 Unity가 알아서 정리하게 둡니다.
        if (!gameObject.scene.isLoaded)
        {
            entries.Clear();
            return;
        }
        
        // 꺼진 뷰는 LateUpdate가 돌지 않아서 이펙트가 허공에 멈춘 채 남습니다. 미리 걷어냅니다.
        DespawnAllImmediate();
    }

    private void OnDestroy() => UnBind();

    private void MarkDirty() => dirty = true;

    #endregion

    #region 동기화

    private void LateUpdate()
    {
        // 한 프레임에 상태가 여러 번 바뀌어도(부여 직후 교체 등) 비교는 한 번만 합니다.
        if (dirty)
        {
            dirty = false;
            Refresh();
        }

        if (entries.Count == 0) return;

        Vector3 basePos = anchor.position;
        float dt = Time.deltaTime;

        // 뒤에서부터 돌아야 지워도 인덱스가 밀리지 않습니다.
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry e = entries[i];

            // 씬 전환 등으로 바깥에서 이미 파괴된 경우
            if (e.Vfx == null)
            {
                entries.RemoveAt(i);
                continue;
            }

            // 알까기로 튕겨 다녀도 따라갑니다. 사라지는 중인 것도 마지막까지 따라갑니다.
            e.Vfx.transform.position = basePos + e.Offset;

            if (e.Fading)
            {
                e.FadeElapsed += dt;

                if (!e.Vfx.IsAlive || e.FadeElapsed >= fadeOutLimit)
                {
                    DeSpawn(e);
                    entries.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// 지금 걸린 상태 목록과 켜진 이펙트를 맞춥니다.
    ///
    /// 키는 StatusEffectData(정의)입니다. 시전자가 달라 인스턴스가 여러 개여도 이펙트는 하나만 켭니다.
    /// 시간 갱신·스택 증가·교체는 같은 정의가 계속 목록에 있으므로 아무 일도 일어나지 않습니다.
    /// 그래서 스턴을 연달아 맞아도 이펙트가 깜빡이지 않습니다.
    /// </summary>
    private void Refresh()
    {
        if (unit == null) return;

        unit.GetActiveStatusData(statusBuffer);

        // 1) 풀린 상태: 방출만 멈추고 페이드 아웃으로 넘깁니다.
        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (e.Fading) continue;
            if (statusBuffer.Contains(e.Status)) continue;
            
            BeginFade(e);
        }

        // 2) 새로 걸린 상태: 켜져 있는 게 없을 때만 띄웁니다.
        //    사라지는 중인 것은 '켜져 있는 것'으로 치지 않습니다.
        //    풀리자마자 다시 걸리면 새로 띄우고, 이전 것은 옆에서 마저 사라집니다.
        for (int i = 0; i < statusBuffer.Count; i++)
        {
            StatusEffectData status = statusBuffer[i];
            if (status.VfxMode != EStatusVfxMode.WhileActive) continue;
            if (HasActiveEntry(status)) continue;
            
            Spawn(status);
        }
    }

    private bool HasActiveEntry(StatusEffectData status)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Status == status && !entries[i].Fading) return true;
        }

        return false;
    }
    
    #endregion

    #region 생성 / 반납

    private void Spawn(StatusEffectData status)
    {
        if (spawner == null) return;
        if (string.IsNullOrEmpty(status.VisualEffectId)) return;

        if (!GlobalManagers.Resource.TryGetResource(status.VisualEffectId, out VisualEffectData data))
        {
            Logger.LogWarning($"[StatusVfx] {status.StatusId}: VisualEffectData '{status.VisualEffectId}'를 찾지 못했습니다.");
            return;
        }

        if (!data.IsPlayable)
        {
            Logger.LogWarning($"[StatusVfx] {status.StatusId}: '{status.VisualEffectId}'에 프리팹이 없습니다.");
            return;
        }

        // 머리 위 / 발밑 같은 위치는 VisualEffectData의 SpawnOffset으로 정합니다.
        // 스턴·실명·동상이 같이 걸릴 수 있으니 상태마다 다르게 주면 겹치지 않습니다.
        Vector3 offset = data.TargetOffset;

        VfxObject vfx = spawner.Spawn(data.EffectPrefab, anchor.position + offset);
        if (vfx == null) return;
        
        entries.Add(new Entry
        {
            Status = status,
            Vfx = vfx,
            Offset = offset,
        });
    }

    /// <summary>
    /// 새 입자만 멈춥니다. 이미 나와 있던 입자는 자기 수명대로 사라집니다.
    /// 뚝 끊기지 않고 스르륵 꺼지는 이유가 이것입니다.
    /// </summary>
    private static void BeginFade(Entry e)
    {
        e.Fading = true;
        e.FadeElapsed = 0f;
        
        if(e.Vfx != null) e.Vfx.StopEmitting();
    }

    private void DespawnAllImmediate()
    {
        for (int i = entries.Count - 1; i >= 0; i--) DeSpawn(entries[i]);
        entries.Clear();
    }

    private void DeSpawn(Entry e)
    {
        if (e.Vfx == null) return;

        // VfxObject.ReturnObject가 InGameManagers.Pool을 직접 부르므로, 풀이 없으면 반납할 곳이 없습니다.
        if (InGameManagers.Pool == null) return;
        
        spawner?.Despawn(e.Vfx);
        e.Vfx = null;
    }
    

    #endregion
}
