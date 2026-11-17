using UnityEngine;

public class FloatingUI : MonoBehaviour
{
    [SerializeField] private float scale;
    [SerializeField] private float speed;
    private RectTransform rectTransform;
    private Vector2 originPos;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        originPos = rectTransform.anchoredPosition;
    }

    void Update()
    {
        rectTransform.anchoredPosition
            = originPos + new Vector2(0, Mathf.Sin(Time.time * speed) * scale);
    }
}