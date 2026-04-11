using UnityEngine;
using UnityEngine.EventSystems;

public class AnchorTapExplorer : MonoBehaviour
{
    [Header("References")]
    public Camera arCamera;
    public AnchorInfoUI infoUI;

    [Header("State Gate (Required)")]
    public ModePanel modePanel;

    [Header("Raycast Settings")]
    public float maxDistance = 100f;

    [Tooltip("Set this to the ARPin layer (your pin prefab layer).")]
    public LayerMask anchorLayerMask;
    [Header("Distance Filter")]
    public bool useOutdoorInteractionRadius = true;
    public float maxOutdoorInteractionDistanceMeters = 30f;
    [Header("Debug")]
    public bool logNoHit = false;

    void OnDisable()
    {
        // If explorer is being turned off (mode switch), don't let UI linger
        if (infoUI != null)
            infoUI.ForceCloseNoDelete();
    }

    void Update()
{
    // 0) Explorer only in explorer active states
    if (modePanel == null ||
        !(modePanel.IsInExplorerIndoorActive() || modePanel.IsInExplorerOutdoorActive()))
        return;

    // 1) Block taps right after mode / back / UI navigation
    if (Time.time - ModePanel.LastModeSwitchTime < 0.8f)
        return;

    // 2) Block taps right after closing info panel
    if (Time.time - AnchorInfoUI.LastCloseTime < 0.3f)
        return;

    // 3) Global UI blocker
    if (UITapBlocker.ShouldBlock())
        return;

    // 4) If info panel is open, ignore taps
    if (infoUI != null && infoUI.IsOpen)
        return;

#if UNITY_EDITOR
    if (Input.GetMouseButtonDown(0))
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        TrySelectAnchor(Input.mousePosition);
    }
#else
    if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
    {
        Touch t = Input.GetTouch(0);

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
            return;

        TrySelectAnchor(t.position);
    }
#endif
}

    void TrySelectAnchor(Vector2 screenPos)
    {
        // Double-check gate (in case Update is bypassed in future edits)
        if (modePanel == null || 
    !(modePanel.IsInExplorerIndoorActive() || modePanel.IsInExplorerOutdoorActive()))
    return;

        if (!arCamera || infoUI == null)
        {
            Debug.LogWarning("[AnchorTapExplorer] missing references");
            return;
        }

        if (anchorLayerMask.value == 0)
        {
            Debug.LogWarning("[AnchorTapExplorer] anchorLayerMask is NOTHING. Set it to ARPin in the Inspector.");
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPos);

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, anchorLayerMask, QueryTriggerInteraction.Collide))
{
    if (logNoHit)
        Debug.Log("[AnchorTapExplorer] No hit on anchor layer mask.");
    return;
}

        Debug.Log($"[AnchorTapExplorer] Hit '{hit.collider.name}' on layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'");

        AnchorData data = hit.collider.GetComponentInParent<AnchorData>();
        if (data != null)
        {
            Debug.Log("[AnchorTapExplorer] Tapped anchor: " + data.title);
            infoUI.OpenExplorer(data);
        }
        else
        {
            Debug.Log("[AnchorTapExplorer] Hit collider, but no AnchorData in parents.");
        }
    }
}