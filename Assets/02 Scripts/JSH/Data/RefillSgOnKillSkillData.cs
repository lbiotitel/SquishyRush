using UnityEngine;

/// <summary>
/// SP_0011 (사신) — 이번 발동으로 처치한 대상이 있으면 시전자의 SG를 채웁니다.
/// 처치가 없으면 아무 일도 일어나지 않습니다.
///
/// 처치 '수'가 아니라 '여부'로 판정합니다. 엑셀 설명이 "처치된 유닛이 있으면"이고,
/// 기본값 100%면 한 번만 채워도 이미 가득 차서 배수가 의미가 없습니다.
/// </summary>
[CreateAssetMenu(fileName = "SP_0011", menuName = "Data/Special/RefillSgOnKill")]
public class RefillSgOnKillSkillData : SpecialSkillData
{
    [Header("수치")] [SerializeField, Range(0, 100f)] [Tooltip("처치 시 채울 SG 비율(%). 100이면 게이지를 가득 채웁니다.")]
    private float sgRefillPercent = 100f;

    public float SgRefillPercent => sgRefillPercent;
}
