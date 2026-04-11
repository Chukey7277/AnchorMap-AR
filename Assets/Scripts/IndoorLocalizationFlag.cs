using System;
using UnityEngine;

public class IndoorLocalizationFlag : MonoBehaviour
{
    public bool IsLocalized { get; private set; }

    [Header("Evaluation Context")]
    [HideInInspector] public string currentMapId = "CDIS";
    [HideInInspector] public string mode = "Unknown"; // set by ModePanel

    // NEW: notify ModePanel (and others) whenever localization changes
    public event Action<bool> OnLocalizationChanged;

    // NEW: optional - track last change time (useful to avoid loading too early)
    public float LastChangeTime { get; private set; } = -999f;

    public void MarkLocalized()
    {
        IsLocalized = true;
        LastChangeTime = Time.time;

        Debug.Log("[IndoorLocalizationFlag] Localized = TRUE");

        EvaluationLogger.Log(
            ev: "IndoorLocalizationResult",
            mode: mode,
            mapId: currentMapId,
            success: "success",
            details: "MultiSet callback"
        );

        OnLocalizationChanged?.Invoke(true);
    }

    public void MarkNotLocalized()
    {
        IsLocalized = false;
        LastChangeTime = Time.time;

        Debug.Log("[IndoorLocalizationFlag] Localized = FALSE");

        EvaluationLogger.Log(
            ev: "IndoorLocalizationResult",
            mode: mode,
            mapId: currentMapId,
            success: "failure",
            details: "MultiSet callback"
        );

        OnLocalizationChanged?.Invoke(false);
    }

   public void ResetLocalized()
{
    if (!IsLocalized)  // already false -> don't rebroadcast loss
    {
        LastChangeTime = Time.time;
        Debug.Log("[IndoorLocalizationFlag] ResetLocalized() called but already FALSE (no event).");
        return;
    }

    IsLocalized = false;
    LastChangeTime = Time.time;

    Debug.Log("[IndoorLocalizationFlag] Localized reset to FALSE");
    OnLocalizationChanged?.Invoke(false);
}
}