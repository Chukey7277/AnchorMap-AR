using UnityEngine;

public class HideMapMeshRuntime : MonoBehaviour
{
    void Start()
    {
        // On real device, hide all renderers under this object.
        // In the Editor we keep them visible for debugging.
#if !UNITY_EDITOR
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }
#endif
    }
}