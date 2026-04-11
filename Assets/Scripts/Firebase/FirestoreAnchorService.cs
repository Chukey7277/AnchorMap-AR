using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks; // IMPORTANT: disambiguate Task
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
#endif

public class FirestoreAnchorService : MonoBehaviour
{
    public static FirestoreAnchorService Instance { get; private set; }

#if UNITY_ANDROID && !UNITY_EDITOR
    private FirebaseFirestore db;
    private bool firebaseReady = false;
#endif

    // ======================================================
    // READY FLAG
    // ======================================================
    public bool IsReady
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return firebaseReady && db != null;
#else
            return false;
#endif
        }
    }

    // ======================================================
    // LIFECYCLE
    // ======================================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeFirebase();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread((Task<DependencyStatus> task) =>
            {
                if (task.IsFaulted)
                {
                    firebaseReady = false;
                    db = null;
                    Debug.LogError("[FirestoreAnchorService] CheckAndFixDependenciesAsync faulted: " + task.Exception);
                    return;
                }

                if (task.Result == DependencyStatus.Available)
                {
                    db = FirebaseFirestore.DefaultInstance;
                    firebaseReady = true;
                    Debug.Log("[FirestoreAnchorService] Firebase ready.");
                }
                else
                {
                    firebaseReady = false;
                    db = null;
                    Debug.LogError("[FirestoreAnchorService] Firebase dependency error: " + task.Result);
                }
            });
    }
#endif

    // ======================================================
    // WAIT-UNTIL-READY HELPER
    // ======================================================
    public void WhenReady(MonoBehaviour owner, Action onReady, float timeoutSeconds = 10f)
    {
        if (owner == null)
        {
            Debug.LogWarning("[FirestoreAnchorService] WhenReady called with null owner.");
            return;
        }

        owner.StartCoroutine(WaitReadyCoroutine(onReady, timeoutSeconds));
    }

    private IEnumerator WaitReadyCoroutine(Action onReady, float timeoutSeconds)
    {
        float start = Time.realtimeSinceStartup;

        while (!IsReady)
        {
            if (timeoutSeconds > 0f && (Time.realtimeSinceStartup - start) > timeoutSeconds)
            {
                Debug.LogWarning("[FirestoreAnchorService] Firebase not ready (timeout).");
                yield break;
            }
            yield return null;
        }

        onReady?.Invoke();
    }

    // ======================================================
    // SAVE ANCHOR (INDOOR + OUTDOOR)
    // ======================================================
    public void SaveAnchor(AnchorData data)
    {
        SaveAnchor(
            data,
            hAccMeters: null,
            vAccMeters: null,
            yawAccDeg: null,
            altitudeMode: null,
            altitudeOffsetMeters: null
        );
    }

    public void SaveAnchor(
        AnchorData data,
        float? hAccMeters,
        float? vAccMeters,
        float? yawAccDeg,
        string altitudeMode,
        float? altitudeOffsetMeters
    )
    {
        if (data == null) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsReady)
        {
            Debug.LogWarning("[FirestoreAnchorService] SaveAnchor called but Firebase not ready yet.");
            return;
        }

        var doc = new Dictionary<string, object>
        {
            { "anchorType",  data.anchorType.ToString() }, // "Indoor" or "Outdoor"
            { "title",       data.title ?? "" },
            { "description", data.description ?? "" },
            { "createdAt",   Timestamp.GetCurrentTimestamp() }
        };

        if (data.anchorType == AnchorType.Indoor)
{
    doc["mapId"]  = data.mapId ?? "";
    doc["localX"] = data.localPosition.x;
    doc["localY"] = data.localPosition.y;
    doc["localZ"] = data.localPosition.z;

    doc["rotX"] = data.indoorLocalRotation.x;
    doc["rotY"] = data.indoorLocalRotation.y;
    doc["rotZ"] = data.indoorLocalRotation.z;
    doc["rotW"] = data.indoorLocalRotation.w;
}
        else
        {
            // -------------------------
            // Outdoor: core geospatial
            // -------------------------
            doc["latitude"]  = data.latitude;
            doc["longitude"] = data.longitude;
            doc["altitude"]  = data.altitude;  // absolute altitude used for placement
            doc["heading"]   = data.heading;

            // -------------------------
            // Save-time accuracy: prefer AnchorData.saved*
            // fallback to the optional params if you still pass them
            // -------------------------
            float hAccToWrite = (data.savedHAcc >= 0f) ? data.savedHAcc : (hAccMeters ?? -1f);
            float vAccToWrite = (data.savedVAcc >= 0f) ? data.savedVAcc : (vAccMeters ?? -1f);
            float yawAccToWrite = (data.savedYawAcc >= 0f) ? data.savedYawAcc : (yawAccDeg ?? -1f);

            if (hAccToWrite >= 0f) doc["hAcc"] = hAccToWrite;
            if (vAccToWrite >= 0f) doc["vAcc"] = vAccToWrite;
            if (yawAccToWrite >= 0f) doc["yawAcc"] = yawAccToWrite;

            // -------------------------
            // Altitude strategy metadata (single source of truth)
            // Prefer AnchorData.saved*; fall back to params.
            // Avoid overwriting the same keys twice.
            // -------------------------
            string modeToWrite =
                !string.IsNullOrEmpty(data.savedAltitudeMode) ? data.savedAltitudeMode : (altitudeMode ?? "");

            if (!string.IsNullOrEmpty(modeToWrite))
                doc["altitudeMode"] = modeToWrite;

            float offsetToWrite =
                (Mathf.Abs(data.savedAltitudeOffset) > 0.0001f) ? data.savedAltitudeOffset : (altitudeOffsetMeters ?? 0f);

            // Only write offset if mode exists OR offset is meaningfully non-zero
            if (!string.IsNullOrEmpty(modeToWrite) || Mathf.Abs(offsetToWrite) > 0.0001f)
                doc["altitudeOffset"] = offsetToWrite;

            // Optional: also store absolute altitude as float snapshot
            if (Mathf.Abs(data.savedAltitudeAbsolute) > 0.0001f)
                doc["savedAltitudeAbsolute"] = data.savedAltitudeAbsolute;

            // =========================
            // EVAL / DEBUG FIELDS (Contributor vs Explorer)
            // =========================
            doc["saveWorldX"] = data.evalSaveWorld.x;
            doc["saveWorldY"] = data.evalSaveWorld.y;
            doc["saveWorldZ"] = data.evalSaveWorld.z;

            doc["saveCamLat"] = data.evalSaveCamLat;
            doc["saveCamLng"] = data.evalSaveCamLng;
            doc["saveCamAlt"] = data.evalSaveCamAlt;

            doc["saveCamHAcc"] = data.evalSaveHAcc;
            doc["saveCamVAcc"] = data.evalSaveVAcc;
            doc["saveCamYawAcc"] = data.evalSaveYawAcc;

            // Keep eval mode/offset separate so they don't clash with real placement strategy
            if (!string.IsNullOrEmpty(data.evalAltitudeMode))
                doc["evalAltitudeMode"] = data.evalAltitudeMode;

            if (Mathf.Abs(data.evalAltitudeOffset) > 0.0001f)
                doc["evalAltitudeOffset"] = data.evalAltitudeOffset;
        }

        db.Collection("anchors").AddAsync(doc)
            .ContinueWithOnMainThread((Task<DocumentReference> task) =>
            {
                if (task.IsFaulted)
                    Debug.LogError("[FirestoreAnchorService] Save failed: " + task.Exception);
                else
                    Debug.Log("[FirestoreAnchorService] Anchor saved: " + task.Result.Id);
            });
#endif
    }

    // ======================================================
    // LOAD – INDOOR (MAP-LOCAL)
    // ======================================================
    public void LoadAnchorsForMap(
        string mapId,
        Transform mapSpace,
        Transform anchorsRoot,
        GameObject anchorPrefab,
        bool ensureTappable = true,
        string pinLayerName = "ARPin",
        Vector3? autoColliderSize = null
    )
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsReady)
        {
            Debug.LogWarning("[FirestoreAnchorService] LoadAnchorsForMap called but Firebase not ready yet.");
            return;
        }

        if (string.IsNullOrEmpty(mapId))
        {
            Debug.LogWarning("[FirestoreAnchorService] LoadAnchorsForMap: mapId is empty.");
            return;
        }

        if (mapSpace == null || anchorsRoot == null || anchorPrefab == null)
        {
            Debug.LogWarning(
                "[FirestoreAnchorService] LoadAnchorsForMap missing refs:" +
                $"\n  mapSpace={mapSpace}" +
                $"\n  anchorsRoot={anchorsRoot}" +
                $"\n  anchorPrefab={anchorPrefab}"
            );
            return;
        }

        // Clear existing pins to avoid duplicates
        for (int i = anchorsRoot.childCount - 1; i >= 0; i--)
            Destroy(anchorsRoot.GetChild(i).gameObject);

        Debug.Log($"[FirestoreAnchorService] Loading indoor anchors for mapId='{mapId}'...");

        db.Collection("anchors")
          .WhereEqualTo("anchorType", "Indoor")
          .WhereEqualTo("mapId", mapId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread((Task<QuerySnapshot> task) =>
          {
              if (task.IsFaulted)
              {
                  Debug.LogError("[FirestoreAnchorService] LoadAnchorsForMap faulted: " + task.Exception);
                  return;
              }
              if (task.IsCanceled)
              {
                  Debug.LogWarning("[FirestoreAnchorService] LoadAnchorsForMap canceled.");
                  return;
              }

              QuerySnapshot snap = task.Result;

              // IMPORTANT FIX: use Firestore's Count property (not Documents.Count)
              int fetched = snap.Count;
              int spawned = 0;

              foreach (var docSnap in snap.Documents)
              {
                  var d = docSnap.ToDictionary();

                  if (!d.TryGetValue("localX", out var lx) ||
                      !d.TryGetValue("localY", out var ly) ||
                      !d.TryGetValue("localZ", out var lz))
                  {
                      Debug.LogWarning($"[FirestoreAnchorService] Doc {docSnap.Id} missing localX/Y/Z. Skipping.");
                      continue;
                  }

                  Vector3 localInMap;
try
{
    localInMap = new Vector3(
        Convert.ToSingle(lx),
        Convert.ToSingle(ly),
        Convert.ToSingle(lz)
    );
}
catch (Exception e)
{
    Debug.LogWarning($"[FirestoreAnchorService] Doc {docSnap.Id} localX/Y/Z parse error: {e.Message}");
    continue;
}

Quaternion localRotInMap = Quaternion.identity;

object rxObj = null;
object ryObj = null;
object rzObj = null;
object rwObj = null;

bool hasRotationX = d.TryGetValue("rotX", out rxObj);
bool hasRotationY = d.TryGetValue("rotY", out ryObj);
bool hasRotationZ = d.TryGetValue("rotZ", out rzObj);
bool hasRotationW = d.TryGetValue("rotW", out rwObj);

bool hasRotation = hasRotationX && hasRotationY && hasRotationZ && hasRotationW;

if (hasRotation)
{
    try
    {
        localRotInMap = new Quaternion(
            Convert.ToSingle(rxObj),
            Convert.ToSingle(ryObj),
            Convert.ToSingle(rzObj),
            Convert.ToSingle(rwObj)
        );
    }
    catch (Exception e)
    {
        Debug.LogWarning($"[FirestoreAnchorService] Doc {docSnap.Id} rotX/Y/Z/W parse error: {e.Message}");
        localRotInMap = Quaternion.identity;
    }
}

                

                 GameObject pin = Instantiate(anchorPrefab);

// compute world pose from stored map-local transform
Vector3 worldPos = mapSpace.TransformPoint(localInMap);
Quaternion worldRot = mapSpace.rotation * localRotInMap;

// parent while preserving world transform
pin.transform.SetParent(anchorsRoot, true);

// restore exact saved world pose
pin.transform.position = worldPos;
pin.transform.rotation = worldRot;
pin.transform.localScale = Vector3.one;

                  var data = pin.GetComponent<AnchorData>();
                  if (data == null) data = pin.AddComponent<AnchorData>();

                  data.SetIndoor(mapId, localInMap, localRotInMap);

                  if (d.TryGetValue("title", out var t) && t != null) data.title = t.ToString();
                  if (d.TryGetValue("description", out var desc) && desc != null) data.description = desc.ToString();

                  if (ensureTappable)
                      EnsurePinTappable(pin, pinLayerName, autoColliderSize ?? new Vector3(0.25f, 0.25f, 0.25f));

                  spawned++;
              }

              Debug.Log($"[FirestoreAnchorService] Indoor anchors fetched={fetched}, spawned={spawned}, mapId='{mapId}'.");
          });
#endif
    }

    // ======================================================
    // LOAD – OUTDOOR DATA ONLY (NO AR HERE)
    // ======================================================
    public void LoadOutdoorAnchorDocuments(
        Action<List<Dictionary<string, object>>> onLoaded,
        Action<Exception> onError = null
    )
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsReady)
        {
            var ex = new Exception("Firebase not ready");
            Debug.LogWarning("[FirestoreAnchorService] LoadOutdoorAnchorDocuments called but Firebase not ready yet.");
            onError?.Invoke(ex);
            return;
        }

        db.Collection("anchors")
          .WhereEqualTo("anchorType", "Outdoor")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread((Task<QuerySnapshot> task) =>
          {
              if (task.IsFaulted)
              {
                  Debug.LogError("[FirestoreAnchorService] LoadOutdoorAnchorDocuments faulted: " + task.Exception);
                  onError?.Invoke(task.Exception);
                  return;
              }

              if (task.IsCanceled)
              {
                  Debug.LogWarning("[FirestoreAnchorService] LoadOutdoorAnchorDocuments canceled.");
                  onError?.Invoke(new Exception("Firestore outdoor query canceled"));
                  return;
              }

              QuerySnapshot snap = task.Result;

              List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();
              foreach (var docSnap in snap.Documents)
              {
                  var dict = docSnap.ToDictionary();
                  dict["docId"] = docSnap.Id;
                  results.Add(dict);
              }

              onLoaded?.Invoke(results);
          });
#else
        onError?.Invoke(new Exception("LoadOutdoorAnchorDocuments called on non-Android or in Editor."));
#endif
    }

    // ======================================================
    // INTERNAL HELPERS (TAP SUPPORT)
    // ======================================================
    private static void EnsurePinTappable(GameObject pinObj, string layerName, Vector3 colliderSize)
    {
        if (pinObj == null) return;

        // collider
        if (pinObj.GetComponentInChildren<Collider>() == null)
        {
            var bc = pinObj.AddComponent<BoxCollider>();
            bc.size = colliderSize;
            bc.center = Vector3.zero;
            bc.isTrigger = false;
        }

        // layer
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0) ApplyLayerRecursively(pinObj, layer);
        else Debug.LogWarning($"[FirestoreAnchorService] Layer '{layerName}' not found. Create it in Project Settings > Tags and Layers.");
    }

    private static void ApplyLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        for (int i = 0; i < root.transform.childCount; i++)
            ApplyLayerRecursively(root.transform.GetChild(i).gameObject, layer);
    }
}