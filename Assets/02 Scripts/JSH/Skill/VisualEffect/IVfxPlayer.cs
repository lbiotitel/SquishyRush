using System.Collections;

/// <summary>
/// 연출 한 개를 재생하는 방법. 전략(Strategy)입니다.
///
/// 코루틴을 돌려주는 이유는 연출이 본질적으로 시간축을 가진 절차이기 때문입니다.
/// 자체 Update 루프와 활성 인스턴스 목록을 따로 굴리는 것보다 짧고,
/// "스폰 -> 이동 -> 도착 -> 반납" 순서가 코드에 그대로 보입니다.
///
/// 재생기는 풀링도 보드도 모릅니다. spawner를 통해서만 오브젝트를 다룹니다.
/// </summary>
public interface IVfxPlayer
{
    IEnumerator Play(VfxPlayContext ctx, IVfxSpawner spawner);
}
