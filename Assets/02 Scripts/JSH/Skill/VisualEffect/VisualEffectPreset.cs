using UnityEngine;

/// <summary>
/// MoveType 하나가 실제로 무엇을 뜻하는지 정의하는 단 하나의 표입니다.
///
/// 임포터(검증)와 런타임(재생)이 같은 이 표를 봅니다.
/// 해석 결과를 SO에 구워두지 않는 이유가 이것입니다.
/// 같은 사실이 두 군데 적히면 언젠가 어긋나고, 어긋나도 아무도 모릅니다.
///
/// 새 프리셋이 생기면 여기 두 switch에만 case를 추가합니다.
/// 재생기나 SO나 임포터는 건드리지 않습니다.
/// </summary>
public static class VisualEffectPreset
{
    private static readonly float[] STAR_SCALE = { 1f, 1.2f, 1.4f, 1.8f };
    public static float GetStarScale(int star)
        => STAR_SCALE[Mathf.Clamp(star, 0, STAR_SCALE.Length - 1)];
    
    // 궤적은 상태가 없어서 인스턴스를 공유합니다.
    private static readonly ITrajectory linear = new LinearTrajectory();
    private static readonly ITrajectory arc = new ArcTrajectory();

    /// <summary>
    /// 비행 경로. 이동하지 않는 타입이면 null입니다.
    /// </summary>
    public static ITrajectory GetTrajectory(EMoveType moveType)
    {
        switch (moveType)
        {
            case EMoveType.Straight : return linear;
            case EMoveType.Bezier : return arc;
            default: return null;
        }
    }

    /// <summary>
    /// 프리팹이 날아가는가. 어느 재생기가 맡을지를 이 값이 결정합니다.
    ///   true  -> ProjectileVfxPlayer (대상마다 하나씩)
    ///   false -> BurstVfxPlayer      (제자리 재생)
    ///
    /// 궤적이 있으면 곧 발사체입니다. 둘을 따로 관리하면 어긋날 수 있어 여기서 파생시킵니다.
    /// </summary>
    public static bool IsProjectile(EMoveType moveType)
        => GetTrajectory(moveType) != null;

    /// <summary>연출이 시작되는 지점.</summary>
    public static ESpawnAnchor GetSpawnAnchor(EMoveType moveType)
    {
        switch (moveType)
        {
            case EMoveType.Straight:
            case EMoveType.Bezier:
                return ESpawnAnchor.Caster;
            
            case EMoveType.AllImpact:
                return ESpawnAnchor.EnemyBoardCenter;
            
            default:
                return ESpawnAnchor.Target;
        }
    }

    /// <summary>ArcHeight 컬럼이 의미를 갖는 타입인가. 임포터 검증에 씁니다.</summary>
    public static bool UsesArcHeight(EMoveType moveType) => moveType == EMoveType.Bezier;
}
