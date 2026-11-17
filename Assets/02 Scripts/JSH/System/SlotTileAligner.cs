using UnityEngine;

/// <summary>
/// 발판 스프라이트를 BoardView 앵커 위치에 맞춰 한 번에 정렬합니다. (에디터 도구, 게임 로직과 무관)
/// 인스펙터 우측 상단 ⋮ 메뉴 → "발판 정렬" 을 누르면 적용됩니다.
///  - 위치: 각 앵커 위치 + offset
///  - 각도: 전부 같은 X 기울기 (Y, Z 회전 0) → 모든 칸이 같은 모양
///  - 크기: 같은 크기, 뒤쪽 줄(화면 위)은 조금 작게 해서 원근감
/// </summary>
public class SlotTileAligner : MonoBehaviour
{
    [System.Serializable]
    public struct Group
    {
        public string name;
        public Transform[] anchors;
        public SpriteRenderer[] tiles;
        public Vector2 size;
    }

    [SerializeField] private Group[] groups;

    [Header("모양")] 
    [SerializeField] private float tiltX = 60f;
    [SerializeField] private Vector3 offset = new Vector3(0f, -0.1f, 0.05f);

    [Header("원근감")] 
    [SerializeField] private bool useDepthScale = true;
    [SerializeField] private float backScale = 0.9f;   // 가장 위(먼) 줄 크기 비율
    [SerializeField] private float frontScale = 1f; // 가장 아래(가까운) 줄 크기 비율

    [ContextMenu("발판 정렬")]
    private void Align()
    {
        if (groups == null) return;

        // 전투 칸들 중 가장 위/아래 y를 찾아 원근 비율 기준으로 씀
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var g in groups)
            if(g.anchors != null)
                foreach (var a in g.anchors)
                    if (a != null)
                    {
                        minY = Mathf.Min(minY, a.position.y);
                        maxY = Mathf.Max(maxY, a.position.y);
                        
                    }

        foreach (var g in groups)
        {
            if (g.anchors == null || g.tiles == null) continue;
            int n = Mathf.Min(g.anchors.Length, g.tiles.Length);
            for (int i = 0; i < n; i++)
            {
                Transform a = g.anchors[i];
                SpriteRenderer t = g.tiles[i];
                if (a == null || t == null) continue;
                
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(t.transform, "Align Slot Tiles");
                UnityEditor.Undo.RecordObject(t, "Align Slot Tiles");
#endif
                t.transform.position = a.position + offset;
                t.transform.rotation = Quaternion.Euler(tiltX, 0f, 0f);
                t.transform.localScale = Vector3.one;

                float k = 1f;
                if (useDepthScale && maxY > minY)
                    k = Mathf.Lerp(frontScale, backScale, Mathf.InverseLerp(minY, maxY, a.position.y));

                t.drawMode = SpriteDrawMode.Simple;
                Vector2 spriteSize = t.sprite != null ? (Vector2)t.sprite.bounds.size : Vector2.one;
                t.transform.localScale = new Vector3(g.size.x / spriteSize.x * k, g.size.y / spriteSize.y * k, 1f);
            }
        }
        
        
    }
}
