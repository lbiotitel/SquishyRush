using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//재능의 효과 데이터 입니다.
[CreateAssetMenu]
public class PerkEffectData : ScriptableObject
{
    [Header("Effect")]
    [SerializeField] private string perkEffectId;

    [SerializeReference, SubclassSelector] private List<IPerkEffect> effects;
    
    public  string PerkId => perkEffectId;
    public List<IPerkEffect> Effects => effects;
}
