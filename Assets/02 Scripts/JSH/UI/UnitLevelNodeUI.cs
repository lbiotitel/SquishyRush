using UnityEngine;
using TMPro;

public class UnitLevelNodeUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text unitLevelText;
    [SerializeField] private TMP_Text levelDescText;

    
    public void Init(LevelBonusEntry entry)
    {
        unitLevelText.text = $"Lv.{entry.Level}";
        levelDescText.text = BuildDescText(entry);
    }

    private string BuildDescText(LevelBonusEntry entry)
    {
        if (entry.StatBonus == null || entry.StatBonus.Value == 0f) return "-";

        string statName = Localization.GetLocaliationText(
            Localization.LocalizationTable.UI, GetStatKey(entry.StatBonus.StatType));

        return $"<color={GlobalDefines.ColorConstants.STAT_NAME_COLOR}>{statName}</color>  <color={GlobalDefines.ColorConstants.STAT_VALUE_COLOR}>+{entry.StatBonus.Value}</color>";
    }

    private static string GetStatKey(EStatType type) => type switch
    {
        EStatType.Atk => LocaliztionKeyCode.UI_STAT_ATK_TEXT,
        EStatType.MaxHp => LocaliztionKeyCode.UI_STAT_MAXHP_TEXT,
        EStatType.CritRate => LocaliztionKeyCode.UI_STAT_CRITRATE_TEXT,
        _ => type.ToString(),
    };
}
