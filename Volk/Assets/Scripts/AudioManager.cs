using UnityEngine;

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

    void Awake()
    {
        Instance = this;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    public void PlayPunch()      => PlayRandom(punchSounds);
    public void PlayKick()       => PlayRandom(kickSounds);
    public void PlayHit()        => PlayRandom(hitReceiveSounds);
    public void PlayFall()       => PlayOneShot(bodyFallSound);
    public void PlayCheer()      => PlayOneShot(crowdCheerSound);
    public void PlayRoundStart() => PlayOneShot(roundStartSound);

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
