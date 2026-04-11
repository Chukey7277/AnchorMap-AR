using UnityEngine;

public class AnchorRegistryHandle : MonoBehaviour
{
    [HideInInspector] public string anchorId;

    private void OnDestroy()
    {
        AnchorRegistry.Unregister(anchorId);
    }
}