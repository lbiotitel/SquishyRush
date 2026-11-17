using UnityEngine;

public class LevelBonusDisplay_TestRunner : MonoBehaviour
{
    [SerializeField] private UnitData unitData;

    /*private void Start()
    {
        if (unitData == null)
        {
            Logger.Log("unitData is null");
            return;
        }

        var display = unitData.GetLevelBonusList();

        Logger.Log($"===== {unitData.UnitID} (MaxLevel: {unitData.MaxLevel}) =====");

        foreach (var entry in display)
        {
            if(entry.StatBonuses.Count == 0 && entry.SkillBonuses.Count == 0)
            {
                continue;
            }

            string log = $"[Level {entry.Level}]";

            foreach(var stat in entry.StatBonuses)
            {
                log += $"{stat.StatType}({stat.ModifireType}) + {stat.Value}  "; 
            }

            foreach(var skill in entry.SkillBonuses)
            {
                log += $"Skill: {skill.SkillName}({skill.SkillID})  ";
            }

            Logger.Log(log);
        }
    }*/
}
