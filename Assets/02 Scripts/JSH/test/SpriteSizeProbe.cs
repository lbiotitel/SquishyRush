using UnityEngine;

public class SpriteSizeProbe : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 deviceResolution = new Vector2(1170, 2532);

    [ContextMenu("Measure")]
    public void Measure()
    {
        var sr = GetComponent<SpriteRenderer>();
        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (sr == null || sr.sprite == null || cam == null)
        {
            Logger.LogError("SpriteRenderer 또는 Camera 없음");
            return;
        }

        Bounds b = sr.bounds;
        Vector3 bottom = cam.WorldToScreenPoint(new Vector3(b.center.x, b.min.y, b.center.z));
        Vector3 top    = cam.WorldToScreenPoint(new Vector3(b.center.x, b.max.y, b.center.z));
        Vector3 left   = cam.WorldToScreenPoint(new Vector3(b.min.x, b.center.y, b.center.z));
        Vector3 right  = cam.WorldToScreenPoint(new Vector3(b.max.x, b.center.y, b.center.z));

        float wFrac = Mathf.Abs(right.x - left.x) / cam.pixelWidth;
        float hFrac = Mathf.Abs(top.y - bottom.y) / cam.pixelHeight;

        Logger.Log(
            $"[{sr.sprite.name}]\n" +
            $"원본 : {sr.sprite.rect.width} x {sr.sprite.rect.height}\n" +
            $"화면 비율 : 가로 {wFrac * 100f:F1}% / 세로 {hFrac * 100f:F1}%\n" +
            $"기기 렌더 : {wFrac * deviceResolution.x:F0} x {hFrac * deviceResolution.y:F0} px\n" +
            $"카메라 거리 : {Vector3.Distance(cam.transform.position, transform.position):F2}");
    }
}
