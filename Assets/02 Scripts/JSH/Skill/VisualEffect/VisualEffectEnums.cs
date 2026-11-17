/// <summary>
/// 엑셀 VisualEffect 시트의 MoveType 컬럼.
/// 연출이 어떻게 도는지를 결정하는 유일한 값입니다.
///
/// ★ 번호는 Enum 시트의 Num과 반드시 같아야 합니다.
///   애셋이 한 번 구워진 뒤에는 절대 번호를 바꾸지 마세요.
///   직렬화된 SO가 인덱스로 값을 들고 있어서 조용히 다른 값으로 바뀝니다.
/// </summary>
public enum EMoveType
{
    Impact = 0,      // 이동 없음. 대상 위에서 즉시 재생
    Straight = 1,    // 시전자 -> 대상, 직선 비행
    Bezier = 2,      // 시전자 -> 대상, 포물선 비행 (ArcHeight가 정점 높이)
    AllImpact = 3,   // 적 보드 중심에서 한 번 재생 (전체공격)
}

/// <summary>연출이 시작되는 지점. MoveType으로부터 파생됩니다.</summary>
public enum ESpawnAnchor
{
    Target = 0,            // 대상 유닛 위
    Caster = 1,            // 시전자 위
    EnemyBoardCenter = 2,  // 적 보드 6칸의 고정 중심
    TeamBoardCenter = 3,   // 아군 보드 6칸의 고정 중심
}