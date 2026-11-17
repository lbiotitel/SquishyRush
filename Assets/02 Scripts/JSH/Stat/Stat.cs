using System;
using System.Collections.Generic;
using UnityEngine;

public class Stat
{
    private readonly float baseValue;
    private readonly List<StatModifier> modifiers = new();
    
    private float lastValue;
    private bool isDirty = true;

    public event Action<float, float> OnValueChanged;
    
    public float Value
    {
        get
        {
            if (!isDirty) return lastValue;

            float prev = lastValue;
            lastValue = CalcModifier(); 
            isDirty = false;         
        
            if(!Mathf.Approximately(prev, lastValue))
                OnValueChanged?.Invoke(prev, lastValue);
            
            return lastValue;
        }
    }

    public Stat(float baseValue)
    {
        this.baseValue = baseValue;
        lastValue = baseValue;
    }

    public void AddModifier(StatModifier statModifier)
    {
        modifiers.Add(statModifier);
        MarkDirty();
    }

    public void RemoveModifier(StatModifier modifier)
    {
        if (modifiers.Remove(modifier)) MarkDirty();
    }

    public void RemoveAllModifier(object source)
    {
        int removed = modifiers.RemoveAll(x => x.Source == source);
        if (removed > 0) MarkDirty();
    }

    public IReadOnlyList<StatModifier> GetAllModifiers() => modifiers;

    private float CalcModifier()
    {
        if(modifiers.Count == 0) return Mathf.Max(baseValue, 0f);
        
        modifiers.Sort((a, b) => a.Order.CompareTo(b.Order));
        return Calculate(null);
    }
    
    public float CalculateWith(Predicate<StatModifier> include) => Calculate(include);
    
    private float Calculate(Predicate<StatModifier> include)
    {
        float addSum = 0f;
        float percentAddSum = 0f;
        float percentMultRes = 1f;
        float finalAddSum = 0f;

        foreach (var modifire in modifiers)
        {
            if (include != null && !include(modifire)) continue;
            
            switch(modifire.Type)
            {
                case EStatModifireType.Add:
                    addSum += modifire.Value;
                    break;
                case EStatModifireType.PercentAdd:
                    percentAddSum += modifire.Value;
                    break;
                case EStatModifireType.PercentMult:
                    percentMultRes *= (1f + modifire.Value);
                    break;
                case EStatModifireType.FinalAdd:
                    finalAddSum += modifire.Value;
                    break;
            }
        }

        float result = (baseValue + addSum) * (1f + percentAddSum) * percentMultRes + finalAddSum;

        return Mathf.Max(result, 0f);
    }

    private void MarkDirty()
    {
        isDirty = true;
        _ = Value;
    }
}