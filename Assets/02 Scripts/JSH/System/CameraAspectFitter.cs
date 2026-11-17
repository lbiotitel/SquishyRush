using UnityEngine;

public class CameraAspectFitter : MonoBehaviour
{
    // 기준 해상도 비율 (가로/세로)
    [SerializeField] private float referenceAspect = 9f / 16f;

    // 기준 해상도에서 쓰던 FOV
    [SerializeField] private float referenceVerticalFov = 80f;

    // 같은 FOV를 써야 하는 카메라 (오버레이 카메라 등). 월드 UI(체력바)가 유닛과 어긋나지 않게 합니다.
    [SerializeField] private Camera[] followCameras;
    
    private Camera cam;
    private float lastAspect;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    private void Update()
    {
        if (!Mathf.Approximately(cam.aspect, lastAspect)) Apply();
    }

    private void Apply()
    {
        lastAspect = cam.aspect;
        
        // 기준보다 넓은 화면: 세로 FOV 유지 (가로가 더 보임)
        if (cam.aspect >= referenceAspect)
        {
            cam.fieldOfView = referenceVerticalFov;
        }
        else
        {
            // 기준보다 좁은 화면: 가로 FOV를 유지하도록 세로 FOV를 늘림
            float hFov = Camera.VerticalToHorizontalFieldOfView(referenceVerticalFov, referenceAspect);
            cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(hFov, cam.aspect);
        }
        
        SyncFollowers();
    }

    private void SyncFollowers()
    {
        if (followCameras == null) return;
        for (int i = 0; i < followCameras.Length; i++)
            if (followCameras[i] != null) followCameras[i].fieldOfView = cam.fieldOfView;
    }
}
