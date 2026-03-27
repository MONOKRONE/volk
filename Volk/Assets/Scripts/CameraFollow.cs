using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public Fighter playerFighter;

    [Header("Position Settings")]
    public float height = 2.5f;
    public float distance = 5.5f;
    public float followSpeed = 8f;

    [Header("Look Settings")]
    public float lookAheadY = 1.0f;

    [Header("Free Look Settings")]
    public float freeLookSensitivity = 2.0f;
    public float freeLookMinY = -20f;
    public float freeLookMaxY = 40f;

    private Vector3 currentVelocity;
    private float freeLookYaw;
    private float freeLookPitch = 10f;
    private int freeLookFingerId = -1;

    void LateUpdate()
    {
        if (player == null) return;

        bool locked = playerFighter == null || playerFighter.lockOnEnabled;

        if (locked)
        {
            freeLookYaw = 0f;
            Vector3 desiredPos = player.position
                - player.forward * distance
                + Vector3.up * height;

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos, ref currentVelocity, 1f / followSpeed);

            Vector3 lookTarget = player.position + Vector3.up * lookAheadY;
            transform.LookAt(lookTarget);
        }
        else
        {
            HandleFreeLookInput();

            Quaternion rotation = Quaternion.Euler(freeLookPitch, freeLookYaw, 0f);
            Vector3 desiredPos = player.position
                + rotation * new Vector3(0, height, -distance);

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos, ref currentVelocity, 1f / followSpeed);

            Vector3 lookTarget = player.position + Vector3.up * lookAheadY;
            transform.LookAt(lookTarget);
        }
    }

    public void TriggerShake(float intensity = 0.1f, float duration = 0.15f)
    {
        StartCoroutine(DoShake(intensity, duration));
    }

    IEnumerator DoShake(float intensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float decay = 1f - (elapsed / duration);
            float x = Random.Range(-intensity, intensity) * decay;
            float y = Random.Range(-intensity, intensity) * decay;
            transform.localPosition += new Vector3(x, y, 0f);
            yield return null;
        }
    }

    void HandleFreeLookInput()
    {
        foreach (Touch touch in Input.touches)
        {
            bool isRightSide = touch.position.x > Screen.width * 0.5f;
            if (!isRightSide) continue;

            if (touch.phase == TouchPhase.Began && freeLookFingerId == -1)
            {
                freeLookFingerId = touch.fingerId;
            }
            else if (touch.fingerId == freeLookFingerId)
            {
                if (touch.phase == TouchPhase.Moved)
                {
                    freeLookYaw += touch.deltaPosition.x * freeLookSensitivity * Time.deltaTime * 10f;
                    freeLookPitch -= touch.deltaPosition.y * freeLookSensitivity * Time.deltaTime * 10f;
                    freeLookPitch = Mathf.Clamp(freeLookPitch, freeLookMinY, freeLookMaxY);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    freeLookFingerId = -1;
                }
            }
        }
    }
}
