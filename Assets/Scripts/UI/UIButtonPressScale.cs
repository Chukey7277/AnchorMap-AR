using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public RectTransform target;       // set to the button root (or leave empty)
    public float pressedScale = 0.96f;  // 0.94–0.98 feels good
    public float speed = 18f;

    Vector3 _startScale;
    Vector3 _goalScale;

    void Awake()
    {
        if (target == null) target = transform as RectTransform;
        _startScale = target.localScale;
        _goalScale = _startScale;
    }

    void Update()
    {
        target.localScale = Vector3.Lerp(target.localScale, _goalScale, Time.unscaledDeltaTime * speed);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _goalScale = _startScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _goalScale = _startScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _goalScale = _startScale;
    }
}