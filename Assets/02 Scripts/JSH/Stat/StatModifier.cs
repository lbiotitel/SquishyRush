
using System;

public enum EStatModifireType
{
    Add,
    PercentAdd,
    PercentMult,
    FinalAdd,  // 모든 배율을 곱한 뒤 더하는 고정값
} 


public class StatModifier
{
    public readonly float Value;
    public readonly EStatModifireType Type;
    public readonly int Order;
    public readonly object Source;

    public StatModifier(float value, EStatModifireType type, int order, object source)
    {
        Value = value;
        Type = type;
        Order = order;
        Source = source;
    }
}

