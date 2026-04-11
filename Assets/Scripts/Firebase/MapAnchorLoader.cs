using System.Collections;
using UnityEngine;

public class MapAnchorLoader : MonoBehaviour
{
    [Header("Map info")]
    [Tooltip("Must match the mapId used by AnchorPlacer when saving (e.g. 'cdis_lab').")]
    public string mapId = "cdis_lab";

    [Header("Scene references")]
    [Tooltip("Map Space (parent of AnchorsRoot), moved by MultiSet/VPS.")]
    public Transform mapSpace;

    [Tooltip("AnchorsRoot (child of Map Space) where pins will be instantiated.")]
    public Transform anchorsRoot;

    [Tooltip("Same anchor prefab used by AnchorPlacer (arrow_location).")]
    public GameObject anchorPrefab;

    [Header("Indoor gating")]
    [Tooltip("Optional but strongly recommended. Used to ensure we only load after localization success.")]
    public IndoorLocalizationFlag indoorLocalizationFlag;

    [Tooltip("After localization becomes TRUE, require it to remain TRUE for this many seconds before loading.")]
    public float requireLocalizedStableSeconds = 1.0f;

    [Tooltip("After stability, wait this extra time to let MultiSet settle mapSpace pose.")]
    public float extraSettleSeconds = 0.75f;

    [Tooltip("Total time to wait before giving up.")]
    public float loadTimeoutSeconds = 20f;

    private Coroutine loadRoutine;

    /// <summary>
    /// Called by ModePanel when entering Explorer mode.
    /// </summary>
    public void LoadAnchors()
    {
        Debug.Log("[MapAnchorLoader] LoadAnchors() called");
        Debug.Log($"[MapAnchorLoader] GameObject='{gameObject.name}' activeInHierarchy={gameObject.activeInHierarchy}");
        Debug.Log($"[MapAnchorLoader] mapId='{mapId}' mapSpace={(mapSpace ? mapSpace.name : "NULL")} anchorsRoot={(anchorsRoot ? anchorsRoot.name : "NULL")} prefab={(anchorPrefab ? anchorPrefab.name : "NULL")} indoorFlag={(indoorLocalizationFlag ? indoorLocalizationFlag.name : "NULL")}");

        if (string.IsNullOrEmpty(mapId))
        {
            Debug.LogWarning("[MapAnchorLoader] mapId is empty, cannot load anchors.");
            return;
        }

        if (mapSpace == null || anchorsRoot == null || anchorPrefab == null)
        {
            Debug.LogWarning(
                "[MapAnchorLoader] missing references:" +
                $"\n  mapSpace   = {mapSpace}" +
                $"\n  anchorsRoot= {anchorsRoot}" +
                $"\n  anchorPrefab= {anchorPrefab}"
            );
            return;
        }

        if (FirestoreAnchorService.Instance == null)
        {
            Debug.LogWarning("[MapAnchorLoader] FirestoreAnchorService.Instance is null (service object missing in scene?).");
            return;
        }

        // Stop previous load attempt if any
        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
            loadRoutine = null;
        }

        loadRoutine = StartCoroutine(LoadWhenReadyRoutine());
    }

    private IEnumerator LoadWhenReadyRoutine()
    {
        float start = Time.realtimeSinceStartup;

        // 1) wait Firebase ready (re-uses your service helper, but we also guard with timeout here)
        while (FirestoreAnchorService.Instance != null && !FirestoreAnchorService.Instance.IsReady)
        {
            if (Time.realtimeSinceStartup - start > loadTimeoutSeconds)
            {
                Debug.LogWarning("[MapAnchorLoader] Timeout waiting for Firebase ready. Not loading anchors.");
                yield break;
            }
            yield return null;
        }

        if (FirestoreAnchorService.Instance == null)
        {
            Debug.LogWarning("[MapAnchorLoader] FirestoreAnchorService.Instance became null while waiting.");
            yield break;
        }

        // 2) wait localization TRUE and stable (if flag provided)
        if (indoorLocalizationFlag != null)
        {
            float stableStart = -1f;

            while (true)
            {
                if (Time.realtimeSinceStartup - start > loadTimeoutSeconds)
                {
                    Debug.LogWarning("[MapAnchorLoader] Timeout waiting for indoor localization success. Not loading anchors.");
                    yield break;
                }

                if (!indoorLocalizationFlag.IsLocalized)
                {
                    stableStart = -1f;
                    yield return null;
                    continue;
                }

                if (stableStart < 0f)
    stableStart = Time.realtimeSinceStartup;

float stableFor = Time.realtimeSinceStartup - stableStart;
                if (stableFor >= requireLocalizedStableSeconds)
                    break;

                yield return null;
            }

            // 3) extra settle time so mapSpace pose finishes snapping
            if (extraSettleSeconds > 0f)
                yield return new WaitForSecondsRealtime(extraSettleSeconds);
        }
        else
        {
            Debug.LogWarning("[MapAnchorLoader] indoorLocalizationFlag not assigned. Loading without localization gate (can cause wrong placement).");
        }

        // Debug: show mapSpace pose at load time
        Debug.Log($"[MapAnchorLoader] LOADING NOW. mapId='{mapId}'");
        Debug.Log($"[MapAnchorLoader] mapSpace pose: pos={mapSpace.position} rot={mapSpace.rotation.eulerAngles}");

        // 4) finally load
        Debug.Log("[MapAnchorLoader] Firebase ready + localization stable -> calling LoadAnchorsForMap");
        FirestoreAnchorService.Instance.LoadAnchorsForMap(
            mapId,
            mapSpace,
            anchorsRoot,
            anchorPrefab
        );

        loadRoutine = null;
    }
}