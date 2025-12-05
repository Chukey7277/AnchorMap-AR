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
        // Small guard-log when we ignore taps
        if (infoUI != null && infoUI.IsOpen)
        {
            // Debug.Log("[AnchorPlacer] Skipping input: info UI is open.");
            return;
        }

        if (Time.time - AnchorInfoUI.LastCloseTime < IgnoreTapAfterCloseSeconds)
        {
            // Debug.Log("[AnchorPlacer] Skipping input: just closed UI.");
            return;
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("[AnchorPlacer] Click over UI – ignoring.");
                return;
            }

            Debug.Log("[AnchorPlacer] Mouse click – trying to place anchor.");
            TryPlaceAnchor(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            Touch t = Input.GetTouch(0);

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(t.fingerId))
            {
                Debug.Log("[AnchorPlacer] Touch over UI – ignoring.");
                return;
            }

            Debug.Log($"[AnchorPlacer] Touch ended at {t.position} – trying to place anchor.");
            TryPlaceAnchor(t.position);
        }
#endif
    }

    void TryPlaceAnchor(Vector2 screenPos)
    {
        if (!arCamera || !mapSpace || !anchorsRoot || !anchorPrefab)
        {
            Debug.LogWarning(
                "[AnchorPlacer] missing references:" +
                $"\n  arCamera={arCamera}" +
                $"\n  mapSpace={mapSpace}" +
                $"\n  anchorsRoot={anchorsRoot}" +
                $"\n  anchorPrefab={anchorPrefab}"
            );
            return;
        }

        Debug.Log($"[AnchorPlacer] TryPlaceAnchor screenPos={screenPos}");

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        Debug.Log($"[AnchorPlacer] Ray origin={ray.origin}, dir={ray.direction}");

        if (Physics.Raycast(ray, out RaycastHit hit, MaxRaycastDistance))
        {
            Debug.Log("[AnchorPlacer] Raycast HIT: " + hit.collider.name +
                      $" at worldPos={hit.point} dist={hit.distance}");

            // Hit in world space
            Vector3 worldPos = hit.point + hit.normal * surfaceOffset;

            // Convert to Map Space local
            Vector3 localPos = mapSpace.InverseTransformPoint(worldPos);

            // Instantiate under AnchorsRoot (child of Map Space)
            GameObject pin = Instantiate(anchorPrefab, anchorsRoot);
            pin.transform.localPosition = localPos;

            Debug.Log($"[AnchorPlacer] CREATED pin {pin.name} under {anchorsRoot.name} " +
                      $"local={pin.transform.localPosition}, world={pin.transform.position}");

            AnchorData data = pin.GetComponent<AnchorData>();
            if (data != null)
            {
                data.localPosition = localPos;
                data.mapId = currentMapId;
            }
            else
            {
                Debug.LogWarning("[AnchorPlacer] placed pin has NO AnchorData component.");
            }

            Debug.Log("[AnchorPlacer] placed pin at local " + localPos);

            if (infoUI != null && data != null)
            {
                infoUI.OpenContributor(data, true);   // new pin; Cancel can delete
            }
        }
        else
        {
            Debug.Log("[AnchorPlacer] raycast MISSED (nothing hit). " +
                      "Running RaycastAll for debug…");

            // Extra debug: see if ANYTHING is along that ray
            RaycastHit[] hits = Physics.RaycastAll(ray, MaxRaycastDistance);
            if (hits.Length == 0)
            {
                Debug.Log("[AnchorPlacer] RaycastAll also found ZERO colliders. " +
                          "Likely no colliders in front of the camera.");
            }
            else
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    var h = hits[i];
                    Debug.Log($"[AnchorPlacer] RaycastAll hit[{i}] " +
                              $"{h.collider.name} at dist={h.distance} " +
                              $"layer={LayerMask.LayerToName(h.collider.gameObject.layer)}");
                }
            }
        }
    }
}