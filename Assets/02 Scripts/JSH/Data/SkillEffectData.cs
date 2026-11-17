using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffectData", menuName = "ScriptableObjects/SkillEffectData")]
public class SkillEffectData : ScriptableObject
{
    [Header("Identifications")]
    public string effectId;
    public string effectGroupId;
    public int order;
    public string visualEffectId;

    [Header("Effect Rules")]
    public EEffectType effectType;
    public ETargetType targetType;
    public ETargetSelectType targetSelectType;
    public EAreaType areaType;

    [Header("Values & Modifiers")] 
    public float[] value = { 1f };
    public EValueModifierType modifierType;
    public EStatType statType;
    public float[] modifierValue = { 0f };

    [Header("Additional FX")]
    public string statusId;
    public string specialSkillId;

    private static float Pick(float[] arr, int star)
    {
        if (arr == null || arr.Length == 0) return 0f;
        return arr[Mathf.Clamp(star, 0, arr.Length - 1)];
    }
    
    public float CalcValue(float baseStat, int star)
    {
        switch (modifierType)
        {
            case EValueModifierType.None: return 0f;
            case EValueModifierType.Flat: return Pick(modifierValue, star);
            default: return baseStat * Pick(modifierValue, star) * 0.01f;
        }
    }

    public int GetStatusStack(int star)
    {
        return Mathf.Max(1, Mathf.RoundToInt(Pick(value, star)));
    }

    public bool HasStatusValueOverride => modifierType != EValueModifierType.None;
    
#if UNITY_EDITOR
    /// <summary>엑셀 임포터 전용. 런타임에서 호출하지 마세요.</summary>
    public void Editor_SetFromImport(
        string effectId, string effectGroupId, int order, string visualEffectId,
        EEffectType effectType, ETargetType targetType,
        ETargetSelectType targetSelectType, EAreaType areaType,
        float[] value,
        EValueModifierType modifierType, EStatType statType, float[] modifierValue,
        string statusId, string specialSkillId)
    {
        this.effectId = effectId;
        this.effectGroupId = effectGroupId;
        this.order = order;
        this.visualEffectId = visualEffectId;
        this.effectType = effectType;
        this.targetType = targetType;
        this.targetSelectType = targetSelectType;
        this.areaType = areaType;
        this.value = (value != null && value.Length > 0) ? value : new[] { 1f };
        this.modifierType = modifierType;
        this.statType = statType;
        this.modifierValue = (modifierValue != null && modifierValue.Length > 0) ? modifierValue : new[] { 0f };
        this.statusId = statusId;
        this.specialSkillId = specialSkillId;
    }
#endif
}