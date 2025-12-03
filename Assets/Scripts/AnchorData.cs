using UnityEngine;

public class AnchorData : MonoBehaviour
{
    public string title;
    public string description;
    public Vector3 localPosition;
        [Header("Map info (optional but useful)")]
    public string mapId;   // set from AnchorPlacer for the current map
}