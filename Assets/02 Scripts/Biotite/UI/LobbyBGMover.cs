using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LobbyBGMover : BaseMeshEffect
{
    [Header("스크롤 속도")]
    [SerializeField] private float scrollSpeed = 0.2f;

    // 왼쪽(-X), 아래(-Y)
    private Vector2 moveDirection = new Vector2(-1f, -1f);
    private Vector2 currentOffset = Vector2.zero;

    protected override void Start()
    {
        base.Start();
        moveDirection = moveDirection.normalized;
    }

    private void Update()
    {
        currentOffset += moveDirection * (scrollSpeed * Time.deltaTime);

        currentOffset.x %= 1f;
        currentOffset.y %= 1f;

        if (graphic != null)
        {
            graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        List<UIVertex> vertices = new List<UIVertex>();
        vh.GetUIVertexStream(vertices);

        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex vertex = vertices[i];
            vertex.uv0 += (Vector4)currentOffset;
            vertices[i] = vertex;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(vertices);
    }
}