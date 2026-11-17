using DG.Tweening;
using UnityEngine;

public static class StoneVisualUpgrader
{
    public static StoneMeshDatabase Database;

    public static void Upgrade(StoneEntity stone, int newStar, Vector3 pos)
    {
        stone.star = newStar;
        stone.transform.position = pos;

        Mesh newMesh = Database.GetMesh(stone.heroId, newStar);
        stone.GetComponent<MeshFilter>().sharedMesh = newMesh;

        var mc = stone.MeshCol;
        mc.sharedMesh = null;
        mc.sharedMesh = newMesh;
        mc.convex = true;

        var rb = stone.Rb;
        rb.velocity = Vector3.zero;

        stone.transform.DOKill();
        stone.transform.localScale = Vector3.one;
        stone.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
    }
}
