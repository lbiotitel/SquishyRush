using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//실제 아티팩트의 오브젝트입니다.
public class ArtifactObject 
{

    private ArtifactData artifactData;

    public void Initialize(ArtifactData artifactData)
    {
        this.artifactData = artifactData;
    }


    public string ArtifactID => artifactData.ArtifactID;
    public Sprite ArtifactImage => artifactData.ArtifactImage;
    public ERarity ArtifactRarity => artifactData.ArtifactRarity;


    public string GetArtifactName()
    {
        return Localization.GetLocaliationText("Artifact", artifactData.ArtifactNamekey);
    }


    //레벨에 따른 아티팩트의 value 값을 리턴합니다.
    public float GetArtifactValue(int level)
    {
        float baseValue = artifactData.ArtifactValue;
        float valuePerLevel = artifactData.ArtifactValuePerLevel;

        return baseValue + (valuePerLevel * level);
    }


    //레벨에 따른 아티팩트의 설명 문자열을 리턴합니다.
    public string GetArtifacDesc(int level)
    {
        string desc = Localization.GetLocaliationText("Artifact", artifactData.ArtifactDescKey); 
        float value = GetArtifactValue(level);
        desc = desc.Replace("{value}", $"<color={GlobalDefines.ColorConstants.ARTIFACT_VALUE_COLOR}>{value}</color>");

        return desc;
    }

}
