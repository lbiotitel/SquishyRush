using System.Collections.Generic;


/// <summary>
/// "이 연출을 틀어달라"는 요청. 전투 로직이 발행하고 VfxSystem이 받습니다.
///
/// 로직(SkillEffect / BattleUnit)은 VfxSystem을 참조하지 않습니다.
/// 그 둘은 Unity 독립이고 결정론적 리플레이가 걸려 있어서,
/// 직접 재생을 부르는 순간 두 성질이 모두 깨집니다.
/// BattleUnitView가 Unit.OnAttackPerformed를 구독하는 것과 같은 방향입니다.
///
/// Targets는 수신 즉시 좌표로 환산되고 보관되지 않습니다.
/// 그래서 발행 측이 내부 리스트를 그대로 넘겨도 안전합니다.
/// </summary>
public class OnVfxRequestedEventData : IEventData
{
    public string VisualEffectId;
    public BattleUnit Caster;
    public IReadOnlyList<BattleUnit> Targets;
    public float Scale = 1f;
}
