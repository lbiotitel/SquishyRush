using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SP_0025~0028 - 대상의 무작위 스탯 하나를 PercentAdd로 올림
/// </summary>
[CreateAssetMenu(fileName = "SP_0025", menuName = "Data/Special/RandomStatBuff")]
public class RandomBuffData : SpecialSkillData
{
    [Header("수치")]
    [SerializeField] private float percent;

    [Header("스탯 후보")]
    [SerializeField] private List<EStatType> statCandidates = new();
    
    [Header("상태이상 버프 (6단계만 사용)")]
    [SerializeField] private int statusPickCount = 5;
    [SerializeField] private int statckPerPick = 1;

    [Header("상태이상 버프 후보")] 
    [SerializeField] private List<string> statusIds = new();
    
    public float Percent => percent;
    public IReadOnlyList<EStatType> StatCandidates => statCandidates;

    public int StatusPickCount => statusPickCount;
    public int StatckPerPick => statckPerPick;
    public IReadOnlyList<string> StatusIds => statusIds;
}
