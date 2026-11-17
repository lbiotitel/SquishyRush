using UnityEngine;

/// <summary>
/// 논리 세계(BattleUnit, 보드 슬롯)를 화면 좌표로 옮깁니다.
///
/// VfxSystem이 BoardView를 직접 참조하지 않게 하려고 뽑은 인터페이스입니다.
/// 나중에 연출 테스트 씬에서 고정 좌표를 돌려주는 더미 구현으로 갈아끼울 수 있습니다.
/// 메서드가 둘뿐이라 구현하는 쪽이 떠안는 부담도 없습니다(ISP).
/// </summary>
public interface IVfxAnchorResolver
{
    
    /// <summary>유닛이 서 있는 위치. 뷰가 없으면 false.</summary>
    bool TryGetUnitPosition(BattleUnit unit, out Vector3 position);

    
    /// <summary>
    /// 보드 전체의 중심. 전체공격(AllImpact)이 터지는 자리입니다.
    ///
    /// 살아 있는 유닛들의 평균이 아니라 슬롯 앵커의 고정 중심입니다.
    /// 누가 죽었느냐에 따라 폭발 위치가 흔들리면 어색하고,
    /// 대상이 0명일 때 터질 자리가 없어집니다.
    /// </summary>
    bool TryGetBoardCenter(bool isEnemyBoard, out Vector3 posiiton);
}
