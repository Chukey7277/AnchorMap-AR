using UnityEngine;
using UnityEngine.EventSystems;

public class AnchorTapExplorer : MonoBehaviour
{
    [Header("References")]
    public Camera arCamera;
    public AnchorInfoUI infoUI;

    [Header("Raycast Settings")]
    public float maxDistance = 50f;

    [Tooltip("Layers that contain tappable anchor pins (e.g. 'AnchorPin'). " +
             "If left as 'Nothing', all layers will be raycast.")]
    public LayerMask anchorLayerMask;

    void Update()
    {
        // If the info panel is already open, ignore taps
        if (infoUI != null && infoUI.IsOpen)
            return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            TrySelectAnchor(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            Touch t = Input.GetTouch(0);

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(t.fingerId))
                return;

            TrySelectAnchor(t.position);
        }
#endif
    }

    void TrySelectAnchor(Vector2 screenPos)
    {
        if (!arCamera || infoUI == null)
        {
            Debug.LogWarning("[AnchorTapExplorer] missing references");
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;
        bool hitSomething;

        // If no layers are set in the mask, raycast against ALL layers
        if (anchorLayerMask.value == 0)
        {
            hitSomething = Physics.Raycast(ray, out hit, maxDistance);
        }
        else
        {
            hitSomething = Physics.Raycast(ray, out hit, maxDistance, anchorLayerMask);
        }

        if (!hitSomething)
            return;

        Debug.Log($"[AnchorTapExplorer] Hit {hit.collider.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

        AnchorData data = hit.collider.GetComponentInParent<AnchorData>();
        if (data != null)
        {
            Debug.Log("[AnchorTapExplorer] tapped anchor: " + data.title);
            // VIEW-ONLY mode now:
            infoUI.OpenExplorer(data);
        }
        else
        {
            Debug.Log("[AnchorTapExplorer] collider has no AnchorData in parents.");
        }
    }
}