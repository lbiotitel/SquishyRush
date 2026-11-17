using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ResourceLoader
{
    // 에셋 주소 -> 로드된 리소스
    private readonly Dictionary<string, UnityEngine.Object> resources = new Dictionary<string, UnityEngine.Object>();
    // 에셋 주소 -> Addressables 핸들
    private readonly Dictionary<string, AsyncOperationHandle> handles = new Dictionary<string, AsyncOperationHandle>();

    // 에셋 주소 -> 소유 라벨 (에셋당 라벨은 1개만 허용)
    private readonly Dictionary<string, string> keyLabel = new Dictionary<string, string>();
    // 라벨 -> 그 라벨로 로드한 에셋 주소들
    private readonly Dictionary<string, HashSet<string>> labelKeys = new Dictionary<string, HashSet<string>>();


    #region Load Resource

    public T Get<T>(string key) where T : UnityEngine.Object
    {
        if (resources.TryGetValue(key, out UnityEngine.Object resource))
            return resource as T;

        return null;
    }

    #endregion


    #region Addressable

    private void LoadAsync<T>(string key, Action<T> callback = null) where T : UnityEngine.Object
    {
        if (resources.TryGetValue(key, out UnityEngine.Object resource))
        {
            callback?.Invoke(resource as T);
            return;
        }
        
        if (handles.TryGetValue(key, out AsyncOperationHandle running))
        {
            running.Completed += (op) => callback?.Invoke(op.Result as T);
            return;
        }

        string loadKey = key;
        if (key.Contains(".sprite"))
            loadKey = $"{key}[{key.Replace(".sprite", "")}]";

        var asyncOperation = Addressables.LoadAssetAsync<T>(loadKey);
        handles.Add(key, asyncOperation); 

        asyncOperation.Completed += (op) =>
        {
            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                Logger.LogWarning($"{key} 로드 실패");
                handles.Remove(key);
                Addressables.Release(asyncOperation);
                callback?.Invoke(null);      
                return;
            }

            resources[key] = op.Result;
            callback?.Invoke(op.Result);
        };
    }


    /// <summary>
    /// 해당 라벨에 해당하고 타입이 T인 데이터를 전부 비동기 로드합니다.
    /// </summary>
    public void LoadAllAsync<T>(string label, Action<string, int, int, object> callback) where T : UnityEngine.Object
    {
        var opHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
        opHandle.Completed += (op) =>
        {
            int loadCount = 0;
            int totalCount = op.Result.Count;

            foreach (var result in op.Result)
            {
                string primaryKey = result.PrimaryKey; 

                Register(label, primaryKey);

                if (primaryKey.Contains(".sprite"))
                {
                    LoadAsync<Sprite>(primaryKey, (obj) =>
                    {
                        loadCount++;
                        callback?.Invoke(primaryKey, loadCount, totalCount, obj);
                    });
                }
                else
                {
                    LoadAsync<T>(primaryKey, (obj) =>
                    {
                        loadCount++;
                        callback?.Invoke(primaryKey, loadCount, totalCount, obj);
                    });
                }
            }

            Addressables.Release(opHandle);   // 위치 조회 핸들 해제
        };
    }


    // 에셋의 소유 라벨을 기록합니다. 다른 라벨이 이미 소유 중이면 에러 로그를 남깁니다.
    private void Register(string label, string key)
    {
        if (keyLabel.TryGetValue(key, out string owner))
        {
            if (owner != label)
            {
                Logger.LogError($"[Addressables] '{key}'에 라벨이 2개 이상 붙어 있습니다. ({owner}, {label}) 에셋당 라벨은 1개만 사용하세요.");
            }
            return;   
        }

        keyLabel.Add(key, label);

        if (!labelKeys.TryGetValue(label, out var keys))
        {
            keys = new HashSet<string>();
            labelKeys.Add(label, keys);
        }
        keys.Add(key);
    }


    // 해당 키의 리소스가 있는지 확인합니다.
    public async UniTask<bool> IsExists<T>(string key)
    {
        var opHandle = Addressables.LoadResourceLocationsAsync(key, typeof(T));
        var locations = await opHandle;

        bool exists = locations != null && locations.Count > 0;
        Addressables.Release(opHandle);

        return exists;
    }


    /// <summary>
    /// 해당 라벨로 로드한 리소스만 해제합니다.
    /// </summary>
    public void Release(string label)
    {
        if (!labelKeys.TryGetValue(label, out var keys)) return;

        foreach (string key in keys)
        {
            keyLabel.Remove(key);
            resources.Remove(key);

            if (handles.TryGetValue(key, out var handle))
            {
                if (handle.IsValid())
                    Addressables.Release(handle);

                handles.Remove(key);
            }
        }

        Resources.UnloadUnusedAssets();
        GC.Collect();
        labelKeys.Remove(label);
    }


    /// <summary>
    /// 전부 해제합니다.
    /// </summary>
    public void Release()
    {
        foreach (var handle in handles)
        {
            if (handle.Value.IsValid())
                Addressables.Release(handle.Value);
        }

        handles.Clear();
        resources.Clear();
        labelKeys.Clear();
        keyLabel.Clear();
        
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    #endregion
}