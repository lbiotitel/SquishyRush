using UnityEngine;

/// <summary>UI를 계속 돌립니다. (추가 시너지 팝업 뒤 빛줄기용)</summary>
public class UISpin : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = -20f;
    [SerializeField] private bool ignoreTimeScale = true;  // 배속·일시정지와 상관없이 일정하게

    private void Update()
    {
        float dt = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
        transform.Rotate(0f,0f, degreesPerSecond * dt);
    }
}
