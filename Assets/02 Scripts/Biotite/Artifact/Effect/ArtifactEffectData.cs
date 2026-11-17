using UnityEngine;



//아티팩트의 효과에 대한 정의입니다.
[CreateAssetMenu]
public class ArtifactEffectData : ScriptableObject
{
    [SerializeField] private string artifactEffectId;

    [SerializeField] private EArtifactEffectType artifactEffectType;
    [SerializeField] private EStatType  statType;
    [SerializeField] private EStatModifireType statModifierType;
    [SerializeField] private EArtifactTriggerType  artifactTriggerType;
    
    public string  ArtifactEffectId => artifactEffectId;
    public EArtifactEffectType ArtifactEffectType => artifactEffectType;
    public EStatType StatType => statType;
    public EStatModifireType StatModifierType => statModifierType;
    public EArtifactTriggerType ArtifactTriggerType => artifactTriggerType;
}


public enum EArtifactTriggerType
{
    None,
    OnGameStart,
    OnFirstBattleStart,
    OnLastBattleStart,
}

public enum EArtifactEffectType
{
    Stat,
    Trigger
}