using UnityEngine;

public class PinDebugDrift : MonoBehaviour
{
    private Vector3 startLocal;
    private Vector3 startWorld;

    void Start()
    {
        startLocal = transform.localPosition;
        startWorld = transform.position;
        Debug.Log($"[PinDebugDrift] START {name} local={startLocal}, world={startWorld}");
    }

    void Update()
    {
        // print once per second
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[PinDebugDrift] {name} local={transform.localPosition}, world={transform.position}");
        }
    }
}