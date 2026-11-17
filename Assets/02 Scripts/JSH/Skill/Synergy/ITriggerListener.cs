/// <summary>
/// 유닛 소유가 아닌 트리거 반응. 시너지와 아티팩트가 씁니다.
///
/// BattleUnit의 스킬은 SkillTriggerSystem이 직접 발동시키지만, 시너지 효과는
/// 주인이 없어서 그 경로를 못 탑니다. 대상도 적 전체처럼 개별 유닛 밖입니다.
///
/// 전투 진입에 등록하고 종료에 해제합니다. 수명은 SynergySystem이 관리합니다.
/// </summary>
public interface ITriggerListener 
{
    ETriggerType Trigger { get; }

    /// <summary>owner는 트리거를 일으킨 유닛, other는 상대(죽인 대상, 때린 쪽 등)입니다.</summary>
    void OnTriggered(BattleUnit owner, BattleUnit other);
}
