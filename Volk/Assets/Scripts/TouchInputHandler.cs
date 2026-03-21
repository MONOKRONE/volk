using UnityEngine;
using UnityEngine.UI;

public class TouchInputHandler : MonoBehaviour
{
    [Header("Joystick UI")]
    public RectTransform joystickBackground;
    public RectTransform joystickKnob;
    public float joystickRadius = 60f;
    public float flickSpeedThreshold = 800f;

    public Vector2 MoveInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool CrouchTriggered { get; private set; }

    private int joystickFingerId = -1;
    private Vector2 joystickStartPos;
    private Vector2 prevTouchPos;
    private float touchStartTime;

    void Start()
    {
        if (joystickBackground) joystickBackground.gameObject.SetActive(false);
    }

    void Update()
    {
        JumpTriggered = false;
        CrouchTriggered = false;

        foreach (Touch touch in Input.touches)
        {
            // Only track touches on LEFT half of screen
            bool isLeftSide = touch.position.x < Screen.width * 0.5f;
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
