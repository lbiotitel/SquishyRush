using UnityEngine;

/// <summary>
/// SP_0013 (파이어 에그) — 대상이 특정 상태이상에 걸려 있으면 시전자 공격력 기준 추가 피해를 줍니다.
/// 조건을 만족하는 대상에게만 들어갑니다.
/// </summary>
[CreateAssetMenu(fileName = "SP_0013", menuName = "Data/Special/BonusDamageIfStatus")]
public class BonusDamageIfStatusSkillData : SpecialSkillData
{
    [Header("조건")] [Tooltip("이 상태이상이 걸린 대상에게만 추가 피해가 들어갑니다.")]
    [SerializeField] private string requiredStatusId;
    
    [Header("수치")]
    [Min(0f)]
    [Tooltip("시전자 공격력 대비 추가 피해 비율(%)")]
    [SerializeField] private float bonusAtkPercent = 20f;

    [Tooltip("켜면 상태이상 스택 수만큼 배수로 들어갑니다.")]
    [SerializeField] private bool scaleByStatusStack = false;
    
    public string RequiredStatusId => requiredStatusId;
    public float BonusAtkPercent => bonusAtkPercent;
    public bool ScaleByStatusStack => scaleByStatusStack;
}
