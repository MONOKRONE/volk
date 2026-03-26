using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Volk.Core;
using System.Collections;

namespace Volk.UI
{
    public class CharacterUnlockPopup : MonoBehaviour
    {
        public static CharacterUnlockPopup Instance;

        [Header("UI")]
        public CanvasGroup popupGroup;
        public Image characterPortrait;
        public TextMeshProUGUI characterNameText;
        public TextMeshProUGUI archetypeText;
        public TextMeshProUGUI skill1Text;
        public TextMeshProUGUI skill2Text;
        public TextMeshProUGUI statsText;
        public VButton playNowButton;
        public VButton closeButton;

        [Header("Flash Effect")]
        public Image flashOverlay;
        public Image glowRing;

        [Header("Animation")]
        public float dramaticPauseDuration = 0.4f;
        public float flashDuration = 0.3f;
        public float revealDuration = 0.6f;

        private CharacterData unlockedCharacter;

        void Awake()
        {
            Instance = this;
            if (popupGroup) popupGroup.alpha = 0;
            if (flashOverlay) flashOverlay.color = new Color(1, 1, 1, 0);
            if (glowRing) glowRing.color = new Color(1, 0.84f, 0, 0);
            gameObject.SetActive(false);
        }

        public void Show(CharacterData character)
        {
            unlockedCharacter = character;
            gameObject.SetActive(true);

            // Name
            if (characterNameText)
            {
                characterNameText.text = character.characterName;
                characterNameText.color = VTheme.Gold;
            }

            // Portrait
            if (characterPortrait && character.portrait)
                characterPortrait.sprite = character.portrait;

            // Archetype based on stats
            if (archetypeText)
            {
                archetypeText.text = GetArchetype(character);
                archetypeText.color = VTheme.TextSecondary;
            }

            // Skills
            if (skill1Text)
            {
                if (character.skill1 != null)
                {
                    skill1Text.text = $"\u25C6 {character.skill1.skillName}";
                    skill1Text.color = VTheme.Blue;
                }
                else skill1Text.text = "";
            }
            if (skill2Text)
            {
                if (character.skill2 != null)
                {
                    skill2Text.text = $"\u25C6 {character.skill2.skillName}";
                    skill2Text.color = VTheme.Red;
                }
                else skill2Text.text = "";
            }

            // Stats
            if (statsText)
            {
                statsText.text = $"HP {character.maxHP}  |  GUC {character.power}  |  DEF {character.defense}  |  HIZ {character.speed}";
                statsText.color = VTheme.TextSecondary;
            }

            StartCoroutine(AnimateReveal());
        }

        string GetArchetype(CharacterData c)
        {
            if (c.power >= 8) return "Agresif Dovuscu";
            if (c.defense >= 8) return "Defansif Tank";
            if (c.speed >= 8) return "Hizli Ninja";
            return "Dengeli Savasci";
        }

        IEnumerator AnimateReveal()
        {
            if (popupGroup == null) yield break;
            popupGroup.alpha = 0;

            // Dramatic pause
            yield return new WaitForSecondsRealtime(dramaticPauseDuration);

            // Flash — bright white burst
            if (flashOverlay)
            {
                float t = 0;
                while (t < flashDuration * 0.3f)
                {
                    t += Time.unscaledDeltaTime;
                    float a = Mathf.Lerp(0, 0.8f, t / (flashDuration * 0.3f));
                    flashOverlay.color = new Color(1, 1, 1, a);
                    yield return null;
                }
                t = 0;
                while (t < flashDuration * 0.7f)
                {
                    t += Time.unscaledDeltaTime;
                    float a = Mathf.Lerp(0.8f, 0, t / (flashDuration * 0.7f));
                    flashOverlay.color = new Color(1, 1, 1, a);
                    yield return null;
                }
                flashOverlay.color = new Color(1, 1, 1, 0);
            }

            // Character reveal — scale up + fade in
            if (characterPortrait)
                characterPortrait.transform.localScale = Vector3.one * 0.3f;
            if (characterNameText)
                characterNameText.transform.localScale = Vector3.zero;

            float elapsed = 0;
            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / revealDuration);
                float ease = 1f - Mathf.Pow(1f - t, 3f); // ease out cubic

                popupGroup.alpha = ease;

                if (characterPortrait)
                {
                    float s = Mathf.LerpUnclamped(0.3f, 1f, EaseOutBack(t));
                    characterPortrait.transform.localScale = Vector3.one * s;
                }

                if (glowRing)
                {
                    float ga = Mathf.Sin(t * Mathf.PI) * 0.6f;
                    glowRing.color = new Color(VTheme.Gold.r, VTheme.Gold.g, VTheme.Gold.b, ga);
                    glowRing.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.3f, t);
                }

                yield return null;
            }
            popupGroup.alpha = 1;

            // Name punch in
            if (characterNameText)
            {
                elapsed = 0;
                while (elapsed < 0.3f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float s = EaseOutBack(Mathf.Clamp01(elapsed / 0.3f));
                    characterNameText.transform.localScale = Vector3.one * s;
                    yield return null;
                }
                characterNameText.transform.localScale = Vector3.one;
            }

            // Fade glow to subtle pulse
            if (glowRing)
                glowRing.color = new Color(VTheme.Gold.r, VTheme.Gold.g, VTheme.Gold.b, 0.2f);
        }

        float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public void OnPlayNow()
        {
            UIAudio.Instance?.PlayClick();
            if (unlockedCharacter != null && GameSettings.Instance != null)
                GameSettings.Instance.selectedCharacter = unlockedCharacter;
            Close();
        }

        public void Close()
        {
            UIAudio.Instance?.PlayClick();
            StartCoroutine(AnimateOut());
        }

        IEnumerator AnimateOut()
        {
            if (popupGroup != null)
            {
                float elapsed = 0;
                while (elapsed < 0.3f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    popupGroup.alpha = 1 - elapsed / 0.3f;
                    yield return null;
                }
            }
            gameObject.SetActive(false);
        }
    }
}
