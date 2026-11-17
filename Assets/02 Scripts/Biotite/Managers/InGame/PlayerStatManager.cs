using UnityEngine;




//플레이어에 관한 종합 스탯을 관리하는 클래스 입니다.
public class PlayerStatManager : MonoBehaviour
{
    private DataDictionary<EStatType, Stat> statDict;

    
    public void Initialize()
    {
        InitializeStatDict();
    }

    
    /// <summary>
    /// 플레이어에 관한 종합 스탯을 초기화 합니다.
    /// </summary>
    private void InitializeStatDict()
    {
        statDict = new DataDictionary<EStatType, Stat>();
        
        RegistStat(EStatType.Atk);
        RegistStat(EStatType.MaxHp);
        RegistStat(EStatType.AtkSpeed);
        RegistStat(EStatType.CritRate);
        RegistStat(EStatType.SGChargeAmount);
        RegistStat(EStatType.StatusEffectDmg);
        RegistStat(EStatType.CriDamageMultiplier);
        RegistStat(EStatType.DmgMultiplier);
        RegistStat(EStatType.HealMultiplier);
        RegistStat(EStatType.Evasion);
        RegistStat(EStatType.ExecuteBonusRate);
        RegistStat(EStatType.FireElementAtkBonus);
        RegistStat(EStatType.DarkElementAtkBonus);
        RegistStat(EStatType.EarthElementAtkBonus);
        RegistStat(EStatType.LightElementAtkBonus);
        RegistStat(EStatType.WaterElementAtkBonus);
        RegistStat(EStatType.RerollCostReduction);
        RegistStat(EStatType.GoldMultiplier);
        RegistStat(EStatType.FirstGold);
    }


    
    /// <summary>
    /// 딕셔너리에 스탯을 등록합니다.
    /// </summary>
    /// <param name="statType"></param>
    private void RegistStat(EStatType statType)
    {
        Stat stat = new Stat(0);
        statDict.Add(statType, stat);
    }



    /// <summary>
    /// 스탯에 해당하는 보너스 값을 가져옵니다.
    /// </summary>
    /// <param name="statType"></param>
    /// <returns></returns>
    public float GetStatValue(EStatType statType)
    {
        if (statDict.TryGetValue(statType, out Stat stat))
        {
            return stat.Value;
        }

        return 0;
    }
    
    
    
    /// <summary>
    /// 스탯에 모디파이어를 붙입니다.
    /// </summary>
    /// <param name="statType"></param>
    /// <param name="modifier"></param>
    public void AddModifier(EStatType statType, StatModifier modifier)
    {
        if (statDict.TryGetValue(statType, out Stat stat))
        {
            stat.AddModifier(modifier);
        }
    }

    
    
    /// <summary>
    /// 스탯에서 소스에 해당하는 모든 모디파이어를 제거합니다.
    /// </summary>
    /// <param name="statType"></param>
    /// <param name="modifier"></param>
    public void RemoveAllModifier(EStatType statType, object source)
    {
        if (statDict.TryGetValue(statType, out Stat stat))
        {
            stat.RemoveAllModifier(source);
        }
    }
}
