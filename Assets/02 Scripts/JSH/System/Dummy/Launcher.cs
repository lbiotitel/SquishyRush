using UnityEngine;

public class Launcher : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float powerMuliplier = 5f;
    [SerializeField] private float maxPower = 30f;
    [SerializeField] private LineRenderer trajectoryLine;
        
    Vector3 dragStartPos;
    bool isDragging;

    private void Start()
    {
        trajectoryLine.positionCount = 2;
        trajectoryLine.enabled = false;
    }

    void Update()
    {
        if (!isDragging) return;

        Vector3 currentWorld = GetWorldPointOnPlane(Input.mousePosition);
        Vector3 launchDir = (dragStartPos - currentWorld);
        float power = Mathf.Clamp(launchDir.magnitude * powerMuliplier, 0f, maxPower);

        DrawTrajectory(launchDir.normalized, power);
    }

    void OnMouseDown()
    {
        dragStartPos = GetWorldPointOnPlane(Input.mousePosition);
        isDragging = true;
        trajectoryLine.enabled = true;
    }

    void OnMouseUp()
    {
        if(!isDragging) return;
        isDragging = false;
        trajectoryLine.enabled = false;

        Vector3 dragEndPos = GetWorldPointOnPlane(Input.mousePosition);
        Vector3 launchDir = (dragStartPos - dragEndPos); //��� �ݴ� ����
        float power = Mathf.Clamp(launchDir.magnitude * powerMuliplier, 0f, maxPower);
        rb.AddForce(launchDir.normalized * power, ForceMode.Impulse);   
    }

    void DrawTrajectory(Vector3 dir, float power)
    {
        float visualLength = Mathf.Clamp(power * 0.5f, 0.5f, 5f);
        Vector3 start = transform.position;
        Vector3 end = start + dir * visualLength;

        trajectoryLine.SetPosition(0, start);   
        trajectoryLine.SetPosition(1, end);
    }

    Vector3 GetWorldPointOnPlane(Vector3 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        Plane fieldPlane = new Plane(Vector3.up, transform.position);
        fieldPlane.Raycast(ray, out float dist);
        return ray.GetPoint(dist);
    }
}
