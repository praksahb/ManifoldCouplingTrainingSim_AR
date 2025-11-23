using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;

public class UIHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("Invoke every frame while the button is held.")]
    public UnityEvent onHold;      // assign via inspector or code
    [Tooltip("Invoke once when released.")]
    public UnityEvent onRelease;

    // C# action subscription if you prefer code wiring
    public Action holdAction;
    public Action releaseAction;

    bool isHeld = false;

    void Update()
    {
        if (isHeld)
        {
            onHold?.Invoke();
            holdAction?.Invoke();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHeld = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHeld = false;
        onRelease?.Invoke();
        releaseAction?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // treat exit as release
        if (isHeld)
        {
            isHeld = false;
            onRelease?.Invoke();
            releaseAction?.Invoke();
        }
    }
}
