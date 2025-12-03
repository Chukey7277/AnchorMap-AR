using System;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

[FirestoreData]
public class AnchorRecord
{
    [FirestoreProperty] public string mapId { get; set; }
    [FirestoreProperty] public float localX { get; set; }
    [FirestoreProperty] public float localY { get; set; }
    [FirestoreProperty] public float localZ { get; set; }
    [FirestoreProperty] public string title { get; set; }
    [FirestoreProperty] public string description { get; set; }
    [FirestoreProperty] public Timestamp createdAt { get; set; }
    [FirestoreProperty] public Timestamp updatedAt { get; set; }
}

public static class AnchorDatabase
{
    static FirebaseFirestore db => FirebaseFirestore.DefaultInstance;

    // Call this when user presses Save
    public static async Task SaveAnchorAsync(string mapId, AnchorData data, Vector3 localPos)
    {
        if (!FirebaseInit.IsReady)
        {
            Debug.LogWarning("Firebase not ready yet, not saving anchor.");
            return;
        }

        var record = new AnchorRecord
        {
            mapId = mapId,
            localX = localPos.x,
            localY = localPos.y,
            localZ = localPos.z,
            title = data.title,
            description = data.description,
            createdAt = Timestamp.GetCurrentTimestamp(),
            updatedAt = Timestamp.GetCurrentTimestamp()
        };

        // anchors collection; Firestore auto-id
        await db.Collection("anchors").AddAsync(record);
        Debug.Log("Anchor saved to Firestore.");
    }

    // Example: load all anchors for current map
    public static async Task<QuerySnapshot> LoadAnchorsForMapAsync(string mapId)
    {
        if (!FirebaseInit.IsReady)
        {
            Debug.LogWarning("Firebase not ready yet, cannot load anchors.");
            return null;
        }

        var query = db.Collection("anchors").WhereEqualTo("mapId", mapId);
        return await query.GetSnapshotAsync();
    }
}