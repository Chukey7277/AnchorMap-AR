using UnityEngine;
using Firebase;
using Firebase.Extensions;
// plus Firestore / Database usings if you added them

public class FirebaseInitTest : MonoBehaviour
{
    void Start()
    {
#if UNITY_EDITOR
        // In the editor: don't initialize Firebase, just log.
        Debug.Log("FirebaseInitTest: skipping Firebase init in Editor.");
        return;
#endif

        // On Android / device: run normally
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var status = task.Result;
            if (status == DependencyStatus.Available)
            {
                Debug.Log("FirebaseInitTest: Firebase is READY.");
                // TODO: your Firestore / DB code later
            }
            else
            {
                Debug.LogError($"FirebaseInitTest: Firebase dependencies not available: {status}");
            }
        });
    }
}