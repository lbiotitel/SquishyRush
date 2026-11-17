using System;
using System.Collections.Generic;


/// <summary>
/// 경험치 바가 한 번에 채워야 하는 "한 레벨 구간"입니다.
/// 예) Lv3 700/1000 에서 +800 → [Lv3 700→1000] [Lv4 0→500]
/// </summary>
public readonly struct ExpSegment
{
    public readonly int Level;
    public readonly int FromExp;
    public readonly int ToExp;
    public readonly int RequiredExp;

    public ExpSegment(int level, int fromExp, int toExp, int requiredExp)
    {
        Level = level;
        FromExp = fromExp;
        ToExp = toExp;
        RequiredExp = requiredExp;
    }

    /// <summary>이 구간이 끝나면 레벨업하는지 여부입니다.</summary>
    public bool IsLevelUp => RequiredExp > 0 && ToExp >= RequiredExp;

    /// <summary>이 구간에서 바가 차오르는 양(0~1). 연출 시간 계산에만 사용합니다.</summary>
    public float FillRate => RequiredExp > 0 ? (float)(ToExp - FromExp) / RequiredExp : 0f;

}

/// <summary>
/// (획득 전 레벨/경험치 + 획득량)을 레벨 구간 리스트로 쪼갭니다.
/// Unity 비의존 순수 로직이라 연출(View)과 분리되어 있습니다.
/// </summary>
public static class ExpSegmentBuilder 
{
    public static List<ExpSegment> Bulid(
        int startLevel,
        int startExp,
        int gainedExp,
        Func<int, int> getRequiredExp,
        int maxLevel)
    {
        List<ExpSegment> segments = new List<ExpSegment>();

        int level = startLevel;
        int exp = startExp;
        int remaining = Math.Max(0, gainedExp);

        while (true)
        {
            int required = getRequiredExp(level);

            // 만렙이거나 차트가 비정상이면 꽉 찬 바로 고정하고 종료 (무한루프 방지)
            if (level >= maxLevel || required <= 0)
            {
                segments.Add(new ExpSegment(level, required, required, required));
                break;
            }

            int to = exp + remaining;

            if (to < required)
            {
                segments.Add(new ExpSegment(level, exp, to, required));
                break;
            }
            
            // 이번 레벨을 꽉 채우고 다음 레벨로 넘어감
            segments.Add(new ExpSegment(level, exp ,required, required));
            remaining = to - required;
            level++;
            exp = 0;
        }

        return segments;
    }
}
