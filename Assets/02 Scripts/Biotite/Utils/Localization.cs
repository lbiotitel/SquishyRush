using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;


// 언어 
public static class Localization
{

    public static class LocalizationTable
    {
        public const string UI = "UI";
        public const string UNIT = "Unit";
        public const string ARTIFACT = "Artifact";
        public const string Quest = "Quest";
        public const string Skill = "Skill";
        public const string Perk = "Perk";
        public const string Error = "Error";
        public const string Synergy = "Synergy";
    }
    
    
    private static readonly Dictionary<SystemLanguage, string> languageCodeMap = new Dictionary<SystemLanguage, string>()
    {
        { SystemLanguage.Korean , "ko-KR" },
        { SystemLanguage.English, "en-US"}
    };
    

    /// <summary>
    /// 언어의 목록을 불러 옵니다.
    /// </summary>
    /// <returns></returns>
    public static List<SystemLanguage> GetLanguages()
    {
        List<SystemLanguage> systemLanguages = new List<SystemLanguage>();

        foreach (var language in languageCodeMap)
        {
            systemLanguages.Add(language.Key);
        }

        return systemLanguages; 
    }

    /// <summary>
    /// 언어를 변경 합니다.
    /// </summary>
    /// <param name="language"></param>
    public static void ChangeLanguage(SystemLanguage language)
    {
        if (languageCodeMap.TryGetValue(language, out string local))
        {
            Locale newLocale = LocalizationSettings.AvailableLocales.GetLocale(local);
            if (newLocale != null)
            {
                LocalizationSettings.SelectedLocale = newLocale;
                Logger.Log($"언어가 {newLocale}로 변경되었습니다.");
            }
            else
            {
                Logger.LogError($"선택한 언어 {local}에 해당하는 Locale이 존재하지 않습니다.");
            }
        }
        else
        { 
            Logger.LogError($"선택한 언어 {language}에 해당하는 Locale 코드가 존재하지 않습니다.");
        }
    }
    


    //테이블 키에 해당하는 로컬라이제이션 텍스트를 가져옵니다.
    public static string GetLocaliationText(string tableKey,string key)
    {
        string localizedString = LocalizationSettings.StringDatabase.GetLocalizedString(tableKey, key);
        return localizedString;
    }
}
