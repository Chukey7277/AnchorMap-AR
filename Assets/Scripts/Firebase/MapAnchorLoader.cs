using UnityEngine;

public class MapAnchorLoader : MonoBehaviour
{
    [Header("Map info")]
    [Tooltip("Must match the mapId used by AnchorPlacer when saving.")]
    public string mapId = "cdis_lab";

    [Header("Scene references")]
    [Tooltip("Map Space (parent of AnchorsRoot), moved by MultiSet/VPS.")]
    public Transform mapSpace;

    [Tooltip("AnchorsRoot (child of Map Space) where pins will be instantiated.")]
    public Transform anchorsRoot;   // AnchorsRoot under Map Space

    [Tooltip("Same anchor prefab used by AnchorPlacer.")]
    public GameObject anchorPrefab;

    public void LoadAnchors()
    {
        if (string.IsNullOrEmpty(mapId))
        {
            Debug.LogWarning("[MapAnchorLoader] mapId is empty, cannot load anchors.");
            return;
        }

        if (mapSpace == null || anchorsRoot == null || anchorPrefab == null)
        {
            Debug.LogWarning(
                $"[MapAnchorLoader] missing references: " +
                $"mapSpace={(mapSpace != null)}, " +
                $"anchorsRoot={(anchorsRoot != null)}, " +
                $"anchorPrefab={(anchorPrefab != null)}"
            );
            return;
        }

        if (FirestoreAnchorService.Instance == null)
        {
            Debug.LogWarning("[MapAnchorLoader] FirestoreAnchorService.Instance is null.");
            return;
        }

        Debug.Log("[MapAnchorLoader] Loading anchors for mapId=" + mapId);
        FirestoreAnchorService.Instance.LoadAnchorsForMap(
            mapId,
            mapSpace,
            anchorsRoot,
            anchorPrefab
        );
    }
}