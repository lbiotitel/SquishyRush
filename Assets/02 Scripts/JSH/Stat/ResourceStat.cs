using System;
using UnityEngine;

public class ResourceStat
{
    private readonly Stat max;
    private float current;

    public event Action<float, float> OnValueChanged;
    
    public float Current => current;
    public float Max => max.Value;
    public bool IsEmpty => current <= 0f;
    public bool IsFull => current >= max.Value;
    public float Ratio => max.Value > 0f ? Mathf.Clamp01(current / max.Value) : 0f;

    public ResourceStat(Stat max, bool startEmpty = false)
    {
        this.max = max;
        current = startEmpty ? 0f : max.Value;

        max.OnValueChanged += OnMaxChanged;
    }

    public void Dispose()
    {
        max.OnValueChanged -= OnMaxChanged;
    }
    
    public void Modify(float delta)
    {
        SetCurrent(current + delta);
    }

    public void Fill() => SetCurrent(max.Value);

    public void Clear() => SetCurrent(0f);

    private void SetCurrent(float next)
    {
        float prev = current;
        current = Mathf.Clamp(next, 0f, max.Value);
        
        if(!Mathf.Approximately(prev, current))
            OnValueChanged?.Invoke(prev, current);
    }

    private void OnMaxChanged(float prevMax, float newMax)
    {
        if (current > newMax)
        {
            SetCurrent(newMax);  // 줄어든 경우는 SetCurrent가 이벤트를 냄
            return;
        }

        OnValueChanged?.Invoke(current, current); // 늘어난 경우에도 비율을 다시 그리게 알림
    }
}