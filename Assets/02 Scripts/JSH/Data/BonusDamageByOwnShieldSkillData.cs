using UnityEngine;

/// <summary>
/// SP_0015 (곰토리) — 시전자가 들고 있는 실드량에 비례한 추가 피해를 줍니다.
/// 실드가 0이면 아무 일도 일어나지 않습니다. 실드는 소모되지 않습니다.
/// </summary>
[CreateAssetMenu(fileName = "SP_0015", menuName = "Data/Special/BonusDamageByOwnShield")]
public class BonusDamageByOwnShieldSkillData : SpecialSkillData
{
    [Header("수치")] [SerializeField, Min(0f)] [Tooltip("시전자 실드량 대비 추가 피해 비율(%)")]
    private float shieldPercent = 50f;
    
    public float ShieldPercent => shieldPercent;
}
