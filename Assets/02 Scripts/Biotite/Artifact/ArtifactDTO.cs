using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//아티팩트 정보를 클라우드에 저장하기 위한 클래스 입니다.
[Serializable]
public class ArtifactDTO 
{

    [JsonProperty("Level")]
    public int Level { get; private set; }

    [JsonProperty("ArtifactStone")]
    public int ArtifactStone { get; private set; }


    [Newtonsoft.Json.JsonConstructor]
    public ArtifactDTO(int level, int artifactStone)
    {
        Level = level;
        ArtifactStone = artifactStone;
    }
}
