using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Volk.UI
{
    public class VScreenTransition : MonoBehaviour
    {
        public static VScreenTransition Instance { get; private set; }

        public CanvasGroup fadeOverlay;
        public Image slidePanel;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = 1f;
                StartCoroutine(FadeIn());
            }
        }

        public IEnumerator FadeIn()
        {
            if (fadeOverlay == null) yield break;
            fadeOverlay.blocksRaycasts = true;
            float t = 0;
            while (t < VTheme.FadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                fadeOverlay.alpha = 1f - (t / VTheme.FadeInDuration);
                yield return null;
            }
            fadeOverlay.alpha = 0;
            fadeOverlay.blocksRaycasts = false;
        }

        public IEnumerator FadeOut()
        {
            if (fadeOverlay == null) yield break;
            fadeOverlay.blocksRaycasts = true;
            float t = 0;
            while (t < VTheme.ScreenTransitionDuration)
            {
                t += Time.unscaledDeltaTime;
                fadeOverlay.alpha = t / VTheme.ScreenTransitionDuration;
                yield return null;
            }
            fadeOverlay.alpha = 1;
            UIAudio.Instance?.PlaySwoosh();
        }

        public IEnumerator SlideIn(RectTransform target, Vector2 from)
        {
            if (target == null) yield break;
            Vector2 original = target.anchoredPosition;
            target.anchoredPosition = from;
            float t = 0;
            while (t < VTheme.ScreenTransitionDuration)
            {
                t += Time.unscaledDeltaTime;
                float ease = 1f - Mathf.Pow(1f - t / VTheme.ScreenTransitionDuration, 3f);
                target.anchoredPosition = Vector2.Lerp(from, original, ease);
                yield return null;
            }
            target.anchoredPosition = original;
        }
    }
}
