using UnityEngine;

/// <summary>
/// SP_0018 (깃털이) — 대상 아군들의 SG를 최대치의 N%만큼 즉시 채웁니다.
///
/// 성급(0~3)별로 값이 다릅니다. v7 원칙대로 Special 수치는 SkillEffect 행이 아니라 이 SO가 단일 출처입니다.
/// 배열 길이가 1이면 전 성급 공통, 4면 0~3성 각각입니다. (SkillEffectData의 "a/b/c/d"와 같은 규칙)
/// </summary>
[CreateAssetMenu(fileName = "SP_0018", menuName = "Data/Special/RefillSgAlly")]
public class RefillSgAllySkillData : SpecialSkillData
{
    [Header("수치")] 
    [SerializeField] private float[] sgRefillPercent = { 60f, 70f, 75f, 100f };
    
    [Header("대상 규칙")]
    [Tooltip("시전자 자신도 채울지. 켜면 SG를 비우는 시점에 따라 자기 연쇄가 생길 수 있어 기본은 끔")]
    [SerializeField] private bool includeCaster = false;
        
    public bool IncludeCaster => includeCaster;

    public float GetRefillPercent(int star)
    {
        if (sgRefillPercent == null || sgRefillPercent.Length == 0) return 0f;
        return sgRefillPercent[Mathf.Clamp(star, 0, sgRefillPercent.Length - 1)];
    }
}
