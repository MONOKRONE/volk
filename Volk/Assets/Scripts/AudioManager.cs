using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Combat SFX")]
    public AudioClip[] punchSounds;
    public AudioClip[] kickSounds;
    public AudioClip[] hitReceiveSounds;
    public AudioClip[] blockSounds;
    public AudioClip bodyFallSound;
    public AudioClip crowdCheerSound;
    public AudioClip roundStartSound;

    [Header("Layered Hit SFX")]
    public AudioClip[] bassThuds;      // 80-150Hz low thud layer
    public AudioClip[] snapClips;      // 2-5kHz high snap layer
    public AudioClip[] whooshClips;    // Wind-up whoosh (plays before hit)
    public AudioClip[] whiffClips;     // Whiff / miss whoosh

    [Header("Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 0.3f)] public float pitchVariation = 0.1f;

    private AudioSource sfxSource;
    private AudioSource pitchSource;    // pitch-varied sounds
    private AudioSource bassSource;     // low-freq bass layer
    private AudioSource snapSource;     // high-freq snap layer
    private AudioSource whooshSource;   // whoosh / wind-up layer
    private Coroutine roundStartCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        sfxSource = CreateAudioSource();
        pitchSource = CreateAudioSource();
        bassSource = CreateAudioSource();
        snapSource = CreateAudioSource();
        whooshSource = CreateAudioSource();
    }

    AudioSource CreateAudioSource()
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        return src;
    }

    // --- Original API (unchanged) ---
    public void PlayPunch()  => PlayRandomWithPitch(punchSounds);
    public void PlayKick()   => PlayRandomWithPitch(kickSounds);
    public void PlayHit()    => PlayRandomWithPitch(hitReceiveSounds);
    public void PlayBlock()  => PlayRandomWithPitch(blockSounds);
    public void PlayFall()   => PlayOneShot(bodyFallSound);
    public void PlayCheer()  => PlayOneShot(crowdCheerSound);

    public void PlayRoundStart()
    {
        if (roundStartSound == null) return;
        if (roundStartCoroutine != null) StopCoroutine(roundStartCoroutine);
        roundStartCoroutine = StartCoroutine(PlayClipLimited(roundStartSound, 3f));
    }

    // --- Layered Hit System ---

    /// <summary>
    /// Layered hit: bass thud + snap + pre-whoosh. Call this from attack hit confirmation.
    /// </summary>
    public void PlayLayeredHit(bool isHeavy, bool isSkill)
    {
        float pitchMod = isSkill ? -0.3f : isHeavy ? -0.15f : 0f;
        float volumeMod = isSkill ? 1.2f : isHeavy ? 1.1f : 1f;

        // Bass layer (low thud)
        if (bassThuds != null && bassThuds.Length > 0)
        {
            var clip = bassThuds[Random.Range(0, bassThuds.Length)];
            if (clip != null)
            {
                bassSource.pitch = 1f + pitchMod;
                bassSource.PlayOneShot(clip, sfxVolume * volumeMod);
            }
        }

        // Snap layer (high-freq crack) — slight pitch down
        if (snapClips != null && snapClips.Length > 0)
        {
            var clip = snapClips[Random.Range(0, snapClips.Length)];
            if (clip != null)
            {
                snapSource.pitch = 1f + pitchMod - 0.3f;
                snapSource.PlayOneShot(clip, sfxVolume * 0.8f * volumeMod);
            }
        }
    }

    /// <summary>
    /// Play whoosh ~100ms before impact. Call from attack wind-up.
    /// </summary>
    public void PlayWhoosh()
    {
        if (whooshClips == null || whooshClips.Length == 0) return;
        var clip = whooshClips[Random.Range(0, whooshClips.Length)];
        if (clip != null)
        {
            whooshSource.pitch = 1f + Random.Range(-0.1f, 0.15f);
            whooshSource.PlayOneShot(clip, sfxVolume * 0.6f);
        }
    }

    /// <summary>
    /// Skill hit: heavier version with lower pitch.
    /// </summary>
    public void PlaySkillHit()
    {
        PlayLayeredHit(false, true);
    }

    /// <summary>
    /// KO sound: body fall with reverb tail, fade other combat sounds.
    /// </summary>
    public void PlayKnockout()
    {
        StartCoroutine(DoKnockoutAudio());
    }

    IEnumerator DoKnockoutAudio()
    {
        // Play fall with reverb-like tail (lower pitch = longer perceived reverb)
        if (bodyFallSound != null)
        {
            sfxSource.pitch = 0.85f;
            sfxSource.PlayOneShot(bodyFallSound, sfxVolume * 1.3f);
        }

        // Fade out other combat sources over 0.8s
        float fadeTime = 0.8f;
        float elapsed = 0f;
        float startBass = bassSource.volume;
        float startSnap = snapSource.volume;
        float startWhoosh = whooshSource.volume;
        float startPitch = pitchSource.volume;

        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - (elapsed / fadeTime);
            bassSource.volume = startBass * t;
            snapSource.volume = startSnap * t;
            whooshSource.volume = startWhoosh * t;
            pitchSource.volume = startPitch * t;
            yield return null;
        }

        // Restore volumes
        bassSource.volume = 1f;
        snapSource.volume = 1f;
        whooshSource.volume = 1f;
        pitchSource.volume = 1f;
    }

    /// <summary>
    /// Whiff sound: light whoosh on miss.
    /// </summary>
    public void PlayWhiff()
    {
        if (whiffClips != null && whiffClips.Length > 0)
        {
            var clip = whiffClips[Random.Range(0, whiffClips.Length)];
            if (clip != null)
            {
                whooshSource.pitch = 1f + Random.Range(-0.05f, 0.1f);
                whooshSource.PlayOneShot(clip, sfxVolume * 0.4f);
            }
        }
    }

    /// <summary>
    /// Pause hit sound layers briefly (for hitstop sync).
    /// Reverb tails continue since we only pause/resume, not stop.
    /// </summary>
    public void PauseHitSounds(float duration)
    {
        StartCoroutine(DoPauseHitSounds(duration));
    }

    IEnumerator DoPauseHitSounds(float duration)
    {
        bassSource.Pause();
        snapSource.Pause();
        yield return new WaitForSecondsRealtime(duration);
        bassSource.UnPause();
        snapSource.UnPause();
    }

    // --- Internal helpers ---

    IEnumerator PlayClipLimited(AudioClip clip, float maxDuration)
    {
        sfxSource.clip = clip;
        sfxSource.volume = sfxVolume;
        sfxSource.pitch = 1f;
        sfxSource.Play();
        yield return new WaitForSeconds(maxDuration);
        sfxSource.Stop();
        sfxSource.clip = null;
    }

    void PlayRandomWithPitch(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        var clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;
        pitchSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        pitchSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
