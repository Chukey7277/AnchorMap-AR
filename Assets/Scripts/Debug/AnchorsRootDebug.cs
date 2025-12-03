using UnityEngine;

public class AnchorsRootDebug : MonoBehaviour
{
    public Transform anchorsRoot;

    void Update()
    {
        if (anchorsRoot != null && Time.frameCount % 60 == 0) // ~once per second
        {
            Debug.Log($"[AnchorsRootDebug] AnchorsRoot child count = {anchorsRoot.childCount}");
        }
    }
}