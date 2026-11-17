
using System;
using UnityEngine;

public static class Logger
{
    public static void Log(string msg)
    {
        #if UNITY_EDITOR
        Debug.Log($"{msg} {DateTime.Now}");
        #endif
    }

    public static void LogWarning(string msg)
    {
        #if UNITY_EDITOR
        Debug.LogWarning($"Warning {msg} {DateTime.Now}");
        #endif
    }

    public static void LogError(string msg)
    {
        #if UNITY_EDITOR
        Debug.LogError($"Error {msg} {DateTime.Now}");
        #endif
    }
}

