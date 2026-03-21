using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchInputHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick UI")]
    public RectTransform joystickBackground;
    public RectTransform joystickKnob;
    public float joystickRange = 50f;

    [Header("Flick Detection")]
    public float flickThreshold = 200f; // pixels per second

    // Output
    public Vector2 MoveInput { get; private set; }
    public bool FlickUp { get; private set; }
    public bool FlickDown { get; private set; }

    private int activePointerId = -1;
    private Vector2 touchStartPos;
    private Vector2 lastTouchPos;
    private float lastTouchTime;
    private bool isActive;
    private Canvas parentCanvas;

    void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (joystickBackground) joystickBackground.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        // Clear one-frame flags
        FlickUp = false;
        FlickDown = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isActive) return;

        activePointerId = eventData.pointerId;
        isActive = true;
        touchStartPos = eventData.position;
        lastTouchPos = eventData.position;
        lastTouchTime = Time.unscaledTime;

        // Show joystick at touch position
        if (joystickBackground && parentCanvas)
        {
            joystickBackground.gameObject.SetActive(true);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                eventData.position,
                parentCanvas.worldCamera,
                out Vector2 localPoint);
            joystickBackground.anchoredPosition = localPoint;
            joystickKnob.anchoredPosition = Vector2.zero;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;

        Vector2 delta = eventData.position - touchStartPos;
        Vector2 clamped = Vector2.ClampMagnitude(delta, joystickRange);
        MoveInput = clamped / joystickRange;

        if (joystickKnob)
            joystickKnob.anchoredPosition = clamped;

        // Track for flick detection
        lastTouchPos = eventData.position;
        lastTouchTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;

        // Flick detection
        float dt = Time.unscaledTime - lastTouchTime;
        if (dt > 0f && dt < 0.3f)
        {
            Vector2 velocity = (eventData.position - touchStartPos) / Mathf.Max(dt, 0.01f);
            if (velocity.y > flickThreshold) FlickUp = true;
            else if (velocity.y < -flickThreshold) FlickDown = true;
        }

        // Reset
        isActive = false;
        activePointerId = -1;
        MoveInput = Vector2.zero;

        if (joystickBackground) joystickBackground.gameObject.SetActive(false);
        if (joystickKnob) joystickKnob.anchoredPosition = Vector2.zero;
    }
}
