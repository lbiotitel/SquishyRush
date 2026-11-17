using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EPerkConsumeTiming
{
    None, // 일반 재능. 칸을 차지하고 판매, 합성 시 인벤토리로 돌아옴
    OnEquip, // 장착 즉시 효과만 실행하고 사라짐 (럭키7)
    OnSell,  // 칸을 차지하다가 유닛을 팔 때 같이 사라짐 (판매비용 2배)
}


public enum EPerkType
{
    Instant,
    Trigger
}

//재능 데이터의 부모 데이터 입니다.
public class PerkDataBase : ScriptableObject
{

    [Header("Base Info")] 
    [SerializeField] private string perkId;
    [SerializeField] private ERarity perkRarity;
    [SerializeField] private Sprite perkIcon;
    [SerializeField] private bool canDuplicate;
    [SerializeField] private int price;
    protected EPerkType perkType;

    [Header("Effects")] 
    [SerializeField] private List<string> effectIds;

    [Header("Special")] [Tooltip("재능이 사라지는 시점. 대부분 None")] 
    [SerializeField] private EPerkConsumeTiming consumeTiming;
    
    public string PerkId => perkId;
    public Sprite PerkIcon => perkIcon;
    public string PerkNameKey => $"{perkId}_Name";
    public string PerkDescKey => $"{perkId}_Desc";
    public List<string> EffectIds => effectIds;
    public bool  CanDuplicate => canDuplicate;
    public  EPerkType PerkType => perkType;
    public ERarity  PerkRarity => perkRarity;
    public int Price => price;

    public bool IsConsumable => consumeTiming == EPerkConsumeTiming.OnEquip;
    public bool ConsumeOnSell => consumeTiming == EPerkConsumeTiming.OnSell;


}
