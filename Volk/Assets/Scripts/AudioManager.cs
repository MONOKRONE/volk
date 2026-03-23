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

    [Header("Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 0.3f)] public float pitchVariation = 0.1f;

    private AudioSource sfxSource;
    private AudioSource pitchSource; // separate source for pitch-varied sounds
    private Coroutine roundStartCoroutine;

    void Awake()
    {
        Instance = this;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        pitchSource = gameObject.AddComponent<AudioSource>();
        pitchSource.playOnAwake = false;
        pitchSource.spatialBlend = 0f;
    }

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

        // Apply pitch variation for more natural feel
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
