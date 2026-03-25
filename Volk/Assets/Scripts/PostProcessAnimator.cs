using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// Animates URP post-processing effects at runtime (KO spike, etc).
/// Attach to the same GameObject as the global Volume or reference it.
/// </summary>
public class PostProcessAnimator : MonoBehaviour
{
    public static PostProcessAnimator Instance;

    [Header("References")]
    public Volume globalVolume;

    private ColorAdjustments colorAdj;
    private ChromaticAberration chromatic;
    private float baseSaturation;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (globalVolume == null)
            globalVolume = GetComponent<Volume>();

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out colorAdj);
            globalVolume.profile.TryGet(out chromatic);

            if (colorAdj != null)
                baseSaturation = colorAdj.saturation.value;

            // Ensure chromatic aberration exists and is off by default
            if (chromatic != null)
            {
                chromatic.intensity.overrideState = true;
                chromatic.intensity.value = 0f;
            }
        }
    }

    /// <summary>
    /// KO hit: saturation spike +40 and chromatic aberration 0.05 pulse over 0.5s.
    /// </summary>
    public void KOPulse()
    {
        StartCoroutine(DoKOPulse());
    }

    IEnumerator DoKOPulse()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        float satSpike = 40f;
        float chromaSpike = 0.05f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float curve = 1f - t; // Linear decay from 1 to 0

            if (colorAdj != null)
            {
                colorAdj.saturation.overrideState = true;
                colorAdj.saturation.value = baseSaturation + satSpike * curve;
            }

            if (chromatic != null)
            {
                chromatic.intensity.value = chromaSpike * curve;
            }

            yield return null;
        }

        // Restore
        if (colorAdj != null)
            colorAdj.saturation.value = baseSaturation;
        if (chromatic != null)
            chromatic.intensity.value = 0f;
    }
}
