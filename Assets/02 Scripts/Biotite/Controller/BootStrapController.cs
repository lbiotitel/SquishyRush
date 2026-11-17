using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BootStrapController : MonoBehaviour
{
    private const string SettingKey = "AutoStartBootStrap_Enabled";

    private void Start()
    {
        
#if UNITY_EDITOR
        //토글창이 true 인 경우
        bool isAutoStartEnabled = EditorPrefs.GetBool(SettingKey, true);
        
        if (!isAutoStartEnabled)
        {
            Debug.Log("[BootStrapController] 테스트 모드 (토글 OFF): Title 씬으로 자동 전환하지 않습니다.");
            return;
        }
#endif
        
        //일반 플레이 모드
        GlobalManagers.Scene.NewTransition()
            .Load(nameof(EScene.Title))
            .LoadResources(nameof(EResourceLabel.Title))
            .LoadResources(nameof(EResourceLabel.Common))
            .Perform().Forget();
    }
}