using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;


public enum EScene
{ 
    BootStrap,
    Title,
    Lobby,
    InGame,
    ForInit
}

//빌더 패턴을 활용한 씬 관리자 입니다.
public class SceneController : IManager
{
    private LoadingUI loadingUI;

    private List<string> loadedScenes;
    public bool IsLoading { get; private set; } = false;
    private bool isInit = false;


    public bool Initialize()
    {
        if (isInit) return true;

        loadingUI = GameObject.FindAnyObjectByType<LoadingUI>();

        if (loadingUI == null)
        {
            Logger.LogError("로딩 ui를 찾을 수 없습니다.");
            return false;
        }

        isInit = true;
        loadedScenes = new List<string>();
        return true;
    }

    
    /// <summary>
    /// 현재 활성화된 제일 위에 있는 씬의 이름을 가져옵니다.
    /// </summary>
    /// <returns></returns>
    public string GetActiveSceneName()
    {
        if (loadedScenes.Count <= 0) return null;
        
        int lastIndex = loadedScenes.Count - 1; 
        return loadedScenes[lastIndex];
    }


    public SceneTransitionPlan NewTransition()
    {
        return new SceneTransitionPlan();
    }


    private async UniTask ExcutePlan(SceneTransitionPlan plan)
    {
        if (IsLoading) return;
        IsLoading = true;
        await ChangeSceneSequence(plan);
    }


    //차례대로 등록된 실행순서에 따라 실행
    private async UniTask ChangeSceneSequence(SceneTransitionPlan plan)
    {
        if (plan.Overlay)
        {
            await loadingUI.FadeIn();
            await UniTask.Delay(500);
        }

        foreach (string sceneName in plan.ScenesToUnload)
        {
            await UnloadScene(sceneName);
        }

        //리소스들 언로드
        if (plan.ResourcesToUnload != null && plan.ResourcesToUnload.Count > 0)
        {
            foreach(var key in plan.ResourcesToUnload)
                GlobalManagers.Resource.UnLoadResource(key);
        }
        
        
        //리소스들 로드
        if (plan.ResourcesToLoad != null && plan.ResourcesToLoad.Count > 0)
        {
            var loadTasks = plan.ResourcesToLoad.Select(key => GlobalManagers.Resource.LoadResourceAsync(key));
            await UniTask.WhenAll(loadTasks);
        }
        
        foreach (var sceneName  in plan.ScenesToLoad)
        {
            bool isActive = plan.ActiveSceneName == sceneName;

            if (loadedScenes.Contains(sceneName))
            {
                if (isActive) SetActiveScene(sceneName);
                continue;
            }

            await LoadScene(sceneName, isActive);
        }
        
        //활성화된 씬 이벤트 호출
        string activeSceneName = GetActiveSceneName();
        GlobalManagers.Event.NotifyEvent<OnSceneChangedEventData>(new OnSceneChangedEventData()
        {
            SceneName = activeSceneName
        });

        if (plan.Overlay)
        {
            await loadingUI.FadeOut();
        }

        IsLoading = false;
    }
    
    
    /// <summary>
    /// 이미 로드된 씬은 그냥 그씬을 활성화
    /// </summary>
    /// <param name="sceneName"></param>
    private void SetActiveScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);

        if (!scene.IsValid() || !scene.isLoaded)
        {
            Logger.LogWarning($"{sceneName} 씬이 로드되어 있지 않아 active로 지정할 수 없습니다.");
            return;
        }

        if (SceneManager.GetActiveScene() == scene) return;

        SceneManager.SetActiveScene(scene);
    }
    

    //씬로드
    private async UniTask LoadScene(string sceneName, bool setActive)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null) return;

        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            await UniTask.Yield();
        }

        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            await UniTask.Yield();
        }

        if (setActive)
        {
            Scene newScene = SceneManager.GetSceneByName(sceneName);
            if (newScene.IsValid() && newScene.isLoaded)
            {
                SceneManager.SetActiveScene(newScene);
            }
        }
        loadedScenes.Add(sceneName);
        GlobalManagers.Event.NotifyEvent<OnSceneChangedEventData>(
            new OnSceneChangedEventData 
            { 
                SceneName = sceneName
            });
    }

    //씬 언로드
    private async UniTask UnloadScene(string sceneName)
    {
        if (!loadedScenes.Contains(sceneName)) return;
        if (string.IsNullOrEmpty(sceneName)) return;

        var op = SceneManager.UnloadSceneAsync(sceneName);
        if (op == null) return;

        while (!op.isDone)
        {
            await UniTask.Yield();
        }

        loadedScenes.Remove(sceneName);
    }


    public void Release()
    {

    }



    //빌더 패턴을 위한 클래스
    public class SceneTransitionPlan
    {
        //어떤 씬을 로드 할 건지
        public List<string> ScenesToLoad { get; } = new List<string>();

        //어떤 씬을 언로드 할 건지
        public List<string> ScenesToUnload { get; } = new List<string>();

        //활성화 된 씬이름
        public string ActiveSceneName { get; private set; }

        //로드할 리소스 키들
        public List<string> ResourcesToLoad { get; } = new List<string>();

        //언로드할 리소스 키들
        public List<string> ResourcesToUnload { get; } = new List<string>();

        //UI를 켜서 부드럽게 전환 유무
        public bool Overlay { get; private set; } = false;
        

        public SceneTransitionPlan Load(string sceneName, bool setActive = false)
        {
            ScenesToLoad.Add(sceneName);
            if (setActive) ActiveSceneName = sceneName;
            return this;
        }

        public SceneTransitionPlan Unload(string sceneName)
        {
            ScenesToUnload.Add(sceneName);
            return this;
        }

        public SceneTransitionPlan WithOverlay()
        {
            Overlay = true;
            return this;
        }

        public SceneTransitionPlan LoadResources(string key)
        {
            ResourcesToLoad.Add(key);
            return this;
        }

        public SceneTransitionPlan UnLoadResources(string key)
        {
            ResourcesToUnload.Add(key);
            return this;
        }
        
        public async UniTask Perform()
        {
            await GlobalManagers.Scene.ExcutePlan(this);
        }
        
    }

}
