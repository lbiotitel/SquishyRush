using System;
using UnityEngine;


//바로 적용되는 퍽 데이터입니다.
[CreateAssetMenu]
public class 
    InstantPerkData : PerkDataBase
{
    private void OnValidate()
    {
        perkType = EPerkType.Instant;
    }
}
