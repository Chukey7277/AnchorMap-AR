using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;

public class AnchorStabilityLogger : MonoBehaviour
{
    [Header("Drift logging")]
    public float intervalSeconds = 2f;
    public bool logIndoor = true;
    public bool logOutdoor = true;

    // Track when we've “armed” the baseline for outdoor anchors
    private readonly HashSet<string> _outdoorBaselineSet = new HashSet<string>();

    private Coroutine _co;

    private void OnEnable() => _co = StartCoroutine(Loop());

    private void OnDisable()
    {
        if (_co != null) StopCoroutine(_co);
        _co = null;
        _outdoorBaselineSet.Clear();
    }

    private IEnumerator Loop()
    {
        var wait = new WaitForSeconds(intervalSeconds);

        while (true)
        {
            foreach (var kv in AnchorRegistry.Records)
            {
                var r = kv.Value;
                if (r == null || r.target == null) continue;

                bool isIndoor = r.mode == "Indoor";
                if (isIndoor && !logIndoor) continue;
                if (!isIndoor && !logOutdoor) continue;

                // ---------- Outdoor: wait until geospatial anchor is actually Tracking ----------
                if (!isIndoor)
                {
                    var geo = r.target.GetComponent<ARGeospatialAnchor>();
                    if (geo != null)
                    {
                        // Don’t measure drift until it’s resolved/tracking
                        if (geo.trackingState != TrackingState.Tracking)
                            continue;

                        // First time it becomes Tracking: set baseline NOW
                        if (!_outdoorBaselineSet.Contains(r.anchorId))
                        {
                            r.initialWorldPos = r.target.position;
                            _outdoorBaselineSet.Add(r.anchorId);
                            continue; // skip logging on the same tick we set baseline
                        }
                    }
                }

                Vector3 currWorld = r.target.position;
                float driftWorld = Vector3.Distance(r.initialWorldPos, currWorld);

                float driftMap = -1f;
                if (isIndoor && r.mapSpace != null)
                {
                    Vector3 currMapLocal = r.mapSpace.InverseTransformPoint(currWorld);
                    driftMap = Vector3.Distance(r.initialMapLocalPos, currMapLocal);
                }

                string details = isIndoor
                    ? $"drift_world_m={driftWorld:F3};drift_map_m={driftMap:F3}"
                    : $"drift_world_m={driftWorld:F3}";

                EvaluationLogger.Log(
                    ev: "AnchorDrift",
                    mode: r.mode,
                    mapId: r.mapId,
                    details: details,
                    pos: currWorld,
                    anchorId: r.anchorId
                );
            }

            yield return wait;
        }
    }
}