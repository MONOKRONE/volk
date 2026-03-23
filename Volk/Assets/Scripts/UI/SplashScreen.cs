using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Volk.UI
{
    public class SplashScreen : MonoBehaviour
    {
        [Header("Studio Logo Phase")]
        public CanvasGroup studioLogoGroup;
        public TextMeshProUGUI studioLogoText;
        public float studioLogoDuration = 2f;

        [Header("Title Phase")]
        public CanvasGroup titleGroup;
        public TextMeshProUGUI volkTitle;
        public TextMeshProUGUI tagline;
        public TextMeshProUGUI tapPrompt;
        public Image backgroundImage;

        [Header("Silhouettes")]
        public RectTransform leftSilhouette;
        public RectTransform rightSilhouette;

        [Header("Settings")]
        public string nextScene = "MainHub";
        public float titleFadeInDuration = 1.5f;

        private bool canProceed;
        private float tapPulseTimer;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        void Start()
        {
            if (studioLogoGroup) studioLogoGroup.alpha = 0;
            if (titleGroup) titleGroup.alpha = 0;

            // Dark background
            if (backgroundImage)
                backgroundImage.color = VTheme.Background;

            StartCoroutine(SplashSequence());
        }

        IEnumerator SplashSequence()
        {
            // Phase 1: Studio logo
            if (studioLogoGroup != null)
            {
                if (studioLogoText)
                {
                    studioLogoText.text = "PLAYVOLK";
                    studioLogoText.color = VTheme.TextPrimary;
                }

                yield return FadeCanvasGroup(studioLogoGroup, 0, 1, 0.8f);
                yield return new WaitForSeconds(studioLogoDuration);
                yield return FadeCanvasGroup(studioLogoGroup, 1, 0, 0.5f);
            }

            yield return new WaitForSeconds(0.3f);

            // Phase 2: Title
            if (titleGroup != null)
            {
                if (volkTitle)
                {
                    volkTitle.text = "VOLK";
                    volkTitle.color = VTheme.Red;
                }
                if (tagline)
                {
                    tagline.text = "FIGHT.";
                    tagline.color = VTheme.Gold;
                }

                // Animate title scale
                if (volkTitle)
                {
                    volkTitle.transform.localScale = Vector3.one * 0.5f;
                }

                yield return FadeCanvasGroup(titleGroup, 0, 1, titleFadeInDuration);

                // Scale animation
                if (volkTitle)
                {
                    float t = 0;
                    while (t < 0.6f)
                    {
                        t += Time.deltaTime;
                        float s = Mathf.Lerp(0.5f, 1f, Mathf.SmoothStep(0, 1, t / 0.6f));
                        volkTitle.transform.localScale = Vector3.one * s;
                        yield return null;
                    }
                    volkTitle.transform.localScale = Vector3.one;
                }

                // Silhouette slide in
                if (leftSilhouette)
                    StartCoroutine(SlideIn(leftSilhouette, new Vector2(-300, 0), 0.8f));
                if (rightSilhouette)
                    StartCoroutine(SlideIn(rightSilhouette, new Vector2(300, 0), 0.8f));

                yield return new WaitForSeconds(1f);

                canProceed = true;

                // Show tap prompt
                if (tapPrompt)
                {
                    tapPrompt.text = "DOKUNARAK BASLA";
                    tapPrompt.color = VTheme.TextSecondary;
                    tapPrompt.gameObject.SetActive(true);
                }
            }
        }

        void Update()
        {
            if (!canProceed) return;

            // Pulse tap prompt
            if (tapPrompt)
            {
                tapPulseTimer += Time.deltaTime * 2f;
                float alpha = 0.4f + Mathf.Sin(tapPulseTimer) * 0.4f;
                tapPrompt.alpha = alpha;
            }

            // Any input proceeds
            bool inputDetected = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.anyKeyDown;
            if (!inputDetected && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                inputDetected = true;

            if (inputDetected)
            {
                canProceed = false;
                StartCoroutine(TransitionOut());
            }
        }

        IEnumerator TransitionOut()
        {
            UIAudio.Instance?.PlaySwoosh();
            if (titleGroup != null)
                yield return FadeCanvasGroup(titleGroup, 1, 0, 0.3f);
            SceneManager.LoadScene(nextScene);
        }

        IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            float t = 0;
            cg.alpha = from;
            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            cg.alpha = to;
        }

        IEnumerator SlideIn(RectTransform rt, Vector2 offset, float duration)
        {
            Vector2 target = rt.anchoredPosition;
            rt.anchoredPosition = target + offset;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float ease = 1f - Mathf.Pow(1f - t / duration, 3f);
                rt.anchoredPosition = Vector2.Lerp(target + offset, target, ease);
                yield return null;
            }
            rt.anchoredPosition = target;
        }
    }
}
