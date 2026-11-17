/// <summary>
/// StatusEffectType에 맞는 런타임 클래스를 만듭니다.
/// StatusEffect가 추상이라 생성 경로를 여기 하나로 모읍니다.
/// </summary>
public static class StatusEffectFactory
{
    public static StatusEffect Create(StatusEffectData data, BattleUnit caster, int stack, float? overrideValue,
        DamageCredit credit = default)
    {
        if (data == null) return null;

        StatusEffect se = data.StatusEffectType switch
        {
            EStatusEffectType.StatModifier => new StatModifierEffect(),
            EStatusEffectType.DamageOverTime => new DotEffect(),
            EStatusEffectType.HealOverTime => new HotEffect(),
            EStatusEffectType.Shield => new ShieldEffect(),
            EStatusEffectType.Control => new ControlEffect(),
            EStatusEffectType.Reflect => new ReflectEffect(),
            EStatusEffectType.DamageReduction => new ProtectionEffect(),
            EStatusEffectType.OnDamagedBonus => new ShockEffect(),
            EStatusEffectType.Revive => new ReviveEffect(),
            EStatusEffectType.StackOnAttack => new SnackStackEffect(),
            EStatusEffectType.ApplyStatusOnHit => new ApplyStatusOnHitEffect(),
            EStatusEffectType.HealAmp => new HealAmpEffect(),
            _ => null,
        };

        if (se == null)
        {
            Logger.LogWarning($"'{data.StatusId}'의 {data.StatusEffectType}에 대응하는 클래스가 없습니다.");
            return null;
        }
        
        se.Init(data, caster, stack, overrideValue, credit);
        return se;
    }
}
