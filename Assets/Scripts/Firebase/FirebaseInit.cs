using UnityEngine;
using Firebase;
using Firebase.Extensions;

public class FirebaseInit : MonoBehaviour
{
    public static bool IsReady { get; private set; }

    async void Awake()
    {
        IsReady = false;

        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available)
        {
            IsReady = true;
            Debug.Log("[FirebaseInit] Firebase ready");
        }
        else
        {
            Debug.LogError($"[FirebaseInit] Could not resolve all Firebase dependencies: {dependencyStatus}");
        }
    }
}