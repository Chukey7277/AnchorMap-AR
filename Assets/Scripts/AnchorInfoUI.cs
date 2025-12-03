using UnityEngine;
using TMPro;

public class AnchorInfoUI : MonoBehaviour
{
    [Header("Panel root")]
    public GameObject panel;

    [Header("Contributor: editable fields")]
    public TMP_InputField titleInput;
    public TMP_InputField descriptionInput;
    public GameObject titleLabel;          // "Title" label
    public GameObject descriptionLabel;    // "Description" label
    public GameObject saveButton;          // SaveButton

    [Header("Explorer: read-only text")]
    public TMP_Text explorerTitleText;         // plain title text
    public TMP_Text explorerDescriptionText;   // plain description text

    [Header("Shared Cancel/Close button")]
    public GameObject cancelButton;        // CancelButton
    public TMP_Text cancelButtonLabel;     // text inside CancelButton

    private AnchorData currentTarget;
    private bool deleteOnCancel = false;

    private enum InfoMode { Contributor, Explorer }
    private InfoMode mode = InfoMode.Contributor;

    // Exposed so other scripts can know if UI is open
    public bool IsOpen => panel != null && panel.activeSelf;

    // Time when the panel was last closed (Save or Cancel)
    private static float lastCloseTime = -999f;
    public static float LastCloseTime => lastCloseTime;

    void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        // Start with explorer text hidden (we mainly use contributor first)
        SetExplorerVisible(false);
    }

    // ---------- Helpers ----------

    void PopulateFromCurrent()
    {
        if (currentTarget == null) return;

        // Contributor fields
        if (titleInput != null)
            titleInput.text = currentTarget.title;

        if (descriptionInput != null)
            descriptionInput.text = currentTarget.description;

        // Explorer text
        if (explorerTitleText != null)
            explorerTitleText.text = currentTarget.title;

        if (explorerDescriptionText != null)
            explorerDescriptionText.text = currentTarget.description;
    }

    void SetContributorVisible(bool visible)
    {
        if (titleLabel        != null) titleLabel.SetActive(visible);
        if (descriptionLabel  != null) descriptionLabel.SetActive(visible);
        if (titleInput        != null) titleInput.gameObject.SetActive(visible);
        if (descriptionInput  != null) descriptionInput.gameObject.SetActive(visible);
        if (saveButton        != null) saveButton.SetActive(visible);
    }

    void SetExplorerVisible(bool visible)
    {
        if (explorerTitleText       != null) explorerTitleText.gameObject.SetActive(visible);
        if (explorerDescriptionText != null) explorerDescriptionText.gameObject.SetActive(visible);
    }

    void ShowPanel()
    {
        if (panel != null)
            panel.SetActive(true);

        if (mode == InfoMode.Contributor && titleInput != null)
            titleInput.ActivateInputField();
    }

    bool IsReadOnly() => mode == InfoMode.Explorer;

    // ---------- PUBLIC API ----------

    // Called from AnchorPlacer (Contributor mode)
    public void OpenContributor(AnchorData target, bool deleteOnCancelFlag)
    {
        mode = InfoMode.Contributor;
        currentTarget = target;
        deleteOnCancel = deleteOnCancelFlag;

        // Show contributor widgets, hide explorer ones
        SetContributorVisible(true);
        SetExplorerVisible(false);

        // Editable inputs
        if (titleInput != null)       titleInput.interactable = true;
        if (descriptionInput != null) descriptionInput.interactable = true;

        if (cancelButtonLabel != null) cancelButtonLabel.text = "Cancel";

        PopulateFromCurrent();
        ShowPanel();
    }

    // Called from AnchorTapExplorer (Explorer mode)
    public void OpenExplorer(AnchorData target)
    {
        mode = InfoMode.Explorer;
        currentTarget = target;
        deleteOnCancel = false;   // never delete pins in explorer

        // Hide contributor widgets, show plain text
        SetContributorVisible(false);
        SetExplorerVisible(true);

        if (cancelButtonLabel != null) cancelButtonLabel.text = "Close";

        PopulateFromCurrent();
        ShowPanel();
    }

    // ---------- BUTTON HANDLERS ----------

    public void SaveAndClose()
    {
        if (mode == InfoMode.Explorer)
        {
            // Should not happen, but be safe
            CloseOnly();
            return;
        }

        if (currentTarget != null)
        {
            if (titleInput != null)
                currentTarget.title = titleInput.text;

            if (descriptionInput != null)
                currentTarget.description = descriptionInput.text;

            Debug.Log($"[AnchorInfoUI] Saved: {currentTarget.title} / {currentTarget.description}");

            if (FirestoreAnchorService.Instance != null)
            {
                FirestoreAnchorService.Instance.SaveAnchor(currentTarget);
            }
            else
            {
                Debug.LogWarning("[AnchorInfoUI] FirestoreAnchorService.Instance is null; not saving to cloud.");
            }
        }

        // Once saved, cancel should no longer delete the pin
        deleteOnCancel = false;
        CloseOnly();
    }

    public void Cancel()
    {
        Debug.Log("[AnchorInfoUI] Cancel/Close clicked, mode=" + mode);

        if (mode == InfoMode.Contributor && deleteOnCancel && currentTarget != null)
        {
            Debug.Log("[AnchorInfoUI] Destroying unsaved pin (Contributor).");
            Destroy(currentTarget.gameObject);
        }

        CloseOnly();
    }

    void CloseOnly()
    {
        if (panel != null)
            panel.SetActive(false);

        currentTarget = null;
        deleteOnCancel = false;
        lastCloseTime = Time.time;
    }
}