using UnityEngine;

public class PinDebug : MonoBehaviour
{
    void OnEnable()
    {
        Debug.Log($"[PinDebug] ENABLED: {name} (instanceID={GetInstanceID()})");
    }

    void OnDisable()
    {
        Debug.Log($"[PinDebug] DISABLED: {name} (instanceID={GetInstanceID()})");
    }

    void OnDestroy()
    {
        Debug.Log($"[PinDebug] DESTROYED: {name} (instanceID={GetInstanceID()})");
    }
}