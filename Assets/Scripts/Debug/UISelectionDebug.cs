using UnityEngine;
using UnityEngine.EventSystems;

public class UISelectionDebug : MonoBehaviour
{
    private GameObject lastSelected;

    void Update()
    {
        if (EventSystem.current == null) return;

        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current != lastSelected)
        {
            Debug.Log("[UISelectionDebug] Selected = " + (current != null ? current.name : "<null>"));
            lastSelected = current;
        }
    }
}