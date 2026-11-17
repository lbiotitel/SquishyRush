using System;
using System.Collections.Generic;

/// <summary>
/// SpecialSkillID -> 시너지 스페셜 클래스 매핑.
///
/// SpecialEffectRegistry와 따로 두는 이유는 타입도 수명도 다르기 때문입니다.
/// 저쪽은 스킬 발동에 맞춰 도는 SkillEffect, 이쪽은 전투 시작에 한 번 도는 ISynergySpecial입니다.
/// 한 사전에 섞으면 ID마다 "어느 쪽 것인가"를 확인해야 합니다.
/// </summary>
public static class SynergySpecialRegistry
{
    private static readonly Dictionary<string, Func<ISynergySpecial>> table = new()
    {
        { "SP_0024", () => new RandomBuffSynergy() },
        { "SP_0025", () => new RandomBuffSynergy() },
        { "SP_0026", () => new RandomBuffSynergy() },
        { "SP_0027", () => new RandomBuffSynergy() },
        { "SP_0034", () => new InstantSgChargeSynergy() },
    };

    public static ISynergySpecial Create(string specialSkillId)
    {
        if (string.IsNullOrEmpty(specialSkillId)) return null;

        return table.TryGetValue(specialSkillId, out var factory) ? factory() : null;
    }

}
