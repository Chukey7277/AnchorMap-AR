using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;

public class AnchorPlacer : MonoBehaviour
{
    [Header("Mode")]
    public bool isIndoor = true;

    [Header("Camera")]
    public Camera arCamera;

    [Header("Indoor (MultiSet)")]
    public Transform mapSpace;
    public string currentMapId = "cdis_lab";
    public IndoorLocalizationFlag indoorLocalizationFlag;

    [Header("Common")]
    public Transform anchorsRoot;
    public GameObject anchorPrefab;
    public AnchorInfoUI infoUI;
    public PlacementFeedbackUI feedbackUI;

    [Header("Mode Gate (ModePanel)")]
    public ModePanel modePanel;

    [Header("Outdoor (ARCore Earth)")]
    public ARRaycastManager raycastManager;
    public ARAnchorManager anchorManager;
    public AREarthManager earthManager;
    public EarthTrackingStatusUI earthTrackingUI;

    [Header("Indoor Placement")]
    public float surfaceOffset = 0f; // e.g., 0.01f

    [Header("Indoor Stability Gate (NEW)")]
    [Tooltip("Require indoor localization and mapSpace pose to be stable before allowing placement.")]
    public bool requireIndoorStable = true;

    [Tooltip("Require indoor localization to stay true continuously for this many seconds.")]
    public float indoorRequireStableSeconds = 1.1f;

    [Tooltip("Require mapSpace motion to be below thresholds for this many seconds (uses same indoorRequireStableSeconds).")]
    public bool requireMapSpaceStable = true;

    [Tooltip("If mapSpace moves more than this (meters) between frames, stability timer resets.")]
    public float maxMapSpaceDeltaPosMeters = 0.03f; // 2 cm

    [Tooltip("If mapSpace rotates more than this (degrees) between frames, stability timer resets.")]
    public float maxMapSpaceDeltaRotDeg = 3.0f;

    [Header("Indoor Raycast Preference (NEW)")]
    [Tooltip("If true: try plane hits first; only use feature points if fallback is enabled.")]
    public bool preferPlaneHitsIndoor = true;

    [Header("Outdoor Placement")]
    public float maxTapHitDistanceMeters = 50f;
    public bool allowFeaturePointFallbackIndoor = true;   // NOW USED
    public bool allowFeaturePointFallbackOutdoor = true;  // NOTE: currently unused in your outdoor code

    [Header("Outdoor Stability (recommended)")]
    public bool requireGoodAccuracy = true;
    public float maxHorizontalAccuracyMeters = 10f;
    public float maxVerticalAccuracyMeters = 10f;
    public float maxYawAccuracyDegrees = 25f;

    [Tooltip("Require accuracy to remain good continuously for this many seconds before allowing placement.")]
    public float requireStableSeconds = 2.0f;

    [Header("Outdoor Altitude Guard")]
    public bool guardAltitude = true;
    public float maxAllowedAltitudeJumpMeters = 20f;
    public float maxVerticalAccuracyForPlacementMeters = 5f;

    [Header("Explorer Tap Compatibility")]
    public bool forceAnchorPinLayer = true;
    public string anchorPinLayerName = "ARPin";
    public bool ensureCollider = true;
    public Vector3 autoColliderSize = new Vector3(0.25f, 0.25f, 0.25f);

    private const float IgnoreTapAfterCloseSeconds = 0.2f;

    private static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    // Outdoor stability timer
    private float outdoorGoodAccStartTime = -1f;

    [Header("Outdoor Gate")]
    public bool requireStrictReadyToPlace = true; // true = use earthTrackingUI.IsReady, false = use IsPlaceable

    // Indoor stability timers (NEW)
    private float indoorLocalizedStartTime = -1f;
    private float mapSpaceStableStartTime = -1f;
    private Vector3 lastMapSpacePos;
    private Quaternion lastMapSpaceRot;
    private bool mapSpacePoseInitialized = false;
    [Header("Indoor Physics Fallback (Map Mesh Collider)")]
    public bool allowPhysicsFallbackIndoor = true;

   [Tooltip("LayerMask for your indoor map mesh colliders (under mapSpace).")]
    public LayerMask indoorMapColliderMask;

    [Tooltip("Max distance for physics fallback raycast.")]
    public float maxPhysicsRayDistance = 30f;
    [Header("Mode Gate")]
    public bool allowPlacement = true;

    // ---- Accuracy wrapper (new API uses OrientationYawAccuracy; old uses HeadingAccuracy)
    private static float GetYawAccuracyDeg(GeospatialPose pose)
    {
        var t = typeof(GeospatialPose);

        var p1 = t.GetProperty("OrientationYawAccuracy");
        if (p1 != null) return Convert.ToSingle(p1.GetValue(pose, null));

        var f1 = t.GetField("OrientationYawAccuracy");
        if (f1 != null) return Convert.ToSingle(f1.GetValue(pose));

        var p2 = t.GetProperty("HeadingAccuracy");
        if (p2 != null) return Convert.ToSingle(p2.GetValue(pose, null));

        var f2 = t.GetField("HeadingAccuracy");
        if (f2 != null) return Convert.ToSingle(f2.GetValue(pose));

        return float.PositiveInfinity;
    }

    void Update()
{
    // 1) Block "tap leak" right after switching mode / pressing UI buttons
    if (Time.time - ModePanel.LastModeSwitchTime < 0.25f)
        return;
    
    if (UITapBlocker.ShouldBlock())
        return;
    if (infoUI != null && infoUI.IsOpen)
        return;

    // 2.5) Hard mode gate: never place in Explorer screens
    if (!IsAllowedInCurrentMode())
        return;

    // 2) Your existing gate
    if (!allowPlacement) return;

    // 3) Your existing stability tick
    TickIndoorStability();

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 pos = Input.mousePosition;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                EvaluationLogger.Log(
                    ev: "TapRejected",
                    mode: isIndoor ? "Indoor" : "Outdoor",
                    mapId: isIndoor ? currentMapId : "",
                    success: "failure",
                    details: "UIBlocked",
                    screenPos: pos
                );
                return;
            }

            TryPlaceAnchor(pos);
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Touch t = Input.GetTouch(0);

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
            {
                EvaluationLogger.Log(
                    ev: "TapRejected",
                    mode: isIndoor ? "Indoor" : "Outdoor",
                    mapId: isIndoor ? currentMapId : "",
                    success: "failure",
                    details: "UIBlocked",
                    screenPos: t.position
                );
                return;
            }

            TryPlaceAnchor(t.position);
        }
#endif
    }

    private void TickIndoorStability()
    {
        // If not indoor, no need to track (but harmless if you leave it)
        // We still track because user can switch modes quickly.

        bool localizedNow = (indoorLocalizationFlag != null && indoorLocalizationFlag.IsLocalized);

        if (!localizedNow)
        {
            indoorLocalizedStartTime = -1f;
            mapSpaceStableStartTime = -1f;
            mapSpacePoseInitialized = false;
            return;
        }

        // Localization has become true (or remains true)
        if (indoorLocalizedStartTime < 0f)
            indoorLocalizedStartTime = Time.time;

        if (!requireMapSpaceStable || mapSpace == null)
        {
            // If not enforcing mapSpace stability, treat as stable.
            if (mapSpaceStableStartTime < 0f) mapSpaceStableStartTime = Time.time;
            return;
        }

        // MapSpace stability: reset timer if pose changes too much between frames.
        Vector3 p = mapSpace.position;
        Quaternion r = mapSpace.rotation;

        if (!mapSpacePoseInitialized)
        {
            mapSpacePoseInitialized = true;
            lastMapSpacePos = p;
            lastMapSpaceRot = r;
            mapSpaceStableStartTime = Time.time;
            return;
        }

        float dp = Vector3.Distance(p, lastMapSpacePos);
        float dr = Quaternion.Angle(r, lastMapSpaceRot);

        lastMapSpacePos = p;
        lastMapSpaceRot = r;

        bool movingTooMuch = (dp > maxMapSpaceDeltaPosMeters) || (dr > maxMapSpaceDeltaRotDeg);
        if (movingTooMuch)
        {
            // Reset stable timer
            mapSpaceStableStartTime = -1f;
        }
        else
        {
            // Start / continue stable timer
            if (mapSpaceStableStartTime < 0f)
                mapSpaceStableStartTime = Time.time;
        }
    }

    private bool IndoorIsStableToPlace(out string reason, out float stableForSec)
    {
        stableForSec = 0f;

        if (indoorLocalizationFlag == null || !indoorLocalizationFlag.IsLocalized)
        {
            reason = "NotLocalized";
            return false;
        }

        if (!requireIndoorStable)
        {
            reason = "";
            return true;
        }

        if (indoorLocalizedStartTime < 0f)
        {
            reason = "IndoorLocalizedTimerNotStarted";
            return false;
        }

        float localizedFor = Time.time - indoorLocalizedStartTime;

        if (requireMapSpaceStable)
        {
            if (mapSpaceStableStartTime < 0f)
            {
                reason = "MapSpaceMoving";
                stableForSec = 0f;
                return false;
            }

            float mapStableFor = Time.time - mapSpaceStableStartTime;
            stableForSec = Mathf.Min(localizedFor, mapStableFor);

            if (stableForSec < indoorRequireStableSeconds)
            {
                reason = $"IndoorNotStableYet stableFor={stableForSec:F1}s need={indoorRequireStableSeconds:F1}s";
                return false;
            }
        }
        else
        {
            stableForSec = localizedFor;
            if (stableForSec < indoorRequireStableSeconds)
            {
                reason = $"IndoorNotStableYet stableFor={stableForSec:F1}s need={indoorRequireStableSeconds:F1}s";
                return false;
            }
        }

        reason = "";
        return true;
    }

    void TryPlaceAnchor(Vector2 screenPos)
    {
        Debug.Log($"[AnchorPlacer] TryPlaceAnchor fired. isIndoor={isIndoor} state? (placer enabled={enabled})");
        if (!IsAllowedInCurrentMode())
    {
        Debug.Log("[AnchorPlacer] TryPlaceAnchor rejected: not in Contributor mode.");
        ShowPlacementFeedback("Placement is only allowed in Contributor mode");
        return;
    }
        string tapId = EvaluationLogger.NewTapId();
        float tapStart = Time.realtimeSinceStartup;

        if (Time.time - AnchorInfoUI.LastCloseTime < IgnoreTapAfterCloseSeconds)
        {
            EvaluationLogger.Log(
                ev: "TapRejected",
                mode: isIndoor ? "Indoor" : "Outdoor",
                mapId: isIndoor ? currentMapId : "",
                success: "failure",
                details: "IgnoreTapAfterClose",
                tapId: tapId,
                screenPos: screenPos
            );
            return;
        }

       if (infoUI != null && infoUI.IsOpen)
{
    EvaluationLogger.Log(
        ev: "TapRejected",
        mode: isIndoor ? "Indoor" : "Outdoor",
        mapId: isIndoor ? currentMapId : "",
        success: "failure",
        details: "InfoUIOpen",
        tapId: tapId,
        screenPos: screenPos
    );
    return;
}

        EvaluationLogger.Log(
            ev: "TapAttempt",
            mode: isIndoor ? "Indoor" : "Outdoor",
            mapId: isIndoor ? currentMapId : "",
            details: "UserTap",
            tapId: tapId,
            screenPos: screenPos
        );

        if (!arCamera || !anchorPrefab)
        {
            EvaluationLogger.Log(
                ev: "TapRejected",
                mode: isIndoor ? "Indoor" : "Outdoor",
                mapId: isIndoor ? currentMapId : "",
                success: "failure",
                details: "MissingCameraOrPrefab",
                tapId: tapId,
                screenPos: screenPos
            );
            ShowPlacementFeedback("Placement failed due to missing setup");
            return;
        }

        // ========================================================
        // 🏠 INDOOR (MultiSet)
        // ========================================================
        if (isIndoor)
        {
            // NEW: stability gate
            if (!IndoorIsStableToPlace(out string reason, out float indoorStableForSec))
            {
                EvaluationLogger.Log(
                    ev: "TapRejected",
                    mode: "Indoor",
                    mapId: currentMapId,
                    success: "failure",
                    details: reason,
                    tapId: tapId,
                    screenPos: screenPos
                );
                if (reason == "NotLocalized")
    ShowPlacementFeedback("Indoor localization required. Please scan the environment");
else if (reason == "MapSpaceMoving")
    ShowPlacementFeedback("Hold the device steady and wait for localization to settle");
else if (reason.StartsWith("IndoorNotStableYet"))
    ShowPlacementFeedback("Tracking is not stable yet. Please hold the device steady");
else
    ShowPlacementFeedback("Placement failed. Please try again");
                return;
            }

            if (!mapSpace || !anchorsRoot || raycastManager == null)
            {
                string missing = (!mapSpace) ? "MapSpaceMissing" :
                                 (!anchorsRoot) ? "AnchorsRootMissing" :
                                 "ARRaycastManagerMissing";

                EvaluationLogger.Log(
                    ev: "TapRejected",
                    mode: "Indoor",
                    mapId: currentMapId,
                    success: "failure",
                    details: missing,
                    tapId: tapId,
                    screenPos: screenPos
                );
                ShowPlacementFeedback("Indoor placement is not ready");
                return;
            }

           s_Hits.Clear();

TrackableType planesMask =
    TrackableType.PlaneWithinPolygon | TrackableType.PlaneEstimated;

bool gotPlaneHit = raycastManager.Raycast(screenPos, s_Hits, planesMask);

Pose hitPose;
string hitLabel;

if (gotPlaneHit)
{
    hitPose = s_Hits[0].pose;
    hitLabel = (s_Hits[0].hitType == TrackableType.PlaneEstimated) ? "PlaneEstimated" : "Plane";
}
else
{
    // ===== Physics fallback: raycast against your indoor map mesh collider =====
    if (!allowPhysicsFallbackIndoor)
    {
        EvaluationLogger.Log(
            ev: "TapRejected",
            mode: "Indoor",
            mapId: currentMapId,
            success: "failure",
            details: "NoARHit_NoPhysicsFallback",
            tapId: tapId,
            screenPos: screenPos,
            hitType: "None"
        );
        ShowPlacementFeedback("Tap failed. Try tapping on a detected surface");
        return;
    }

    Ray ray = arCamera.ScreenPointToRay(screenPos);
    if (!Physics.Raycast(ray, out RaycastHit phit, maxPhysicsRayDistance, indoorMapColliderMask, QueryTriggerInteraction.Ignore))
    {
        EvaluationLogger.Log(
            ev: "TapRejected",
            mode: "Indoor",
            mapId: currentMapId,
            success: "failure",
            details: "NoARHit_NoPhysicsHit",
            tapId: tapId,
            screenPos: screenPos,
            hitType: "None"
        );
        ShowPlacementFeedback("No valid surface found. Try moving closer or scanning more");
        return;
    }

    // Build a pose from physics hit:
    // position = hit point
    // rotation = align "up" to surface normal, face roughly toward camera forward projected on surface
    Vector3 up = phit.normal;

    Vector3 camFwd = arCamera.transform.forward;
    Vector3 fwdOnSurface = Vector3.ProjectOnPlane(camFwd, up).normalized;
    if (fwdOnSurface.sqrMagnitude < 1e-6f)
        fwdOnSurface = Vector3.ProjectOnPlane(arCamera.transform.up, up).normalized;

    Quaternion rot = Quaternion.LookRotation(fwdOnSurface, up);

    hitPose = new Pose(phit.point, rot);
    hitLabel = "PhysicsMesh";
}

          string trackableTypeStr = gotPlaneHit ? s_Hits[0].hitType.ToString() : "PhysicsMesh";
string trackableIdStr   = gotPlaneHit ? s_Hits[0].trackableId.ToString() : "";

Vector3 worldPos = hitPose.position + hitPose.rotation * Vector3.up * surfaceOffset;

            // Store mapSpace-local position (stable w.r.t. indoor map)
            Vector3 localInMapSpace = mapSpace.InverseTransformPoint(worldPos);
            

            // Convert saved map-local position back to WORLD (exact location in AR world)
Vector3 computedWorld = mapSpace.TransformPoint(localInMapSpace);

// Instantiate WITHOUT parent first
GameObject pin = Instantiate(anchorPrefab);

// WORLD pose first -> exactly where you tapped
pin.transform.position = computedWorld;

// Keep pin upright; rotate only around Y so it faces the camera
Vector3 toCam = arCamera.transform.position - computedWorld;
toCam.y = 0f;
if (toCam.sqrMagnitude < 1e-6f)
    toCam = arCamera.transform.forward;

Quaternion yawOnly = Quaternion.LookRotation(-toCam.normalized, Vector3.up);

// Apply model correction ONCE (only if your prefab needs it)
Quaternion modelFix = Quaternion.Euler(-90f, 0f, 0f);
pin.transform.rotation = yawOnly * modelFix;

Quaternion localRotInMapSpace = Quaternion.Inverse(mapSpace.rotation) * pin.transform.rotation;

// Parent after setting world pose, keep world pose unchanged
pin.transform.SetParent(anchorsRoot, true);

// Stable scale
pin.transform.localScale = Vector3.one;


            EnsureTappable(pin);

            string anchorId = EvaluationLogger.NewAnchorId();
            double latencyMs = (Time.realtimeSinceStartup - tapStart) * 1000.0;

            AnchorData data = pin.GetComponent<AnchorData>();
            if (data == null) data = pin.AddComponent<AnchorData>();
            data.SetIndoor(currentMapId, localInMapSpace, localRotInMapSpace);

            AnchorRegistry.Register(new AnchorRegistry.Record
            {
                anchorId = anchorId,
                mode = "Indoor",
                mapId = currentMapId,
                target = pin.transform,
                mapSpace = mapSpace,
                initialWorldPos = pin.transform.position,
                initialMapLocalPos = localInMapSpace
            });

            var handle = pin.GetComponent<AnchorRegistryHandle>();
            if (handle == null) handle = pin.AddComponent<AnchorRegistryHandle>();
            handle.anchorId = anchorId;

            EvaluationLogger.Log(
                ev: "AnchorPlaced",
                mode: "Indoor",
                mapId: currentMapId,
                success: "success",
                details: hitLabel,
                pos: localInMapSpace,
                tapId: tapId,
                anchorId: anchorId,
                screenPos: screenPos,
                latencyMs: latencyMs,
                hitType: hitLabel,
                trackableType: trackableTypeStr,
                trackableId: trackableIdStr
            );
            if (feedbackUI != null) feedbackUI.HideNow();
            infoUI?.OpenContributor(data, true);
            return;
        }

        // ========================================================
        // 🌍 OUTDOOR (ARCore Earth) — unchanged from your version
        // ========================================================
#if UNITY_ANDROID && !UNITY_EDITOR
        if (earthTrackingUI == null || earthManager == null || anchorManager == null || raycastManager == null)
        {
            EvaluationLogger.Log(
                ev: "TapRejected",
                mode: "Outdoor",
                mapId: "",
                success: "failure",
                details: "EarthManagersOrUI_Missing",
                tapId: tapId,
                screenPos: screenPos
            );
            ShowPlacementFeedback("Outdoor placement is not ready");
            return;
        }

        bool earthReady =
            earthManager.EarthTrackingState == TrackingState.Tracking &&
            earthManager.EarthState == EarthState.Enabled;

        if (!earthReady)
        {
            outdoorGoodAccStartTime = -1f;
            EvaluationLogger.Log(
                ev: "TapRejected",
                mode: "Outdoor",
                mapId: "",
                success: "failure",
                details: "EarthNotReady",
                tapId: tapId,
                screenPos: screenPos,
                earthState: earthManager.EarthState.ToString(),
                earthTracking: earthManager.EarthTrackingState.ToString()
            );
            ShowPlacementFeedback("Outdoor tracking is not ready yet");
            return;
        }

        bool gateOk = requireStrictReadyToPlace ? earthTrackingUI.IsReady : earthTrackingUI.IsPlaceable;

        if (!gateOk)
        {
            outdoorGoodAccStartTime = -1f;
            EvaluationLogger.Log(
                ev: "TapRejected",
                mode: "Outdoor",
                mapId: "",
                success: "failure",
                details: requireStrictReadyToPlace ? "EarthNotStrictReady" : "EarthNotPlaceable",
                tapId: tapId,
                screenPos: screenPos
            );
            ShowPlacementFeedback("Wait for outdoor tracking to become ready");
            return;
        }

        if (FirestoreAnchorService.Instance == null || !FirestoreAnchorService.Instance.IsReady)
        {
            outdoorGoodAccStartTime = -1f;
            EvaluationLogger.Log(
                ev: "TapRejected",
                mode: "Outdoor",
                mapId: "",
                success: "failure",
                details: "FirebaseNotReady",
                tapId: tapId,
                screenPos: screenPos
            );
            ShowPlacementFeedback("Please wait. Storage is not ready yet");
            return;
        }

        GeospatialPose camGeoPose = earthManager.CameraGeospatialPose;
        float yawAcc = GetYawAccuracyDeg(camGeoPose);

        bool goodAcc =
            (!requireGoodAccuracy) ||
            (camGeoPose.HorizontalAccuracy <= maxHorizontalAccuracyMeters &&
             camGeoPose.VerticalAccuracy <= maxVerticalAccuracyMeters &&
             yawAcc <= maxYawAccuracyDegrees);

        if (!goodAcc)
        {
            outdoorGoodAccStartTime = -1f;
            EvaluationLogger.Log(
                ev: "TapRejected",
                mode: "Outdoor",
                mapId: "",
                success: "failure",
                details: $"AccuracyBad H={camGeoPose.HorizontalAccuracy:F1} V={camGeoPose.VerticalAccuracy:F1} YawAcc={yawAcc:F1}",
                tapId: tapId,
                screenPos: screenPos,
                earthState: earthManager.EarthTrackingState.ToString(),
                earthTracking: "Tracking",
                earthHAcc: (float)camGeoPose.HorizontalAccuracy,
                earthVAcc: (float)camGeoPose.VerticalAccuracy,
                earthHeadAcc: yawAcc
            );
            ShowPlacementFeedback("Location accuracy is poor. Move slowly and wait a moment");
            return;
        }

        if (camGeoPose.VerticalAccuracy > maxVerticalAccuracyForPlacementMeters)
        {
            outdoorGoodAccStartTime = -1f;
            EvaluationLogger.Log(
                ev: "TapRejected",
                mode: "Outdoor",
                mapId: "",
                success: "failure",
                details: $"VerticalAccuracyTooHigh V={camGeoPose.VerticalAccuracy:F1}",
                tapId: tapId,
                screenPos: screenPos,
                earthState: earthManager.EarthTrackingState.ToString(),
                earthTracking: "Tracking",
                earthHAcc: (float)camGeoPose.HorizontalAccuracy,
                earthVAcc: (float)camGeoPose.VerticalAccuracy,
                earthHeadAcc: yawAcc
            );
            ShowPlacementFeedback("Vertical accuracy is too low. Please wait and try again");
            return;
        }

        if (outdoorGoodAccStartTime < 0f) outdoorGoodAccStartTime = Time.time;
        float stableFor = Time.time - outdoorGoodAccStartTime;
        if (stableFor < requireStableSeconds)
        {
            EvaluationLogger.Log(
                ev: "TapRejected",
                mode: "Outdoor",
                mapId: "",
                success: "failure",
                details: $"NotStableYet stableFor={stableFor:F1}s need={requireStableSeconds:F1}s",
                tapId: tapId,
                screenPos: screenPos,
                earthState: earthManager.EarthTrackingState.ToString(),
                earthTracking: "Tracking",
                earthHAcc: (float)camGeoPose.HorizontalAccuracy,
                earthVAcc: (float)camGeoPose.VerticalAccuracy,
                earthHeadAcc: yawAcc
            );
            ShowPlacementFeedback("Tracking is stabilizing. Hold the device steady");
            return;
        }

        s_Hits.Clear();
        var outdoorMask = TrackableType.PlaneWithinPolygon | TrackableType.PlaneEstimated | TrackableType.FeaturePoint;

        bool gotHitOutdoor = raycastManager.Raycast(screenPos, s_Hits, outdoorMask);
        if (!gotHitOutdoor)
        {
            EvaluationLogger.Log(
                ev: "TapRejected",
                mode: "Outdoor",
                mapId: "",
                success: "failure",
                details: "NoHit",
                tapId: tapId,
                screenPos: screenPos,
                hitType: "None",
                earthState: earthManager.EarthTrackingState.ToString(),
                earthTracking: "Tracking",
                earthHAcc: (float)camGeoPose.HorizontalAccuracy,
                earthVAcc: (float)camGeoPose.VerticalAccuracy,
                earthHeadAcc: yawAcc
            );
            ShowPlacementFeedback("No surface found. Try aiming at the ground or a nearby surface");
            return;
        }

        string hitLabelOutdoor =
            (s_Hits[0].hitType == TrackableType.FeaturePoint) ? "FeaturePoint" :
            (s_Hits[0].hitType == TrackableType.PlaneEstimated) ? "PlaneEstimated" :
            "Plane";

        Pose hitWorldPose = s_Hits[0].pose;

        float hitDist = Vector3.Distance(arCamera.transform.position, hitWorldPose.position);
        if (hitDist > maxTapHitDistanceMeters)
        {
            EvaluationLogger.Log(
                ev: "TapRejected",
                mode: "Outdoor",
                mapId: "",
                success: "failure",
                details: $"HitTooFar dist={hitDist:F1}",
                pos: hitWorldPose.position,
                tapId: tapId,
                screenPos: screenPos,
                hitType: hitLabelOutdoor,
                trackableType: s_Hits[0].hitType.ToString(),
                trackableId: s_Hits[0].trackableId.ToString(),
                earthState: earthManager.EarthTrackingState.ToString(),
                earthTracking: "Tracking",
                earthHAcc: (float)camGeoPose.HorizontalAccuracy,
                earthVAcc: (float)camGeoPose.VerticalAccuracy,
                earthHeadAcc: yawAcc
            );
            ShowPlacementFeedback("Tap failed. Please move closer to the location");
            return;
        }

        GeospatialPose tapGeoPose;
        try { tapGeoPose = earthManager.Convert(hitWorldPose); }
        catch (Exception e)
        {
            EvaluationLogger.Log(
                ev: "TapRejected",
                mode: "Outdoor",
                mapId: "",
                success: "failure",
                details: "ConvertFailed " + e.Message,
                pos: hitWorldPose.position,
                tapId: tapId,
                screenPos: screenPos,
                hitType: hitLabelOutdoor,
                trackableType: s_Hits[0].hitType.ToString(),
                trackableId: s_Hits[0].trackableId.ToString(),
                earthState: earthManager.EarthTrackingState.ToString(),
                earthTracking: "Tracking",
                earthHAcc: (float)camGeoPose.HorizontalAccuracy,
                earthVAcc: (float)camGeoPose.VerticalAccuracy,
                earthHeadAcc: yawAcc
            );
            ShowPlacementFeedback("Failed to resolve location. Please try again");
            return;
        }

        double altOffsetMeters = tapGeoPose.Altitude - camGeoPose.Altitude;

        if (guardAltitude)
        {
            if (Mathf.Abs((float)altOffsetMeters) > maxAllowedAltitudeJumpMeters)
            {
                EvaluationLogger.Log(
                    ev: "TapRejected",
                    mode: "Outdoor",
                    mapId: "",
                    success: "failure",
                    details: $"AltitudeJumpTooBig dz={altOffsetMeters:F1} camAlt={camGeoPose.Altitude:F1} tapAlt={tapGeoPose.Altitude:F1}",
                    pos: hitWorldPose.position,
                    tapId: tapId,
                    screenPos: screenPos,
                    hitType: hitLabelOutdoor,
                    trackableType: s_Hits[0].hitType.ToString(),
                    trackableId: s_Hits[0].trackableId.ToString(),
                    earthState: earthManager.EarthTrackingState.ToString(),
                    earthTracking: "Tracking",
                    earthHAcc: (float)camGeoPose.HorizontalAccuracy,
                    earthVAcc: (float)camGeoPose.VerticalAccuracy,
                    earthHeadAcc: yawAcc
                );
                ShowPlacementFeedback("Placement failed due to unstable altitude. Try again nearby");
                return;
            }
        }

        float headingToUse = (float)camGeoPose.Heading;
        Quaternion geoRot = Quaternion.Euler(0f, headingToUse, 0f);

        ARGeospatialAnchor earthAnchor = anchorManager.AddAnchor(
            tapGeoPose.Latitude,
            tapGeoPose.Longitude,
            tapGeoPose.Altitude,
            geoRot
        );

        if (earthAnchor == null)
        {
            EvaluationLogger.Log(
                ev: "TapRejected",
                mode: "Outdoor",
                mapId: "",
                success: "failure",
                details: "AddAnchorFailed",
                pos: hitWorldPose.position,
                tapId: tapId,
                screenPos: screenPos,
                earthState: earthManager.EarthTrackingState.ToString(),
                earthTracking: "Tracking",
                earthHAcc: (float)camGeoPose.HorizontalAccuracy,
                earthVAcc: (float)camGeoPose.VerticalAccuracy,
                earthHeadAcc: yawAcc
            );
            ShowPlacementFeedback("Failed to create anchor. Please try again");
            return;
        }

        string anchorIdOut = EvaluationLogger.NewAnchorId();
        double latencyMsOut = (Time.realtimeSinceStartup - tapStart) * 1000.0;

        AnchorRegistry.Register(new AnchorRegistry.Record
        {
            anchorId = anchorIdOut,
            mode = "Outdoor",
            mapId = "",
            target = earthAnchor.transform,
            mapSpace = null,
            initialWorldPos = earthAnchor.transform.position,
            initialMapLocalPos = Vector3.zero
        });

        var earthHandle = earthAnchor.GetComponent<AnchorRegistryHandle>();
        if (earthHandle == null) earthHandle = earthAnchor.gameObject.AddComponent<AnchorRegistryHandle>();
        earthHandle.anchorId = anchorIdOut;

        EvaluationLogger.Log(
            ev: "AnchorPlaced",
            mode: "Outdoor",
            mapId: "",
            success: "success",
            details: hitLabelOutdoor,
            pos: hitWorldPose.position,
            tapId: tapId,
            anchorId: anchorIdOut,
            screenPos: screenPos,
            latencyMs: latencyMsOut,
            hitType: hitLabelOutdoor,
            trackableType: s_Hits[0].hitType.ToString(),
            trackableId: s_Hits[0].trackableId.ToString(),
            earthState: earthManager.EarthTrackingState.ToString(),
            earthTracking: "Tracking",
            earthHAcc: (float)camGeoPose.HorizontalAccuracy,
            earthVAcc: (float)camGeoPose.VerticalAccuracy,
            earthHeadAcc: yawAcc
        );

        GameObject pinObj = Instantiate(anchorPrefab, earthAnchor.transform);
        Debug.Log($"PIN parent = {pinObj.transform.parent?.name}");
Debug.Log($"PIN root   = {pinObj.transform.root.name}");
Debug.Log($"PIN path   = {GetPath(pinObj.transform)}");
        pinObj.transform.localPosition = Vector3.zero;
        pinObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        pinObj.transform.localScale = Vector3.one;

        EnsureTappable(pinObj);

        AnchorData anchorData = pinObj.GetComponent<AnchorData>();
        if (anchorData == null) anchorData = pinObj.AddComponent<AnchorData>();

        anchorData.SetOutdoor(tapGeoPose.Latitude, tapGeoPose.Longitude, tapGeoPose.Altitude, headingToUse);

        var camNow = earthManager.CameraGeospatialPose;
        anchorData.evalSaveWorld = hitWorldPose.position;
        anchorData.evalSaveCamLat = camNow.Latitude ;
        anchorData.evalSaveCamLng = camNow.Longitude;
        anchorData.evalSaveCamAlt = camNow.Altitude;

        anchorData.evalSaveHAcc = (float)camNow.HorizontalAccuracy;
        anchorData.evalSaveVAcc = (float)camNow.VerticalAccuracy;
        anchorData.evalSaveYawAcc = GetYawAccuracyDeg(camNow);

        anchorData.evalAltitudeMode = "relative";
        anchorData.evalAltitudeOffset = (float)altOffsetMeters;

        if (feedbackUI != null) feedbackUI.HideNow();
        infoUI?.OpenContributor(anchorData, true);
#endif
    }

    // ---------------- helpers ----------------
    void EnsureTappable(GameObject pinObj)
    {
        if (pinObj == null) return;

        if (ensureCollider && pinObj.GetComponentInChildren<Collider>() == null)
        {
            var bc = pinObj.AddComponent<BoxCollider>();
            bc.size = autoColliderSize;
            bc.center = Vector3.zero;
            bc.isTrigger = false;
        }

        if (forceAnchorPinLayer)
        {
            int layer = LayerMask.NameToLayer(anchorPinLayerName);
            if (layer >= 0) ApplyLayerRecursively(pinObj, layer);
            else Debug.LogWarning($"[AnchorPlacer] Layer '{anchorPinLayerName}' not found. Create it in Project Settings > Tags and Layers.");
        }
    }

    static void ApplyLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        for (int i = 0; i < root.transform.childCount; i++)
            ApplyLayerRecursively(root.transform.GetChild(i).gameObject, layer);
    }
    static string GetPath(Transform t)
{
    if (t == null) return "<null>";
    string p = t.name;
    while (t.parent != null)
    {
        t = t.parent;
        p = t.name + "/" + p;
    }
    return p;
}
private void ShowPlacementFeedback(string message)
{
    Debug.Log("[AnchorPlacer] ShowPlacementFeedback: " + message);

    if (feedbackUI != null)
        feedbackUI.ShowMessage(message);
    else
        Debug.LogWarning("[AnchorPlacer] feedbackUI is NULL");
}
private bool IsAllowedInCurrentMode()
{
    if (modePanel == null)
    {
        Debug.LogWarning("[AnchorPlacer] modePanel not assigned; mode gating disabled.");
        return true;
    }

    // AnchorPlacer should ONLY be active in contributor modes:
    // - ContributorIndoorActive
    // - ContributorOutdoorActive
    return modePanel.IsInContributorIndoorActive() || modePanel.IsInContributorOutdoorActive();
}
}