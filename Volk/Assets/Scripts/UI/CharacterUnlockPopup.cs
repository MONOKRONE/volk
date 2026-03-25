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
        public TextMeshProUGUI skill1Text;
        public TextMeshProUGUI skill2Text;
        public TextMeshProUGUI statsText;
        public VButton playNowButton;
        public VButton closeButton;

        [Header("Animation")]
        public float fadeInDuration = 0.5f;
        public float dramaticPauseDuration = 0.3f;

        private CharacterData unlockedCharacter;

        void Awake()
        {
            Instance = this;
            if (popupGroup) popupGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        public void Show(CharacterData character)
        {
            unlockedCharacter = character;
            gameObject.SetActive(true);

            if (characterNameText) characterNameText.text = character.characterName;
            if (characterPortrait && character.portrait) characterPortrait.sprite = character.portrait;

            if (skill1Text && character.skill1 != null)
                skill1Text.text = $"Skill 1: {character.skill1.skillName}";
            if (skill2Text && character.skill2 != null)
                skill2Text.text = $"Skill 2: {character.skill2.skillName}";
            if (statsText)
                statsText.text = $"HP: {character.maxHP}  POW: {character.power}  DEF: {character.defense}  SPD: {character.speed}";

            StartCoroutine(AnimateIn());
        }

        IEnumerator AnimateIn()
        {
            if (popupGroup == null) yield break;

            // Dramatic pause
            yield return new WaitForSecondsRealtime(dramaticPauseDuration);

            // Fade in
            float elapsed = 0;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                popupGroup.alpha = elapsed / fadeInDuration;
                yield return null;
            }
            popupGroup.alpha = 1;
        }

        public void OnPlayNow()
        {
            if (unlockedCharacter != null && GameSettings.Instance != null)
                GameSettings.Instance.selectedCharacter = unlockedCharacter;
            Close();
        }

        public void Close()
        {
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
