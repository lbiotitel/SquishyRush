using UnityEngine;

[CreateAssetMenu(fileName = "StoneMeshDatabase", menuName = "Game/Stone Mesh Database")]
public class StoneMeshDatabase : ScriptableObject
{
    [System.Serializable]
    public class HeroMeshSet
    {
        public HeroId heroId;
        public Mesh star0Mesh;
        public Mesh star1Mesh;
        public Mesh star2Mesh;
        public Mesh star3Mesh;
    }

    public HeroMeshSet[] heroSets;

    public Mesh GetMesh(HeroId heroId, int star)
    {
        foreach (var set in heroSets)
        {
            if(set.heroId != heroId) continue;
            return star switch
            {
                0 => set.star0Mesh,
                1 => set.star1Mesh,
                2 => set.star2Mesh,
                3 => set.star3Mesh,
                _ => set.star0Mesh
            };
        }
        Logger.Log($"No mesh set found for {heroId}");
        return null;
    }
}
