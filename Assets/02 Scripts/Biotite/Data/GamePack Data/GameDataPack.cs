using System;
using System.Collections.Generic;
using UnityEngine;


//인게임에서 사용할 아티팩트 데이터 입니다.
[Serializable]
public struct ArtifactDataEntry
{
    [field:SerializeField] public string ArtifactId { get; private set; }
    [field:SerializeField] public int Level { get; private set; }

    public ArtifactDataEntry(string artifactId, int level)
    {
        this.ArtifactId = artifactId;
        this.Level = level;
    }
}



//인 게임에서 사용할 유닛 데이터 입니다.
[Serializable]
public struct UnitDataEntry 
{
    [field:SerializeField] public string UnitId { get; private set; }
    [field:SerializeField] public int Level { get; private set; }

    public UnitDataEntry(string unitId, int level)
    {
        this.UnitId = unitId;
        this.Level = level;
    }
}


//로비에서 인게임에 전달할 데이터 팩입니다.
public static class GameDataPack
{
    public static EStageType StageType { get; set; }
    public static IReadOnlyList<ArtifactDataEntry> ArtifactDatas { get; set; }
    public static IReadOnlyList<UnitDataEntry> UnitDatas { get; set; }
}
