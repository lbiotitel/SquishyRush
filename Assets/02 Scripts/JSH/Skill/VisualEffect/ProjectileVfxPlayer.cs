using System.Collections;
using UnityEngine;

/// <summary>
/// 시전자에서 대상까지 날아가는 연출. Straight와 Bezier를 함께 맡습니다.
///
/// 둘의 차이는 궤적 함수 하나뿐이라 클래스를 나누지 않았습니다.
/// 어떤 곡선을 탈지는 VisualEffectPreset이 알려주고, 여기서는 호출만 합니다.
///
///   비행 시간은 거리/속도가 아니라 VisualEffectData.TravelTime 고정입니다.
///   로직(PendingHitQueue)도 같은 값을 기다렸다가 데미지를 넣으므로,
///   발사체가 닿아서 사라지는 프레임에 데미지 숫자가 뜹니다.
///   이 클래스는 데미지를 전혀 모릅니다. 날아가서 사라지기만 합니다.
///
/// 도착하면 머리(스프라이트)는 즉시 사라지고, 꼬리 파티클만 잠깐 남습니다.
/// </summary>
public class ProjectileVfxPlayer : IVfxPlayer
{
    //이제 비행 시간 계산에는 쓰지 않고, 진행 방향 회전에서만 씁니다.
    private const float MIN_DISTANCE = 0.001f;

    //출발 직후 방향을 잡을 때 궤적을 얼마나 앞서 볼지.
    private const float HEADING_PROBE_T = 0.02f;
    
    public IEnumerator Play(VfxPlayContext ctx, IVfxSpawner spawner)
    {
        VisualEffectData data = ctx.Data;
 
        VfxObject vfx = spawner.Spawn(data.EffectPrefab, ctx.From, ctx.Scale);
        if (vfx == null)
        {
            ctx.OnArrive?.Invoke();
            yield break;
        }
 
        ITrajectory trajectory = VisualEffectPreset.GetTrajectory(data.MoveType);
        Transform tr = vfx.transform;

        float duration = Mathf.Max(0f, data.TravelTime);

        //   첫 프레임 방향 맞추기.
        //   루프는 다음 프레임부터 돌기 때문에, 스폰된 첫 프레임에는 프리팹 기본 방향으로 보입니다.
        //   파티클은 티가 안 났지만 화살 같은 스프라이트는 한 프레임 엉뚱한 쪽을 보는 게 보입니다.
        Vector3 probe = Evaluate(trajectory, ctx.From, ctx.To, HEADING_PROBE_T, data.ArcHeight);
        FaceMoveDirection(tr, ctx.From, probe, vfx.FacingOffset);
        
        float elapsed = 0f;
        Vector3 previous = ctx.From;
 
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 next = Evaluate(trajectory, ctx.From, ctx.To, t, data.ArcHeight);
 
            tr.position = next;
            FaceMoveDirection(tr, previous, next, vfx.FacingOffset);
            previous = next;
 
            yield return null;
        }
 
        tr.position = ctx.To;
        
        // 닿는 순간 머리(스프라이트)를 숨깁니다.
        // 예전엔 파티클 방출만 멈춰서, 스프라이트가 있으면 LINGER 동안 대상 위에 그대로 남았습니다.
        vfx.Arrive();
        
        // 순수 연출용 콜백. 데미지는 여기서 넣지 않습니다(PendingHitQueue 담당).
        ctx.OnArrive?.Invoke();
 
        // 방출만 멈추고, 이미 나간 꼬리 파티클은 자기 수명대로 사라지게 둡니다.
        // 스프라이트만 있는 프리팹이면 IsAlive가 곧바로 false라 이 프레임에 반납됩니다.
        vfx.StopEmitting();

        float lingerLimit = Mathf.Min(vfx.TailLifetime, VfxObject.MAX_LIFETIME);
        
        float linger = 0f;
        while (vfx.IsAlive && linger < lingerLimit)
        {
            linger += Time.deltaTime;
            yield return null;
        }
 
        spawner.Despawn(vfx);
    }
    
    // 궤적이 없을 때의 직선 폴백을 한 곳으로 모았습니다(루프 안과 첫 프레임 두 곳에서 씀).
    private static Vector3 Evaluate(ITrajectory trajectory, Vector3 from, Vector3 to, float t, float arcHeight)
        => trajectory != null ? trajectory.Evaluate(from, to, t, arcHeight)
            : Vector3.Lerp(from, to, t);
 
    /// <summary>진행 방향으로 머리를 돌립니다. 화살처럼 방향성이 있는 프리팹에 필요합니다.</summary>
    private static void FaceMoveDirection(Transform tr, Vector3 from, Vector3 to, float facingOffset)
    {
        Vector3 delta = to - from;
        if (delta.sqrMagnitude < MIN_DISTANCE * MIN_DISTANCE) return;
 
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        tr.rotation = Quaternion.Euler(0f, 0f, angle + facingOffset);
    }
}
