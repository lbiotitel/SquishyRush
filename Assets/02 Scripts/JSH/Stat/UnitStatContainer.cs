using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>UI 아이콘 한 칸. 같은 상태의 인스턴스를 묶은 결과입니다.</summary>
public readonly struct StatusIconInfo
{
    public readonly StatusEffectData Data;
    public readonly int Stack;

    public Sprite Icon => Data.Icon;

    public StatusIconInfo(StatusEffectData data, int stack)
    {
        Data = data;
        Stack = stack;
    }
}

/// <summary>
/// 한 유닛에게 걸린 상태이상 전부를 보관하고 매 틱 갱신합니다.
///
/// 무엇을 하는지는 각 StatusEffect 파생이 알고, 여기는 세 가지만 담당합니다.
///   - 보관   타입별 버킷
///   - 수명   부여·중첩·만료·해제
///   - 순서   피격 시 보호 -> 감전 -> 실드 -> HP -> 가시
///
/// 조회는 StatusEffectData 참조 비교로 합니다.
/// DataTableManager가 로드 시점에 한 번만 Instantiate하고 그 인스턴스를 계속 돌려주므로,
/// 같은 StatusID면 런타임 내내 같은 객체입니다.
/// </summary>
public class UnitStatContainer
{
    private BattleUnit owner;
    
    private readonly Dictionary<EStatusEffectType, List<StatusEffect>> dict = new();

    //Control 계열 개수. TickAttack에서 전 유닛이 매 프레임 읽어서 카운터로 듭니다.
    private int controlCount;

    // 틱 순회용 복사본. 틱 도중 원본 리스트가 바뀌어도 순회가 깨지지 않게 합니다.
    private readonly List<StatusEffect> tickBuffer = new();
    
    public event Action OnChanged;
    
    public void Init(BattleUnit owner)
    {
        this.owner = owner;
        
        // 전투 중 dict에 새 키가 추가되면 순회 중 예외가 나므로 모든 타입의 버킷을 미리 만듭니다.
        foreach(EStatusEffectType type in Enum.GetValues(typeof(EStatusEffectType)))
            if (!dict.ContainsKey(type)) dict[type] = new List<StatusEffect>();
        
        ClearAll();
    }
    
    //스턴 등으로 행동이 막혔는지 확인
    public bool IsControlled => controlCount  > 0;

    #region 조회

    public List<StatusEffect> GetByType(EStatusEffectType type)
        => dict.TryGetValue(type, out var list) && list.Count > 0 ? list : null;

    public bool HasType(EStatusEffectType type) => GetByType(type) != null;
    
    
    //특정 상태이상이 걸려있는지 찾음. data가 자기 타입을 알아서 버킷 하나만 봄.
    public StatusEffect Find(StatusEffectData data)
    {
        if (data == null) return null;
        if (!dict.TryGetValue(data.StatusEffectType, out var list)) return null;

        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i].Data, data)) return list[i];
        }

        return null;
    }

    //첫 인스턴스 스택만 반환 → 같은 상태 전부 합산
    //같은 상태의 스택 합. Independent는 인스턴스가 여러 개라 전부 더합니다.
    public int GetStack(StatusEffectData data)
    {
        if (data == null) return 0;
        if (!dict.TryGetValue(data.StatusEffectType, out var list)) return 0;

        int total = 0;
        for(int i = 0; i < list.Count; i++)
            if (ReferenceEquals(list[i].Data, data)) total += list[i].Stack;

        return total;
    }
    
    //실드 총량 필요한 얘들이 부름
    public float GetTotalShield()
    {
        List<StatusEffect> list = GetByType(EStatusEffectType.Shield);
        if (list == null) return 0f;
        
        float total = 0f;
        for (int i = 0; i < list.Count; i++) total += ((ShieldEffect)list[i]).Amount;
        return total;
    }

    public void ScaleShields(float multiplier)
    {
        List<StatusEffect> list = GetByType(EStatusEffectType.Shield);
        if (list == null || list.Count == 0) return;

        float before = GetTotalShield();
        
        for(int i =0; i<list.Count; i++)
            ((ShieldEffect)list[i]).Scale(multiplier);

        float gained = GetTotalShield() - before;
        if(gained > 0f)
            InGameManagers.Battle?.Stats.RecordShieldGrant(DamageCredit.FromUnit(owner), gained);
        
        OnChanged?.Invoke();
    }

    /// <summary>
    /// 부활 상태 중 회복 비율이 가장 큰 것을 하나 소모합니다.
    ///
    /// 큰 것부터 쓰는 이유는 30%로 살아나면 바로 다시 죽기 때문입니다.
    /// 오토배틀러라 플레이어가 아껴 쓸 수단이 없으니 항상 유리한 쪽을 먼저 씁니다.
    /// </summary>
    public bool TryConsumeRevive(out float hpPercent)
    {
        hpPercent = 0f;

        List<StatusEffect> list = GetByType(EStatusEffectType.Revive);
        if (list == null || list.Count == 0) return false;

        ReviveEffect best = null;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is not ReviveEffect revive || revive.Stack <= 0) continue;
            if (best == null || revive.HpPercent > best.HpPercent) best = revive;
        }

        if (best == null || !best.TryConsume()) return false;

        hpPercent = best.HpPercent;
        
        if(best.Data.VfxMode == EStatusVfxMode.OnTrigger)
            PublishStatusVfx(best.Data);
        
        if (best.IsConsumed) Remove(best.Data);

        return true;
    }

    public void ClearDebuffs()
    {
        List<StatusEffectData> targets = new List<StatusEffectData>();

        foreach (StatusEffect e in All())
            if (e.Data.StatusType == EStatusType.Debuff) targets.Add(e.Data);
        
        for (int i = 0; i < targets.Count; i++) Remove(targets[i]);
    }
    
    //디버그용 전체 순회. 매 프레임 호출x    
    public IEnumerable<StatusEffect> All()
    {
        foreach (var pair in dict)
        {
            List<StatusEffect> list = pair.Value;
            for (int i = 0; i < list.Count; i++) yield return list[i];
        }
    }


    public IEnumerable<StatusEffect> Visible()
    {
        foreach (var pair in dict)
        {
            var list = pair.Value;
            for (int i =0; i < list.Count; i++)
                if (list[i].Data.Icon != null)
                    yield return list[i];
        }
    }

    /// <summary>
    /// 아이콘 있는 상태이상을 result에 채움. yield 방식과 달리 할당 없음 (UI 갱신용).
    /// 같은 상태의 인스턴스(Independent)는 아이콘 하나로 묶고 스택을 합칩니다.
    /// </summary>
    public void GetVisible(List<StatusIconInfo> result)
    {
        result.Clear();
        foreach (var pair in dict)
        {
            var list = pair.Value;
            for (int i = 0; i < list.Count; i++)
            {
                StatusEffect se = list[i];
                if (se.Data.Icon == null) continue;

                int idx = IndexOf(result, se.Data);
                if (idx < 0) result.Add(new StatusIconInfo(se.Data, se.Stack));
                else result[idx] = new StatusIconInfo(se.Data, result[idx].Stack + se.Stack);
            }
        }
    }

    private static int IndexOf(List<StatusIconInfo> list, StatusEffectData data)
    {
        for(int i = 0; i < list.Count; i++)
            if (ReferenceEquals(list[i].Data, data)) return i;

        return -1;
    }


    public void GetActiveData(List<StatusEffectData> result)
    {
        result.Clear();
        foreach ( var pair in dict)
            for(int i = 0; i < pair.Value.Count; i++)
                if(!result.Contains(pair.Value[i].Data)) result.Add(pair.Value[i].Data);
    }
    
    #endregion


    #region 부여

    /// <summary>
    /// 상태이상을 부여합니다.
    /// overrideValue가 있으면 StatusEffectData의 ModifierValue 대신 그 값을 씁니다.
    /// ApplyStatus 행에서만 넘어옵니다.
    /// Damage/Heal 행의 ModifierValue는 피해 계수라 여기로 오면 안 됩니다.
    ///
    /// [미터기] credit 인자 추가. 맨 앞에서 비어 있으면 caster 몫으로 채운 뒤
    /// 새로 만들 때(Create), 교체·갱신할 때(OverWrite), 중첩할 때(OnStack) 그대로 넘깁니다.
    /// </summary>
    public void ApplyStatusEffect(StatusEffectData data, BattleUnit caster, int stack, float? overrideValue = null,
        DamageCredit credit = default)
    {
        if (data == null || owner == null || !owner.IsAlive) return;

        credit = credit.OrUnit(caster);
        
        //IndependentRefresh / IndependentIgnore는 시전자별로 인스턴스가 따로 있습니다.
        //  같은 시전자가 다시 걸면 → Refresh: 남은 시간만 갱신(틱 타이머 유지) / Ignore: 무시
        //  다른 시전자가 걸면     → 새 인스턴스. 상한이면 가장 먼저 끝날 것을 밀어냅니다.
        bool independent = data.StackBehavior.IsIndependent();
        if (independent)
        {
            StatusEffect mine = FindByCaster(data, caster);
            if (mine != null)
            {
                if (data.StackBehavior == EStackBehavior.IndependentRefresh)
                {
                    mine.RefreshDuration();
                    Logger.Log($"[상태이상] {owner.RuntimeId} 갱신 {data.StatusId} (from {caster?.RuntimeId})");
                }
                else Logger.Log($"[상태이상] {owner.RuntimeId} 무시 {data.StatusId} - 같은 시전자 (from {caster?.RuntimeId})");

                return;
            }
            
            EvictIfFull(data);
        }

        StatusEffect exist = independent ? null : Find(data);
        
        if (exist != null)
        {
            switch (data.StackBehavior)
            {
                case EStackBehavior.Ignore:
                    return;
                
                case EStackBehavior.Refresh:
                    exist.RefreshDuration();
                    if (overrideValue.HasValue &&
                        (!exist.OverrideValue.HasValue ||
                         Mathf.Abs(overrideValue.Value) > Mathf.Abs(exist.OverrideValue.Value)))
                    {
                        exist.OverWrite(caster, exist.Stack, overrideValue, credit);
                        exist.OnApply(owner);
                        OnChanged?.Invoke();
                    }
                    Logger.Log($"[상태이상] {owner.RuntimeId} 갱신 {data.StatusId} (총 {exist.Stack})");
                    return;
                
                case EStackBehavior.Replace:
                    exist.OverWrite(caster, stack, overrideValue, credit);
                    exist.OnApply(owner);
                    Logger.Log($"[상태이상] {owner.RuntimeId} 교체 {data.StatusId} (총 {exist.Stack})");
                    OnChanged?.Invoke();
                    return;    
                
                default: //Add
                    int before = exist.Stack;
                    bool changed = exist.AddStack(stack);

                    exist.RenewDuration();
                    
                    exist.OnStack(owner, caster, overrideValue, credit);
                    
                    if (changed)
                    {
                        Logger.Log($"[상태이상] {owner.RuntimeId} 중첩 {data.StatusId} {before} -> {exist.Stack} (from {caster?.RuntimeId})");
                        OnChanged?.Invoke();
                    }
                    else  Logger.Log($"[상태이상] {owner.RuntimeId} 중첩 상한 {data.StatusId} ({data.MaxStack})");

                    return;
            }
        }

        StatusEffect se = StatusEffectFactory.Create(data, caster, stack, overrideValue, credit);
        if (se == null) return;
        
        GetOrCreateBucket(data.StatusEffectType).Add(se);
        if (data.StatusEffectType == EStatusEffectType.Control) controlCount++;

        se.OnApply(owner);
        
        //Logger.Log($"[상태이상] {owner.RuntimeId} <- {data.StatusId} (총 {se.Stack})");
        OnChanged?.Invoke();
    }
    
    //상태이상을 즉시 해제. Independent처럼 같은 상태가 여러 개면 전부 해제합니다.
    public void Remove(StatusEffectData data)
    {
        if (data == null) return;
        if (!dict.TryGetValue(data.StatusEffectType, out var list)) return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(list[i].Data, data))
            {
                RemoveAt(list, i);
            }
        }
    }

    //같은 데이터 + 같은 시전자인 인스턴스. Independent 계열 전용
    private StatusEffect FindByCaster(StatusEffectData data, BattleUnit caster)
    {
        if (!dict.TryGetValue(data.StatusEffectType, out var list)) return null;

        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i].Data, data) && ReferenceEquals(list[i].Caster, caster))
                return list[i];
        }

        return null;
    }

    /// <summary>
    /// Independent 계열 상한 처리. 같은 상태 인스턴스가 MaxStack개 이상이면 남은 시간이 가장 짧은 것을 해제합니다.
    /// </summary>
    private void EvictIfFull(StatusEffectData data)
    {
        if (!dict.TryGetValue(data.StatusEffectType, out var list)) return;

        int max = Mathf.Max(1, data.MaxStack);
        int count = 0;
        int oldest = -1;

        for (int i = 0; i < list.Count; i++)
        {
            if (!ReferenceEquals(list[i].Data, data)) continue;
            count++;
            if (oldest < 0 || list[i].RemainingTime < list[oldest].RemainingTime) oldest = i;
        }

        if (count >= max && oldest >= 0)
        {
            Logger.Log($"[상태이상] {owner.RuntimeId} {data.StatusId} 상한({max}) - 가장 오래된 것 교체");
            RemoveAt(list, oldest);
        }
    }

    public void ClearAll()
    {
        foreach (var pair in dict)
        {
            List<StatusEffect> list = pair.Value;
            for (int i = 0; i < list.Count; i++) list[i].OnDetach(owner);
            list.Clear();
        }

        controlCount = 0;
        OnChanged?.Invoke();
    }
    #endregion

    #region 갱신

    public void UpdateEffect(float dt)
    {
        // 프레임당 한 번 규칙(tickVfxPublished)은 이 호출 단위로 다시 셉니다.
        tickVfxPublished.Clear();
       
        foreach (var pair in dict)
        {  
            List<StatusEffect> list = pair.Value;
            if(list.Count == 0) continue;
            
            tickBuffer.Clear();
            tickBuffer.AddRange(list);
            
            // 원본 대신 복사본을 돌아서, 도는 중에 원본에서 지워도 순회가 안밀림
            for (int i = 0; i < tickBuffer.Count; i++)
            {
                StatusEffect se = tickBuffer[i];

                // 앞선 틱의 여파로 이미 빠졌으면 건너뜁니다. (사망 ClearAll, 부활 ClearDebuffs 등)
                if (!list.Contains(se)) continue;
                
                se.Tick(dt);

                // 이번 프레임에 틱이 한 번이라도 났는지.
                // 연출은 while 안이 아니라 밖에서 한 번만 냅니다.
                // dt가 큰 프레임(렉, 배속)에서 틱이 2~3번 밀려 나오면
                // 같은 자리에 연출이 겹쳐 터져 하나처럼 보이기만 하고 풀만 낭비합니다.
                bool ticked = false;
                
                // 틱을 먼저 처리하고 만료를 검사합니다.
                // 출혈은 Duration 5 / TickInterval 5 라서 만료되는 그 프레임에 유일한 틱이 터집니다.
                // dt가 큰 프레임에서 두 번 이상 밀렸을 수 있으므로 while로 비웁니다.
                while (se.ConsumeTick())
                {
                    se.OnTick(owner);
                    ticked = true;
                    
                    // 틱 피해로 죽었으면 ClearAll로 전부 비워졌으니 더 볼 게 없습니다.
                    // 단, 마지막 화상에 죽는 장면은 보여야 하므로 연출을 먼저 내고 나갑니다.
                    // (Data 참조는 ClearAll 뒤에도 살아 있어서 안전합니다)
                    if (!owner.IsAlive)
                    {
                        PublishTickVfx(se.Data);
                        return;
                    }
                }
                
                if(ticked) PublishTickVfx(se.Data);
                
                if (se.IsExpired || se.IsConsumed)
                {
                    int idx = list.IndexOf(se);
                    if(idx >= 0) RemoveAt(list, idx);
                }
            }

            //틱 피해로 죽었으면 남은 상태이상을 더 굴릴 필요가 없음
            if (!owner.IsAlive) return;
        }
    }
    
    #endregion


    #region 코어

    private List<StatusEffect> GetOrCreateBucket(EStatusEffectType type)
    {
        if (!dict.TryGetValue(type, out var list))
        {
            list = new List<StatusEffect>();
            dict[type] = list;
        }

        return list;
    }

    private void RemoveAt(List<StatusEffect> list, int idx)
    {
        StatusEffect se = list[idx];
        
        se.OnDetach(owner);
        list.RemoveAt(idx);

        if (se.Data.StatusEffectType == EStatusEffectType.Control) controlCount--;
        
        //Logger.Log($"[상태이상] {owner.RuntimeId} 해제 {se.Data.StatusId}");
        OnChanged?.Invoke();
    }
    
    #endregion

    #region 피해 파이프라인

    /// <summary>
    /// 피격 시 피해량을 가공합니다. 순서가 여기 적혀 있고, 계산은 각 상태이상이 합니다.
    ///
    ///   1. 보호   피해를 통째로 무시하고 스택 1 감소. false를 돌려 즉시 끝냅니다.
    ///   2. 감전   공격자 공격력 기준 추가 피해를 계산합니다.
    ///   3. 실드   기본과 감전을 합쳐서 흡수합니다.
    ///
    /// 기본과 감전을 나눠서 돌려주는 것은 HP 차감을 두 번에 나누기 위해서입니다.
    /// 감전이 기본 피해에 묻히지 않고 별도 수치로 보이게 하려는 것입니다.
    /// 가시는 HP가 깎인 뒤라 여기 없습니다.
    /// 
    /// [미터기] shockCredit: 감전 추가 피해를 누구 몫으로 기록할지.
    /// 감전을 "건 사람" 몫입니다(감전이 없었으면 없는 피해이므로). 때린 사람 몫이 아닙니다.
    /// 감전 인스턴스가 여럿이면 각자 낸 추가 피해 비율로 나눕니다(Split).
    /// 감전이 없으면 빈 영수증입니다.
    /// 반환된 Split은 이 컨테이너의 버퍼를 빌려 쓰므로, 다음 TryTakeDamage 전에 기록을 끝내야 합니다.
    /// (BattleUnit.TakeDamage가 HP 차감 직후 바로 기록하므로 안전합니다)
    /// </summary>
    public bool TryTakeDamage(float amount, BattleUnit attacker, EDamageSource source,
        out float baseDamage, out float shockDamage, out float absorbed, out DamageCredit shockCredit)
    {
        baseDamage = 0f;
        shockDamage = 0f;
        absorbed = 0f;
        shockCredit = default;
        
        if (source != EDamageSource.Environment && TryConsumeProtection())
        {
            //Logger.Log($"[보호] {owner.RuntimeId} 피해 무시 ({amount:F1})");
            return false;
        }

        float shock = 0f;
        if (source.IsDirect() && attacker != null)
        {
            shock = CalcShockBonus(attacker);
            if (shock > 0f) shockCredit = DamageCredit.FromSplit(shockCreditBuffer, shockWeightBuffer);
        }

        float total = amount + shock;
        float remain = AbsorbByShields(total);

        absorbed = total - remain;
        
        baseDamage = Mathf.Min(remain, amount);
        shockDamage = remain - baseDamage;

        return true;
    }
    
    /// <summary>
    /// 가시 반사량. 실드가 얼마나 막았든 상관없이 원본 피해 기준입니다.
    /// 실드 두꺼운 탱커가 가시를 들면 반사가 0이 되는 것을 막으려는 것입니다.
    /// </summary>
    public float CalcReflect(float originDamage)
    {
        List<StatusEffect> list = GetByType(EStatusEffectType.Reflect);
        if (list == null) return 0f;

        float total = 0f;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var reflect = (ReflectEffect)list[i];
            total += reflect.Consume(originDamage);
            
            if(reflect.IsConsumed) RemoveAt(list, i);
        }
        
        return total;
    }

    public float ConsumeHealAmp()
    {
        if (!dict.TryGetValue(EStatusEffectType.HealAmp, out var list) || list.Count == 0) return 1f;

        float percent = 0f;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] is not HealAmpEffect amp) continue;

            percent += amp.Consume();
            
            if(amp.IsConsumed) RemoveAt(list, i);
            else OnChanged?.Invoke();
        }

        return 1f + percent * 0.01f;
    }

    //보호 스택 차감
    private bool TryConsumeProtection()
    {
        List<StatusEffect> list = GetByType(EStatusEffectType.DamageReduction);
        if (list == null) return false;

        var protection = (ProtectionEffect)list[0];
        bool consumed = protection.TryConsume();
        
        if (protection.IsConsumed) Remove(protection.Data);
        
        return consumed;
    }

    // [미터기] 감전 인스턴스별 몫과 추가 피해. 피격마다 new 하지 않도록 재사용합니다.
    private readonly List<DamageCredit> shockCreditBuffer = new();
    private readonly List<float> shockWeightBuffer = new();

    /// <summary>
    /// 감전 추가 피해 합계. 공식은 기존 그대로(때린 사람 공격력 × 감전 % × 스택)입니다.
    /// [미터기] 계산하면서 인스턴스마다 (건 사람 몫 = 그 감전의 Credit, 그 인스턴스가 낸 추가 피해)를
    /// 버퍼에 적어 둡니다. TryTakeDamage가 이 버퍼로 Split 영수증을 만듭니다.
    /// </summary>
    private float CalcShockBonus(BattleUnit attacker)
    {
        shockCreditBuffer.Clear();
        shockWeightBuffer.Clear();
        
        List<StatusEffect> list = GetByType(EStatusEffectType.OnDamagedBonus);
        if (list == null) return 0f;
        
        float total = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            float bonus = ((ShockEffect)list[i]).CalcBonus(attacker);
            if(bonus <= 0f) continue;

            total += bonus;
            shockCreditBuffer.Add(list[i].Credit);
            shockWeightBuffer.Add(bonus);
        }

        return total;
    }

    //실드로 흡수하고 남은 피해 돌려줌
    private float AbsorbByShields(float amount)
    {
        List<StatusEffect> list = GetByType(EStatusEffectType.Shield);
        if (list == null) return amount;

        for (int i = list.Count - 1; i >= 0 && amount > 0f; i--)
        {
            var shield = (ShieldEffect)list[i];
            float before = shield.Amount;

            amount = shield.Absorb(amount);

            if (before > shield.Amount)
            {
                Logger.Log($"[실드] {owner.RuntimeId} {before:F1} -> {shield.Amount:F1}");
                OnChanged?.Invoke();
            }

            if(shield.IsConsumed) RemoveAt(list, i);
        }

        return amount;
    }

    public void NotifyOwnerAttacked(BattleUnit owner, BattleUnit target)
    {
        bool changed = false;
        foreach (StatusEffect e in All()) 
            changed |= e.OnOwnerAttack(owner, target);
        
        if(changed) OnChanged?.Invoke();
    }
    
    #endregion

    #region 상태이상 연출

    // 연출 요청 객체는 컨테이너마다 하나를 재사용합니다. (SkillEffect.vfxEvent와 같은 방식)
    // VfxSystem이 받는 즉시 좌표로 바꾸고 보관하지 않으므로, 다음 발행에서 덮어써도 안전합니다.
    // 틱마다 new를 하면 화상 걸린 유닛 수 × 초당 틱 수만큼 GC가 쌓입니다.
    private readonly OnVfxRequestedEventData vfxEvent = new();
    private readonly List<BattleUnit> vfxTargets = new(1);

    // 이번 UpdateEffect 한 번 동안 이미 연출을 낸 상태이상.
    //
    // 같은 화상이 시전자별로 따로 걸려 있으면(독립 중첩) 같은 프레임에 틱이 여러 번 납니다.
    // 피해는 각각 들어가야 맞지만, 연출까지 겹쳐 터지면 한 덩어리로 뭉개져 보이기만 합니다.
    // 그래서 "이 유닛, 이 상태이상, 이 프레임"에는 한 번만 냅니다.
    private readonly List<StatusEffectData> tickVfxPublished = new();
    
    /// <summary>
    /// 틱형(화상·독·출혈·재생) 연출. 모드가 OnTick인 것만, 프레임당 한 번만 냅니다.
    /// </summary>
    private void PublishTickVfx(StatusEffectData data)
    {
        if (data == null || data.VfxMode != EStatusVfxMode.OnTick) return;
        if (tickVfxPublished.Contains(data)) return;

        tickVfxPublished.Add(data);
        PublishStatusVfx(data);
    }


    /// <summary>
    /// 이 유닛 자리에서 연출을 한 번 터뜨려 달라고 요청합니다.
    ///
    /// 로직은 VfxSystem을 직접 부르지 않고 이벤트만 던집니다.
    /// 이벤트 발행은 전투 상태를 하나도 바꾸지 않으므로 결정론적 리플레이에 영향이 없습니다.
    /// (OnVfxRequestedEventData 주석 참고)
    ///
    /// 상태이상 연출은 Impact(대상 위에서 터짐)라 VfxSystem이 Caster를 쓰지 않습니다.
    /// 그래도 비워 두지 않고 owner를 넣습니다. 나중에 수신 측에서 Caster를 보는 코드가 생겨도
    /// "이 연출의 주인은 걸려 있는 이 유닛"으로 자연스럽게 읽히게 하려는 것입니다.
    /// </summary>
    private void PublishStatusVfx(StatusEffectData data)
    {
        if (string.IsNullOrEmpty(data.VisualEffectId)) return;
        
        vfxTargets.Clear();
        vfxTargets.Add(owner);
        
        vfxEvent.VisualEffectId = data.VisualEffectId;
        vfxEvent.Caster = owner;
        vfxEvent.Targets = vfxTargets;
        
        GlobalManagers.Event?.NotifyEvent(vfxEvent);
    }

    #endregion
}
