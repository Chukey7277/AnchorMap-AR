using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class ModePanel : MonoBehaviour
{
    private enum UiState
    {
        Welcome,

        // Contributor
        ContributorChoice,
        ContributorIndoorMapList,
        ContributorIndoorActive,
        ContributorOutdoorActive,

        // Explorer
        ExplorerChoice,
        ExplorerIndoorMapList,
        ExplorerIndoorActive,
        ExplorerOutdoorActive
    }

    private UiState currentState = UiState.Welcome;
    public static float LastModeSwitchTime = -999f;   // used to debounce tap-leaks

    [Header("Main Panels")]
    public GameObject welcomePanel;
    public GameObject contributorChoicePanel;
    public GameObject explorerChoicePanel;
    public GameObject indoorMapListPanel;

    [Header("Top UI")]
    public GameObject backButton;

    [Header("Wait Panels")]
    public GameObject earthWaitPanel;
    public GameObject indoorWaitPanel;

    [Header("Core Systems")]
    public AnchorPlacer anchorPlacer;
    public AnchorTapExplorer explorer;

    [Header("Indoor")]
    public MapAnchorLoader loader;
    public IndoorLocalizationFlag indoorLocalizationFlag;

    [Header("Outdoor")]
    public EarthTrackingStatusUI earthTrackingUI;
    public OutdoorEarthAnchorLoader outdoorEarthAnchorLoader;

    [Header("Wait Settings")]
    [Tooltip("Show wait overlay at least this long even if tracking becomes ready instantly.")]
    public float minWaitSeconds = 2.0f;

    [Header("Indoor UX")]
    public GameObject indoorReadyPanel;           // small panel that says “Ready!”
    public float indoorReadySeconds = 1.5f;

    [Header("Debug")]
    public bool verboseLogs = true;
    [Header("UI Overlays")]
    public AnchorInfoUI infoUI;

    private Coroutine earthRoutine;
    private Coroutine indoorRoutine;
    private Coroutine indoorReadyRoutine;

    // Cache so UI navigation does not force a fresh localization cycle
    private string activeIndoorMapId = "";        // currently selected map id
    private bool indoorSessionLocalized = false;  // localized for activeIndoorMapId during this session
    [Header("Mode Banner")]
    public GameObject modeBanner;
    public TMP_Text modeBannerText;
    public Image modeBannerImage;   // optional, only if you want different colors

    void Start()
    {
        if (indoorLocalizationFlag != null)
            indoorLocalizationFlag.OnLocalizationChanged += OnIndoorLocalizationChanged;

        GoToWelcome();
    }

    private void OnDestroy()
    {
        if (indoorLocalizationFlag != null)
            indoorLocalizationFlag.OnLocalizationChanged -= OnIndoorLocalizationChanged;
    }

    // Called whenever MultiSet flips localized <-> not localized
    private void OnIndoorLocalizationChanged(bool isLocalized)
    {
        if (verboseLogs)
            Debug.Log($"[ModePanel] OnIndoorLocalizationChanged -> {isLocalized} (state={currentState}, map={activeIndoorMapId})");

        // If we lose localization at any time, invalidate cache.
       if (!isLocalized)
{
    indoorSessionLocalized = false;

    if (anchorPlacer != null) anchorPlacer.enabled = false;
    if (explorer != null) explorer.enabled = false;

    if (indoorRoutine != null)
    {
        StopCoroutine(indoorRoutine);
        indoorRoutine = null;
    }

    if (currentState == UiState.ContributorIndoorActive || currentState == UiState.ExplorerIndoorActive)
    {
        ShowIndoorWait(true);
        ShowModeBanner("Localization lost. Please scan the environment again");

        bool isExplorer = (currentState == UiState.ExplorerIndoorActive);
        indoorRoutine = StartCoroutine(WaitForIndoorLocalized_ThenProceed(isExplorer));
    }

    return;
}

        // If we regained localization while in indoor active state, proceed.
        if (isLocalized)
        {
            indoorSessionLocalized = true;

            if (currentState == UiState.ContributorIndoorActive || currentState == UiState.ExplorerIndoorActive)
            {
                // give MultiSet a tiny settle time (optional but helps)
                StartCoroutine(ReenableAfterSettle());
            }
        }
    }

    private IEnumerator ReenableAfterSettle()
    {
        // minimum UX wait if panel just popped
        yield return new WaitForSeconds(0.5f);

        // hide wait and enable the correct behaviour
        ShowIndoorWait(false);

        bool isExplorer = (currentState == UiState.ExplorerIndoorActive);

        if (isExplorer)
        {
    
            if (anchorPlacer != null) anchorPlacer.enabled = false;  
            if (explorer != null) explorer.enabled = true;
        }
        else
        {
            if (anchorPlacer != null) anchorPlacer.enabled = true;

            if (indoorReadyPanel != null)
            {
                if (indoorReadyRoutine != null) StopCoroutine(indoorReadyRoutine);
                indoorReadyRoutine = StartCoroutine(ShowIndoorReadyMessage());
            }
        }

        // load anchors after regained localization (explorer especially)
        if (loader != null && !string.IsNullOrEmpty(loader.mapId))
            loader.LoadAnchors();
    }

    // ===================== UI HELPERS =====================

    private void SetMainPanels(bool welcome, bool contributorChoice, bool explorerChoice, bool mapList)
    {
        SafeSet(welcomePanel, welcome);
        SafeSet(contributorChoicePanel, contributorChoice);
        SafeSet(explorerChoicePanel, explorerChoice);
        SafeSet(indoorMapListPanel, mapList);
    }

   private void EnterState(UiState next)
{
    if (infoUI != null && infoUI.IsOpen)
        infoUI.ForceCloseNoDelete();
     
    HideModeBanner();
    SetState(next);

    HideAllWaitPanels();
    StopAllRoutines();

    DisableAllBehaviours();
    DisableEarthUI();
}

    private void WarmupIndoorLocalization()
    {
        if (!verboseLogs) return;

        if (indoorSessionLocalized)
            Debug.Log("[ModePanel] WarmupIndoorLocalization: already localized (session cached).");
        else
            Debug.Log("[ModePanel] WarmupIndoorLocalization: waiting for localization (no cache).");
    }

    private void DebugIndoorCache(string where, string mapId = null)
    {
        if (!verboseLogs) return;
        Debug.Log($"[ModePanel] {where} | state={currentState} activeIndoorMapId='{activeIndoorMapId}' cachedLocalized={indoorSessionLocalized} flag={(indoorLocalizationFlag != null ? indoorLocalizationFlag.IsLocalized.ToString() : "null")} mapIdArg='{mapId ?? ""}'");
    }

    // ===================== HOME BUTTONS =====================

    public void OnContributorClicked()
    {
        UITapBlocker.NotifyUI();
        BlockArInputFor(0.6f);
        EnterState(UiState.ContributorChoice);
        SetMainPanels(false, true, false, false);
        SafeSet(backButton, true);
        DebugIndoorCache("OnContributorClicked");
    }

    public void OnExplorerClicked()
    {
        UITapBlocker.NotifyUI();
        BlockArInputFor(0.6f);
        EnterState(UiState.ExplorerChoice);
        SetMainPanels(false, false, true, false);
        SafeSet(backButton, true);
        DebugIndoorCache("OnExplorerClicked");
    }

    // ===================== CONTRIBUTOR CHOICE =====================

    public void OnIndoorContributorClicked()
    {
        UITapBlocker.NotifyUI();
        BlockArInputFor(0.6f);
        EnterState(UiState.ContributorIndoorMapList);
        SetMainPanels(false, false, false, true);
        SafeSet(backButton, true);

        EvaluationLogger.Log("EnterIndoorMode", mode: "Contributor", mapId: "", details: "OpenMapList");

        if (anchorPlacer != null)
            anchorPlacer.isIndoor = true;

        WarmupIndoorLocalization();
        DebugIndoorCache("OnIndoorContributorClicked");
    }

    public void OnOutdoorContributorClicked()
    {
        UITapBlocker.NotifyUI();
        BlockArInputFor(0.6f);
        RequestLocationPermission();

        EnterState(UiState.ContributorOutdoorActive);
        SetMainPanels(false, false, false, false);
        SafeSet(backButton, true);

        if (anchorPlacer != null)
        {
            anchorPlacer.isIndoor = false;
            anchorPlacer.currentMapId = "";
            anchorPlacer.enabled = false;
        }

        EnableEarthUI();
        ShowEarthWait(true);

        earthRoutine = StartCoroutine(EarthWait_MinSeconds_ThenUntilReady(() =>
        {
            if (currentState != UiState.ContributorOutdoorActive) return;

            ShowEarthWait(false);
            SetModeBannerContributor();

            if (anchorPlacer != null)
            {
                Debug.Log("[ModePanel] Earth ready → enabling AnchorPlacer (Contributor Outdoor)");
                anchorPlacer.enabled = true;
            }
        }));
    }

    // ===================== EXPLORER CHOICE =====================

    public void OnExplorerIndoorClicked()
    {
        UITapBlocker.NotifyUI();
        BlockArInputFor(0.6f);
        EnterState(UiState.ExplorerIndoorMapList);
        SetMainPanels(false, false, false, true);
        SafeSet(backButton, true);

        EvaluationLogger.Log("EnterIndoorMode", mode: "Explorer", mapId: "", details: "OpenMapList");

        WarmupIndoorLocalization();
        DebugIndoorCache("OnExplorerIndoorClicked");
    }

    public void OnExplorerOutdoorClicked()
    {
        UITapBlocker.NotifyUI();
        BlockArInputFor(0.6f);
        RequestLocationPermission();

        EnterState(UiState.ExplorerOutdoorActive);
        SetMainPanels(false, false, false, false);
        SafeSet(backButton, true);

        EnableEarthUI();
        ShowEarthWait(true);

        earthRoutine = StartCoroutine(EarthWait_MinSeconds_ThenUntilReady(() =>
        {
            if (currentState != UiState.ExplorerOutdoorActive) return;

            Debug.Log("[ModePanel] Earth ready → enabling Explorer + loading outdoor anchors");
            ShowEarthWait(false);
            SetModeBannerExplorer();
            
            if (anchorPlacer != null) anchorPlacer.enabled = false;
            if (explorer != null) explorer.enabled = true;

            if (outdoorEarthAnchorLoader != null)
            {
                outdoorEarthAnchorLoader.enabled = true;
                outdoorEarthAnchorLoader.ResetAndLoad(true);
            }
            else
            {
                Debug.LogWarning("[ModePanel] outdoorEarthAnchorLoader not assigned.");
            }
        }));
    }

    // ===================== MAP LIST =====================

    public void OnIndoorMapChosen_Cdis()
    {
        UITapBlocker.NotifyUI();
        BlockArInputFor(0.6f);
        StartIndoorFlowWithMap("cdis_lab");
    }

    private void StartIndoorFlowWithMap(string mapId)
    {
        bool isExplorer = (currentState == UiState.ExplorerIndoorMapList);

        bool mapChanged = (activeIndoorMapId != mapId);
        DebugIndoorCache("StartIndoorFlowWithMap (before)", mapId);

        EnterState(isExplorer ? UiState.ExplorerIndoorActive : UiState.ContributorIndoorActive);
        SetMainPanels(false, false, false, false);
        SafeSet(backButton, true);

        activeIndoorMapId = mapId;

        EvaluationLogger.Log(ev: "IndoorMapSelected", mode: isExplorer ? "Explorer" : "Contributor", mapId: mapId, details: "UserSelectedMap");

        if (indoorLocalizationFlag != null)
        {
            indoorLocalizationFlag.currentMapId = mapId;
            indoorLocalizationFlag.mode = isExplorer ? "Explorer" : "Contributor";
        }

        if (anchorPlacer != null)
        {
            anchorPlacer.isIndoor = true;
            anchorPlacer.currentMapId = mapId;
            anchorPlacer.enabled = false;
        }

        if (loader != null)
            loader.mapId = mapId;

        // IMPORTANT: Cache is only valid if flag is TRUE right now.
        bool flagLocalizedNow = (indoorLocalizationFlag != null && indoorLocalizationFlag.IsLocalized);

        if (!mapChanged && indoorSessionLocalized && flagLocalizedNow)
        {
            Debug.Log("[ModePanel] Already localized for this map and flag is TRUE → skip wait");

            EvaluationLogger.Log(ev: "IndoorLocalizationCached", mode: isExplorer ? "Explorer" : "Contributor", mapId: mapId, success: "success", details: "AlreadyLocalizedInSession");

            ProceedAfterIndoorLocalized(isExplorer);
            return;
        }

        ShowIndoorWait(true);

        EvaluationLogger.Log("IndoorLocalizationStart", mode: isExplorer ? "Explorer" : "Contributor", mapId: mapId, details: "ShowIndoorWaitPanel");

       

// Only force-reset if we actually need a fresh localization cycle
bool shouldReset = mapChanged || !flagLocalizedNow;

if (shouldReset)
{
    indoorSessionLocalized = false;
    if (indoorLocalizationFlag != null)
        indoorLocalizationFlag.ResetLocalized();
}
else
{
    // We are already localized; don't kill the session by resetting.
    indoorSessionLocalized = true;
}

indoorRoutine = StartCoroutine(WaitForIndoorLocalized_ThenProceed(isExplorer));
    }

    private IEnumerator WaitForIndoorLocalized_ThenProceed(bool isExplorer)
{
    if (indoorLocalizationFlag == null)
    {
        Debug.LogWarning("[ModePanel] indoorLocalizationFlag not assigned.");
        yield return new WaitForSeconds(minWaitSeconds);
        ShowIndoorWait(false);
        yield break;
    }

    float t0 = Time.time;
    while (Time.time - t0 < minWaitSeconds)
        yield return null;

    float timeoutSeconds = 20f;
    float waitStart = Time.time;

    while (!indoorLocalizationFlag.IsLocalized)
    {
        if (Time.time - waitStart > timeoutSeconds)
        {
            Debug.LogWarning("[ModePanel] Timed out waiting for indoor localization.");
            ShowModeBanner("Localization failed. Please rescan the area");
            yield break;
        }

        yield return null;
    }

    // allow mapSpace pose settle
    yield return new WaitForSeconds(0.75f);

    if (isExplorer && currentState != UiState.ExplorerIndoorActive) yield break;
    if (!isExplorer && currentState != UiState.ContributorIndoorActive) yield break;

    indoorSessionLocalized = true;
    ShowIndoorWait(false);

    ProceedAfterIndoorLocalized(isExplorer);
}

    private void ProceedAfterIndoorLocalized(bool isExplorer)
    {
        if (isExplorer)
        {
            if (anchorPlacer != null) anchorPlacer.enabled = false;
            if (explorer != null) explorer.enabled = true;
            SetModeBannerExplorer();
        }
        else
        {
            if (anchorPlacer != null) anchorPlacer.enabled = true;
            if (indoorReadyPanel != null)
            {
                if (indoorReadyRoutine != null) StopCoroutine(indoorReadyRoutine);
                indoorReadyRoutine = StartCoroutine(ShowIndoorReadyMessage());
            }
            SetModeBannerContributor();
        }

        if (loader != null && !string.IsNullOrEmpty(loader.mapId))
            loader.LoadAnchors();

        DebugIndoorCache("ProceedAfterIndoorLocalized");
    }

    private IEnumerator ShowIndoorReadyMessage()
    {
        if (indoorReadyPanel == null) yield break;
        indoorReadyPanel.SetActive(true);
        yield return new WaitForSeconds(indoorReadySeconds);
        indoorReadyPanel.SetActive(false);
        indoorReadyRoutine = null;
    }

    // ===================== BACK =====================

    public void OnBackClicked()
    {
        UITapBlocker.NotifyUI();
        BlockArInputFor(0.8f);
        // If InfoUI is open, close it and consume Back.
    if (infoUI != null && infoUI.IsOpen)
    {
        infoUI.ForceCloseNoDelete();
        return; // important: don't navigate state this click
    }
        Debug.Log("[ModePanel] Back clicked from " + currentState);

        switch (currentState)
        {
            case UiState.ContributorIndoorActive:
                GoToContributorIndoorMapList();
                break;

            case UiState.ContributorIndoorMapList:
            case UiState.ContributorOutdoorActive:
                GoToContributorChoice();
                break;

            case UiState.ContributorChoice:
                GoToWelcome();
                break;

            case UiState.ExplorerIndoorActive:
                GoToExplorerIndoorMapList();
                break;

            case UiState.ExplorerIndoorMapList:
            case UiState.ExplorerOutdoorActive:
                GoToExplorerChoice();
                break;

            case UiState.ExplorerChoice:
                GoToWelcome();
                break;

            default:
                GoToWelcome();
                break;
        }
    }

    private void GoToWelcome()
    {
        EnterState(UiState.Welcome);
        SetMainPanels(true, false, false, false);
        SafeSet(backButton, false);
    }

    private void GoToContributorChoice()
    {
        EnterState(UiState.ContributorChoice);
        SetMainPanels(false, true, false, false);
        SafeSet(backButton, true);
    }

    private void GoToExplorerChoice()
    {
        EnterState(UiState.ExplorerChoice);
        SetMainPanels(false, false, true, false);
        SafeSet(backButton, true);
    }

    private void GoToContributorIndoorMapList()
    {
        EnterState(UiState.ContributorIndoorMapList);
        SetMainPanels(false, false, false, true);
        SafeSet(backButton, true);
        WarmupIndoorLocalization();
    }

    private void GoToExplorerIndoorMapList()
    {
        EnterState(UiState.ExplorerIndoorMapList);
        SetMainPanels(false, false, false, true);
        SafeSet(backButton, true);
        WarmupIndoorLocalization();
    }

    private void SetState(UiState next)
    {
        currentState = next;
        if (verboseLogs) Debug.Log("[ModePanel] State → " + currentState);
    }
    // ---- State query helpers (used by AnchorTapExplorer / AnchorPlacer gates) ----
    public bool IsInExplorerIndoorActive()
    {
        return currentState == UiState.ExplorerIndoorActive;
    }

    public bool IsInContributorIndoorActive()
    {
        return currentState == UiState.ContributorIndoorActive;
    }

    // ===================== WAIT HELPERS =====================

    private IEnumerator EarthWait_MinSeconds_ThenUntilReady(System.Action onReady)
    {
        if (earthTrackingUI == null)
        {
            yield return new WaitForSeconds(minWaitSeconds);
            ShowEarthWait(false);
            yield break;
        }

        float t0 = Time.time;
        while (Time.time - t0 < minWaitSeconds)
            yield return null;

        while (!earthTrackingUI.IsReady)
            yield return null;

        onReady?.Invoke();
    }

    private void StopAllRoutines()
    {
        if (earthRoutine != null) { StopCoroutine(earthRoutine); earthRoutine = null; }
        if (indoorRoutine != null) { StopCoroutine(indoorRoutine); indoorRoutine = null; }

        if (indoorReadyRoutine != null)
        {
            StopCoroutine(indoorReadyRoutine);
            indoorReadyRoutine = null;
        }
    }

    private void HideAllWaitPanels()
    {
        ShowEarthWait(false);
        ShowIndoorWait(false);

        if (indoorReadyPanel != null)
            indoorReadyPanel.SetActive(false);
    }

    private void ShowEarthWait(bool show) => SafeSet(earthWaitPanel, show);
    private void ShowIndoorWait(bool show) => SafeSet(indoorWaitPanel, show);

    // ===================== EARTH STATUS UI =====================

    private void EnableEarthUI()
    {
        if (earthTrackingUI != null)
            earthTrackingUI.allowDisplay = true;
    }

    private void DisableEarthUI()
    {
        if (earthTrackingUI != null)
            earthTrackingUI.allowDisplay = false;
    }

    // ===================== UTIL =====================

    private void DisableAllBehaviours()
    {
        if (anchorPlacer != null) anchorPlacer.enabled = false;
        if (explorer != null) explorer.enabled = false;
        if (outdoorEarthAnchorLoader != null) outdoorEarthAnchorLoader.enabled = false;
    }

    private void SafeSet(GameObject go, bool on)
    {
        if (go != null) go.SetActive(on);
    }

    private void RequestLocationPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            Permission.RequestUserPermission(Permission.FineLocation);
#endif
    }

    public void ResetIndoorCache()
    {
        indoorSessionLocalized = false;
        activeIndoorMapId = "";
        if (indoorLocalizationFlag != null) indoorLocalizationFlag.ResetLocalized();
        DebugIndoorCache("ResetIndoorCache()");
    }
    private void ForceCloseInfoUI(string why)
{
    if (infoUI == null || !infoUI.IsOpen) return;

    Debug.Log($"[ModePanel] ForceCloseInfoUI: {why}");
    infoUI.ForceCloseNoDelete();
}
private Coroutine inputBlockRoutine;

private void BlockArInputFor(float seconds = 0.8f)
{
    LastModeSwitchTime = Time.time; // stronger debounce

    if (inputBlockRoutine != null) StopCoroutine(inputBlockRoutine);
    inputBlockRoutine = StartCoroutine(BlockArInputRoutine(seconds));
}

private IEnumerator BlockArInputRoutine(float seconds)
{
    // Close any open info panel immediately
    if (infoUI != null && infoUI.IsOpen)
        infoUI.ForceCloseNoDelete();

    // Hard-disable both systems so they can't read the same UI click
    if (anchorPlacer != null) anchorPlacer.enabled = false;
    if (explorer != null) explorer.enabled = false;

    // give 1 frame + some time for UI click to finish
    yield return null;
    yield return new WaitForSecondsRealtime(seconds);

    inputBlockRoutine = null;

    // IMPORTANT: do NOT re-enable here
    // Your state machine already enables the correct one in ProceedAfterIndoorLocalized / earth ready
}
public bool IsInContributorOutdoorActive()
{
    return currentState == UiState.ContributorOutdoorActive;
}
private void ShowModeBanner(string message)
{
    Debug.Log("[ModePanel] ShowModeBanner called: " + message);
    if (modeBannerText != null)
        modeBannerText.text = message;

    if (modeBanner != null)
        modeBanner.SetActive(true);
}

private void HideModeBanner()
{
    Debug.Log("[ModePanel] HideModeBanner called");
    if (modeBanner != null)
        modeBanner.SetActive(false);
}

private void SetModeBannerContributor()
{
    Debug.Log("[ModePanel] Showing Contributor banner");
    ShowModeBanner("Tap to place a pin");

    if (modeBannerImage != null)
        modeBannerImage.color = new Color(0f, 0f, 0f, 0.45f); // dark transparent
}

private void SetModeBannerExplorer()
{
    Debug.Log("[ModePanel] Showing Explorer banner");
    ShowModeBanner("Tap a pin to view details");

    if (modeBannerImage != null)
        modeBannerImage.color = new Color(0f, 0f, 0f, 0.45f); // same dark transparent
}
public bool IsInExplorerOutdoorActive()
{
    return currentState == UiState.ExplorerOutdoorActive;
}
}
