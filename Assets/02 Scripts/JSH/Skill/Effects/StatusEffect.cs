using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛에게 실제로 걸려 있는 상태이상 하나.
/// StatusEffectData는 정의(불변)이고, 이 클래스가 스택·남은 시간 같은 런타임 상태를 듭니다.
///
/// 무엇을 하는지는 파생 클래스가 압니다. UnitStatContainer는 보관·수명·순서만 담당합니다.
/// 파생은 StatusEffectFactory 를 통해서만 만들어집니다.
///
/// 이 인스턴스 자체를 StatModifier의 Source로 씁니다.
/// 해제할 때 Stat.RemoveAllModifier(this) 한 번이면 이 상태가 붙인 모디파이어가 전부 떨어집니다.
/// </summary>
public abstract class StatusEffect
{
    public StatusEffectData Data { get; private set; }
    
    //상태를 건 유닛. 시전자 스탯기준 계산에 사용
    public BattleUnit Caster { get; private set; }
    
    /// <summary>
    /// [미터기] 이 상태가 넣는 피해·실드를 누구 몫으로 기록할지.
    /// 대부분 Caster와 같지만, 시너지가 건 상태(화염 결속 시작 화상)는 Caster가 대표 유닛이고
    /// Credit은 시너지입니다. 계산은 Caster, 기록은 Credit을 봅니다.
    /// </summary>
    public DamageCredit Credit { get; private set; }
    
    public int Stack { get;  private set; }
    
    //DurationType이 Time일 때만 의미가 있는 시간
    public float RemainingTime { get; private set; }
    
    //다음 틱까지 남은 시간. DamageOverTime / HealOverTime 전용
    public float TickTimer { get; private set; }
    
    /// <summary>
    /// 스킬이 지정한 수치. null이면 StatusEffectData의 값을 씁니다.
    /// 실드처럼 스킬마다 수치가 다른 상태가 이 경로를 탑니다.
    /// 부여 시점에 굳히므로 시전자가 나중에 죽거나 스탯이 바뀌어도 흔들리지 않습니다.
    /// </summary>
    public float? OverrideValue {get; private set;}

    //영구적인지 확인 
    public bool IsPermanent => Data.DurationType != EDurationType.Time;
    //끝났는지 확인
    public bool IsExpired => !IsPermanent && RemainingTime <= 0f;

    public virtual bool IsConsumed => false;

    /// <summary>
    /// 첫 모디파이어의 기준이 대상 본인인지.
    /// MaxHp는 대상 값이라 매 틱 따라가야 하고,
    /// 그 외(Atk)는 시전자 값이라 부여 시점에 굳혀야 합니다.
    /// </summary>
    protected bool IsOwnerBased
    {
        get
        {
            var modifiers = Data.Modifiers;
            if (modifiers.Count == 0) return false;

            return modifiers[0].statType == EStatType.MaxHp;
        }
    }
    
    public void Init(StatusEffectData data, BattleUnit caster, int stack, float? overrideValue,
        DamageCredit credit = default)
    {
        Data = data;
        Caster = caster;
        Credit = credit.OrUnit(caster);
        OverrideValue = overrideValue;
        Stack = Mathf.Clamp(stack, 1, Mathf.Max(1, data.MaxStack));
        
        ResetDuration();
    }

    //부여/스택 변경/교체 시 스탯 모디파이어나 실드 누적
    public virtual void OnApply(BattleUnit owner) {}

    /// <summary>
    /// 이미 걸려 있는 상태에 같은 상태가 다시 걸려 중첩될 때 호출됩니다.
    /// 기본은 기존처럼 OnApply를 다시 부릅니다. (기존 상태이상들의 동작은 그대로)
    /// 시전자마다 수치가 다른 상태(쉴드)는 재정의해서 새 시전자의 값을 쓰면 됩니다.
    /// </summary>
    public virtual void OnStack(BattleUnit owner, BattleUnit caster, float? overrideValue, in DamageCredit credit) => OnApply(owner);
    
    //해제 시 OnApply가 붙인 것을 되돌림
    public virtual void OnDetach(BattleUnit owner) {}
    
    //TickInterval마다 DoT / HoT만 사용
    public virtual void OnTick(BattleUnit caster) {}

    /// <summary>
    /// 소유자가 공격에 성공했을 때. 공격 횟수에 반응하는 상태이상이 재정의합니다.
    /// 빗나간 공격은 오지 않습니다.
    /// </summary>
    public virtual bool OnOwnerAttack(BattleUnit owner, BattleUnit target) => false;

    // 남은 시간만 되돌리고 틱 타이머는 그대로 둡니다.
    // ResetDuration은 틱 타이머까지 되감아서, 틱 간격보다 자주 갱신되면 영원히 안 터집니다.
    public void RefreshDuration()
    {
        RemainingTime = IsPermanent ? -1f : Data.Duration;
    }
    
    public void ResetDuration()
    {
        RemainingTime = IsPermanent ? -1f : Data.Duration;
        //TickOnApply일 때 즉시 1틱 부여
        TickTimer = Data.TickOnApply ? 0f : Data.TickInterval;
    }

    //스택을 더하는 부분
    public bool AddStack(int amount)
    {
        int next = Mathf.Clamp(Stack + amount, 1, Mathf.Max(1, Data.MaxStack));
        if (next == Stack) return false;

        Stack = next;
        return true;
    }
    
    //스택을 지정된 값으로 덮어쓰는 부분
    public bool SetStack(int amount)
    {
        int next = Mathf.Clamp(amount, 0, Mathf.Max(1, Data.MaxStack));
        if (next == Stack) return false;

        Stack = next;
        return true;
    }
    
    //Replace용 시전자와 수치를 새 것으로 갈아끼움
    public void OverWrite(BattleUnit caster, int stack, float? overrideValue, DamageCredit credit = default)
    {
        Caster = caster;
        Credit = credit.OrUnit(caster);
        OverrideValue = overrideValue;
        Stack = Mathf.Clamp(stack, 1, Mathf.Max(1, Data.MaxStack));
        ResetDuration();
    }

    /// 같은 상태가 다시 걸렸을 때의 지속시간 갱신.
    /// 만료가 없는 상태(Battle/Count)는 틱 타이머를 유지하고,
    /// 시간제는 틱과 만료를 다시 맞춥니다.
    public void RenewDuration()
    {
        if(IsPermanent) RefreshDuration();
        else ResetDuration();
    }

    public void Tick(float dt)
    {
        if (!IsPermanent) RemainingTime -= dt;
        if (Data.TickInterval > 0f) TickTimer -= dt;
    }

    //틱이 찼는지 확인하고 되감는 부분
    public bool ConsumeTick()
    {
        if (Data.TickInterval <= 0f) return false;
        if (TickTimer > 0f) return false;

        TickTimer += Data.TickInterval;
        return true;
    }
    
    /// <summary>
    /// 모디파이어 하나의 수치를 기준 스탯으로 환산합니다.
    ///
    /// 기준을 누구 것으로 볼지가 갈립니다.
    ///  - MaxHp : 대상 본인. 화상·재생처럼 "최대체력의 몇 %"는 맞는 쪽 기준이 자연스럽습니다.
    ///  - 그 외 : 시전자. 출혈·지옥불의 Atk 300%/500%는 건 사람의 공격력이 기준입니다.
    ///
    /// OverrideValue는 첫 모디파이어에만 적용합니다.
    /// 광폭처럼 여러 줄인 경우 두 번째부터는 정의값을 그대로 씁니다.
    /// </summary>
    protected float CalcAmount(BattleUnit owner, int idx = 0)
    {
        var modifiers = Data.Modifiers;
        if (modifiers.Count <= idx) return 0f;

        StatusModifierEntry entry = modifiers[idx];

        float baseStat = 0f;
        if (entry.modifierType == EValueModifierType.StatPercent)
        {
            if (entry.statType == EStatType.MaxHp)
            {
                baseStat = owner.Stat.GetStat(EStatType.MaxHp);
            }
            else if (Caster != null)
            {
                baseStat = Caster.Stat.GetStat(entry.statType);
            }
            else return 0f;
        }

        return Data.CalcModifierAmount(entry, baseStat, idx == 0 ? OverrideValue : null);
    }
    
    //첫 모디파이어의 원시 수치, 퍼센트를 직접 쓰는 쪽이 사용
    protected float RawValue()
    {
        var modifiers = Data.Modifiers;
        if (modifiers.Count == 0) return 0f;

        return OverrideValue ?? modifiers[0].modifierValue;
    }
}
