using UnityEngine;

// 타이틀에서 무한으로 이동하는 카메라입니다.
public class TitleCamera : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Property")] 
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Vector3 startPos;
    [SerializeField] private Vector3 endPos;

    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = transform;
        
        cameraTransform.position = startPos;
    }

    private void Update()
    {
        MoveCamera();
    }

    private void MoveCamera()
    {
        cameraTransform.position = Vector3.MoveTowards(
            cameraTransform.position, 
            endPos, 
            moveSpeed * Time.deltaTime
        );
        
        if (Vector3.Distance(cameraTransform.position, endPos) <= 0.05f)
        {
            cameraTransform.position = startPos;
        }
    }
}