using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance;
    public StoneMeshDatabase meshDatabase;

    Queue<(StoneEntity a, StoneEntity b)> mergeQueue = new();   

    public List<MergeResult> thisTurnResults = new();

    private void Awake()
    {
        Instance = this;
        StoneVisualUpgrader.Database = meshDatabase;
    }

    public void QueueMerge(StoneEntity a, StoneEntity b)
    {
        mergeQueue.Enqueue((a, b));
    }

    private void LateUpdate()
    {
        while(mergeQueue.Count > 0)
        {
            var (a, b) = mergeQueue.Dequeue();
            ProcessMerge(a, b);
        }
    }

    void ProcessMerge(StoneEntity a, StoneEntity b)
    {
        StoneEntity hero, wildcard = null;

        if (a.isWildcard) { wildcard = a; hero = b; }
        else if(b.isWildcard) { wildcard = b; hero = a; }
        else
        {
            hero = (a.star >= b.star) ? a : b;
            wildcard = (a.star >= b.star) ? b : a;
        }

        StoneEntity survivor = hero;
        StoneEntity consumed = wildcard;

        int newStar = survivor.star + 1;
        Vector3 mergePos = (a.transform.position + b.transform.position) / 2f;

        thisTurnResults.Add(new MergeResult { heroId = survivor.heroId, newStar = newStar });

        StonePhysicsReactor.ApplyMergePop(survivor, newStar);
        StoneVisualUpgrader.Upgrade(survivor, newStar, mergePos);

        consumed.transform.DOKill();
        Destroy(consumed.gameObject);
        survivor.IsMerging = false;
    }

    public struct MergeResult
    {
        public HeroId heroId;
        public int newStar;
    }
}
