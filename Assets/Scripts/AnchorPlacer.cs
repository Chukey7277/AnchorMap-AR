using UnityEngine;
using UnityEngine.EventSystems;

public class AnchorPlacer : MonoBehaviour
{
    [Header("References")]
    public Camera arCamera;          // AR Camera
    public Transform mapSpace;       // Map Space (parent of AnchorsRoot)
    public Transform anchorsRoot;    // AnchorsRoot (MUST be child of Map Space)
    public GameObject anchorPrefab;  // Pin prefab
    public AnchorInfoUI infoUI;      // UI controller for title/description

    [Header("Map info")]
    [Tooltip("ID of the current map (for Firestore filtering)")]
    public string currentMapId = "cdis_lab";

    [Header("Placement Settings")]
    [Tooltip("How far above the clicked point to place the pin (meters).")]
    public float surfaceOffset = 0.07f;

    const float MaxRaycastDistance = 50f;
    const float IgnoreTapAfterCloseSeconds = 0.20f;   // small safety window

    void Update()
    {
        if (infoUI != null && infoUI.IsOpen)
            return;

        if (Time.time - AnchorInfoUI.LastCloseTime < IgnoreTapAfterCloseSeconds)
            return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            TryPlaceAnchor(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            Touch t = Input.GetTouch(0);

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(t.fingerId))
                return;

            TryPlaceAnchor(t.position);
        }
#endif
    }

    void TryPlaceAnchor(Vector2 screenPos)
    {
        if (!arCamera || !mapSpace || !anchorsRoot || !anchorPrefab)
        {
            Debug.LogWarning("AnchorPlacer: missing references");
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, MaxRaycastDistance))
        {
            Debug.Log("AnchorPlacer: hit " + hit.collider.name);

            // Hit in world space
            Vector3 worldPos = hit.point + hit.normal * surfaceOffset;

            // Convert to Map Space local
            Vector3 localPos = mapSpace.InverseTransformPoint(worldPos);

            // Instantiate under AnchorsRoot (child of Map Space)
            GameObject pin = Instantiate(anchorPrefab, anchorsRoot);
            pin.transform.localPosition = localPos;
            Debug.Log($"[AnchorPlacer] CREATED pin {pin.name} under {anchorsRoot.name} " +
          $"local={pin.transform.localPosition}, world={pin.transform.position}");
            // pin.transform.localRotation = Quaternion.identity;

            AnchorData data = pin.GetComponent<AnchorData>();
            if (data != null)
            {
                data.localPosition = localPos;
                data.mapId = currentMapId;
            }

            Debug.Log("AnchorPlacer: placed pin at local " + localPos);

            if (infoUI != null && data != null)
            {
            infoUI.OpenContributor(data, true);   // new pin; Cancel can delete
            }
            else if (infoUI != null)
            {
                Debug.LogWarning("AnchorPlacer: placed pin has no AnchorData component.");
            }
        }
        else
        {
            Debug.Log("AnchorPlacer: raycast missed (nothing hit)");
        }
    }
}