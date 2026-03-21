using UnityEngine;

public class CameraMenuDrift : MonoBehaviour
{
    private Vector3 startPos;
    public float driftAmount = 0.08f;
    public float driftSpeed = 0.4f;

    void Start() { startPos = transform.position; }

    void Update()
    {
        float x = Mathf.Sin(Time.time * driftSpeed) * driftAmount;
        float y = Mathf.Cos(Time.time * driftSpeed * 0.7f) * driftAmount * 0.5f;
        transform.position = startPos + new Vector3(x, y, 0);
    }
}
