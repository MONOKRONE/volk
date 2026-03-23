using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Volk.Core;

namespace Volk.UI
{
    public class CharacterSelectManager : MonoBehaviour
    {
        [Header("Character Data")]
        public CharacterData[] allCharacters;

        [Header("UI References")]
        public Transform cardContainer;
        public GameObject cardPrefab;
        public TextMeshProUGUI characterNameText;
        public Image portraitImage;
        public Slider speedBar;
        public Slider powerBar;
        public Slider defenseBar;
        public TextMeshProUGUI hpText;
        public Button selectButton;
        public Button backButton;
        public CanvasGroup canvasGroup;

        [Header("Scenes")]
        public string combatSceneName = "CombatTest";
        public string mainMenuSceneName = "MainMenu";

        private int selectedIndex = -1;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        void Start()
        {
            // Ensure GameSettings exists
            if (GameSettings.Instance == null)
            {
                var go = new GameObject("GameSettings");
                go.AddComponent<GameSettings>();
            }

            PopulateCards();

            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelectPressed);
            if (backButton != null)
                backButton.onClick.AddListener(OnBackPressed);

            selectButton.interactable = false;
            StartCoroutine(FadeIn());

            // Auto-select first unlocked character
            for (int i = 0; i < allCharacters.Length; i++)
            {
                if (allCharacters[i].unlockedByDefault || IsCharacterUnlocked(allCharacters[i]))
                {
                    SelectCharacter(i);
                    break;
                }
            }
        }

        void PopulateCards()
        {
            if (cardPrefab == null || cardContainer == null) return;

            for (int i = 0; i < allCharacters.Length; i++)
            {
                var card = Instantiate(cardPrefab, cardContainer);
                var data = allCharacters[i];
                int index = i;

                // Card name
                var nameText = card.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null)
                    nameText.text = data.characterName;

                // Card portrait
                var portrait = card.transform.Find("Portrait")?.GetComponent<Image>();
                if (portrait != null && data.portrait != null)
                    portrait.sprite = data.portrait;

                // Lock overlay
                bool unlocked = data.unlockedByDefault || IsCharacterUnlocked(data);
                var lockOverlay = card.transform.Find("LockOverlay");
                if (lockOverlay != null)
                    lockOverlay.gameObject.SetActive(!unlocked);

                // Button click
                var btn = card.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        if (unlocked)
                            SelectCharacter(index);
                        else
                            ShowUnlockRequirement(data);
                    });
                }
            }
        }

        void SelectCharacter(int index)
        {
            if (index < 0 || index >= allCharacters.Length) return;
            selectedIndex = index;
            var data = allCharacters[index];

            if (characterNameText != null)
                characterNameText.text = data.characterName;
            if (portraitImage != null && data.portrait != null)
                portraitImage.sprite = data.portrait;
            if (speedBar != null) speedBar.value = data.speed / 10f;
            if (powerBar != null) powerBar.value = data.power / 10f;
            if (defenseBar != null) defenseBar.value = data.defense / 10f;
            if (hpText != null) hpText.text = $"HP: {data.maxHP}";

            selectButton.interactable = true;
        }

        void ShowUnlockRequirement(CharacterData data)
        {
            string msg = data.unlockType switch
            {
                UnlockCondition.StoryProgress => $"Chapter {data.unlockValue} tamamla",
                UnlockCondition.WinCount => $"{data.unlockValue} mac kazan",
                UnlockCondition.Currency => $"{data.unlockValue} coin gerekli",
                _ => "Kilitli"
            };
            Debug.Log($"[CharSelect] {data.characterName}: {msg}");
        }

        bool IsCharacterUnlocked(CharacterData data)
        {
            return PlayerPrefs.GetInt($"char_unlocked_{data.characterName}", 0) == 1;
        }

        void OnSelectPressed()
        {
            if (selectedIndex < 0) return;
            GameSettings.Instance.selectedCharacter = allCharacters[selectedIndex];
            StartCoroutine(FadeOutAndLoad(combatSceneName));
        }

        void OnBackPressed()
        {
            StartCoroutine(FadeOutAndLoad(mainMenuSceneName));
        }

        IEnumerator FadeOutAndLoad(string sceneName)
        {
            if (canvasGroup != null)
            {
                float t = 0;
                while (t < 0.4f)
                {
                    t += Time.deltaTime;
                    canvasGroup.alpha = 1 - (t / 0.4f);
                    yield return null;
                }
            }
            SceneManager.LoadScene(sceneName);
        }

        IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;
            canvasGroup.alpha = 0;
            float t = 0;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = t / 0.6f;
                yield return null;
            }
            canvasGroup.alpha = 1;
        }
    }
}
