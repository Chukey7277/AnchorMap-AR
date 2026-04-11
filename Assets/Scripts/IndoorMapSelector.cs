using UnityEngine;

/// <summary>
/// Handles choosing which INDOOR map we are working on,
/// and wires that into AnchorPlacer + MapAnchorLoader.
/// For now it's explicit methods per map (CDIS, ESB, etc.)
/// </summary>
public class IndoorMapSelector : MonoBehaviour
{
    [Header("Core components")]
    public AnchorPlacer anchorPlacer;
    public MapAnchorLoader mapLoader;

    [Header("CDIS Lab Map")]
    public string cdisMapId = "cdis_lab";
    public Transform cdisMapSpace;
    public Transform cdisAnchorsRoot;

    [Header("ESB Lab Map (optional)")]
    public string esbMapId = "esb_lab";
    public Transform esbMapSpace;
    public Transform esbAnchorsRoot;

    [Header("Museum Map (optional)")]
    public string museumMapId = "museum";
    public Transform museumMapSpace;
    public Transform museumAnchorsRoot;

    /// <summary>
    /// Called by the "CDIS Lab" button.
    /// </summary>
    public void OnChooseCdisLab()
    {
        SetIndoorMap(cdisMapId, cdisMapSpace, cdisAnchorsRoot);
    }

    /// <summary>
    /// Called by the "ESB Lab" button.
    /// </summary>
    public void OnChooseEsbLab()
    {
        SetIndoorMap(esbMapId, esbMapSpace, esbAnchorsRoot);
    }

    /// <summary>
    /// Called by the "Museum" button.
    /// </summary>
    public void OnChooseMuseum()
    {
        SetIndoorMap(museumMapId, museumMapSpace, museumAnchorsRoot);
    }

    private void SetIndoorMap(string mapId, Transform mapSpace, Transform anchorsRoot)
    {
        if (anchorPlacer == null || mapLoader == null)
        {
            Debug.LogError("[IndoorMapSelector] anchorPlacer or mapLoader is NULL.");
            return;
        }

        if (mapSpace == null || anchorsRoot == null)
        {
            Debug.LogError($"[IndoorMapSelector] Map '{mapId}' has missing mapSpace/anchorsRoot.");
            return;
        }

        Debug.Log($"[IndoorMapSelector] Switching to indoor map '{mapId}'");

        // 1) AnchorPlacer wires
        anchorPlacer.currentMapId = mapId;
        anchorPlacer.mapSpace     = mapSpace;
        anchorPlacer.anchorsRoot  = anchorsRoot;

        // 2) Loader wires (Explorer mode)
        mapLoader.mapId      = mapId;
        mapLoader.mapSpace   = mapSpace;
        mapLoader.anchorsRoot= anchorsRoot;

        // Optional: you can also enable/disable map meshes here
        // (e.g., only show the chosen map’s 3D mesh).
    }
}