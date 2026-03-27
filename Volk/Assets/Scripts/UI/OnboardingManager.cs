using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Volk.UI
{
    public class OnboardingManager : MonoBehaviour
    {
        public static OnboardingManager Instance;

        [Header("Overlay")]
        public CanvasGroup overlayGroup;
        public Image overlayBG;

        [Header("Step UI")]
        public TextMeshProUGUI stepTitleText;
        public TextMeshProUGUI stepDescriptionText;
        public TextMeshProUGUI stepCounterText;
        public Image stepIcon;
        public Slider progressBar;

        [Header("Buttons")]
        public VButton nextButton;
        public VButton skipButton;

        [Header("Highlight")]
        public RectTransform highlightRect;
        public Image highlightImage;

        [Header("Settings")]
        public string prefsKey = "onboarding_done";

        private int currentStep;
        private bool isActive;

        static readonly TutorialStep[] Steps = {
            new TutorialStep {
                title = "Hareket",
                description = "Sol joystick ile karakterini hareket ettir.\nYukariya fiske = zipla, asagiya = egilme.",
                highlightAnchorMin = new Vector2(0, 0),
                highlightAnchorMax = new Vector2(0.25f, 0.5f)
            },
            new TutorialStep {
                title = "Saldiri",
                description = "Yumruk ve Tekme butonlarina dokun.\nBasili tut = agir saldiri.\nBlok = savunma.",
                highlightAnchorMin = new Vector2(0.75f, 0),
                highlightAnchorMax = new Vector2(1f, 0.5f)
            },
            new TutorialStep {
                title = "Skill",
                description = "Butona cift dokun = ozel beceri.\nHer karakterin 2 farkli skill'i var.\nSkill cooldown'a dikkat et.",
                highlightAnchorMin = new Vector2(0.75f, 0.5f),
                highlightAnchorMax = new Vector2(1f, 1f)
            }
        };

        struct TutorialStep
        {
            public string title;
            public string description;
            public Vector2 highlightAnchorMin;
            public Vector2 highlightAnchorMax;
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (PlayerPrefs.GetInt(prefsKey, 0) == 1)
            {
                gameObject.SetActive(false);
                return;
            }

            currentStep = 0;
            isActive = true;
            if (overlayGroup) overlayGroup.alpha = 0;

            if (nextButton) nextButton.onClick.AddListener(NextStep);
            if (skipButton) skipButton.onClick.AddListener(Skip);

            StartCoroutine(ShowFirstStep());
        }

        IEnumerator ShowFirstStep()
        {
            yield return new WaitForSeconds(0.5f);

            if (overlayGroup)
            {
                float t = 0;
                while (t < 0.4f)
                {
                    t += Time.unscaledDeltaTime;
                    overlayGroup.alpha = t / 0.4f;
                    yield return null;
                }
                overlayGroup.alpha = 1;
            }

            ShowStep(0);
        }

        void ShowStep(int index)
        {
            if (index >= Steps.Length)
            {
                Complete();
                return;
            }

            currentStep = index;
            var step = Steps[index];

            if (stepTitleText)
            {
                stepTitleText.text = step.title;
                stepTitleText.color = VTheme.Gold;
            }
            if (stepDescriptionText)
            {
                stepDescriptionText.text = step.description;
                stepDescriptionText.color = VTheme.TextPrimary;
            }
            if (stepCounterText)
            {
                stepCounterText.text = $"{index + 1} / {Steps.Length}";
                stepCounterText.color = VTheme.TextSecondary;
            }
            if (progressBar)
                progressBar.value = (float)(index + 1) / Steps.Length;

            // Highlight area
            if (highlightRect)
            {
                highlightRect.anchorMin = step.highlightAnchorMin;
                highlightRect.anchorMax = step.highlightAnchorMax;
                highlightRect.offsetMin = Vector2.zero;
                highlightRect.offsetMax = Vector2.zero;
            }
            if (highlightImage)
                highlightImage.color = new Color(VTheme.Gold.r, VTheme.Gold.g, VTheme.Gold.b, 0.15f);

            // Pulse highlight
            StartCoroutine(PulseHighlight());

            // Next button text
            if (nextButton)
            {
                var txt = nextButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = index == Steps.Length - 1 ? "TAMAM" : "SONRAKI";
            }
        }

        IEnumerator PulseHighlight()
        {
            if (highlightImage == null) yield break;
            float baseAlpha = 0.15f;
            float t = 0;
            while (t < 0.8f && currentStep < Steps.Length)
            {
                t += Time.unscaledDeltaTime;
                float pulse = baseAlpha + Mathf.Sin(t * 6f) * 0.08f;
                highlightImage.color = new Color(VTheme.Gold.r, VTheme.Gold.g, VTheme.Gold.b, pulse);
                yield return null;
            }
        }

        public void NextStep()
        {
            UIAudio.Instance?.PlayClick();
            ShowStep(currentStep + 1);
        }

        public void Skip()
        {
            UIAudio.Instance?.PlayClick();
            Complete();
        }

        void Complete()
        {
            PlayerPrefs.SetInt(prefsKey, 1);
            PlayerPrefs.Save();
            isActive = false;
            Debug.Log("[Onboarding] Tutorial completed");
            StartCoroutine(FadeOut());
        }

        IEnumerator FadeOut()
        {
            if (overlayGroup)
            {
                float t = 0;
                while (t < 0.3f)
                {
                    t += Time.unscaledDeltaTime;
                    overlayGroup.alpha = 1 - t / 0.3f;
                    yield return null;
                }
            }
            gameObject.SetActive(false);
        }
    }
}
