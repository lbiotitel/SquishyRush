using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rect;
    private Rect lastSafeArea;
    private Vector2Int lastScreen;

    private void Awake()
    {
        rect = (RectTransform)transform;
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreen.x ||
            Screen.height != lastScreen.y)
            Apply();
    }

    private void Apply()
    {
        Rect safe = Screen.safeArea;
        lastSafeArea = safe;
        lastScreen = new Vector2Int(Screen.width, Screen.height);

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;

        min.x /= Screen.width; min.y /= Screen.height;
        max.x /= Screen.width; max.y /= Screen.height;

        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
