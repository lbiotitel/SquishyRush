using UnityEngine;

/// <summary>
/// 프로젝트의 PoolingSystem을 IVfxSpawner 모양으로 맞춰주는 어댑터입니다.
///
/// 연출 전용 풀을 따로 만들지 않는 이유는, 같은 일을 하는 풀링 구현이
/// 프로젝트에 둘 이상 존재하는 상황 자체가 나중에 반드시 문제가 되기 때문입니다.
/// PoolingSystem은 Queue 기반이라 꺼내기와 반납이 모두 O(1)이고,
/// 반납할 때 어느 풀 소속인지는 IPoolingObject.OriginPrefab이 스스로 답합니다.
/// </summary>
public class PoolingVfxSpawner : IVfxSpawner
{
    private readonly PoolingSystem pooling;
    
    public PoolingVfxSpawner(PoolingSystem pooling)
    {
        this.pooling = pooling;
    }

    public VfxObject Spawn(GameObject prefab, Vector3 position, float scale = 1f)
    {
        if (prefab == null || pooling == null) return null;

        var vfx = pooling.GetObject<VfxObject>(prefab, position, prefab.transform.localRotation);
        if (vfx == null)
        {
            Logger.LogWarning($"[Vfx] '{prefab.name}' 에 VfxObject가 없어 풀링할 수 없습니다.");
            return null;
        }
        
        vfx.ResetObject(scale);
        return vfx;
    }

    public void Despawn(VfxObject instance)
    {
        if (instance == null) return;
        instance.ReturnObject();
    }
}
