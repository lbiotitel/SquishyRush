using System.Collections.Generic;
using UnityEngine;


//아티팩트에 대한 데이터 정의 입니다.
[CreateAssetMenu]
public class ArtifactData : ScriptableObject
{
    [SerializeField] private Sprite artifactImage;
    [SerializeField] private string artifactID;
    [SerializeField] private ERarity artifactRarity;

    [Header("value")]
    [SerializeField] private float baseArtifactValue;
    [SerializeField] private float artifactValuePerLevel;

    [Header("Effect")]
    [SerializeField] private List<string> artifactEffectIds;

    

    public Sprite ArtifactImage => artifactImage;
    public string ArtifactID => artifactID;
    public string ArtifactNamekey => $"{artifactID}_Name";
    public string ArtifactDescKey => $"{artifactID}_Desc";
    public ERarity ArtifactRarity => artifactRarity;
    public float ArtifactValue => baseArtifactValue;
    public float ArtifactValuePerLevel => artifactValuePerLevel;
    public List<string>  ArtifactEffectIds => artifactEffectIds;
}
