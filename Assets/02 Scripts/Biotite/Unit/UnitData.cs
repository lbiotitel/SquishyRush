using System.Collections.Generic;
using UnityEngine;


public enum EElementType
{
    None = -1,
    FIRE,
    WATER,
    EARTH,
    DARK,
    LIGHT
}

public enum ERace
{
    None = -1,
    Wild,
    Fly,
    Aquatic,
    Plant,
    Ghost,
    Spirit,
    Creature,
    Snack,
    Golem,
    Dragon,
    Death
}


public enum ERarity
{
    Normal,
    Rare,
    Unique,
    Legend
}

//유닛 데이터를 정의합니다.
[CreateAssetMenu]
public class UnitData : ScriptableObject
{
    [SerializeField] private Sprite unitIcon;
    [SerializeField] private string unitId;

    
    [SerializeField] private EElementType elementType;
    [SerializeField] private List<ERace> races = new();
    [SerializeField] ERarity unitRarity;

    [SerializeField] private int atk;
    [SerializeField] private int maxHp;
    [SerializeField] private float criticalRate;
    [SerializeField] private float accuracy;
    [SerializeField] private float atkSpeed;
    [SerializeField] private float sg;
    [SerializeField] private float sgChargeAmount;

    private int maxLevel = GlobalDefines.MAX_VALUE.UNIT_MAX_LEVEL;

    [SerializeField] private List<StatLevelBonus> levelEntries = new();
    [SerializeField] private string skillId;

    // 일반공격 연출(VisualEffectData ID). 비우면 연출 없이 즉시 명중(근접).
    // 스킬처럼 ID 문자열로 참조합니다. SO를 직접 물면 연출 애셋이 유닛 번들마다 복제됩니다.
    // 비행 시간은 여기 두지 않습니다. VisualEffectData.TravelTime 한 곳에만 둬야
    // 로직과 연출이 서로 다른 숫자를 보는 일이 생기지 않습니다.
    [SerializeField] private string normalAttackVfxId;

    public Sprite UnitImage => unitIcon;
    public string UnitId => unitId;
    public string UnitUIAnimator => $"UI_{unitId}_AC";
    public string UnitSpine => $"{unitId}_SD";
    public string UnitNameKey => $"{unitId}_Name";
    public EElementType ElementType => elementType;
    public IReadOnlyList<ERace> Races => races;
    public ERarity UnitRarity => unitRarity;

    public int Atk => atk;
    public int MaxHp => maxHp;
    public float CriticalRate => criticalRate;
    public float Accuracy => accuracy;
    public float AtkSpeed => atkSpeed;
    public float SG => sg;
    public float SGChargeAmount => sgChargeAmount;
    
    public int MaxLevel => maxLevel;
    public IReadOnlyList<StatLevelBonus> LevelEntries => levelEntries;
    public string SkillId => skillId;
    public string NormalAttackVfxId => normalAttackVfxId;
    
    // 지정된 레벨에 대한 보너스를 반환합니다. 레벨이 유효하지 않은 경우 빈 목록을 반환합니다.
    public StatLevelBonus GetLevelBonus(int level)
    {
        if (level < 1 || level > maxLevel)
        {
            return null;
        }
        return levelEntries[level - 1];
    }

    public List<LevelBonusEntry> GetLevelBonusList()
    {
        var result = new List<LevelBonusEntry>();

        for (int lv = 1; lv <= maxLevel; lv++)
        {
            result.Add(new LevelBonusEntry
            {
                Level = lv,
                StatBonus = GetLevelBonus(lv)
            });
        }

        return result;
    }

    public bool HasRace(ERace target)
    {
        if (target == ERace.None) return false;
        
        for (int i = 0; i < races.Count; i++)
            if (races[i] == target) return true;

        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        while(levelEntries.Count < maxLevel)
        {
            levelEntries.Add(new StatLevelBonus());
        }

        while (levelEntries.Count > maxLevel)
        {
            levelEntries.RemoveAt(levelEntries.Count - 1);
        }
    }
#endif
}
