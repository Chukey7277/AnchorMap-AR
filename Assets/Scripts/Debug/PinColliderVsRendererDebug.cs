using UnityEngine;

public class PinColliderVsRendererDebug : MonoBehaviour
{
    void Update()
    {
        var col = GetComponentInChildren<Collider>();
        var rend = GetComponentInChildren<Renderer>();

        if (col == null || rend == null) return;

        var dc = Vector3.Distance(col.bounds.center, rend.bounds.center);
        if (dc > 0.05f) // 5 cm threshold
        {
            Debug.LogWarning($"[PinMismatch] '{name}' collider-center != renderer-center dist={dc:F3} " +
                             $" col={col.bounds.center} rend={rend.bounds.center}");
        }
    }
}