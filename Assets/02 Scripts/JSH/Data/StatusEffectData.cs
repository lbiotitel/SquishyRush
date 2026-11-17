using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatusModifierEntry
{
    public EStatType statType;
    public EValueModifierType modifierType;
    public float modifierValue;
    
    public StatusModifierEntry() {}

    public StatusModifierEntry(EStatType statType, EValueModifierType modifierType, float modifierValue)
    {
        this.statType = statType;
        this.modifierType = modifierType;
        this.modifierValue = modifierValue;
    }
}

[Serializable]
public class OnHitApplyEntry
{
    public string statusId;
    public int stack = 1;
    public int chance = 10000;
    
    public OnHitApplyEntry() {}

    public OnHitApplyEntry(string statusId, int stack, int chance)
    {
        this.statusId = statusId;
        this.stack = stack;
        this.chance = chance;
    }
}


[CreateAssetMenu(menuName = "Data/StatusEffect")]
public class StatusEffectData : ScriptableObject
{
    [Header("식별")] 
    [SerializeField] private string statusId;
    [SerializeField] private string nameKey;
    [SerializeField] private string descKey;

    [Header("동작")] 
    [SerializeField] private EStatusType statusType;
    [SerializeField] private EStatusEffectType statusEffectType;
    [SerializeField] private List<StatusModifierEntry> modifiers = new();
    
    [Header("지속")]
    [SerializeField] private EDurationType durationType;
    [SerializeField] private float duration;
    [SerializeField] private float tickInterval;
    [SerializeField] private bool tickOnApply;

    [Header("중첩")]
    [SerializeField] private int maxStack = 1;
    [SerializeField] private EStackBehavior stackBehavior;

    [Header("표시")] 
    [SerializeField] private string iconKey;
    [SerializeField] private Sprite icon;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float textScale = 0.85f;

    [Header("연출")] 
    [SerializeField] private EStatusVfxMode vfxMode;
    [SerializeField] private string visualEffectId;
    
    [Header("적중 시 부여 (ApplyStatusOnHit 전용)")]
    [SerializeField] private OnHitApplyEntry onHitApply;
    
    public string StatusId => statusId;
    public string NameKey => nameKey;
    public string DescKey => descKey;
    
    public EStatusType StatusType => statusType;
    public EStatusEffectType StatusEffectType => statusEffectType;
    public IReadOnlyList<StatusModifierEntry> Modifiers => modifiers;
    
    public EDurationType DurationType => durationType;
    public float Duration => duration;
    public float TickInterval => tickInterval;
    public bool TickOnApply => tickOnApply;
    
    public int MaxStack => maxStack;
    public EStackBehavior StackBehavior => stackBehavior;

    public string IconKey => iconKey;
    public Sprite Icon => icon;
    public Color TextColor => textColor;
    public float TextScale => textScale;
    
    public EStatusVfxMode VfxMode => vfxMode;
    public string VisualEffectId => visualEffectId;
    
    public OnHitApplyEntry OnHitApply => onHitApply;

    public EStatType StatType => modifiers.Count > 0 ? modifiers[0].statType : EStatType.None;
    public EValueModifierType ModifierType => modifiers.Count > 0 ? modifiers[0].modifierType : EValueModifierType.None;
    public float ModifierValue => modifiers.Count > 0 ? modifiers[0].modifierValue : 0f;
    
    /// <summary>
    /// DamageOverTime / HealOverTime의 총 틱 수.
    /// TickOnApply가 true면 부여 즉시 1틱이 더 발생합니다(3초 화상이 3틱이냐 4틱이냐가 여기서 갈립니다).
    /// </summary>
    public int GetTotalTickCount()
    {
        if (tickInterval <= 0f) return 0;
        int tickCount = Mathf.FloorToInt(duration / tickInterval);
        return tickOnApply ? tickCount + 1 : tickCount;
    }

    
    /// <summary>
    /// 모디파이어 수치를 계산
    /// overrideValue가 있으면 첫 모디파이어에 한해 그 값을 사용
    /// </summary>
    public float CalcModifierAmount(StatusModifierEntry entry, float baseStat, float? overrideValue = null)
    {
        if (entry == null) return 0f;

        float raw = overrideValue ?? entry.modifierValue;

        switch (entry.modifierType)
        {
            case EValueModifierType.None:
                return 0f;
            case EValueModifierType.Flat:
                return raw;
            default:
                return baseStat * raw * 0.01f;
        }
    }
    
#if UNITY_EDITOR
    /// <summary>엑셀 임포터 전용. 런타임에서 호출하지 마세요.</summary>
    public void Editor_SetFromImport(
        string statusId, string nameKey, string descKey,
        EStatusType statusType, EStatusEffectType statusEffectType,
        List<StatusModifierEntry> modifiers,
        EDurationType durationType, float duration, float tickInterval, bool tickOnApply,
        int maxStack, EStackBehavior stackBehavior, string iconKey, Sprite icon)
    {
        this.statusId = statusId;
        this.nameKey = nameKey;
        this.descKey = descKey;
        this.statusType = statusType;
        this.statusEffectType = statusEffectType;
        this.modifiers = modifiers ?? new List<StatusModifierEntry>();
        this.durationType = durationType;
        this.duration = duration;
        this.tickInterval = tickInterval;
        this.tickOnApply = tickOnApply;
        this.maxStack = maxStack;
        this.stackBehavior = stackBehavior;
        this.iconKey = iconKey;
        if (icon != null) this.icon = icon;
    }
#endif
}
