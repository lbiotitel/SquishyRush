using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 스킬 설명 토큰을 실제 수치로 바꿉니다. 숫자는 SkillData.EffectIds 순서(= 엑셀 Order)입니다.
///   {D0} 실제 수치 0~3성   {d0} 실제 수치 현재 성급
///   {P0} 계수 0~3성        {p0} 계수 현재 성급
///   {S0} 부여 스택 0~3성   {s0} 부여 스택 현재 성급
/// 0~3성 표시는 현재 성급만 진하게, 나머지는 흐리게. 네 값이 같으면 하나로 줄입니다.
/// stat이 null이면(전투 밖) 실제 수치 자리에 계수 %를 대신 보여줍니다.
/// </summary>
public static class SkillDescFormatter
{
    private static readonly Regex Token = new(@"\{([DPSdps])(\d)\}", RegexOptions.Compiled);
    private static readonly StringBuilder sb = new();

    public static string Build(string template, SkillData skill, UnitStat stat, int curStar)
    {
        if (string.IsNullOrEmpty(template) || skill == null) return template;

        return Token.Replace(template, m =>
        {
            int idx = m.Groups[2].Value[0] - '0';
            if (idx >= skill.EffectIds.Count) return m.Value;
            if (!GlobalManagers.Resource.TryGetResource(skill.EffectIds[idx], out SkillEffectData e)) return m.Value;

            char c = m.Groups[1].Value[0];
            char kind = char.ToUpper(c);

            Func<int, string> f = kind switch
            {
                'S' => s => e.GetStatusStack(s).ToString(),
                'P' => s => Coef(e, s),
                 _  => s => Actual(e, stat, s),
            };

            bool single = kind switch
            {
                'S' => e.value.Length <= 1,
                'P' => e.modifierValue.Length <= 1,
                _ => false,
            };

            if (single) return f(Mathf.Max(curStar, 0));
            if (char.IsLower(c) && curStar >= 0) return f(curStar);
            return JoinStars(f, curStar);
        });
    }

    private static string JoinStars(Func<int, string> f, int cur)
    {
        sb.Clear();
        for (int s = 0; s <= GlobalDefines.MAX_VALUE.IG_MAX_STAR; s++)
        {
            if (s > 0) sb.Append('/');
            if (cur < 0) sb.Append(f(s));
            else if (s == cur) sb.Append("<b>").Append(f(s)).Append("</b>");
            else sb.Append("<alpha=#80>").Append(f(s)).Append("<alpha=#FF>");
        }

        return sb.ToString();
    }

    private static string Coef(SkillEffectData e, int star) => e.CalcValue(100f, star).ToString("0.#");

    private static string Actual(SkillEffectData e, UnitStat stat, int star)
    {
        bool pct = e.modifierType == EValueModifierType.StatPercent;
        if (stat == null) return pct ? Coef(e, star) + "%" : Coef(e, star);  // 유닛 정보가 없을 때

        float baseStat = pct ? StatAt(stat, e.statType, star) : 0f;
        float v = e.CalcValue(baseStat, star);

        switch (e.effectType)
        {
            case EEffectType.Damage : v *= stat.GetStat(EStatType.DmgMultiplier); break;
            case EEffectType.Heal : v *= stat.GetStat(EStatType.HealMultiplier); break;
        }

        return Mathf.RoundToInt(v).ToString();
    }

    // 성급 배율을 뺀 스탯 x 해당 성급 배율 → 0~3성 수치를 한 번에 계산
    private static float StatAt(UnitStat stat, EStatType type, int star)
    {
        Stat s = stat.GetStatObject(type);
        if (s == null) return 0f;
        if (type != EStatType.Atk && type != EStatType.MaxHp) return s.Value;

        float noStar = s.CalculateWith(mod => !Equals(mod.Source, InGameBalance.STAR_MODIFIER_SOURCE));
        float bonus = type == EStatType.Atk ? InGameBalance.GetAtkMultBonus(star) : InGameBalance.GetHpMultBonus(star);
        return noStar * (1f + bonus);
    }
}
