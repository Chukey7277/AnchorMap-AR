using System.Collections;
using UnityEngine;

public class ModePanel : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Panel with welcome text + Contributor / Explorer buttons")]
    public GameObject welcomePanel;          // WelcomePanel

    [Tooltip("Back button shown while inside Contributor / Explorer modes")]
    public GameObject backButton;            // Small UI Button at top-left

    [Tooltip("Overlay shown in Explorer while localization / pins are loading")]
    public GameObject explorerWaitPanel;     // ExplorerWaitPanel

    [Header("Behaviour")]
    [Tooltip("Script used in Contributor mode for placing new pins")]
    public AnchorPlacer anchorPlacer;

    [Tooltip("Script used in Explorer mode for tapping existing pins")]
    public AnchorTapExplorer explorer;

    [Header("Data loading")]
    [Tooltip("Loader that spawns anchors from Firestore when entering Explorer mode.")]
    public MapAnchorLoader loader;

    [Header("Explorer wait settings")]
    [Tooltip("How long the Explorer wait overlay stays visible (seconds).")]
    public float explorerWaitSeconds = 5f;

    // Keep handle to the running wait coroutine (if any)
    private Coroutine waitRoutine;

    void Start()
    {
        if (welcomePanel != null)
            welcomePanel.SetActive(true);

        if (backButton != null)
            backButton.SetActive(false);

        if (explorerWaitPanel != null)
            explorerWaitPanel.SetActive(false);

        if (anchorPlacer != null) anchorPlacer.enabled = false;
        if (explorer     != null) explorer.enabled     = false;

        if (loader != null) loader.enabled = true;
    }

    // ---------- Contributor button ----------

    public void OnContributorClicked()
    {
        Debug.Log("[ModePanel] Contributor clicked");

        if (welcomePanel != null)
            welcomePanel.SetActive(false);

        if (backButton != null)
            backButton.SetActive(true);

        if (explorerWaitPanel != null)
            explorerWaitPanel.SetActive(false);

        // Stop any pending wait coroutine from Explorer
        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }

        if (anchorPlacer != null) anchorPlacer.enabled = true;
        if (explorer     != null) explorer.enabled     = false;
    }

    // ---------- Explorer button ----------

    public void OnExplorerClicked()
    {
        Debug.Log("[ModePanel] Explorer clicked");

        if (welcomePanel != null)
            welcomePanel.SetActive(false);

        if (backButton != null)
            backButton.SetActive(true);

        if (anchorPlacer != null) anchorPlacer.enabled = false;
        if (explorer     != null) explorer.enabled     = true;

        // Show "please wait" overlay
        if (explorerWaitPanel != null)
            explorerWaitPanel.SetActive(true);

        // Start loading anchors
        if (loader != null)
        {
            Debug.Log("[ModePanel] Explorer clicked – loading anchors from Firestore");
            loader.LoadAnchors();
        }
        else
        {
            Debug.LogWarning("[ModePanel] Explorer clicked but Loader is not assigned.");
        }

        // Hide the wait panel after a few seconds
        if (explorerWaitPanel != null)
        {
            if (waitRoutine != null)
                StopCoroutine(waitRoutine);

            waitRoutine = StartCoroutine(HideExplorerWaitAfterDelay(explorerWaitSeconds));
        }
    }

    private IEnumerator HideExplorerWaitAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (explorerWaitPanel != null)
            explorerWaitPanel.SetActive(false);

        waitRoutine = null;
    }

    // ---------- Back button ----------

    public void OnBackClicked()
    {
        Debug.Log("[ModePanel] Back clicked – returning to mode selection");

        if (anchorPlacer != null) anchorPlacer.enabled = false;
        if (explorer     != null) explorer.enabled     = false;

        if (welcomePanel != null)
            welcomePanel.SetActive(true);

        if (backButton != null)
            backButton.SetActive(false);

        if (explorerWaitPanel != null)
            explorerWaitPanel.SetActive(false);

        // Stop any wait coroutine
        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }
    }
}