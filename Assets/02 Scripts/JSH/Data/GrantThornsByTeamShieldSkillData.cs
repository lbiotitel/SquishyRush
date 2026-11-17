using UnityEngine;

/// <summary>
/// SP_0014 (곰쿠키) — 아군이 실드를 보유 중이면 가시를 부여합니다.
///
/// 실드 총량을 스택으로 환산하던 방식을 버리고 "있다/없다" 조건으로 바꿨습니다.
/// 가시 스택은 배수가 아니라 남은 반사 횟수라(ReflectEffect 참조)
/// 실드량 같은 체력 단위 수치를 그대로 스택에 꽂으면 사실상 무한 반사가 됩니다.
/// 고정 스택이면 그 위험이 원천적으로 없습니다.
/// </summary>
[CreateAssetMenu(fileName = "SP_0014", menuName = "Data/Special/GrantThornsByTeamShield")]
public class GrantThornsByTeamShieldSkillData : SpecialSkillData
{
    [Header("상태이상")]
    [Tooltip("부여할 상태이상 ID (가시)")]
    [SerializeField] private string thornsStatusId;

    [Header("수치")] [Min(1)] [Tooltip("부여할 가시 스택 = 반사 횟수")]
    [SerializeField] private int thornsStack = 3;

    [Header("범위 규칙")] [Tooltip("켜면 아군 중 한 명이라도 실드가 있을 때 아군 전체에게 부여합니다.\n끄면 실드를 들고 있는 아군에게만 부여합니다.")]
    [SerializeField] private bool grantToAllAllies = false;
    
    public string ThornsStatusId => thornsStatusId;
    public int ThornsStack => thornsStack;
    public bool GrantToAllAllies => grantToAllAllies;
}
