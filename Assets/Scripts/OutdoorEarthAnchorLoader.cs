using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;
using System.Collections.Generic;

public class OutdoorEarthAnchorLoader : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("AREarthManager on XR Origin")]
    public AREarthManager earthManager;

    [Tooltip("ARAnchorManager on XR Origin (AR Foundation)")]
    public ARAnchorManager anchorManager;

    [Tooltip("Optional parent for spawned pins (NOT used when parentUnderEarthAnchor=true).")]
    public Transform anchorsRoot;

    public GameObject anchorPrefab;

    [Header("Explorer Tap (required for details on tap)")]
    public bool forceAnchorPinLayer = true;

    [Tooltip("Your prefab layer is ARPin, so keep this as ARPin to match AnchorTapExplorer mask.")]
    public string anchorPinLayerName = "ARPin";

    public bool ensureCollider = true;

    [Tooltip("Fallback collider size if we can't fit to renderers.")]
    public Vector3 autoColliderSize = new Vector3(0.25f, 0.25f, 0.25f);

    [Header("Outdoor visibility / relevance (IMPORTANT)")]
    [Tooltip("Only spawn outdoor anchors within this radius (meters) from current camera geo pose.")]
    public float maxLoadRadiusMeters = 250f;

    [Tooltip("If true, skip anchors that are too far away. Strongly recommended.")]
    public bool filterByDistance = true;

    [Header("Gating (recommended)")]
    public EarthTrackingStatusUI earthTrackingUI;
    public bool requirePlaceable = true;

    [Header("Accuracy gate (HIGHLY recommended)")]
    public bool requireGoodAccuracy = true;
    public float maxHorizontalAccuracyMeters = 6f;
    public float maxVerticalAccuracyMeters = 10f;
    public float maxYawAccuracyDegrees = 25f;

    [Header("Stability gate (recommended)")]
    public float requireStableSeconds = 2.0f;

    [Header("Reload / Debug")]
    public bool parentUnderEarthAnchor = true;
    public bool logEveryFrame = false;
    public bool logPerAnchor = true;

    [Header("Critical behavior")]
    public bool warnIfUnderCamera = true;
    public bool warnIfNotUnderXROrigin = true;
    public bool driftDetector = true;

    private bool hasLoaded = false;
    private bool loadInFlight = false;
    private float goodAccStartTime = -1f;

    private Vector3 loaderStartPos;
    private Vector3 camStartPos;

    private readonly List<ARGeospatialAnchor> createdEarthAnchors = new List<ARGeospatialAnchor>();

    // ------------------------------------------------------------
    // PUBLIC API (called from ModePanel)
    // ------------------------------------------------------------
    public void ResetAndLoad(bool clearSpawned = true)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[OutdoorLoader] ResetAndLoad()");
        hasLoaded = false;
        loadInFlight = false;
        goodAccStartTime = -1f;

        if (clearSpawned)
            ClearSpawned();
#endif
    }

    public void ClearSpawned()
    {
        for (int i = createdEarthAnchors.Count - 1; i >= 0; i--)
        {
            if (createdEarthAnchors[i] != null)
                Destroy(createdEarthAnchors[i].gameObject);
        }
        createdEarthAnchors.Clear();

        if (anchorsRoot != null)
        {
            for (int i = anchorsRoot.childCount - 1; i >= 0; i--)
                Destroy(anchorsRoot.GetChild(i).gameObject);
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void Start()
    {
        loaderStartPos = transform.position;
        if (Camera.main != null) camStartPos = Camera.main.transform.position;
    }

    void Update()
    {
        // -------------------- hierarchy sanity checks --------------------
        if (warnIfUnderCamera && Camera.main != null && transform.IsChildOf(Camera.main.transform))
        {
            Debug.LogError("[OutdoorLoader] ERROR: OutdoorEarthAnchorLoader is under ARCamera in hierarchy. " +
                           "Move this object OUTSIDE ARCamera / Camera Offset.");
        }

        if (warnIfNotUnderXROrigin && anchorManager != null)
        {
            if (!transform.IsChildOf(anchorManager.transform))
            {
                Debug.LogWarning("[OutdoorLoader] WARNING: OutdoorEarthAnchorLoader is NOT under XR Origin / AnchorManager. " +
                                 "Recommended: put this loader under XR Origin (not under ARCamera).");
                warnIfNotUnderXROrigin = false; // warn once
            }
        }

        if (driftDetector && Time.frameCount % 60 == 0 && Camera.main != null)
        {
            Vector3 loaderNow = transform.position;
            Vector3 camNow = Camera.main.transform.position;

            Vector3 loaderDelta = loaderNow - loaderStartPos;
            Vector3 camDelta = camNow - camStartPos;

            Debug.Log($"[OutdoorLoader][DriftDetector] loaderΔ={loaderDelta} camΔ={camDelta} " +
                      $"(If loaderΔ tracks camΔ, your loader is parented under something moving.)");
        }

        if (logEveryFrame)
        {
            Debug.Log(
                $"[OutdoorLoader][Update] " +
                $"hasLoaded={hasLoaded}, loadInFlight={loadInFlight}, " +
                $"earthState={(earthManager ? earthManager.EarthTrackingState.ToString() : "null")}, " +
                $"placeable={(earthTrackingUI ? earthTrackingUI.IsPlaceable.ToString() : "null")}, " +
                $"firebaseReady={(FirestoreAnchorService.Instance != null && FirestoreAnchorService.Instance.IsReady)}"
            );
        }

        if (hasLoaded || loadInFlight) return;

        if (earthManager == null || anchorManager == null || anchorPrefab == null)
            return;

        if (earthManager.EarthTrackingState != TrackingState.Tracking)
            return;

        if (requirePlaceable && earthTrackingUI != null && !earthTrackingUI.IsPlaceable)
            return;

        if (FirestoreAnchorService.Instance == null || !FirestoreAnchorService.Instance.IsReady)
        {
            goodAccStartTime = -1f;
            return;
        }

        // -------------------- Accuracy gate + stability window --------------------
        var camGeo = earthManager.CameraGeospatialPose;
        float yawAcc = GetYawAccuracyDeg(camGeo);

        bool goodAcc =
            (!requireGoodAccuracy) ||
            (camGeo.HorizontalAccuracy <= maxHorizontalAccuracyMeters &&
             camGeo.VerticalAccuracy <= maxVerticalAccuracyMeters &&
             yawAcc <= maxYawAccuracyDegrees);

        if (!goodAcc)
        {
            goodAccStartTime = -1f;
            return;
        }

        if (goodAccStartTime < 0f)
            goodAccStartTime = Time.time;

        float goodFor = Time.time - goodAccStartTime;
        if (goodFor < requireStableSeconds)
            return;

        Debug.Log(
            $"[OutdoorLoader] LOAD START camGeo lat={camGeo.Latitude:F6}, lng={camGeo.Longitude:F6}, alt={camGeo.Altitude:F1}, " +
            $"HAcc={camGeo.HorizontalAccuracy:F1} VAcc={camGeo.VerticalAccuracy:F1} YawAcc={yawAcc:F1}, " +
            $"stableFor={goodFor:F1}s"
        );

        LoadOutdoorAnchors();
    }

    private static float GetYawAccuracyDeg(GeospatialPose pose)
{
    var t = typeof(GeospatialPose);

    // Newer API name
    var p1 = t.GetProperty("OrientationYawAccuracy");
    if (p1 != null) return Convert.ToSingle(p1.GetValue(pose, null));

    var f1 = t.GetField("OrientationYawAccuracy");
    if (f1 != null) return Convert.ToSingle(f1.GetValue(pose));

    // Older API name
    var p2 = t.GetProperty("HeadingAccuracy");
    if (p2 != null) return Convert.ToSingle(p2.GetValue(pose, null));

    var f2 = t.GetField("HeadingAccuracy");
    if (f2 != null) return Convert.ToSingle(f2.GetValue(pose));

    return float.PositiveInfinity;
}

    void LoadOutdoorAnchors()
    {
        loadInFlight = true;
        Debug.Log("[OutdoorLoader] Loading Outdoor anchors from FirestoreAnchorService...");

        FirestoreAnchorService.Instance.LoadOutdoorAnchorDocuments(
            onLoaded: docs =>
            {
                loadInFlight = false;

                var camGeo = earthManager.CameraGeospatialPose;
                Camera cam = Camera.main;

                Debug.Log($"[OutdoorLoader] Received {docs.Count} outdoor docs. filterByDistance={filterByDistance} radius={maxLoadRadiusMeters}m");

                int spawned = 0;
                int skippedFar = 0;
                int skippedBad = 0;

                foreach (var d in docs)
                {
                    string docId = d.TryGetValue("docId", out var idObj) ? idObj?.ToString() : "(no-docId)";

                    if (!TryGetDouble(d, "latitude", out double lat) ||
                        !TryGetDouble(d, "longitude", out double lng))
                    {
                        if (logPerAnchor) Debug.LogWarning($"[OutdoorLoader] Doc {docId} missing lat/lng (skipping).");
                        skippedBad++;
                        continue;
                    }

                    // Altitude strategy:
                    // - If doc has altitudeMode=cameraRelative, use camGeo.Altitude + altitudeOffset (meters).
                    // - Else, use saved absolute altitude field "altitude".
                    double alt = camGeo.Altitude;

// NEW FIELD NAMES (match AnchorPlacer)
string altitudeMode = TryGetString(d, "evalAltitudeMode", out var am) ? am : "";

if (!string.IsNullOrEmpty(altitudeMode) && altitudeMode.Equals("relative", StringComparison.OrdinalIgnoreCase))
{
    double offset = 0.0;
    TryGetDouble(d, "evalAltitudeOffset", out offset);

    alt = camGeo.Altitude + offset;

    if (logPerAnchor)
        Debug.Log($"[OutdoorLoader] Using RELATIVE altitude: camAlt={camGeo.Altitude:F2} + offset={offset:F2} = {alt:F2}");
}
else
{
    // ABSOLUTE fallback
    if (!TryGetDouble(d, "savedAltitudeAbsolute", out alt))
    {
        // fallback to legacy field
        if (!TryGetDouble(d, "altitude", out alt))
            alt = camGeo.Altitude;
    }

    if (logPerAnchor)
        Debug.Log($"[OutdoorLoader] Using ABSOLUTE altitude: {alt:F2}");
}

                    if (Math.Abs(lat) < 0.000001 && Math.Abs(lng) < 0.000001)
                    {
                        if (logPerAnchor) Debug.LogWarning($"[OutdoorLoader] Doc {docId} invalid geo (0,0) (skipping).");
                        skippedBad++;
                        continue;
                    }

                    if (filterByDistance)
                    {
                        double groundDist = GeoDistanceMeters(camGeo.Latitude, camGeo.Longitude, lat, lng);
                        if (groundDist > maxLoadRadiusMeters)
                        {
                            skippedFar++;
                            if (logPerAnchor) Debug.Log($"[OutdoorLoader] Doc {docId} skipped (too far) dist={groundDist:F1}m");
                            continue;
                        }
                    }

                    // Heading
                    double heading = camGeo.Heading;
                    if (TryGetDouble(d, "heading", out double savedHeading))
                        heading = savedHeading;

                    Quaternion rot = Quaternion.Euler(0f, (float)heading, 0f);

                    if (logPerAnchor)
                    {
                        Debug.Log($"[OutdoorLoader] DOC {docId} geo lat={lat:F6}, lng={lng:F6}, alt={alt:F2} mode='{altitudeMode}', heading={heading:F1}");
                    }

                    ARGeospatialAnchor earthAnchor = anchorManager.AddAnchor(lat, lng, alt, rot);

                    if (earthAnchor == null)
                    {
                        Debug.LogWarning($"[OutdoorLoader] Doc {docId} AddAnchor FAILED for lat={lat:F6}, lng={lng:F6}, alt={alt:F2}");
                        skippedBad++;
                        continue;
                    }
                    Vector3 loadWorld = earthAnchor.transform.position;

if (loadWorld == Vector3.zero)
{
    Debug.LogWarning($"[EVAL][{docId}] loadWorld is ZERO right after AddAnchor (may update next frame).");
}

double savedAbsAlt = double.NaN;
TryGetDouble(d, "altitude", out savedAbsAlt);

// --- world drift eval (Contributor world vs Explorer world) ---
if (TryGetDouble(d, "saveWorldX", out double sx) &&
    TryGetDouble(d, "saveWorldY", out double sy) &&
    TryGetDouble(d, "saveWorldZ", out double sz))
{
    Vector3 saveWorld = new Vector3((float)sx, (float)sy, (float)sz);
    float worldDrift = Vector3.Distance(saveWorld, loadWorld);

    Debug.Log($"[EVAL][{docId}] worldDrift={worldDrift:F2}m saveWorld={saveWorld} loadWorld={loadWorld}");
}

// --- altitude drift eval (Camera altitude at save time vs now) ---
if (TryGetDouble(d, "saveCamAlt", out double saveCamAlt))
{
    var camNow = earthManager.CameraGeospatialPose;

    double relAltThen = alt - saveCamAlt;
    double relAltNow  = alt - camNow.Altitude;

    Debug.Log($"[EVAL][{docId}] relAltThen={relAltThen:F1}m relAltNow={relAltNow:F1}m camAltNow={camNow.Altitude:F1} usedAlt={alt:F1} savedAbsAlt={savedAbsAlt:F1}");
}

                    createdEarthAnchors.Add(earthAnchor);

                    Vector3 earthWorld = earthAnchor.transform.position;
                    float dist3D = (cam != null) ? Vector3.Distance(cam.transform.position, earthWorld) : -1f;

                    if (logPerAnchor)
                        Debug.Log($"[OutdoorLoader] Doc {docId} AddAnchor OK earthWorld={earthWorld} dist3DFromCam={dist3D:F1}m");

                    GameObject pin;
                    if (parentUnderEarthAnchor)
                    {
                        pin = Instantiate(anchorPrefab, earthAnchor.transform);
                        pin.transform.localPosition = Vector3.zero;
                        pin.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                        pin.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        Transform parent = (anchorsRoot != null) ? anchorsRoot : earthAnchor.transform;
                        pin = Instantiate(anchorPrefab, parent);
                        pin.transform.position = earthAnchor.transform.position;
                        pin.transform.rotation = earthAnchor.transform.rotation;
                        pin.transform.Rotate(-90f, 0f, 0f, Space.Self);
                        pin.transform.localScale = Vector3.one;
                    }

                    EnsureTappable(pin);

                    AnchorData data = pin.GetComponent<AnchorData>();
                    if (data == null) data = pin.AddComponent<AnchorData>();

                    data.SetOutdoor(lat, lng, alt, heading);

                    if (d.TryGetValue("title", out var t) && t != null) data.title = t.ToString();
                    if (d.TryGetValue("description", out var desc) && desc != null) data.description = desc.ToString();

                    spawned++;

                    if (logPerAnchor)
                    {
                        string layerName = LayerMask.LayerToName(pin.layer);
                        Debug.Log($"[OutdoorLoader] Doc {docId} Spawned pin '{data.title}' layer={layerName} (rootLayer={pin.layer})");
                        if (cam != null)
                        {
                            Vector3 vp = cam.WorldToViewportPoint(pin.transform.position);
                            Debug.Log($"[OutdoorLoader] Doc {docId} viewport={vp} (z<0 means behind camera; x/y outside 0..1 means off-screen)");
                        }
                    }
                }

                Debug.Log($"[OutdoorLoader] Done. spawned={spawned}, skippedFar={skippedFar}, skippedBad={skippedBad}.");
                hasLoaded = true;
            },
            onError: ex =>
            {
                loadInFlight = false;
                Debug.LogError("[OutdoorLoader] LoadOutdoorAnchorDocuments error: " + ex);
            }
        );
    }

    void EnsureTappable(GameObject pin)
    {
        if (pin == null) return;

        // 1) collider: fit to renderers if possible (more reliable than a fixed cube at origin)
        if (ensureCollider)
        {
            Collider existing = pin.GetComponentInChildren<Collider>();
            if (existing == null)
            {
                // try fit to renderers
                var renderers = pin.GetComponentsInChildren<Renderer>();
                if (renderers != null && renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                        b.Encapsulate(renderers[i].bounds);

                    // convert world bounds -> local center/size on the root
                    Vector3 localCenter = pin.transform.InverseTransformPoint(b.center);

                    // approximate local size by projecting bounds extents (ok for mostly axis-aligned pins)
                    Vector3 worldSize = b.size;
                    Vector3 localSize = new Vector3(
                        Mathf.Abs(Vector3.Dot(worldSize, pin.transform.right.normalized)),
                        Mathf.Abs(Vector3.Dot(worldSize, pin.transform.up.normalized)),
                        Mathf.Abs(Vector3.Dot(worldSize, pin.transform.forward.normalized))
                    );

                    var bc = pin.AddComponent<BoxCollider>();
                    bc.center = localCenter;
                    bc.size = new Vector3(
                        Mathf.Max(0.05f, localSize.x),
                        Mathf.Max(0.05f, localSize.y),
                        Mathf.Max(0.05f, localSize.z)
                    );
                    bc.isTrigger = false;

                    if (logPerAnchor) Debug.Log("[OutdoorLoader] Added fitted BoxCollider from renderer bounds.");
                }
                else
                {
                    var bc = pin.AddComponent<BoxCollider>();
                    bc.size = autoColliderSize;
                    bc.center = Vector3.zero;
                    bc.isTrigger = false;

                    if (logPerAnchor) Debug.Log("[OutdoorLoader] Added fallback BoxCollider (autoColliderSize).");
                }
            }
        }

        // 2) layer
        if (forceAnchorPinLayer)
        {
            int layer = LayerMask.NameToLayer(anchorPinLayerName);
            if (layer >= 0)
                ApplyLayerRecursively(pin, layer);
            else
                Debug.LogWarning($"[OutdoorLoader] Layer '{anchorPinLayerName}' not found. Create it in Project Settings > Tags and Layers.");
        }
    }
#endif

    static bool TryGetDouble(Dictionary<string, object> d, string key, out double value)
    {
        value = 0;
        if (!d.TryGetValue(key, out var obj) || obj == null) return false;
        try { value = Convert.ToDouble(obj); return true; }
        catch { return false; }
    }

    static bool TryGetString(Dictionary<string, object> d, string key, out string value)
    {
        value = null;
        if (!d.TryGetValue(key, out var obj) || obj == null) return false;
        value = obj.ToString();
        return true;
    }

    // Haversine (meters)
    static double GeoDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000.0;
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    static void ApplyLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        for (int i = 0; i < root.transform.childCount; i++)
            ApplyLayerRecursively(root.transform.GetChild(i).gameObject, layer);
    }
}