using UnityEngine;

public class HideMapVisuals : MonoBehaviour
{
    void Start()
    {
        HideEverything();
    }

    void OnEnable()
    {
        HideEverything();
    }

    void OnTransformChildrenChanged()
    {
        // If SDK spawns more children under MapVisuals, hide them too
        HideEverything();
    }

    void HideEverything()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.enabled = false;
        }

        Debug.Log($"[HideMapVisuals] Disabled {renderers.Length} renderers under {name}");
    }
}