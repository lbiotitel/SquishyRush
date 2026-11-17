using UnityEngine;

/// <summary>
/// SP_0034 (영혼의 공명) ? 전투 시작 시 대상의 SG 게이지를 즉시 채웁니다.
///
/// 상태이상이 아니라 스페셜인 이유는, 게이지 현재값이 ResourceStat이라
/// StatModifierEffect가 닿지 못하기 때문입니다.
/// SG_Charge_Amount를 올리는 '충전' 상태이상과는 다른 물건입니다.
/// 그쪽은 앞으로 빨리 차게 하는 것이고, 이쪽은 지금 채우는 것입니다.
/// </summary>
[CreateAssetMenu(fileName = "SP_0034", menuName = "Data/Special/InstantSgCharge")]
public class InstantSgChargeData : SpecialSkillData
{
    [Header("수치")] 
    [SerializeField] private float sgChargePercent = 50f;
    
    public float SgChargePercent => sgChargePercent;
}
