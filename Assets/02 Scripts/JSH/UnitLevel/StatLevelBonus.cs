using System;
using UnityEngine;

[Serializable]
public class StatLevelBonus
{
    [SerializeField] private EStatType statType;
    [SerializeField] private float value;

    public EStatType StatType => statType;  
    public float Value => value;
    
#if UNITY_EDITOR
    public void Editor_SetFromImport(EStatType type, float value)
    {
        statType = type;
        this.value = value;
    }
#endif
}
