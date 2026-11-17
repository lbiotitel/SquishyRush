using Newtonsoft.Json;
using System;




//유닛 정보를 클라우드에 저장하기 위한 데이터 오브젝트입니다.
[Serializable]
public class UnitDTO
{
    [JsonProperty("Level")]
    public int Level { get; private set; }

    [JsonProperty("SoulStoneCount")]
    public int SoulStoneCount { get; private set; }


    [Newtonsoft.Json.JsonConstructor]
    public UnitDTO(int level, int soulStoneCount)
    {
        Level = level;
        SoulStoneCount = soulStoneCount;
    }
}

