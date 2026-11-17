using UnityEngine;

/// <summary>
/// 피해 1회의 '표시용' 결과. 전투 로직은 이 구조체를 읽지 않습니다.
/// 연출이 필요로 하는 것만 담고, 계산은 전부 끝난 상태로 넘어옵니다.
///
/// struct인 이유: 공격 한 번마다 생성되므로 클래스면 GC가 계속 쌓입니다.
/// EventManager를 안 타고 직접 이벤트로 넘기는 것도 같은 이유입니다.
/// </summary>
public readonly struct DamageReport
{
    public readonly float Amount;
    public readonly float ShockAmount;
    public readonly float ShieldAbsorbed;
    public readonly bool IsCrit;
    public readonly bool IsBlocked;
    public readonly EDamageSource Source;
    public readonly StatusEffectData StatusSource;

    public DamageReport(float amount, float shockAmount, float shieldAbsorbed,
        bool isCrit, EDamageSource source, StatusEffectData statusSource)
    {
        Amount = amount;
        ShockAmount = shockAmount;
        ShieldAbsorbed = shieldAbsorbed;
        IsCrit = isCrit;
        IsBlocked = false;
        Source = source;
        StatusSource = statusSource;
    }

    private DamageReport(EDamageSource source, StatusEffectData statusSource, bool blocked)
    {
        Amount = 0f;
        ShockAmount = 0f;
        ShieldAbsorbed = 0f;
        IsCrit = false;
        IsBlocked = blocked;
        Source = source;
        StatusSource = statusSource;
    }

    public static DamageReport Blocked(EDamageSource source, StatusEffectData statusSource)
        => new DamageReport(source, statusSource, true);

    public Color ResolveColor(out float scale)
    {
        if (StatusSource != null)
        {
            scale = StatusSource.TextScale;
            return StatusSource.TextColor;
        }

        if (Source == EDamageSource.Reflect)
        {
            scale = GlobalDefines.DAMAGE_TEXT.SCALE_SUB;
            return GlobalDefines.DAMAGE_TEXT.REFLECT;
        }

        if (IsCrit)
        {
            scale = GlobalDefines.DAMAGE_TEXT.SCALE_CRIT;
            return GlobalDefines.DAMAGE_TEXT.CRIT;
        }

        scale = GlobalDefines.DAMAGE_TEXT.SCALE_NORMAL;
        return GlobalDefines.DAMAGE_TEXT.NORMAL;
    }
}
