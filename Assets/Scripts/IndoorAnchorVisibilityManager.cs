using UnityEngine;

public class IndoorAnchorVisibilityManager : MonoBehaviour
{
    [Header("References")]
    public Camera arCamera;
    public Transform anchorsRoot;

    [Header("Visibility")]
    public float visibilityRadiusMeters = 10f;
    public float refreshIntervalSeconds = 0.2f;

    private float nextRefreshTime = 0f;

    void Update()
    {
        if (arCamera == null || anchorsRoot == null)
            return;

        if (Time.time < nextRefreshTime)
            return;

        nextRefreshTime = Time.time + refreshIntervalSeconds;

        Vector3 camPos = arCamera.transform.position;

        for (int i = 0; i < anchorsRoot.childCount; i++)
        {
            Transform pin = anchorsRoot.GetChild(i);
            float dist = Vector3.Distance(camPos, pin.position);
            bool shouldShow = dist <= visibilityRadiusMeters;

            SetPinVisible(pin.gameObject, shouldShow);
        }
    }

    private void SetPinVisible(GameObject pinObj, bool visible)
    {
        var renderers = pinObj.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            r.enabled = visible;

        var colliders = pinObj.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
            c.enabled = visible;
    }
}