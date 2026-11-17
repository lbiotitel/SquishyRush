using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;




[Serializable]
public class UserSettingData 
{
    public bool SpeedUp { get; set;} = false;
    public bool BgmOn { get; set; } = true;
    public bool SfxOn { get; set; } = true;
    public float BgmVolume { get; set; } = 0.5f;
    public float SfxVolume { get; set; } = 0.5f;
    public SystemLanguage Language { get; set; } = SystemLanguage.Korean;
    
    
    /// <summary>
    /// 데이터를 로드 합니다.
    /// </summary>
    /// <returns></returns>
    public bool LoadData()
    {
        string savePath = GlobalDefines.PathKey.GetOptionPath();

        if (!File.Exists(savePath))
        {
            Logger.Log("저장된 옵션 값이 없어 기본값 설정 및 저장 진행합니다.");
            SetDefaultData();
            return SaveData();
        }

        try
        {
            string jsonText = File.ReadAllText(savePath);
            JsonConvert.PopulateObject(jsonText, this);
            Localization.ChangeLanguage(Language);
            Logger.Log("저장된 옵션 값 로드 완료");
        }
        catch (Exception e)
        {
            Logger.LogError($"옵션 로드 중 에러 발생, 기본값 지정: {e.Message}");
            SetDefaultData();
        }
        return true;
    }

    
    /// <summary>
    /// 데이터를 비동기로 로드합니다.
    /// </summary>
    /// <returns></returns>
    public UniTask<bool> LoadDataAsync() => UniTask.FromResult(LoadData());

    
    /// <summary>
    /// 데이터를 저장합니다.
    /// </summary>
    /// <returns></returns>
    public bool SaveData()
    {
        string savePath = GlobalDefines.PathKey.GetOptionPath();
        try
        {
            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string jsonText = JsonConvert.SerializeObject(this, Formatting.Indented);
            
            string tempPath = savePath + ".tmp";
            File.WriteAllText(tempPath, jsonText);

            if (File.Exists(savePath))
                File.Delete(savePath);
            File.Move(tempPath, savePath);

            Logger.Log($"옵션 데이터 저장 완료: {savePath}");
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError($"옵션 저장 중 에러 발생: {e.Message}");
            return false;
        }
    }
    
    
    /// <summary>
    /// 데이터를 비동기로 저장합니다.
    /// </summary>
    /// <returns></returns>
    public UniTask<bool> SaveDataAsync() => UniTask.FromResult(SaveData());
    
    
    
    /// <summary>
    /// 데이터를 기본값으로 저장합니다.
    /// </summary>
    private void SetDefaultData()
    {
        SpeedUp = false;
        BgmOn = true;
        SfxOn = true;
        BgmVolume = 0.5f;
        SfxVolume = 0.5f;
        Language = SystemLanguage.Korean;
    }

}