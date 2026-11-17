using UnityEngine;

/// <summary>
/// 연출 오브젝트를 꺼내고 돌려줍니다.
///
/// 재생기(IVfxPlayer)가 풀링 구현을 전혀 모르게 하려고 나눴습니다.
/// 재생기는 "달라"와 "다 썼다"만 말하고, 그게 새 오브젝트인지 재사용인지는 관심 밖입니다.
/// 덕분에 연출 테스트 씬에서는 풀 없이 Instantiate만 하는 더미 구현을 끼울 수 있습니다.
/// </summary>
public interface IVfxSpawner
{
    VfxObject Spawn(GameObject prefab, Vector3 position, float scale = 1f);
    void Despawn(VfxObject instance);
}
