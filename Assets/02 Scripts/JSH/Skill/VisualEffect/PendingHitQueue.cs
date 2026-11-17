using System.Collections.Generic;

/// <summary>
/// 날아가는 중인 타격들을 들고 있다가, 비행 시간이 다 되면 적용합니다.
/// BattleManager가 하나 소유하고 Tick에서 돌립니다.
///
/// 왜 VFX의 OnArrive가 아니라 여기서 데미지를 넣는가
///   1. VfxSystem은 위치를 못 찾거나 프리팹이 없으면 재생을 건너뜁니다.
///      OnArrive에 데미지를 걸면 그때 데미지가 통째로 사라집니다.
///   2. 로직이 연출(MonoBehaviour 코루틴)에 의존하게 됩니다.
///      BattleManager는 Unity 독립이어야 테스트/리플레이가 가능합니다.
///
/// 발사체를 "속도"가 아니라 "시간"으로 바꾼 덕분에 이게 가능해졌습니다.
/// 속도 기반이면 도착 시점 = 거리 / 속도 라서 화면 좌표를 알아야 하는데,
/// 시간 기반이면 좌표 없이도 로직이 도착 시점을 압니다.
/// VFX도 같은 TravelTime, 같은 Time.deltaTime(배속 timeScale 포함)으로 흐르므로
/// 발사체가 닿는 프레임에 데미지가 들어갑니다.
/// </summary>
public class PendingHitQueue
{
    private readonly List<PendingHit> hits = new();

    // 도착한 것들을 잠깐 모아두는 곳. 매 Tick 새로 만들지 않으려고 필드로 둡니다.
    private readonly List<PendingHit> arrived = new();

    public int Count => hits.Count;

    /// <summary>
    /// 타격을 내보냅니다. 비행 시간이 0이면(근접, 즉발) 즉시 적용합니다.
    ///
    /// 즉시냐 예약이냐를 여기 한 곳에서 판단하므로
    /// 호출하는 쪽(NormalAttack, 스킬 이펙트)은 분기문을 갖지 않습니다.
    /// </summary>
    public void Dispatch(PendingHit hit)
    {
        if (hit.Handler == null) return;

        if (hit.Remaining <= 0f)
        {
            hit.IsDeferred = false;
            hit.Handler.ApplyHit(hit);
            return;
        }

        hit.IsDeferred = true;
        hits.Add(hit);
    }

    public void Tick(float dt)
    {
        if (hits.Count == 0) return;
        
        arrived.Clear();

        // 1단계: 시간을 줄이고, 도착한 것만 골라냅니다.
        //   - 앞으로 당겨 채우는 방식이라 순서가 유지됩니다(먼저 쏜 것이 먼저 적용 = 결정론).
        //   - struct라 hits[i].Remaining -= dt 는 복사본만 바뀝니다. 꺼내서 고친 뒤 다시 써넣어야 합니다.
        int write = 0;
        for (int read = 0; read < hits.Count; read++)
        {
            PendingHit h = hits[read];
            h.Remaining -= dt;

            if (h.Remaining <= 0f) arrived.Add(h);
            else hits[write++] = h;
        }
        hits.RemoveRange(write, hits.Count - write);

        // 2단계: 적용합니다.
        // 1단계와 나눈 이유: 적용 중에 가시(반사) -> OnHit 트리거 -> 스킬 발동 -> 새 발사체가
        // hits에 추가될 수 있습니다. 순회 중인 리스트를 건드리면 방금 쏜 것까지
        // 이번 dt만큼 시간이 깎여 한 프레임 일찍 도착합니다.
        for (int i = 0; i < arrived.Count; i++)
        {
            PendingHit h = arrived[i];
            h.Handler.ApplyHit(h);
        }
        
        arrived.Clear();
    }

    /// <summary>전투 종료 시. 날아가던 타격이 다음 라운드로 넘어가면 안 됩니다.</summary>
    public void Clear()
    {
        hits.Clear();
        arrived.Clear();
    }
}
