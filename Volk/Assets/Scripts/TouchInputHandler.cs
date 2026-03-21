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
    public float flickThreshold = 200f;

    // Output
    public Vector2 MoveInput { get; private set; }
    public bool FlickUp { get; private set; }
    public bool FlickDown { get; private set; }

    private int activePointerId = -1;
    private Vector2 touchOrigin;
    private Vector2 lastPos;
    private float touchStartTime;
    private bool isActive;
    private RectTransform canvasRect;

    void Awake()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.transform as RectTransform;
        if (joystickBackground)
            joystickBackground.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        FlickUp = false;
        FlickDown = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isActive) return;

        activePointerId = eventData.pointerId;
        isActive = true;
        touchOrigin = eventData.position;
        lastPos = eventData.position;
        touchStartTime = Time.unscaledTime;

        if (joystickBackground && canvasRect)
        {
            joystickBackground.gameObject.SetActive(true);
            // For Screen Space Overlay, camera is null
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, null, out Vector2 localPoint);
            joystickBackground.anchoredPosition = localPoint;
            if (joystickKnob)
                joystickKnob.anchoredPosition = Vector2.zero;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;

        Vector2 delta = eventData.position - touchOrigin;
        Vector2 clamped = Vector2.ClampMagnitude(delta, joystickRange);
        MoveInput = clamped / joystickRange;

        if (joystickKnob)
            joystickKnob.anchoredPosition = clamped;

        lastPos = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;

        // Flick detection based on total gesture
        float totalTime = Time.unscaledTime - touchStartTime;
        if (totalTime > 0f && totalTime < 0.25f)
        {
            Vector2 totalDelta = eventData.position - touchOrigin;
            float speed = totalDelta.magnitude / totalTime;
            if (speed > flickThreshold)
            {
                if (totalDelta.y > 0 && Mathf.Abs(totalDelta.y) > Mathf.Abs(totalDelta.x))
                    FlickUp = true;
                else if (totalDelta.y < 0 && Mathf.Abs(totalDelta.y) > Mathf.Abs(totalDelta.x))
                    FlickDown = true;
            }
        }

        isActive = false;
        activePointerId = -1;
        MoveInput = Vector2.zero;

        if (joystickBackground)
            joystickBackground.gameObject.SetActive(false);
        if (joystickKnob)
            joystickKnob.anchoredPosition = Vector2.zero;
    }
}
