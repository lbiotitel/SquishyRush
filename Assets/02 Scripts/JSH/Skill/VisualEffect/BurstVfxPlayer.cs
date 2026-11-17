using System.Collections;
using UnityEngine;

/// <summary>
/// 제자리에서 한 번 터지는 연출. Impact와 AllImpact를 함께 맡습니다.
///
/// 둘은 하는 일이 같고 터지는 자리만 다릅니다(대상 위 / 보드 중심).
/// 그 자리는 VfxSystem이 이미 정해서 ctx.From에 담아 넘겨주므로
/// 여기서는 프리셋을 구분할 필요가 없습니다.
/// </summary>
public class BurstVfxPlayer : IVfxPlayer
{
    public IEnumerator Play(VfxPlayContext ctx, IVfxSpawner spawner)
    {
        VfxObject vfx = spawner.Spawn(ctx.Data.EffectPrefab, ctx.From, ctx.Scale);
        if (vfx == null)
        {
            ctx.OnArrive?.Invoke();
            yield break;
        }
        
        ctx.OnArrive?.Invoke();

        float elapsed = 0f;
        while (vfx.IsAlive && elapsed < VfxObject.MAX_LIFETIME)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        spawner.Despawn(vfx);
    }
}
