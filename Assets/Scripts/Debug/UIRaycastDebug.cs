using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRaycastDebug : MonoBehaviour
{
    private PointerEventData pointerData;
    private readonly List<RaycastResult> results = new List<RaycastResult>();

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            LogUIHits(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            LogUIHits(Input.GetTouch(0).position);
        }
#endif
    }

    private void LogUIHits(Vector2 screenPos)
    {
        if (EventSystem.current == null) return;

        pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = screenPos;

        results.Clear();
        EventSystem.current.RaycastAll(pointerData, results);

        Debug.Log($"[UIRaycastDebug] Hit count = {results.Count} at {screenPos}");

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            Debug.Log($"[UIRaycastDebug] {i}: {r.gameObject.name}  layer={LayerMask.LayerToName(r.gameObject.layer)}");
        }
    }
}