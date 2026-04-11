using UnityEngine;

/// <summary>
/// Type of anchor.
/// Indoor  = MultiSet VPS (map-local)
/// Outdoor = ARCore Earth (geospatial)
/// </summary>
public enum AnchorType
{
    Indoor,
    Outdoor
}

public class AnchorData : MonoBehaviour
{
    [Header("Common")]
    public AnchorType anchorType = AnchorType.Indoor;

    [Tooltip("Display title for the anchor")]
    public string title = "";

    [Tooltip("Display description for the anchor")]
    public string description = "";

    // =========================
    // Indoor (VPS / Map-local)
    // =========================
    [Header("Indoor (VPS / Map-local)")]
    [Tooltip("Indoor map ID (e.g. cdis_lab)")]
    public string mapId = "";

    [Tooltip("Position in MAP-LOCAL coordinates")]
    public Vector3 localPosition;
    public Quaternion indoorLocalRotation;

    // =========================
    // Outdoor (ARCore Earth)
    // =========================
    [Header("Outdoor (ARCore Earth)")]
    [Tooltip("Latitude in degrees")]
    public double latitude;

    [Tooltip("Longitude in degrees")]
    public double longitude;

    [Tooltip("Altitude in meters (absolute geospatial altitude)")]
    public double altitude;

    [Tooltip("Heading in degrees (0..360). Recommended for consistent orientation in Explorer.")]
    public double heading;

    // ============================================================
    // Save-time quality + altitude strategy metadata
    // (For fallback strategies + debugging)
    // ============================================================
    [Header("Outdoor Save Metadata (optional but recommended)")]
    [Tooltip("Camera HorizontalAccuracy (meters) at save time")]
    public float savedHAcc = -1f;

    [Tooltip("Camera VerticalAccuracy (meters) at save time")]
    public float savedVAcc = -1f;

    [Tooltip("Camera yaw/heading accuracy (degrees) at save time")]
    public float savedYawAcc = -1f;

    [Tooltip("Altitude mode used/stored. e.g. 'absolute' or 'cameraRelative'")]
    public string savedAltitudeMode = "";

    [Tooltip("tapAlt - camAlt (meters). Useful for camera-relative fallback.")]
    public float savedAltitudeOffset = 0f;

    [Tooltip("Absolute altitude captured at save time (meters). Usually same as 'altitude' but stored as float for quick checks.")]
    public float savedAltitudeAbsolute = 0f;

    // =========================
    // Evaluation / Debug (Outdoor)
    // =========================
    [Header("Eval / Debug (Outdoor)")]
    [Tooltip("Unity world position of the EARTH ANCHOR at save-time (Contributor).")]
    public Vector3 evalSaveWorld;

    [Tooltip("Camera geospatial pose at save-time (Contributor).")]
    public double evalSaveCamLat;
    public double evalSaveCamLng;
    public double evalSaveCamAlt;

    [Tooltip("Save-time accuracy (Contributor).")]
    public float evalSaveHAcc;
    public float evalSaveVAcc;
    public float evalSaveYawAcc;

    [Tooltip("Altitude strategy used when saving.")]
    public string evalAltitudeMode = ""; // "absolute" or "cameraRelative"

    [Tooltip("tapAlt - camAlt if cameraRelative (or for eval even if absolute).")]
    public float evalAltitudeOffset = 0f;

    // =========================
    // Setters
    // =========================
    public void SetIndoor(string mapId, Vector3 localPos, Quaternion localRot)
{
    anchorType = AnchorType.Indoor;
    this.mapId = mapId ?? "";
    localPosition = localPos;
    indoorLocalRotation = localRot;

    // Hygiene: clear outdoor fields
    latitude = longitude = altitude = 0.0;
    heading = 0.0;

    // Clear outdoor metadata
    savedHAcc = savedVAcc = savedYawAcc = -1f;
    savedAltitudeMode = "";
    savedAltitudeOffset = 0f;
    savedAltitudeAbsolute = 0f;

    // Clear eval/debug
    evalSaveWorld = Vector3.zero;
    evalSaveCamLat = evalSaveCamLng = evalSaveCamAlt = 0.0;
    evalSaveHAcc = evalSaveVAcc = evalSaveYawAcc = -1f;
    evalAltitudeMode = "";
    evalAltitudeOffset = 0f;
}

    public void SetOutdoor(double lat, double lng, double alt, double headingDeg = 0.0)
    {
        anchorType = AnchorType.Outdoor;
        latitude = lat;
        longitude = lng;
        altitude = alt;

        // Normalize heading into [0, 360)
        heading = headingDeg % 360.0;
        if (heading < 0.0) heading += 360.0;

        // Hygiene: clear indoor fields
        mapId = "";
        localPosition = Vector3.zero;
        indoorLocalRotation = Quaternion.identity;

        // Keep metadata as-is (caller fills it), but keep float absolute in sync by default
        savedAltitudeAbsolute = (float)alt;
    }

    /// <summary>
    /// Convenience helper so AnchorPlacer can set ALL save/eval fields consistently
    /// from ONE camera pose and ONE computed altitude offset.
    /// </summary>
    public void SetOutdoorSaveSnapshot(
        Vector3 anchorWorld,
        double camLat, double camLng, double camAlt,
        float camHAcc, float camVAcc, float camYawAcc,
        string altitudeMode,
        float altitudeOffsetMeters
    )
    {
        // "Saved" metadata (strategy + quality)
        savedHAcc = camHAcc;
        savedVAcc = camVAcc;
        savedYawAcc = camYawAcc;

        savedAltitudeMode = altitudeMode ?? "";
        savedAltitudeOffset = altitudeOffsetMeters;

        // For completeness: keep float absolute in sync with current 'altitude'
        savedAltitudeAbsolute = (float)altitude;

        // Eval/debug snapshot (same values, one source of truth)
        evalSaveWorld = anchorWorld;

        evalSaveCamLat = camLat;
        evalSaveCamLng = camLng;
        evalSaveCamAlt = camAlt;

        evalSaveHAcc = camHAcc;
        evalSaveVAcc = camVAcc;
        evalSaveYawAcc = camYawAcc;

        evalAltitudeMode = savedAltitudeMode;
        evalAltitudeOffset = savedAltitudeOffset;
    }
}