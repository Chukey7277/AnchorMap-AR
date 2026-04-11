using UnityEngine;
using TMPro;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;

public class EarthTrackingStatusUI : MonoBehaviour
{
    [Header("References")]
    public AREarthManager earthManager;

    [Header("UI")]
    public GameObject waitPanel;
    public TMP_Text statusText;

    [Tooltip("Optional. Add CanvasGroup on EarthTrackingWaitPanel and drag it here.")]
    public CanvasGroup waitPanelCanvasGroup;

    [Header("Accuracy thresholds (STRICT)")]
    public float strictHorizontalAccuracy = 10f;
    public float strictYawAccuracy = 15f;

    [Header("Accuracy thresholds (RELAXED)")]
    public float relaxedHorizontalAccuracy = 20f;
    public float relaxedYawAccuracy = 30f;

    [HideInInspector] public bool allowDisplay = false;

    public bool IsPlaceable { get; private set; }
    public bool IsReady { get; private set; }

    private static float GetYawAccuracyDeg(GeospatialPose pose)
    {
        var t = typeof(GeospatialPose);

        var p1 = t.GetProperty("OrientationYawAccuracy");
        if (p1 != null) return System.Convert.ToSingle(p1.GetValue(pose, null));
        var f1 = t.GetField("OrientationYawAccuracy");
        if (f1 != null) return System.Convert.ToSingle(f1.GetValue(pose));

        var p2 = t.GetProperty("HeadingAccuracy");
        if (p2 != null) return System.Convert.ToSingle(p2.GetValue(pose, null));
        var f2 = t.GetField("HeadingAccuracy");
        if (f2 != null) return System.Convert.ToSingle(f2.GetValue(pose));

        return float.PositiveInfinity;
    }

    void Update()
    {
        // Welcome/Indoor/etc: tracking HUD should be hidden.
        if (!allowDisplay)
        {
            SetPanelVisible(false, blockTouches: false);
            SetStatus("");
            IsPlaceable = false;
            IsReady = false;
            return;
        }

        if (earthManager == null)
        {
            SetPanelVisible(true, blockTouches: true);
            SetStatus("⚠️ AREarthManager missing");
            IsPlaceable = false;
            IsReady = false;
            return;
        }

        bool earthReady =
            earthManager.EarthTrackingState == TrackingState.Tracking &&
            earthManager.EarthState == EarthState.Enabled;

        if (!earthReady)
        {
            // Show HUD while waiting; you can choose to block touches here if you want.
            SetPanelVisible(true, blockTouches: true);
            SetStatus(
                "🌍 Initializing Earth tracking\n\n" +
                "• Move phone slowly\n" +
                "• Point camera at surroundings\n" +
                "• Ensure GPS + internet"
            );
            IsPlaceable = false;
            IsReady = false;
            return;
        }

        // Earth is TRACKING + ENABLED:
        // Keep HUD visible, but DON'T block touches.
        SetPanelVisible(true, blockTouches: false);

        GeospatialPose pose = earthManager.CameraGeospatialPose;
        float yawAcc = GetYawAccuracyDeg(pose);

        IsPlaceable =
            pose.HorizontalAccuracy <= relaxedHorizontalAccuracy &&
            yawAcc <= relaxedYawAccuracy;

        IsReady =
            pose.HorizontalAccuracy <= strictHorizontalAccuracy &&
            yawAcc <= strictYawAccuracy;

        string geo =
            $"Lat: {pose.Latitude:F6}\n" +
            $"Lng: {pose.Longitude:F6}\n" +
            $"Alt: {pose.Altitude:F1} m\n";

        string acc =
            $"HAcc: ±{pose.HorizontalAccuracy:F1} m\n" +
            $"VAcc: ±{pose.VerticalAccuracy:F1} m\n" +
            $"YawAcc: ±{yawAcc:F1}°\n";

        if (!IsPlaceable)
        {
            SetStatus(
                "📡 Tracking ready, improving location…\n\n" +
                geo + "\n" +
                acc
            );
        }
        else if (!IsReady)
        {
            SetStatus(
                "🟡 Location usable\n\n" +
                geo + "\n" +
                acc
            );
        }
        else
        {
            SetStatus(
                "✅ Earth tracking READY\n\n" +
                geo + "\n" +
                acc
            );
        }
    }

    void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }

    void SetPanelVisible(bool visible, bool blockTouches)
    {
        if (waitPanel != null)
            waitPanel.SetActive(visible);

        if (waitPanelCanvasGroup != null)
        {
            waitPanelCanvasGroup.alpha = visible ? 1f : 0f;
            waitPanelCanvasGroup.blocksRaycasts = visible && blockTouches;
            waitPanelCanvasGroup.interactable = visible && blockTouches;
        }
    }
}