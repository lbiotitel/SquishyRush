using System;
using System.Collections.Generic;
using UnityEngine;


public partial class BattleUnit
{
    //보드에 같은 유닛이 여러마리 있을때 분리용
    public int RuntimeId { get; private set; } 
    public UnitData CurrentUnitData { get; private set; }
    public int Star { get; private set; }
    public int Level { get; private set; }
    
    //진영 판단
    public bool IsAlly { get; private set; }
    
    //스탯
    public UnitStat Stat { get; private set; }
    
    private UnitBaseStat baseStat;
    private ResourceStat hp;
    private ResourceStat sg;

    public float CurrentHp => hp.Current;
    public float MaxHp => hp.Max;
    public float HpRatio => hp.Ratio;
    public float SGRatio => sg.Ratio;
    public bool IsAlive => !hp.IsEmpty;

    
    //전투 상태
    private UnitStatContainer unitStatContainer;
    private Skill skill;
    
    public SkillData SkillData => skill?.Data;
    
    /// <summary>
    /// 일반공격 간격
    /// </summary>
    private float attackTimer;
    
    /// <summary>
    /// 사망처리 플래그
    /// </summary>
    private bool isDead;
    
    /// <summary>
    /// 마지막으로 나를 공격한 유닛
    /// </summary>
    private BattleUnit lastAttacker;

    public float EffectiveAtkSpeed
        => Stat.GetStat(EStatType.AtkSpeed) * (InGameManagers.Battle?.SuddenDeath.AtkSpeedFactor ?? 1f);
    
    private float AttckInterval => 1f / Mathf.Max(0.1f, EffectiveAtkSpeed);

    public event Action OnAttackPerformed;
    public event Action<EDamageSource> OnDamaged;
    public event Action OnMissed;
    public event Action OnDied;

    public event Action<float, float> OnHpChanged;
    public event Action<float, float> OnSGChanged;
    public event Action<int> OnStarChanged;
    public event Action<DamageReport> OnDamageTaken;
    public event Action<float, StatusEffectData, bool> OnHealed;
    public event Action OnStatusChanged;
    
    public void ReportMiss() => OnMissed?.Invoke();
    
    public void Init(int runtimeId, UnitData unitData, int level, UnitBaseStat baseStat, bool isAlly, int star = 0)
    {
        RuntimeId = runtimeId;
        CurrentUnitData = unitData;
        Level = level;
        Star = Mathf.Clamp(star, 0, GlobalDefines.MAX_VALUE.IG_MAX_STAR);
        IsAlly = isAlly;
        this.baseStat = baseStat;
        
        unitStatContainer = new UnitStatContainer();
        isDead = false;
        attackTimer = 0f;
        
        RebuildStat();
        unitStatContainer.Init(this);
        unitStatContainer.OnChanged += RaiseStatusChanged;
        InitSkill();
        InitNormalAttack();
    }

    private void InitSkill()
    {
        skill = null;
        if (CurrentUnitData == null) return;

        string skillId = CurrentUnitData.SkillId;
        if (string.IsNullOrEmpty(skillId)) return;

        skill = new Skill();
        if (!skill.Init(skillId, this)) skill = null;
        if(skill == null)
            Logger.LogWarning($"{CurrentUnitData.UnitId}의 스킬 '{skillId}'를 찾을 수 없습니다.");
    }

    
    public void SetStar(int star)
    {
        int prevStar = Star;
        Star = Mathf.Clamp(star, 0, GlobalDefines.MAX_VALUE.IG_MAX_STAR);
        
        RebuildStat();
        RestoreHp();
        OnStarChanged?.Invoke(Star);
    }

    public void RebuildStat()
    {
        UnbindResource();
        hp?.Dispose();
        sg?.Dispose();
        
        Stat = new UnitStat();
        Stat.Init(baseStat);

        if (Star > 0)
        {
            Stat.AddModifier(EStatType.Atk, new StatModifier(InGameBalance.GetAtkMultBonus(Star), EStatModifireType.PercentMult,0, InGameBalance.STAR_MODIFIER_SOURCE));
            Stat.AddModifier(EStatType.MaxHp, new StatModifier(InGameBalance.GetHpMultBonus(Star), EStatModifireType.PercentMult,0, InGameBalance.STAR_MODIFIER_SOURCE));
        }
        
        NotifyPerksStatRebuilt();

        hp = new ResourceStat(Stat.GetStatObject(EStatType.MaxHp));
        sg = new ResourceStat(Stat.GetStatObject(EStatType.SG), startEmpty: true);

        BindResource();
        PushSnapshot();
    }

    private void BindResource()
    {
        hp.OnValueChanged += HandleHp;
        sg.OnValueChanged += HandleSg;
    }

    private void UnbindResource()
    {
        if (hp != null) hp.OnValueChanged -= HandleHp;
        if (sg != null) sg.OnValueChanged -= HandleSg;
    }

    private void HandleHp(float prev, float cur) => OnHpChanged?.Invoke(cur, hp.Max);
    private void HandleSg(float prev, float cur) => OnSGChanged?.Invoke(cur, sg.Max);
    private void RaiseStatusChanged() => OnStatusChanged?.Invoke();
    
    public IEnumerable<StatusEffect> ActiveStatusEffects() => unitStatContainer.Visible();
    public void GetVisibleStatusEffects(List<StatusIconInfo> result) => unitStatContainer.GetVisible(result);
    public void PushSnapshot()
    {
        OnHpChanged?.Invoke(hp.Current, hp.Max);
        OnSGChanged?.Invoke(sg.Current, sg.Max);
        OnStatusChanged?.Invoke();
    }

    public void RestoreHp() => hp.Fill();

    public void TickStatus(float dt)
    {
        if (!IsAlive) return;
        unitStatContainer.UpdateEffect(dt);
    }

    public string DebugStatus()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var se in unitStatContainer.All())
        {
            string time = se.IsPermanent ? "영구" : $"{se.RemainingTime:F1}s";
            string extra = se is ShieldEffect sh ? $" 실드{sh.Amount:F1}" : "";
            sb.Append($"{se.Data.StatusId} x{se.Stack} ({time}){extra} ");
        }

        return sb.Length == 0 ? "-" : sb.ToString();
    }
    
    public void CheckDeath()
    {
        if (IsAlive || isDead) return;

        Die();

        isDead = !IsAlive;
    }

    /// <summary>
    /// 이 트리거에 걸린 스킬이면 발동합니다. SkillTriggerSystem만 부릅니다.
    /// OnDeath는 이미 죽은 뒤에 오므로 생존 검사에서 빼 줍니다.
    /// </summary>
    public void TryCastByTrigger(ETriggerType type)
    {
        if (skill == null || skill.TriggerType != type) return;

        bool ignoreAlive = type == ETriggerType.OnDeath;
        if (!ignoreAlive && !IsAlive) return;
        
        skill.TryCast(ignoreAlive);
    }

    /// <summary>
    /// 오프닝 동안 장전만 합니다. 발사는 하지 않고 1발분까지만 채웁니다.
    /// </summary>
    public void ChargeAttack(float dt)
    {
        if (!IsAlive) return;
        attackTimer = Mathf.Min(attackTimer + dt, AttckInterval);
    }

    /// <summary>
    /// SG가 찼을 때 공격 차례를 넘겨받을 스킬이 있는가.
    /// OnCast가 아닌 스킬(OnDeath 등)이나 횟수를 다 쓴 스킬은 차례를 가져가지 않습니다.
    /// </summary>
    private bool CanTakeAttackTurn
        => skill != null && skill.TriggerType == ETriggerType.OnCast && !skill.IsExhausted;

    public void TickAttack(float dt)
    {
        if (!IsAlive) return;
        if (unitStatContainer.IsControlled) return;

        attackTimer += dt;
        float interval = AttckInterval;
        if (attackTimer < interval) return;
        attackTimer -= interval;

        // SG가 차 있으면 이번 차례는 일반공격 대신 스킬만 나갑니다.
        if (sg.IsFull && CanTakeAttackTurn)
        {
            sg.Clear();
            OnAttackPerformed?.Invoke();
            InGameManagers.Battle.Trigger.Raise(ETriggerType.OnCast, this);
            return;
        }

        EAttackResult result = NormalAttack.TryPerpare(this, out PendingHit hit);
        if (result == EAttackResult.NoTarget) return;
        OnAttackPerformed?.Invoke();

        PublishAttackVfx(hit.Target);
        InGameManagers.Battle.Hits.Dispatch(hit);

        if (result == EAttackResult.Miss) return;

        InGameManagers.Battle.Trigger.Raise(ETriggerType.OnAttack, this);

        sg.Modify(Stat.GetStat(EStatType.SGChargeAmount));

        // 차례를 넘겨받을 스킬이 없는 유닛은 기존처럼 바로 비우고 OnCast만 알립니다(재능 반응용).
        if (sg.IsFull && !CanTakeAttackTurn)
        {
            sg.Clear();
            InGameManagers.Battle.Trigger.Raise(ETriggerType.OnCast, this);
        }
    }

    /// <summary>
    /// 피해를 받습니다. 순서: 보호 → 감전 → 실드 → HP → (미터기 기록) → 사망 → 가시 → OnHit.
    /// </summary>
    public void TakeDamage(float amount, BattleUnit attacker, EDamageSource source = EDamageSource.Skill,
        bool isCrit = false, StatusEffectData statusSource = null, DamageCredit credit = default)
    {
        if (amount <= 0f || !IsAlive) return;

        float origin = amount;

        if (!unitStatContainer.TryTakeDamage(amount, attacker, source,
                out float baseDamage, out float shockDamage, out float absorbed, out DamageCredit shockCredit))
        {
            OnDamageTaken?.Invoke(DamageReport.Blocked(source, statusSource));
            return;
        }
        
        if(attacker != null) lastAttacker = attacker;
        
        OnDamaged?.Invoke(source);
        
        // [미터기] 실드가 먹은 양을 본 피해 몫과 감전 몫으로 나눕니다.
        float absorbedBase = Mathf.Max(0f, amount - baseDamage);
        float absorbedShock = Mathf.Max(0f, absorbed - absorbedBase);

        // [미터기] 실제로 깎인 HP. Modify가 0에서 자르므로 오버킬은 여기서 빠집니다.
        float hpBefore = hp.Current;
        if(baseDamage > 0f) hp.Modify(-baseDamage);
        float lostBase = hpBefore - hp.Current;

        float lostShock = 0f;
        if (shockDamage > 0f && IsAlive)
        {
            //Logger.Log($"[감전] {RuntimeId} 추가 피해 {shockDamage:F1} (남은 HP {CurrentHp - shockDamage:F1})");
            float shockBefore = hp.Current;
            hp.Modify(-shockDamage);
            lostShock = shockBefore - hp.Current;
        }
        
        // [미터기] 확정된 피해를 기록합니다. 본 피해는 때린 사람(credit), 감전분은 건 사람(shockCredit) 몫입니다.
        InGameManagers.Battle?.Stats.RecordDamage(this, source,
            credit.OrUnit(attacker), lostBase + absorbedBase,
            shockCredit, lostShock + absorbedShock);
        
        OnDamageTaken?.Invoke(new DamageReport(baseDamage, shockDamage, absorbed, isCrit, source, statusSource));

        CheckDeath();
        
        if (source.IsDirect() && attacker != null && attacker.IsAlive)
        {
            float reflect = unitStatContainer.CalcReflect(origin);
            if (reflect > 0f)
            {
                //Logger.Log($"[가시] {RuntimeId} -> {attacker.RuntimeId} : {reflect:F1}");
                // [미터기] 반사 피해는 가시를 가진 이 유닛 몫(흰색). attacker는 기존처럼 null.
                attacker.TakeDamage(reflect, null, EDamageSource.Reflect, false, null, DamageCredit.FromUnit(this));
            }
        }
        
        if(IsAlive && source.IsDirect()) InGameManagers.Battle.Trigger.Raise(ETriggerType.OnHit, this, attacker);
    }

    public float TotalShield => unitStatContainer.GetTotalShield();

    public void ScaleShield(float multiplier) => unitStatContainer.ScaleShields(multiplier);
    
    /// <summary>
    /// 회복합니다. 최대 체력을 넘는 양은 버려집니다.
    ///
    /// [미터기] 실제로 찬 양(healed)을 회복을 넣은 유닛 몫으로 기록합니다.
    /// caster가 없으면 본인 몫입니다. 부활은 이 함수를 거치지 않으므로(Die에서 hp.Modify) 자연히 빠집니다.
    /// </summary>
    public void Heal(float amount, BattleUnit caster, StatusEffectData statusSource = null, bool isCrit = false)
    {
        if(amount <= 0f || !IsAlive) return;
        if (CurrentHp >= MaxHp) return;
        
        if (caster != null)
        {
            amount *= caster.Stat.GetStat(EStatType.HealMultiplier);
            if (amount <= 0f) return;
        }

        amount *= InGameManagers.Battle?.SuddenDeath.HealFactor ?? 1f;
        amount *= unitStatContainer.ConsumeHealAmp();

        float before = hp.Current;
        hp.Modify(amount);
        float healed = hp.Current - before;
        if (healed <= 0f) return;
        
        InGameManagers.Battle?.Stats.RecordHeal(caster ?? this, healed);
        
        OnHealed?.Invoke(healed, statusSource, isCrit);
        InGameManagers.Battle?.Trigger?.Raise(ETriggerType.OnHeal, this);
    }

    /// <summary>
    /// 상태이상을 겁니다.
    /// [미터기] credit 인자 추가. 시너지가 걸 때만 넘기고, 나머지는 비워 두면 caster 몫입니다.
    /// </summary>
    public void ApplyStatus(StatusEffectData data, BattleUnit caster, int stack, float? overrideValue = null,
        DamageCredit credit = default)
        => unitStatContainer.ApplyStatusEffect(data, caster, stack, overrideValue, credit);
    

    private void Die()
    {
        if (unitStatContainer.TryConsumeRevive(out float percent))
        {
            unitStatContainer.ClearDebuffs();
            hp.Modify(Stat.GetStat(EStatType.MaxHp) * percent * 0.01f);
            
            Logger.Log($"[부활] {RuntimeId} {percent:F0}% 부활 (HP {CurrentHp:F1})");
            return;
        }
        
        InGameManagers.Battle.Trigger.Raise(ETriggerType.OnDeath, this, lastAttacker);

        if (lastAttacker != null && lastAttacker.IsAlive)
            InGameManagers.Battle.Trigger.Raise(ETriggerType.OnKill, lastAttacker, this);

        
        unitStatContainer.ClearAll();
        sg.Clear();
        
        OnDied?.Invoke();
    }

    public void ResetForNext()
    {
        isDead = false;
        lastAttacker = null;
        attackTimer = 0f;
        
        unitStatContainer.ClearAll();
        RestoreOriginalSkill();
        skill?.ResetForNext();
        
        hp.Fill();
        sg.Clear();
    }

    public void GetActiveStatusData(List<StatusEffectData> result) => unitStatContainer.GetActiveData(result);

    public void Release()
    {
        UnbindResource();
        if (unitStatContainer != null)
            unitStatContainer.OnChanged -= RaiseStatusChanged;
        
        hp?.Dispose();
        sg?.Dispose();
        unitStatContainer?.ClearAll();
    }
}
