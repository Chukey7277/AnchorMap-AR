using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;

public class FirebaseWriteTest : MonoBehaviour
{
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError("[FirebaseWriteTest] Firebase deps not available: " + task.Result);
                    return;
                }

                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

                // Create a simple test document
                DocumentReference docRef = db.Collection("debug_anchors")
                                             .Document("unity_test");

                var data = new Dictionary<string, object>
                {
                    { "message", "Hello from Unity!" },
                    { "platform", Application.platform.ToString() },
                    { "time_ms", System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                };

                docRef.SetAsync(data).ContinueWithOnMainThread(writeTask =>
                {
                    if (writeTask.IsFaulted)
                    {
                        Debug.LogError("[FirebaseWriteTest] Write failed: " + writeTask.Exception);
                    }
                    else
                    {
                        Debug.Log("[FirebaseWriteTest] Write SUCCESS!");
                    }
                });
            });
    }
}