using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TMPInputCaretDebug : MonoBehaviour, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    public TMP_InputField input;

    void Reset()
    {
        input = GetComponent<TMP_InputField>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        LogState("OnPointerClick");
    }

    public void OnSelect(BaseEventData eventData)
    {
        LogState("OnSelect");
    }

    public void OnDeselect(BaseEventData eventData)
    {
        LogState("OnDeselect");
    }

    void Update()
    {
        if (input != null && input.isFocused)
        {
            if (Input.touchCount > 0 || Input.GetMouseButtonDown(0) || Input.anyKeyDown)
                LogState("UpdateFocused");
        }
    }

    private void LogState(string tag)
    {
        if (input == null) return;

        Debug.Log(
            $"[TMPInputCaretDebug][{tag}] name={input.name} " +
            $"focused={input.isFocused} " +
            $"textLen={(input.text != null ? input.text.Length : 0)} " +
            $"caret={input.caretPosition} " +
            $"anchor={input.selectionAnchorPosition} " +
            $"focus={input.selectionFocusPosition}"
        );
    }
}