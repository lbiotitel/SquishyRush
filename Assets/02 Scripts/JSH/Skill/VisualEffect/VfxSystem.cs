using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 연출 요청을 받아 재생기에 넘기는 조정자. 씬에 하나 둡니다.
///
/// 책임은 셋뿐입니다.
///   1. 요청을 좌표로 환산한다 (IVfxAnchorResolver에게 물어봄)
///   2. 대상 수만큼 펼친다
///   3. 알맞은 재생기를 골라 코루틴을 돌린다
///
/// 어떤 곡선인지, 어디서 출발하는지는 VisualEffectPreset이 답합니다.
/// 오브젝트를 어떻게 만들고 돌려주는지는 IVfxSpawner(PoolingSystem)가 맡습니다.
/// 이 클래스는 그 사이를 잇기만 합니다.
public class VfxSystem : MonoBehaviour
{
    private readonly IVfxPlayer burstPlayer = new BurstVfxPlayer();
    private readonly IVfxPlayer projectilePlayer = new ProjectileVfxPlayer();

    private IVfxAnchorResolver anchors;
    private IVfxSpawner spawner;
    private VfxPrewarmer prewarmer;

    private bool initalized;

    /// <summary>
    /// InGameController가 보드 준비를 마친 뒤 부릅니다.
    ///
    /// BoardView를 SerializeField로 직접 물리지 않는 이유는,
    /// 이 시스템이 특정 뷰 구현에 묶이면 연출 테스트 씬을 따로 만들 수 없기 때문입니다.
    /// </summary>
    public void Init(IVfxAnchorResolver anchorResolver)
    {
        anchors = anchorResolver;
        spawner = new PoolingVfxSpawner(InGameManagers.Pool);
        prewarmer = new VfxPrewarmer(spawner);
        
        GlobalManagers.Event?.RemoveEvent<OnVfxRequestedEventData>(OnVfxRequested);
        GlobalManagers.Event?.AddEvent<OnVfxRequestedEventData>(OnVfxRequested);

        initalized = true;
    }
    
    public void Release()
    {
        if (!initalized) return;
        
        GlobalManagers.Event?.RemoveEvent<OnVfxRequestedEventData>(OnVfxRequested);
        
        StopAllCoroutines();

        // 풀 자체는 PoolingSystem이 소유합니다. 여기서 비우면 남의 오브젝트까지 지웁니다.
        prewarmer?.Clear();

        anchors = null;
        spawner = null;
        prewarmer = null;
        initalized = false;
    }

    private void OnDestroy() => Release();

    /// <summary>
    /// 이번 전투에 올라온 유닛들이 쓸 연출을 미리 풀에 채웁니다.
    /// 보드 배치가 끝난 뒤(전투 시작 직전) 한 번 부르세요.
    ///
    /// 아군과 적을 따로 부르지 말고 합쳐서 한 번에 넘기면 중복이 자동으로 걸러집니다.
    /// </summary>
    public void Prewarm(IEnumerable<BattleUnit> units, int countPerPrefab = 2)
    {
        if (!initalized) return;
        
        prewarmer.Prewarm(units, countPerPrefab);
    }

    #region 요청 처리

    private void OnVfxRequested(OnVfxRequestedEventData e)
    {
        if (!initalized || e == null) return;
        if (string.IsNullOrEmpty(e.VisualEffectId)) return;

        if(!GlobalManagers.Resource.TryGetResource(e.VisualEffectId, out VisualEffectData data))
        {
            Logger.LogWarning($"[VfxSystem] VisualEffectData '{e.VisualEffectId}'를 찾지 못했습니다.");
            return;
        }

        if (!data.IsPlayable) 
        {
            Logger.LogWarning($"[VfxSystem] '{e.VisualEffectId}'에 프리팹이 없습니다.");
            return;
        }

        PlaySfx(data.CastSfxKey);
        
        ESpawnAnchor anchor = VisualEffectPreset.GetSpawnAnchor(data.MoveType);

        if (anchor == ESpawnAnchor.EnemyBoardCenter || anchor == ESpawnAnchor.TeamBoardCenter)
            PlayAtBoardCenter(data, anchor, e.Caster, e.Scale);
        else
            PlayPerTarget(data, e.Caster, e.Targets, e.Scale);
    }

    private static void PlaySfx(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        GlobalManagers.Audio.PlaySfx(key, true);
    }

    /// <summary>전체공격. 대상이 몇이든 보드 중심에서 한 번만 터집니다.</summary>
    private void PlayAtBoardCenter(VisualEffectData data, ESpawnAnchor anchor, BattleUnit caster, float scale)
    {
        bool enemyBoard = anchor == ESpawnAnchor.EnemyBoardCenter;
        if (caster != null && !caster.IsAlly) enemyBoard = !enemyBoard;

        if (!anchors.TryGetBoardCenter(enemyBoard, out Vector3 center)) return;
        
        Play(burstPlayer, new VfxPlayContext(data, center, center, null, scale));
    }

    /// <summary>대상마다 하나씩. 제자리든 발사체든 같은 규칙입니다.</summary>
    private void PlayPerTarget(VisualEffectData data, BattleUnit caster, IReadOnlyList<BattleUnit> targets, float scale)
    {
        if (targets == null || targets.Count == 0) return;

        bool isProjectile = VisualEffectPreset.IsProjectile(data.MoveType);
        IVfxPlayer player = isProjectile ? projectilePlayer : burstPlayer;

        Vector3 casterPos = Vector3.zero;
        if (isProjectile && !anchors.TryGetUnitPosition(caster, out casterPos)) return;

        for (int i = 0; i < targets.Count; i++)
        {
            if (!anchors.TryGetUnitPosition(targets[i], out Vector3 targetPos)) continue;

            Vector3 from = isProjectile ? casterPos : targetPos;
            Play(player, new VfxPlayContext(data, from, targetPos, null, scale));
        }
    }

    private void Play(IVfxPlayer player, VfxPlayContext ctx)
    {
        Vector3 spawnOffset = ctx.Data.SpawnOffset;
        Vector3 offset = ctx.Data.TargetOffset;

        var shifted = new VfxPlayContext(ctx.Data, ctx.From + spawnOffset, ctx.To + offset, ctx.OnArrive, ctx.Scale);

        StartCoroutine(player.Play(shifted, spawner));
    }

    /// <summary>
    /// 유닛 없이 좌표 두 개로 발사체 연출을 재생합니다. (재능 구매처럼 전투 밖의 연출용)
    /// 전투 연출은 여전히 OnVfxRequestedEventData로만 들어옵니다.
    /// 재생을 시작했는지 반환합니다. false면 도착 콜백이 불리지 않으므로 호출자가 직접 처리해야 합니다.
    /// </summary>
    public bool PlayProjectile(VisualEffectData data, Vector3 from, Vector3 to, Action onArrive = null)
    {
        if (!initalized || data == null) return false;
        
        Play(projectilePlayer, new VfxPlayContext(data, from, to, onArrive));
        return true;
    }
    
    #endregion
}
