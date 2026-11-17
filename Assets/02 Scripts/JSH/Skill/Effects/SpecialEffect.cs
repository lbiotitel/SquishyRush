using System.Collections.Generic;

/// <summary>
/// 스페셜 스킬의 런타임 본체입니다. SkillEffect의 한 갈래라 Skill 입장에서는
/// Damage / Heal / ApplyStatus와 똑같이 생긴 전략(Strategy) 하나로 보입니다.
///
/// 일반 이펙트와 다른 점은 딱 하나, 대상이 0명이어도 할 일이 남아 있을 수 있다는 것입니다.
/// </summary>
public abstract class SpecialEffect : SkillEffect
{
    protected override bool RequireTargets => false;

    /// <summary>
    /// 이 스페셜이 SO 수치를 필요로 하는지.
    /// 자폭(SP_0003)처럼 수치가 하나도 없는 스킬은 false로 두어
    /// 빈 SO 애셋을 만들지 않아도 되게 합니다.
    /// </summary>
    internal virtual bool RequiresData => true;
    
    internal virtual bool HasData => true;
    
    /// <summary>SkillEffect.Init이 생성 직후 한 번만 부릅니다.</summary>
    internal virtual void BindData(SpecialSkillData data) {}
}

/// <summary>
/// 자기 데이터 타입을 아는 스페셜 이펙트.
///
/// 이 한 겹이 있어야 파생 클래스마다 (MySkillData)data 캐스팅을 반복하지 않습니다.
/// 짝이 어긋나면 BindData에서 즉시 에러를 뱉으므로, 잘못된 조합이 전투 중에
/// NullReference로 터지지 않고 로드 시점에 드러납니다.
/// </summary>
public abstract class SpecialEffect<TData> : SpecialEffect where TData : SpecialSkillData
{
    protected TData Data { get; private set; }

    internal sealed override bool HasData => Data != null;

    internal sealed override void BindData(SpecialSkillData data)
    {
        Data = data as TData;

        if (Data == null)
        {
            Logger.LogError(
                $"{GetType().Name}는 {typeof(TData).Name}를 기대하는데 " +
                $"{(data == null ? "null" : data.GetType().Name)}이 들어왔습니다. " +
                $"SpecialEffectRegistry의 매핑과 SO 타입이 맞는지 확인하세요.");
        }
    }

    /// <summary>
    /// StatusID 문자열을 실제 데이터로 바꿉니다. OnInit에서 한 번만 부름.
    ///
    /// 직접 참조(SerializeField StatusEffectData) 대신 문자열 ID를 쓰는 이유는
    /// 상태이상 애셋이 각 유닛의 Addressable 번들에 통째로 복제되는 것을 막기 위해서입니다.
    /// </summary>
    protected StatusEffectData ResolveStatus(string statusId)
    {
        if (string.IsNullOrEmpty(statusId))
        {
            Logger.LogWarning($"{GetType().Name}({Data?.SpecialSkillId}): StatusID가 비어 있습니다.");
            return null;
        }

        if(!GlobalManagers.Resource.TryGetResource(statusId, out StatusEffectData data))
            Logger.LogWarning($"{GetType().Name}({Data?.SpecialSkillId}): StatusEffectData '{statusId}'를 찾지 못했습니다.");

        return data;
    }

    /// <summary>
    /// 피해를 대상에게 넣고 처치 여부를 컨텍스트에 기록합니다.
    ///
    /// source가 증폭 종류를 결정합니다.
    ///   Skill / Attack : DmgMultiplier
    ///   DoT            : StatusEffectDmg  (지옥불처럼 상태이상이 출처인 피해)
    /// </summary>
    protected void DealDamage(BattleUnit caster, BattleUnit target, float rawAmount,
        EDamageSource source = EDamageSource.Skill)
    {
        if (target == null || !target.IsAlive || rawAmount <= 0f) return;

        float amount = DamageFormula.Resolve(caster, target, rawAmount, source, out bool isCrit);
        target.TakeDamage(amount, caster, source, isCrit);

        if (!target.IsAlive) context?.ReportKill(target);
    }

    protected void DealDamage(BattleUnit caster, List<BattleUnit> targets, float rawAmount,
        EDamageSource source = EDamageSource.Skill)
    {
        for (int i = 0; i < targets.Count; i++)
            DealDamage(caster, targets[i], rawAmount, source);
    }
}
