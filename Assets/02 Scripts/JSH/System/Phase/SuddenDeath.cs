using System;
using System.Collections.Generic;
using UnityEngine;


public class SuddenDeath
{
    public static readonly object Source = new();

    private const float StartTime = 30f;
    private const float TimeLimit = 3600f;
    
    private const float DamagePerStack = 0.1f;
    private const float AtkSpeedStack = 0.05f;
    private const int MaxAtkSpeedStack = 10;
    private const float HealCutPerStack = 0.1f;
    private const int MaxHealCutStack = 7; // -70% 상한
    private const float EnvDamagePerStack = 0.01f;
    
    /// <summary>최대 중첩 수. 뷰가 연출 강도(0~1)를 구할 때 씁니다.</summary>
    public const int MaxStack = (int)(TimeLimit - StartTime);

    private List<BattleUnit> units;
    private float elapsed;
    
    public int Stack { get; private set; }
    public bool IsTimeUp => elapsed >= TimeLimit;

    public float DamageFactor => 1f + Stack * DamagePerStack;
    public float AtkSpeedFactor => 1f + Math.Min(Stack, MaxAtkSpeedStack) * AtkSpeedStack;
    public float HealFactor => 1f - Mathf.Min(Stack, MaxHealCutStack) * HealCutPerStack;

    /// <summary>중첩이 바뀔 때마다(0 포함) 발행. 맵의 SuddenDeathEnvView가 구독합니다.</summary>
    public event Action<int> OnStackChanged;

    public void Reset(List<BattleUnit> battleUnits)
    {
        units = battleUnits;
        elapsed = 0f;
        Stack = 0;
    }

    public void Tick(float dt)
    {
        if (units == null) return;
        
        elapsed += dt;
        while (Stack < MaxStack && elapsed - StartTime >= Stack + 1) 
            AddStack();
    }

    private void AddStack()
    {
        Stack++;
        
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit u = units[i];
            if (!u.IsAlive) continue;

            float dmg = u.Stat.GetStat(EStatType.MaxHp) * Stack * EnvDamagePerStack;
            
            u.TakeDamage(dmg, null, EDamageSource.Environment);
        }
        
        OnStackChanged?.Invoke(Stack);
    }
    
    
    /// <summary>
    /// BattleManager.Release에서 호출합니다. 다음 라운드를 위해 상태를 비우고
    /// 뷰에 0을 알려 이펙트를 페이드아웃시킵니다.
    /// </summary>
    public void Release()
    {
        bool wasActive = Stack > 0;
        
        units = null;
        elapsed = 0f;
        Stack = 0;
        
        if (wasActive) OnStackChanged?.Invoke(0);
    }
}
