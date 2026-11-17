using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;



public enum EResourceLabel
{
    BootStrap,
    Title,
    Lobby,
    InGame,
    Common
}

//인 게임 데이터를 관리하는 매니저 입니다.
public class ResourceTableManager : IManager
{
    private readonly ResourceLoader resourceLoader = new ResourceLoader();

    private readonly Dictionary<Type, Dictionary<string, object>> dataDict
        = new Dictionary<Type, Dictionary<string, object>>();
    
    private readonly Dictionary<string, List<(Type type, string name)>> keyEntries
        = new Dictionary<string, List<(Type type, string name)>>();

    private readonly HashSet<string> loadedKeys = new HashSet<string>();
    private readonly HashSet<string> loadingKeys = new HashSet<string>();


    public bool IsLoaded => loadingKeys.Count == 0 && loadedKeys.Count > 0;

    public bool Initialize()
    {
        return true;
    }


    /// <summary>
    /// 키에 해당하는 리소스 데이터를 전부 로드 합니다.
    /// </summary>
    public async UniTask LoadResourceAsync(string key)
    {
        if (loadedKeys.Contains(key)) return;
        
        if (loadingKeys.Contains(key))
        {
            await UniTask.WaitUntil(() => !loadingKeys.Contains(key));
            return;
        }

        loadingKeys.Add(key);
        keyEntries[key] = new List<(Type, string)>();

        await UniTask.WhenAll(
            LoadResource<ScriptableObject>(key),
            LoadResource<GameObject>(key),
            LoadResource<AudioClip>(key),
            LoadResource<Sprite>(key),
            LoadResource<SpriteAtlas>(key),
            LoadResource<RuntimeAnimatorController>(key)
        );

        loadingKeys.Remove(key);
        loadedKeys.Add(key);

        GlobalManagers.Event.NotifyEvent<OnGameResourceDataLoadedEventData>(new OnGameResourceDataLoadedEventData { Key = key });
        Logger.Log($"{key} 인 게임 데이터 로드 완료");
    }


    /// <summary>
    /// key 에 있는 타입 T 의 데이터를 전부 로드 합니다.
    /// </summary>
    private async UniTask LoadResource<T>(string key) where T : UnityEngine.Object
    {
        bool isExists = await resourceLoader.IsExists<T>(key);
        if (!isExists)
        {
            Logger.LogWarning($"{key}에 해당하는 {typeof(T)}데이터가 없습니다");
            return;
        }
        
        var utcs = new UniTaskCompletionSource();

        resourceLoader.LoadAllAsync<T>(key, (name, loadCount, totalCount, resource) =>
        {
            if (resource != null)  
            {
                Type actualType = resource.GetType();

                if (!dataDict.TryGetValue(actualType, out var typeDict))
                {
                    typeDict = new Dictionary<string, object>();
                    dataDict.Add(actualType, typeDict);
                }

                if (typeDict.ContainsKey(name))
                    Logger.LogWarning($"{actualType}의 {name} 이름이 중복되었습니다. (key: {key})");

                typeDict[name] = resource;            
                keyEntries[key].Add((actualType, name));
            }

            if (loadCount >= totalCount)            
                utcs.TrySetResult();
        });

        await utcs.Task;
    }


    /// <summary>
    /// 해당 키로 로드한 리소스만 해제합니다.
    /// </summary>
    public void UnLoadResource(string key)
    {
        if (loadingKeys.Contains(key))
        {
            Logger.LogWarning($"{key}는 로딩 중이라 해제할 수 없습니다.");
            return;
        }

        if (!loadedKeys.Contains(key)) return;

        if (keyEntries.TryGetValue(key, out var entries))
        {
            foreach (var (type, name) in entries)
            {
                if (dataDict.TryGetValue(type, out var typeDict))
                {
                    typeDict.Remove(name);
                    if (typeDict.Count == 0) dataDict.Remove(type);
                }
            }
            keyEntries.Remove(key);
        }

        loadedKeys.Remove(key);
        resourceLoader.Release(key);
    }


    /// <summary>
    /// 타입 T 에 해당하는 데이터를 가져옵니다.
    /// </summary>
    public bool TryGetResource<T>(string key, out T resource) where T : Object
    {
        resource = null;
        
        if (dataDict.TryGetValue(typeof(T), out var specificDict))
        {
            if (specificDict.TryGetValue(key, out object obj))
            {
                resource = obj as T;
                return true;
            }
        }

        foreach (var pair in dataDict)
        {
            if (typeof(T).IsAssignableFrom(pair.Key))
            {
                if (pair.Value.TryGetValue(key, out object obj))
                {
                    resource = obj as T;
                    return true;
                }
            }
        }

        return false;
    }


    /// <summary>
    /// 타입 T에 해당하는 데이터를 전부 가져옵니다.
    /// </summary>
    public List<T> GetResources<T>() where T : Object
    {
        List<T> result = new List<T>();

        foreach (var pair in dataDict)
        {
            if (typeof(T).IsAssignableFrom(pair.Key))
            {
                foreach (var obj in pair.Value.Values)
                {
                    if (obj is T t && t != null) result.Add(t);
                }
            }
        }

        return result;
    }


    /// <summary>
    /// 전부 해제합니다. 
    /// </summary>
    public void Release()
    {
        dataDict.Clear();
        keyEntries.Clear();
        loadedKeys.Clear();
        loadingKeys.Clear();
        resourceLoader.Release();
    }
}