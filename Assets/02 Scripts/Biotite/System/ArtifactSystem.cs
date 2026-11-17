using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;


public class ArtifactSystem 
{

    private readonly ArtifactEffectFactory artifactEffectFactory;

    public ArtifactSystem()
    {
        artifactEffectFactory = new ArtifactEffectFactory();
    }


    /// <summary>
    /// 아티팩트 효과를 초기화 합니다.
    /// </summary>
    public void Initialize(IReadOnlyList<ArtifactDataEntry> artifactDatas)
    {
        if (artifactDatas == null || artifactDatas.Count == 0)
            return;
        
        foreach (var artifactData in artifactDatas)
        {
            List<IArtifactEffect> artifactEffectList = artifactEffectFactory.Create(artifactData.ArtifactId, artifactData.Level);
            
            if(artifactEffectList == null ||  artifactEffectList.Count == 0)
                continue;
            
            foreach (var artifactEffect in artifactEffectList)
            {
                artifactEffect.ApplyEffect();
            }
        }
    }

    
}
