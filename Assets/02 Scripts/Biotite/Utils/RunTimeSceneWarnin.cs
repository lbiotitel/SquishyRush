using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class AutoStartBootStrap
{
    private const string MenuName = "Tools/Always Start With BootStrap";
    private const string SettingKey = "AutoStartBootStrap_Enabled";

#if UNITY_EDITOR
    static AutoStartBootStrap()
    {
        // 에디터 로드 완료 시 및 씬 상태 변경 시 강제 동기화
        EditorApplication.delayCall += UpdatePlayModeStartScene;
    }
    
    [MenuItem(MenuName)]
    private static void ToggleAction()
    {
        bool isEnabled = EditorPrefs.GetBool(SettingKey, true);
        EditorPrefs.SetBool(SettingKey, !isEnabled);
        
        UpdatePlayModeStartScene();
    }
    
    [MenuItem(MenuName, true)]
    private static bool ToggleValidation()
    {
        bool isEnabled = EditorPrefs.GetBool(SettingKey, true);
        Menu.SetChecked(MenuName, isEnabled);
        return true;
    }

    private static void UpdatePlayModeStartScene()
    {
        // 기본값을 false로 변경하여 디버깅 테스트를 더욱 명확히 함
        bool isEnabled = EditorPrefs.GetBool(SettingKey, false);

        if (isEnabled)
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene " + nameof(EScene.BootStrap));
            
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                SceneAsset bootStrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                
                EditorSceneManager.playModeStartScene = bootStrapScene;
            }
        }
        else
        {
            // 껐을 때는 유니티 에디터 설정의 시작 씬을 확실하게 제거
            EditorSceneManager.playModeStartScene = null;
        }
    }
#endif

    // =========================================================================
    // 런타임 로직 (에디터 강제 시작이 OFF일 때만 동작)
    // =========================================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoad()
    {
#if UNITY_EDITOR
        // 메뉴 토글이 ON(true)이면 런타임 Additive 로더는 동작하지 않음
        if (EditorPrefs.GetBool(SettingKey, false)) return;
#endif

        string currentScene = SceneManager.GetActiveScene().name;
        string bootStrapName = nameof(EScene.BootStrap);

        if (currentScene != bootStrapName)
        {
            SceneManager.LoadScene(bootStrapName, LoadSceneMode.Additive);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
#if UNITY_EDITOR
        if (EditorPrefs.GetBool(SettingKey, false)) return;
#endif

        string currentScene = SceneManager.GetActiveScene().name;
        string bootStrapName = nameof(EScene.BootStrap);

        if (currentScene != bootStrapName)
        {
            LoadDataAsync(currentScene).Forget();
        }
    }

    private static async UniTask LoadDataAsync(string currentScene)
    {
        await UniTask.WaitUntil(() => GlobalManagers.Resource != null);
        await GlobalManagers.Resource.LoadResourceAsync(nameof(EResourceLabel.Common));
        await UniTask.NextFrame();  
        await GlobalManagers.Resource.LoadResourceAsync(nameof(EResourceLabel.InGame));
        await UniTask.NextFrame();
    }
}