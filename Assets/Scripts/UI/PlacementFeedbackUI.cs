using System.Collections;
using UnityEngine;
using TMPro;

public class PlacementFeedbackUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public TMP_Text messageText;

    [Header("Timing")]
    public float showSeconds = 2.0f;

    private Coroutine hideRoutine;

    void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        if (root != null)
            root.SetActive(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(showSeconds);

        if (root != null)
            root.SetActive(false);

        hideRoutine = null;
    }

    public void HideNow()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        if (root != null)
            root.SetActive(false);
    }
}