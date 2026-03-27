using UnityEngine;
using System.Collections;

public class HitstopManager : MonoBehaviour
{
    public static HitstopManager Instance;

    // Hitstop durations (in seconds)
    public const float LightHit = 8f / 60f;   // 0.133s
    public const float HeavyHit = 12f / 60f;  // 0.200s
    public const float SkillHit = 16f / 60f;  // 0.267s

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private bool isHitstopActive;

    public void Trigger(float duration)
    {
        if (isHitstopActive) return;
        StopAllCoroutines();
        StartCoroutine(DoHitstop(duration));
    }

    IEnumerator DoHitstop(float duration)
    {
        isHitstopActive = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        isHitstopActive = false;
    }
}
