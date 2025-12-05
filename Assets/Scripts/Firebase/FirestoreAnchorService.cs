using System;
using System.Collections.Generic;
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

    void Awake()
    {
        // Simple singleton
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
    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var status = task.Result;
            if (status == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                firebaseReady = true;
                Debug.Log("[FirestoreAnchorService] Firebase ready.");
            }
            else
            {
                Debug.LogError("[FirestoreAnchorService] Could not resolve all Firebase dependencies: " + status);
            }
        });
    }
#endif

    // ----------------------------------------------------------------------
    // SAVE
    // ----------------------------------------------------------------------

    /// <summary>
    /// Save one anchor document to Firestore.
    /// Assumes data.transform.localPosition is MAP-LOCAL (child of AnchorsRoot under Map Space).
    /// </summary>
    public void SaveAnchor(AnchorData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[FirestoreAnchorService] SaveAnchor called with null data.");
            return;
        }

        Vector3 localPos = data.transform.localPosition;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!firebaseReady || db == null)
        {
            Debug.LogWarning("[FirestoreAnchorService] Firebase not ready, skipping save.");
            return;
        }

        var doc = new Dictionary<string, object>
        {
            { "mapId",        data.mapId },
            { "title",        data.title },
            { "description",  data.description },

            // store MAP-LOCAL coordinates (relative to Map Space)
            { "localX",       localPos.x },
            { "localY",       localPos.y },
            { "localZ",       localPos.z },

            { "createdAt",    Timestamp.GetCurrentTimestamp() }
        };

        db.Collection("anchors")
          .AddAsync(doc)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
              {
                  Debug.LogError("[FirestoreAnchorService] Error writing document: " + task.Exception);
              }
              else
              {
                  Debug.Log("[FirestoreAnchorService] Anchor saved with ID: " + task.Result.Id +
                            $"  (mapId={data.mapId}, local=({localPos.x:F3},{localPos.y:F3},{localPos.z:F3}))");
              }
          });
#else
        Debug.Log($"[FirestoreAnchorService] (Editor) Would save anchor: " +
                  $"{data.mapId} / {data.title} @ local ({localPos.x:F3}, {localPos.y:F3}, {localPos.z:F3})");
#endif
    }

    // ----------------------------------------------------------------------
    // LOAD
    // ----------------------------------------------------------------------

    /// <summary>
    /// Load all anchors for a mapId and spawn prefabs.
    /// We treat stored localX/Y/Z as Map-Space local coordinates:
    /// Contributor:   world → local via mapSpace.InverseTransformPoint
    /// Explorer:      local → world via mapSpace.TransformPoint
    /// </summary>
    public void LoadAnchorsForMap(
        string mapId,
        Transform mapSpace,
        Transform anchorsRoot,
        GameObject anchorPrefab
    )
    {
        if (string.IsNullOrEmpty(mapId) || mapSpace == null || anchorsRoot == null || anchorPrefab == null)
        {
            Debug.LogWarning(
                "[FirestoreAnchorService] LoadAnchorsForMap: missing parameters.\n" +
                $"  mapId={mapId}\n" +
                $"  mapSpace={mapSpace}\n" +
                $"  anchorsRoot={anchorsRoot}\n" +
                $"  anchorPrefab={anchorPrefab}"
            );
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!firebaseReady || db == null)
        {
            Debug.LogWarning("[FirestoreAnchorService] Firebase not ready, cannot load anchors.");
            return;
        }

        // Optional: clear existing children so we don't duplicate
        for (int i = anchorsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(anchorsRoot.GetChild(i).gameObject);
        }

        db.Collection("anchors")
          .WhereEqualTo("mapId", mapId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
              {
                  Debug.LogError("[FirestoreAnchorService] Error loading anchors: " + task.Exception);
                  return;
              }

              var snapshot = task.Result;
              Debug.Log($"[FirestoreAnchorService] Loaded {snapshot.Count} anchors for map '{mapId}'.");

              foreach (var doc in snapshot.Documents)
              {
                  var dict = doc.ToDictionary();

                  // Use the SAME field names we used when saving
                  float x = Convert.ToSingle(dict["localX"]);
                  float y = Convert.ToSingle(dict["localY"]);
                  float z = Convert.ToSingle(dict["localZ"]);

                  string title       = dict.ContainsKey("title")       ? dict["title"]       as string : "";
                  string description = dict.ContainsKey("description") ? dict["description"] as string : "";

                  // 1) Map-local position
                  Vector3 localPosMap = new Vector3(x, y, z);

                  // 2) Convert back to world using mapSpace
                  Vector3 worldPos = mapSpace.TransformPoint(localPosMap);

                  // 3) Instantiate under AnchorsRoot at that world position
                  GameObject pin = GameObject.Instantiate(anchorPrefab, anchorsRoot);
                  pin.transform.position = worldPos;   // NOTE: position, not localPosition
                  // keep prefab rotation / scale as-is

                  Debug.Log($"[FirestoreAnchorService] spawn '{title}' " +
                            $"map-local=({localPosMap.x:F3},{localPosMap.y:F3},{localPosMap.z:F3}) " +
                            $"world=({worldPos.x:F3},{worldPos.y:F3},{worldPos.z:F3})");

                  var data = pin.GetComponent<AnchorData>();
                  if (data != null)
                  {
                      data.mapId         = mapId;
                      data.localPosition = localPosMap;
                      data.title         = title;
                      data.description   = description;
                  }
                  else
                  {
                      Debug.LogWarning("[FirestoreAnchorService] Spawned pin has no AnchorData component.");
                  }
              }
          });
#else
        Debug.Log($"[FirestoreAnchorService] (Editor) Would load anchors for map '{mapId}'.");
#endif
    }
}