using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections;

public class JuiceManager : MonoBehaviour
{
    public static JuiceManager Instance;

    [Header("Screen Flash")]
    public Image flashOverlay; // Assign a fullscreen white Image (alpha=0) on a Canvas overlay

    [Header("Slow Motion")]
    public AudioMixer masterMixer; // Optional: set pitch parameter "MasterPitch"

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

    // --- 1. SCREEN FLASH (KO) ---
    public void ScreenFlash(float alpha = 0.4f)
    {
        if (flashOverlay == null) return;
        StartCoroutine(DoScreenFlash(alpha));
    }

    IEnumerator DoScreenFlash(float alpha)
    {
        Color c = flashOverlay.color;
        c.a = alpha;
        flashOverlay.color = c;
        yield return new WaitForSecondsRealtime(3f / 60f); // 3 frames at 60fps
        c.a = 0f;
        flashOverlay.color = c;
    }

    // --- 2. CHARACTER SHAKE (on hit — not camera) ---
    public void CharacterShake(Transform target, float amplitude = 0.05f, int frames = 8)
    {
        if (target == null) return;
        StartCoroutine(DoCharacterShake(target, amplitude, frames));
    }

    IEnumerator DoCharacterShake(Transform target, float amplitude, int frames)
    {
        Vector3 originalLocal = target.localPosition;
        float frameDuration = 1f / 60f;

        for (int i = 0; i < frames; i++)
        {
            float decay = 1f - ((float)i / frames); // exponential-ish decay
            decay *= decay;
            float offset = Mathf.Sin(Time.unscaledTime * 80f) * amplitude * decay;
            target.localPosition = originalLocal + new Vector3(offset, 0f, 0f);
            yield return new WaitForSecondsRealtime(frameDuration);
        }

        target.localPosition = originalLocal;
    }

    // --- 3. SLOW MOTION (KO) ---
    public void SlowMotionKO(float timeScale = 0.2f, float durationRealtime = 1f)
    {
        StartCoroutine(DoSlowMotion(timeScale, durationRealtime));
    }

    IEnumerator DoSlowMotion(float timeScale, float duration)
    {
        Time.timeScale = timeScale;
        Time.fixedDeltaTime = 0.02f * timeScale;

        if (masterMixer != null)
            masterMixer.SetFloat("MasterPitch", timeScale);

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (masterMixer != null)
            masterMixer.SetFloat("MasterPitch", 1f);
    }
}
