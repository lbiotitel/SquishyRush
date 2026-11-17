using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Synergy")]
public class SynergyData : ScriptableObject
{
    [Header("식별")] 
    [SerializeField] private string synergyId;
    [SerializeField] private string synergyNameKey;
    [SerializeField] private string synergyDescKey;
    [SerializeField] private string synergyIconKey;
    [SerializeField] private Sprite synergyIcon;

    [Header("발동 조건 - 속성/종족 중 하나만")] 
    [SerializeField] private EElementType targetElement = EElementType.None;
    [SerializeField] private ERace targetRace = ERace.None;
    [SerializeField] private int requiredCount;

    [Header("Effect")]
    [SerializeField] private List<string> effectIds = new();
    
    public string SynergyId => synergyId;
    public string SynergyNameKey => synergyNameKey;
    public string SynergyDescKey => synergyDescKey;
    public string SynergyIconKey => synergyIconKey;
    public Sprite SynergyIcon => synergyIcon;
    
    public EElementType TargetElement => targetElement;
    public ERace TargetRace => targetRace;
    public int RequiredCount => requiredCount;
    
    public List<string> EffectIds => effectIds;

    public bool IsElementSynergy => targetElement != EElementType.None;
    
    public int GetCount(SynergyCounter counter)
    {
        if (counter == null) return 0;

        if(targetElement != EElementType.None) return counter.GetCount(targetElement);
        if (targetRace != ERace.None) return counter.GetCount(targetRace);
 

        return 0;
    }
    
    public bool IsSatisfied(SynergyCounter counter) => GetCount(counter) >= requiredCount;

    public bool Matches(UnitData unit)
    {
        if (unit == null) return false;
        return IsElementSynergy ? unit.ElementType == targetElement : unit.HasRace(targetRace);
    }

    public Sprite DisplayIcon => (IsElementSynergy
        ? GlobalManagers.Sprite.GetUnitElementalIcon(targetElement)
        : GlobalManagers.Sprite.GetUnitRaceIcon(targetRace)) ?? synergyIcon;
    
#if UNITY_EDITOR    
    /// <summary>엑셀 임포터 전용. 런타임에서 호출하지 마세요.</summary>
    public void Editor_SetFromImport(
        string synergyId, string nameKey, string descKey,
        string iconKey, Sprite icon,
        EElementType targetElement, ERace targetRace, int requiredCount,
        List<string> effectIds)
    {
        this.synergyId = synergyId;
        synergyNameKey = nameKey;
        synergyDescKey = descKey;
        synergyIconKey = iconKey;
        if (icon != null) synergyIcon = icon;
        this.targetElement = targetElement;
        this.targetRace = targetRace;
        this.requiredCount = requiredCount;
        this.effectIds = effectIds ?? new List<string>();
    }
#endif
}
