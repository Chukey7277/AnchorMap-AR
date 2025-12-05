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

    private Coroutine waitRoutine;

    void Start()
    {
        if (welcomePanel != null)
            welcomePanel.SetActive(true);

        if (backButton != null)
            backButton.SetActive(false);

        if (explorerWaitPanel != null)
            explorerWaitPanel.SetActive(false);

        if (anchorPlacer != null)
        {
            anchorPlacer.enabled = false;
            Debug.Log("[ModePanel] Start: anchorPlacer disabled");
        }
        else
        {
            Debug.LogWarning("[ModePanel] Start: anchorPlacer NOT ASSIGNED");
        }

        if (explorer != null)
        {
            explorer.enabled = false;
            Debug.Log("[ModePanel] Start: explorer disabled");
        }
        else
        {
            Debug.LogWarning("[ModePanel] Start: explorer NOT ASSIGNED");
        }

        if (loader != null)
            loader.enabled = true;
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

        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }

        if (anchorPlacer != null)
        {
            anchorPlacer.enabled = true;
            Debug.Log("[ModePanel] Contributor: anchorPlacer.enabled = true");
        }
        else
        {
            Debug.LogError("[ModePanel] Contributor: anchorPlacer is NULL");
        }

        if (explorer != null)
        {
            explorer.enabled = false;
            Debug.Log("[ModePanel] Contributor: explorer.enabled = false");
        }
    }

    // ---------- Explorer button ----------

    public void OnExplorerClicked()
    {
        Debug.Log("[ModePanel] Explorer clicked");

        if (welcomePanel != null)
            welcomePanel.SetActive(false);

        if (backButton != null)
            backButton.SetActive(true);

        if (anchorPlacer != null)
        {
            anchorPlacer.enabled = false;
            Debug.Log("[ModePanel] Explorer: anchorPlacer.enabled = false");
        }

        if (explorer != null)
        {
            explorer.enabled = true;
            Debug.Log("[ModePanel] Explorer: explorer.enabled = true");
        }
        else
        {
            Debug.LogError("[ModePanel] Explorer: explorer is NULL");
        }

        if (explorerWaitPanel != null)
            explorerWaitPanel.SetActive(true);

        if (loader != null)
        {
            Debug.Log("[ModePanel] Explorer: calling loader.LoadAnchors()");
            loader.LoadAnchors();
        }
        else
        {
            Debug.LogWarning("[ModePanel] Explorer clicked but Loader is not assigned.");
        }

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

        if (anchorPlacer != null)
        {
            anchorPlacer.enabled = false;
            Debug.Log("[ModePanel] Back: anchorPlacer.enabled = false");
        }

        if (explorer != null)
        {
            explorer.enabled = false;
            Debug.Log("[ModePanel] Back: explorer.enabled = false");
        }

        if (welcomePanel != null)
            welcomePanel.SetActive(true);

        if (backButton != null)
            backButton.SetActive(false);

        if (explorerWaitPanel != null)
            explorerWaitPanel.SetActive(false);

        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }
    }
}