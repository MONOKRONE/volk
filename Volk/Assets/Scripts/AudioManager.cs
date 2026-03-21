using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Combat SFX")]
    public AudioClip[] punchSounds;
    public AudioClip[] kickSounds;
    public AudioClip[] hitReceiveSounds;
    public AudioClip bodyFallSound;
    public AudioClip crowdCheerSound;
    public AudioClip roundStartSound;

    [Header("Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private AudioSource sfxSource;
    private Coroutine roundStartCoroutine;

    void Awake()
    {
        Instance = this;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    public void PlayPunch()  => PlayRandom(punchSounds);
    public void PlayKick()   => PlayRandom(kickSounds);
    public void PlayHit()    => PlayRandom(hitReceiveSounds);
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
        sfxSource.Play();
        yield return new WaitForSeconds(maxDuration);
        sfxSource.Stop();
        sfxSource.clip = null;
    }

    void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }

    void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
