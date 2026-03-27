using UnityEngine;
using UnityEngine.UI;

public class TouchInputHandler : MonoBehaviour
{
    [Header("Joystick UI")]
    public RectTransform joystickBackground;
    public RectTransform joystickKnob;
    public float joystickRadius = 60f;
    public float flickSpeedThreshold = 800f;

    // PLA-125: Swipe gesture thresholds (right side of screen)
    [Header("Swipe Gestures (Right Side)")]
    public float swipeSpeedThreshold = 300f; // px/s

    public Vector2 MoveInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool CrouchTriggered { get; private set; }
    public bool SwipeUpTriggered { get; private set; }   // PLA-125: Skill1
    public bool SwipeDownTriggered { get; private set; } // PLA-125: Dodge/Block

    private int joystickFingerId = -1;
    private Vector2 joystickStartPos;
    private Vector2 prevTouchPos;
    private float touchStartTime;

    // PLA-125: Right-side swipe tracking
    private int swipeFingerId = -1;
    private Vector2 swipeStartPos;
    private float swipeStartTime;

    void Start()
    {
        if (joystickBackground) joystickBackground.gameObject.SetActive(false);
    }

    void Update()
    {
        JumpTriggered = false;
        CrouchTriggered = false;
        SwipeUpTriggered = false;
        SwipeDownTriggered = false;

        foreach (Touch touch in Input.touches)
        {
            bool isLeftSide = touch.position.x < Screen.width * 0.5f;
            bool isRightSide = touch.position.x >= Screen.width * 0.5f;

            // PLA-125: Track right-side swipes for Skill1 / Dodge
            if (isRightSide && touch.phase == TouchPhase.Began && swipeFingerId == -1)
            {
                swipeFingerId = touch.fingerId;
                swipeStartPos = touch.position;
                swipeStartTime = Time.time;
            }
            else if (touch.fingerId == swipeFingerId &&
                     (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
            {
                float duration = Time.time - swipeStartTime;
                if (duration > 0.01f)
                {
                    Vector2 delta = touch.position - swipeStartPos;
                    float speed = delta.magnitude / duration;

                    if (speed > swipeSpeedThreshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * 1.2f)
                    {
                        if (delta.y > 0) SwipeUpTriggered = true;   // Skill1
                        else SwipeDownTriggered = true;              // Dodge/Block
                    }
                }
                swipeFingerId = -1;
            }

            // Only track LEFT side touches for joystick
            if (!isLeftSide && joystickFingerId != touch.fingerId) continue;

            if (touch.phase == TouchPhase.Began && isLeftSide && joystickFingerId == -1)
            {
                joystickFingerId = touch.fingerId;
                joystickStartPos = touch.position;
                prevTouchPos = touch.position;
                touchStartTime = Time.time;

                // Show joystick at touch position
                if (joystickBackground)
                {
                    joystickBackground.gameObject.SetActive(true);
                    joystickBackground.position = touch.position;
                    joystickKnob.position = touch.position;
                }
            }
            else if (touch.fingerId == joystickFingerId)
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    Vector2 delta = touch.position - joystickStartPos;
                    Vector2 clamped = Vector2.ClampMagnitude(delta, joystickRadius);
                    Vector2 raw = clamped / joystickRadius;
                    MoveInput = raw.magnitude > 0.15f ? raw : Vector2.zero;

                    if (joystickKnob)
                        joystickKnob.position = joystickStartPos + clamped;
                    prevTouchPos = touch.position;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    MoveInput = Vector2.zero;

                    // Flick detection
                    float touchDuration = Time.time - touchStartTime;
                    Vector2 totalDelta = touch.position - joystickStartPos;
                    float speed = totalDelta.magnitude / touchDuration;

                    if (speed > flickSpeedThreshold)
                    {
                        if (totalDelta.y > Mathf.Abs(totalDelta.x) * 1.5f)
                            JumpTriggered = true;
                        else if (-totalDelta.y > Mathf.Abs(totalDelta.x) * 1.5f)
                            CrouchTriggered = true;
                    }

                    joystickFingerId = -1;
                    MoveInput = Vector2.zero;
                    if (joystickBackground) joystickBackground.gameObject.SetActive(false);
                }
            }
        }
    }
}
