using UnityEngine;

/// <summary>SP_0033 (유령의 장난) ? 처치 시 적 전체 SG 감소량.</summary>
[CreateAssetMenu(fileName = "SP_0033", menuName = "Data/Special/ReduceEnemySgOnKill")]
public class ReduceEnemySgOnKillData : SpecialSkillData
{
    [Header("수치")] 
    [SerializeField] private float percent = 25f;

    public float Percent => percent;
}
