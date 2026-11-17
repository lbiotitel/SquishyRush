using UnityEngine;

public enum HeroId
{
    Wildcard,
    HeroA,
    HeroB,
    HeroC,
    HeroD
}

public class StoneEntity : MonoBehaviour
{
    public HeroId heroId;
    public int star;
    public bool isWildcard => heroId == HeroId.Wildcard;

    MeshCollider meshCollider;
    public MeshCollider MeshCol => meshCollider ?? GetComponent<MeshCollider>();

    Rigidbody rb;
    public Rigidbody Rb => rb ??= GetComponent<Rigidbody>();

    bool isMerging;
    public bool IsMerging { get => isMerging; set => isMerging = value; }

    private void OnEnable() => StoneRegistry.ActiveStones.Add(this);
    private void OnDisable() => StoneRegistry.ActiveStones.Remove(this);

}
