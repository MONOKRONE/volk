using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Position Settings")]
    public float height = 2.5f;
    public float distance = 5.5f;
    public float followSpeed = 8f;

    [Header("Look Settings")]
    public float lookAheadY = 1.0f;

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 desiredPos = player.position
            - player.forward * distance
            + Vector3.up * height;

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos, ref currentVelocity, 1f / followSpeed);

        Vector3 lookTarget = player.position + Vector3.up * lookAheadY;
        transform.LookAt(lookTarget);
    }
}
