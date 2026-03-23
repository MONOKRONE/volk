using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Volk.Core;

namespace Volk.UI
{
    public class CharacterCarouselUI : MonoBehaviour
    {
        [Header("Data")]
        public CharacterData[] characters;

        [Header("Carousel")]
        public RectTransform carouselContainer;
        public Button leftArrow;
        public Button rightArrow;

        [Header("Character Info")]
        public TextMeshProUGUI nameText;
        public Image portraitImage;
        public TextMeshProUGUI bioText;

        [Header("Stat Bars")]
        public Slider speedBar;
        public Slider powerBar;
        public Slider defenseBar;
        public TextMeshProUGUI speedValue;
        public TextMeshProUGUI powerValue;
        public TextMeshProUGUI defenseValue;

        [Header("Stat Bar Colors")]
        public Image speedFill;
        public Image powerFill;
        public Image defenseFill;

        [Header("Skills")]
        public TextMeshProUGUI skill1Name;
        public TextMeshProUGUI skill2Name;

        public int SelectedIndex { get; private set; }
        public CharacterData SelectedCharacter => characters != null && characters.Length > 0 ? characters[SelectedIndex] : null;

        public event System.Action<int> OnSelectionChanged;

        void Start()
        {
            if (leftArrow) leftArrow.onClick.AddListener(() => Navigate(-1));
            if (rightArrow) rightArrow.onClick.AddListener(() => Navigate(1));

            // Stat bar colors
            if (speedFill) speedFill.color = VTheme.Blue;
            if (powerFill) powerFill.color = VTheme.Red;
            if (defenseFill) defenseFill.color = VTheme.Gold;

            if (characters != null && characters.Length > 0)
                ShowCharacter(0);
        }

        public void Navigate(int dir)
        {
            if (characters == null || characters.Length == 0) return;
            SelectedIndex = (SelectedIndex + dir + characters.Length) % characters.Length;
            UIAudio.Instance?.PlayClick();
            StartCoroutine(TransitionToCharacter(SelectedIndex, dir));
        }

        void ShowCharacter(int index)
        {
            var data = characters[index];

            if (nameText) { nameText.text = data.characterName; nameText.color = VTheme.TextPrimary; }
            if (portraitImage && data.portrait) portraitImage.sprite = data.portrait;

            // Stats with smooth fill
            if (speedBar) speedBar.value = data.speed / 10f;
            if (powerBar) powerBar.value = data.power / 10f;
            if (defenseBar) defenseBar.value = data.defense / 10f;
            if (speedValue) speedValue.text = data.speed.ToString("F0");
            if (powerValue) powerValue.text = data.power.ToString("F0");
            if (defenseValue) defenseValue.text = data.defense.ToString("F0");

            // Skills
            if (skill1Name) skill1Name.text = data.skill1 != null ? data.skill1.skillName : "---";
            if (skill2Name) skill2Name.text = data.skill2 != null ? data.skill2.skillName : "---";

            OnSelectionChanged?.Invoke(index);
        }

        IEnumerator TransitionToCharacter(int index, int dir)
        {
            if (carouselContainer == null) { ShowCharacter(index); yield break; }

            // Slide out
            Vector2 original = carouselContainer.anchoredPosition;
            Vector2 slideOut = original + new Vector2(dir * -200f, 0);
            float t = 0;
            while (t < 0.15f)
            {
                t += Time.unscaledDeltaTime;
                carouselContainer.anchoredPosition = Vector2.Lerp(original, slideOut, t / 0.15f);
                yield return null;
            }

            ShowCharacter(index);

            // Slide in from opposite side
            Vector2 slideIn = original + new Vector2(dir * 200f, 0);
            carouselContainer.anchoredPosition = slideIn;
            t = 0;
            while (t < 0.2f)
            {
                t += Time.unscaledDeltaTime;
                float ease = 1f - Mathf.Pow(1f - t / 0.2f, 3f);
                carouselContainer.anchoredPosition = Vector2.Lerp(slideIn, original, ease);
                yield return null;
            }
            carouselContainer.anchoredPosition = original;
        }
    }
}
