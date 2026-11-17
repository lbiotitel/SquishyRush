using UnityEngine;

public interface IPoolingObject
{
    public GameObject OriginPrefab { get; set; }
    public void ResetObject();
    public void ReturnObject();
}