using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 월드 오브젝트(데미지 텍스트, VFX) 공용 풀.
///
/// 규칙
///  - 꺼낸 오브젝트도, 반납된 오브젝트도 전부 이 오브젝트(풀 루트) 밑에 있습니다.
///    부모를 오가지 않으므로 하이어라키가 씬 루트에 흩어지지 않고, 재부모화 비용도 없습니다.
///  - 풀 루트는 위치 (0,0,0) / 회전 없음 / 스케일 1 이어야 합니다. 자식의 로컬 = 월드가 되도록.
///  - 전투 종료 시에는 ReturnAll로 회수만 하고 파괴하지 않습니다. 다음 전투에 그대로 재사용.
///    파괴는 씬이 끝날 때 풀 루트와 함께 자동으로 일어납니다.
/// </summary>
public class PoolingSystem : MonoBehaviour
{
    // 프리팹별 대기 중인(꺼진) 오브젝트
    private Dictionary<GameObject, Queue<GameObject>> poolDict = new();

    private readonly HashSet<GameObject> activeObjects = new();

    // ReturnAll에서 순회 중 HashSet을 수정할 수 없으므로 복사용 버퍼 (재사용)
    private readonly List<GameObject> returnBuffer = new();


    public T GetObject<T>(GameObject goPrefab, Vector3 position, Quaternion rotation) where T : Component
    {
        GameObject res = GetObject(goPrefab, position, rotation);
        return res != null && res.TryGetComponent(out T component) ? component : null;
    }
    

    public GameObject GetObject(GameObject goPrefab, Vector3 position, Quaternion rotation)
    {
        if (goPrefab == null || !goPrefab.TryGetComponent(out IPoolingObject _))
        {
            Logger.LogWarning($"[Pool] IPoolingObject가 없는 프리팹입니다 : {goPrefab}");
            return null;
        }

        if (!poolDict.TryGetValue(goPrefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            poolDict.Add(goPrefab, pool);
        }
        
        GameObject res = null;
        
        // 큐에 남아 있어도 외부에서 파괴됐을 수 있으므로 살아 있는 것이 나올 때까지 꺼냄
        while (pool.Count > 0 && res == null)
            res = pool.Dequeue();

        if (res == null)
        {
            res = Instantiate(goPrefab, transform); //처음부터 풀 루트 밑에 생성
            res.GetComponent<IPoolingObject>().OriginPrefab = goPrefab;
        }

        res.transform.SetPositionAndRotation(position, rotation);
        res.gameObject.SetActive(true);

        activeObjects.Add(res);
        return res;
    }


    public void ReturnObject(GameObject returnObject)
    {
        if (returnObject == null) return;
        
        // 이미 반납된 오브젝트면 무시. 두 번 Enqueue되면 같은 오브젝트가 동시에 두 번 꺼내지는 버그가 생김
        if (!activeObjects.Remove(returnObject)) return;

        GameObject prefabKey = returnObject.TryGetComponent(out IPoolingObject po) ? po.OriginPrefab : null;
        if (prefabKey == null || !poolDict.TryGetValue(prefabKey, out Queue<GameObject> pool))
        {
            Destroy(returnObject); //풀 소속을 알 수 없으면 버림
            return;
        }

        returnObject.SetActive(false);
        
        // 사용 중에 다른 곳(유닛 등)에 붙였을 수 있으니 풀 루트로 되돌림
        if (returnObject.transform.parent != transform)
        {
            returnObject.transform.SetParent(transform, false);
        }

        pool.Enqueue(returnObject);
    }

    /// <summary>
    /// 전투 종료: 사용 중인 오브젝트를 전부 회수합니다. 파괴하지 않습니다.
    /// 각 오브젝트의 ReturnObject를 부르므로 진행 중인 트윈 정리(ResetObject)도 함께 됩니다.
    /// </summary>
    public void ReturnAll()
    {
        returnBuffer.Clear();
        returnBuffer.AddRange(activeObjects);

        for (int i = 0; i < returnBuffer.Count; i++)
        {
            GameObject go = returnBuffer[i];

            if (go == null)
            {
                activeObjects.Remove(go);
                continue;
            }
            
            if(go.TryGetComponent(out IPoolingObject po)) po.ResetObject();
            else ReturnObject(go);
        }

        activeObjects.RemoveWhere(go => go == null);
    }
}