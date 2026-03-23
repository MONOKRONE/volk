using UnityEngine;

namespace Volk.UI
{
    public class UIAudio : MonoBehaviour
    {
        public static UIAudio Instance { get; private set; }

        [Header("Clips")]
        public AudioClip clickSound;
        public AudioClip swooshSound;
        public AudioClip coinSound;
        public AudioClip levelUpSound;
        public AudioClip errorSound;

        private AudioSource source;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0.5f;
        }

        public void PlayClick()
        {
            if (clickSound != null) source.PlayOneShot(clickSound, 0.6f);
        }

        public void PlaySwoosh()
        {
            if (swooshSound != null) source.PlayOneShot(swooshSound, 0.4f);
        }

        public void PlayCoin()
        {
            if (coinSound != null) source.PlayOneShot(coinSound, 0.7f);
        }

        public void PlayLevelUp()
        {
            if (levelUpSound != null) source.PlayOneShot(levelUpSound, 0.8f);
        }

        public void PlayError()
        {
            if (errorSound != null) source.PlayOneShot(errorSound, 0.5f);
        }
    }
}
