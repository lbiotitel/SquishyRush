using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    [SerializeField] private Transform wildcardSpawnPoint;
    [SerializeField] private GameObject wildcardPrefab;
    GameObject currentWildcard;

    private void Awake() => Instance = this;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
        {
            ResetWildcard();
        }
    }


    public void ResetWildcard()
    {
        if(currentWildcard != null)
        {
            Destroy(currentWildcard);
        }

        currentWildcard = Instantiate(wildcardPrefab, wildcardSpawnPoint.position, Quaternion.identity);
    }
}
