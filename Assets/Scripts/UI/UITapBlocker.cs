using UnityEngine;

public static class UITapBlocker
{
    private static float _lastUiTime = -999f;

    // how long to block world taps after any UI click
    public static float BlockSeconds = 0.25f;

    public static void NotifyUI()
    {
        _lastUiTime = Time.time;
    }

    public static bool ShouldBlock()
    {
        return (Time.time - _lastUiTime) < BlockSeconds;
    }
}