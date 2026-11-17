using System.Collections.Generic;
using System;

/// <summary>
/// SpecialSkillID -> 런타임 이펙트 클래스 매핑.
///
/// 데이터는 문자열 ID만 들고, 그 ID를 클래스로 바꾸는 지식은 런타임 쪽인 여기 둡니다.
///
/// 대가는 분명합니다. 스페셜을 추가할 때 이 파일을 수정해야 합니다(OCP 위반).
/// 항목이 수십 개로 늘어 이 파일이 병목이 되면 그때 속성 기반 자동 등록으로 옮기면 됩니다.
/// 지금 규모(15개)에서는 명시적 표가 더 읽기 쉽다
/// </summary>
public static class SpecialEffectRegistry
{
    private static readonly Dictionary<string, Func<SpecialEffect>> table = new()
    {
        { "SP_0003", () => new SelfDestructSkill() },
        { "SP_0011", () => new RefillSgOnKillSkill() },
        { "SP_0012", () => new ConvertBurnToHellfireSkill() },
        { "SP_0013", () => new BonusDamageIfStatusSkill() },
        { "SP_0014", () => new GrantThornsByTeamShieldSkill() },
        { "SP_0015", () => new BonusDamageByOwnShieldSkill() },
        { "SP_0016", () => new DispelSkill() },
        { "SP_0017", () => new CopyAllySkillSkill() },
        { "SP_0018", () => new RefillSgAllySkill() },

    };
    
    /// <summary>등록된 ID인지. 에디터 검증용입니다.</summary>
    public static bool Contains(string specialSkillId)
        => !string.IsNullOrEmpty(specialSkillId) &&  table.ContainsKey(specialSkillId);

    public static IEnumerable<string> RegisteredIds => table.Keys;

    public static SpecialEffect Create(string specialSkillId)
    {
        if (string.IsNullOrEmpty(specialSkillId))
        {
            Logger.LogWarning("SpecialSkillID가 비어 있습니다.");
            return null;
        }

        if (!table.TryGetValue(specialSkillId, out var factory))
        {
            Logger.LogWarning(
                $"SpecialSkillID '{specialSkillId}'에 대응하는 클래스가 등록되어 있지 않습니다. " +
                $"SpecialEffectRegistry를 확인하세요.");
            return null;
        }

        return factory();
    }
}
