using UnityEngine;
using UnityEngine.Playables;

namespace Volk.Cinematic
{
    /// <summary>
    /// Controls PlayableDirector for the VOLK trailer cinematic.
    /// Auto-plays on scene start. Provides skip and replay API.
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    public class CinematicDirector : MonoBehaviour
    {
        [Header("Settings")]
        public bool autoPlay = true;
        public bool allowSkip = true;
        public string returnScene = "MainMenu";

        [Header("Fade")]
        public CanvasGroup fadeOverlay; // Optional fullscreen black overlay for fade in/out
        public float fadeInDuration = 1f;
        public float fadeOutDuration = 1.5f;

        private PlayableDirector director;
        private bool isPlaying;
        private bool hasFinished;

        void Awake()
        {
            director = GetComponent<PlayableDirector>();
            director.stopped += OnDirectorStopped;
        }

        void Start()
        {
            if (autoPlay)
            {
                Play();
            }
        }

        void Update()
        {
            // Skip on any input
            if (allowSkip && isPlaying && !hasFinished)
            {
                bool skipInput = Input.GetKeyDown(KeyCode.Escape) ||
                                 Input.GetKeyDown(KeyCode.Space) ||
                                 (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

                if (skipInput)
                {
                    Skip();
                }
            }

            // Fade in effect
            if (fadeOverlay != null && isPlaying)
            {
                float t = (float)(director.time / fadeInDuration);
                if (t < 1f)
                    fadeOverlay.alpha = 1f - t;
                else
                    fadeOverlay.alpha = 0f;
            }
        }

        public void Play()
        {
            if (director.playableAsset == null)
            {
                Debug.LogWarning("[Cinematic] No PlayableAsset assigned to PlayableDirector!");
                return;
            }

            isPlaying = true;
            hasFinished = false;
            director.time = 0;
            director.Play();
            Debug.Log("[Cinematic] Playing trailer");
        }

        public void Skip()
        {
            Debug.Log("[Cinematic] Skipped");
            director.Stop();
        }

        public void Replay()
        {
            hasFinished = false;
            Play();
        }

        void OnDirectorStopped(PlayableDirector pd)
        {
            isPlaying = false;
            hasFinished = true;
            Debug.Log("[Cinematic] Finished");

            // Fade out then load return scene
            if (fadeOverlay != null)
                StartCoroutine(FadeOutAndReturn());
            else
                ReturnToMenu();
        }

        System.Collections.IEnumerator FadeOutAndReturn()
        {
            float t = 0;
            while (t < fadeOutDuration)
            {
                t += Time.unscaledDeltaTime;
                if (fadeOverlay != null)
                    fadeOverlay.alpha = t / fadeOutDuration;
                yield return null;
            }
            ReturnToMenu();
        }

        void ReturnToMenu()
        {
            if (!string.IsNullOrEmpty(returnScene))
                UnityEngine.SceneManagement.SceneManager.LoadScene(returnScene);
        }

        void OnDestroy()
        {
            if (director != null)
                director.stopped -= OnDirectorStopped;
        }
    }
}
