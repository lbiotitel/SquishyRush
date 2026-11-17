using System.Collections;
using System.Collections.Generic;
using UnityEngine;




//아티팩트 팩토리 입니다. 
public class ArtifactEffectFactory
{
    
    
    /// <summary>
    /// 이펙트 데이터 id를 받아 이펙트 데이터 효과를 반환합니다.
    /// </summary>
    /// <param name="artifactEffectId"></param>
    /// <returns></returns>
    public List<IArtifactEffect> Create(string artifactId, int level)
    {
        if (!GlobalManagers.Resource.TryGetResource<ArtifactData>(artifactId, out ArtifactData artifactData))
        {
            Logger.LogError($"{artifactId}의 데이터는 없는 데이터 입니다.");
            return null;
        }
        
        
        float baseValue = artifactData.ArtifactValue;
        float finalValue = baseValue + (level * artifactData.ArtifactValuePerLevel);

        List<string> effectIds = artifactData.ArtifactEffectIds;

        List<IArtifactEffect> result = new List<IArtifactEffect>();

        
        foreach (string effectId in effectIds)
        {
            IArtifactEffect artifactEffect = Create(effectId, finalValue);
            if(artifactEffect != null) result.Add(artifactEffect);
        }

        return result;
    }

    
    
    /// <summary>
    /// 아티팩트 이펙트 효과를 만들어 리턴 합니다.
    /// </summary>
    /// <param name="effectId"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private IArtifactEffect Create(string effectId, float value)
    {
        if (!GlobalManagers.Resource.TryGetResource<ArtifactEffectData>(effectId, out ArtifactEffectData artifactEffectData))
        {
            Logger.LogError($"{effectId}는 존재하지 않는 아티팩트 효과 입니다.");
            return null;
        }

        switch (artifactEffectData.ArtifactEffectType)
        {
            case EArtifactEffectType.Stat:
            {
                ArtifactStatModifierEffect artifactStatModifierEffect =
                    new ArtifactStatModifierEffect(artifactEffectData.StatType,
                        artifactEffectData.StatModifierType, value);
                
                return artifactStatModifierEffect;
            }

            case EArtifactEffectType.Trigger:
            {

                return null;
            }

        }

        return null;
    }
}
