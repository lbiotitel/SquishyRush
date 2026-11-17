using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MemoryPool 
{
    private class PoolItem
    {
        public bool isActive;
        public GameObject gameObject;
    }


    private int increaseCount = 5;
    private int maxCount;
    private int activeCount;

    private GameObject poolObject;
    private List<PoolItem> poolingObjects;

    public MemoryPool(GameObject poolObject)
    {
        maxCount = 0;
        activeCount = 0;
        this.poolObject = poolObject;

        poolingObjects = new List<PoolItem>();

        InstantiateObjects();
    }
    
    
    /// <summary>
    /// 오브젝트를 정해진 개수 만큼 생성
    /// </summary>
    public void InstantiateObjects()
    {
        maxCount += increaseCount;

        for (int i = 0; i < increaseCount; i++)
        {
            PoolItem poolItem = new PoolItem();

            poolItem.isActive = false;
            poolItem.gameObject = GameObject.Instantiate(poolObject);
            poolItem.gameObject.SetActive(false);
            
            poolingObjects.Add(poolItem);
        }
    }

    
    /// <summary>
    /// 풀링한 오브젝트를 전부 파괴합니다.
    /// </summary>
    public void DestroyObjects()
    {
        if (poolingObjects == null) return;

        int count = poolingObjects.Count;
        
        for(int i = 0; i < count ; i++)
            GameObject.Destroy(poolingObjects[i].gameObject);
        
        poolingObjects.Clear();
    }

    
    
    /// <summary>
    /// 메모리 풀에서 비활성화된 오브젝트를 하나 가져옵니다.
    /// </summary>
    /// <returns></returns>
    public GameObject GetObject()
    {
        if (poolingObjects == null) return null;
        
        //활성화된 오브젝트의 수가 전체 수 와 같다면 더 많은 오브젝트를 풀링 합니다.
        if(maxCount == activeCount)
            InstantiateObjects();

        foreach (var pObj in poolingObjects)
        {
            if (!pObj.isActive)
            {
                activeCount++;

                pObj.isActive = true;
                pObj.gameObject.SetActive(true);

                return pObj.gameObject;
            }
        }

        return null;
    }


    /// <summary>
    /// 사용이 완료된 오브젝트를 반환합니다.
    /// </summary>
    /// <param name="returnObject"></param>
    public void ReturnObject(GameObject returnObject)
    {
        if (poolingObjects == null || returnObject == null) return;

        foreach (var pObj in poolingObjects)
        {
            if (pObj.gameObject == returnObject)
            {
                activeCount--;

                pObj.gameObject.transform.SetParent(null);
                pObj.isActive = false;
                pObj.gameObject.SetActive(false);

                return;
            }
        }
    }

    
    /// <summary>
    /// 풀링 된 모든 오브젝트를 비활성화 합니다.
    /// </summary>
    public void DeactivateAllObjects()
    {
        if (poolingObjects == null) return;

        foreach (var pObj in poolingObjects)
        {
            if (pObj.gameObject != null && pObj.isActive)
            {
                pObj.isActive = false;
                pObj.gameObject.SetActive(false);
            }
        }

        activeCount = 0;
    }


    
    /// <summary>
    /// 풀링된 오브젝트를 전부 가져옵니다.
    /// </summary>
    /// <returns></returns>
    public List<GameObject> GetPoolObjects()
    {
        return poolingObjects
            .Where(x => x != null)
            .Select(x => x.gameObject)
            .ToList();
    }

}
