using UnityEngine;

public class StoneCollisionHandler : MonoBehaviour
{
    StoneEntity self;

    private void Awake() => self = GetComponent<StoneEntity>();

    private void OnCollisionEnter(Collision col)
    {
        var other = col.gameObject.GetComponent<StoneEntity>();
        if(other == null) return;
        if(self.IsMerging || other.IsMerging) return;   

        if(CanMerge(self, other))
        {
            self.IsMerging = true;
            other.IsMerging = true;
            MergeManager.Instance.QueueMerge(self, other);
        }

        bool CanMerge(StoneEntity a, StoneEntity b)
        {
            if(a.isWildcard || b.isWildcard)
            {
                var hero = a.isWildcard ? b : a;
                return hero.star < 3;
            }

            return a.heroId == b.heroId && a.star == b.star && a.star < 3 && b.star < 3;
        }
    }
}
