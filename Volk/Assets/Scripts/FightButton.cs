using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class FightButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public string buttonId; // "Punch", "Kick", "Parry", "SK1", "SK2"

    [Header("Timing")]
    public float holdThreshold = 0.3f;
    public float doubleTapWindow = 0.3f;
    public float slideDistance = 60f;

    // Events
    public event Action OnTap;
    public event Action OnHold;
    public event Action OnDoubleTap;
    public event Action<FightButton> OnSlideTo;

    private float pressTime;
    private float lastTapTime;
    private bool isPressed;
    private bool holdFired;
    private Vector2 pressStartPos;
    private bool slideFired;

    // For slide detection - find nearby buttons
    private static FightButton[] allButtons;

    void OnEnable()
    {
        allButtons = FindObjectsByType<FightButton>(FindObjectsSortMode.None);
    }

    void Update()
    {
        if (isPressed && !holdFired && Time.unscaledTime - pressTime > holdThreshold)
        {
            holdFired = true;
            OnHold?.Invoke();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        holdFired = false;
        slideFired = false;
        pressTime = Time.unscaledTime;
        pressStartPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (slideFired || !isPressed) return;

        Vector2 delta = eventData.position - pressStartPos;
        if (delta.magnitude > slideDistance)
        {
            // Find button under current position
            foreach (var btn in allButtons)
            {
                if (btn == this) continue;
                RectTransform rt = btn.GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(rt, eventData.position, eventData.pressEventCamera))
                {
                    slideFired = true;
                    OnSlideTo?.Invoke(btn);
                    break;
                }
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed) return;
        isPressed = false;

        if (slideFired || holdFired) return;

        float elapsed = Time.unscaledTime - pressTime;
        if (elapsed < holdThreshold)
        {
            // Check double tap
            if (Time.unscaledTime - lastTapTime < doubleTapWindow)
            {
                OnDoubleTap?.Invoke();
                lastTapTime = 0f;
            }
            else
            {
                OnTap?.Invoke();
                lastTapTime = Time.unscaledTime;
            }
        }
    }
}
