using UnityEngine;

/// <summary>
/// 자폭 재능 데이터입니다.
/// 전투 동안 걸리는 효과(데미지 증가 등)는 EffectIds에 넣고,
/// 스킬 시전 시 사망은 SelfDestructPerk가 직접 처리합니다.
/// </summary>
[CreateAssetMenu(menuName = "Perk/SelfDestruct Perk")]
public class SelfDestructPerkData : PerkDataBase
{
}
