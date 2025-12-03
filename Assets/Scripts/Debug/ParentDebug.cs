using UnityEngine;

public class ParentDebug : MonoBehaviour
{
    void OnEnable()
    {
        Debug.Log($"[ParentDebug] ENABLED: {name} (instanceID={GetInstanceID()})");
    }

    void OnDisable()
    {
        Debug.Log($"[ParentDebug] DISABLED: {name} (instanceID={GetInstanceID()})");
    }
}