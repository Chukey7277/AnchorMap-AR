using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpeechInputUI : MonoBehaviour, ISpeechToTextListener
{
    public TMP_InputField titleInput;
    public TMP_InputField descriptionInput;

    public TMP_Text listeningIndicator;

    public Button titleMicButton;
    public Button descriptionMicButton;

    public Image titleMicImage;
    public Image descriptionMicImage;

    public Color idleColor = Color.white;
    public Color listeningColor = Color.red;

    [Header("Optional: link AnchorInfoUI so recognized text is also stored in AnchorData")]
    public AnchorInfoUI anchorInfoUI;

    private TMP_InputField currentField;
    private Button currentButton;
    private Image currentMicImage;

    private bool isListening = false;
    private string baseTextBeforeSpeech = "";

    private void Start()
    {
        HideListeningUI();
        ResetMicVisuals();
    }

    public void SpeakTitle()
    {
        if (isListening || titleInput == null) return;

        currentField = titleInput;
        currentButton = titleMicButton;
        currentMicImage = titleMicImage;
        baseTextBeforeSpeech = titleInput.text ?? "";

        StartSpeech();
    }

    public void SpeakDescription()
    {
        if (isListening || descriptionInput == null) return;

        currentField = descriptionInput;
        currentButton = descriptionMicButton;
        currentMicImage = descriptionMicImage;
        baseTextBeforeSpeech = descriptionInput.text ?? "";

        StartSpeech();
    }

    private void StartSpeech()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        ShowListeningUI("Starting microphone...");
        SetMicListeningVisual(true);

        if (!SpeechToText.IsServiceAvailable())
        {
            ShowListeningUI("Speech service unavailable");
            CancelListeningState();
            return;
        }

        SpeechToText.RequestPermissionAsync((permission) =>
        {
            if (permission != SpeechToText.Permission.Granted)
            {
                ShowListeningUI("Microphone permission denied");
                CancelInvoke(nameof(HideListeningUI));
                Invoke(nameof(HideListeningUI), 1.5f);
                CancelListeningState();
                return;
            }

            bool started = SpeechToText.Start(this, preferOfflineRecognition: false);
            if (!started)
            {
                ShowListeningUI("Could not start listening");
                CancelInvoke(nameof(HideListeningUI));
                Invoke(nameof(HideListeningUI), 1.5f);
                CancelListeningState();
                return;
            }

            isListening = true;
            ShowListeningUI("Listening... Speak now");
        });
#endif
    }

    private void ShowListeningUI(string message)
    {
        if (listeningIndicator != null)
        {
            listeningIndicator.text = message;
            listeningIndicator.gameObject.SetActive(true);
        }
    }

    private void HideListeningUI()
    {
        if (listeningIndicator != null)
            listeningIndicator.gameObject.SetActive(false);
    }

    private void SetMicListeningVisual(bool listening)
    {
        if (currentMicImage != null)
            currentMicImage.color = listening ? listeningColor : idleColor;

        if (currentButton != null)
            currentButton.interactable = !listening;
    }

    private void ResetMicVisuals()
    {
        if (titleMicImage != null) titleMicImage.color = idleColor;
        if (descriptionMicImage != null) descriptionMicImage.color = idleColor;

        if (titleMicButton != null) titleMicButton.interactable = true;
        if (descriptionMicButton != null) descriptionMicButton.interactable = true;
    }

    private void CancelListeningState()
    {
        isListening = false;
        currentButton = null;
        currentMicImage = null;
        currentField = null;
        baseTextBeforeSpeech = "";
        ResetMicVisuals();
    }

    private void ApplyRecognizedText(string spokenText, bool isFinal)
    {
        if (currentField == null || string.IsNullOrWhiteSpace(spokenText))
            return;

        string newText;

        if (string.IsNullOrWhiteSpace(baseTextBeforeSpeech))
            newText = spokenText.Trim();
        else
            newText = baseTextBeforeSpeech.TrimEnd() + " " + spokenText.Trim();

        currentField.SetTextWithoutNotify(newText);

        currentField.Select();
        currentField.ActivateInputField();

        if (isFinal)
        {
            int pos = currentField.text.Length;
            currentField.caretPosition = pos;
            currentField.selectionAnchorPosition = pos;
            currentField.selectionFocusPosition = pos;
        }

        if (anchorInfoUI != null)
        {
            if (currentField == titleInput)
                anchorInfoUI.SetDraftTitleOnlyData(newText);
            else if (currentField == descriptionInput)
                anchorInfoUI.SetDraftDescriptionOnlyData(newText);
        }
    }

    void ISpeechToTextListener.OnReadyForSpeech()
    {
        ShowListeningUI("Listening... Speak now");
    }

    void ISpeechToTextListener.OnBeginningOfSpeech()
    {
        ShowListeningUI("Listening...");
    }

    void ISpeechToTextListener.OnVoiceLevelChanged(float normalizedVoiceLevel)
    {
    }

    void ISpeechToTextListener.OnPartialResultReceived(string spokenText)
    {
        ApplyRecognizedText(spokenText, false);
    }

    void ISpeechToTextListener.OnResultReceived(string spokenText, int? errorCode)
    {
        ApplyRecognizedText(spokenText, true);

        if (currentField != null)
            currentField.DeactivateInputField();

        HideListeningUI();
        CancelListeningState();
    }
}