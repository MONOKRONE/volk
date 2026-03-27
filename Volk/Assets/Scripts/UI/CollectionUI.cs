using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Volk.Core;

namespace Volk.UI
{
    public class CollectionUI : MonoBehaviour
    {
        [Header("Data")]
        public CharacterData[] allCharacters;
        public ComboData[] allCombos;

        [Header("Character Grid")]
        public Transform characterGrid;
        public GameObject characterCardPrefab;

        [Header("Combo Grid")]
        public Transform comboGrid;
        public GameObject comboCardPrefab;

        [Header("Tabs")]
        public Button charactersTab;
        public Button combosTab;
        public GameObject characterPanel;
        public GameObject comboPanel;

        [Header("Detail Panel")]
        public GameObject detailPanel;
        public Image detailPortrait;
        public TextMeshProUGUI detailName;
        public TextMeshProUGUI detailBio;
        public Slider detailSpeed;
        public Slider detailPower;
        public Slider detailDefense;
        public TextMeshProUGUI detailStatus;
        public Button detailCloseButton;

        [Header("Background")]
        public Image backgroundImage;

        void Start()
        {
            if (backgroundImage) backgroundImage.color = VTheme.Background;
            if (detailPanel) detailPanel.SetActive(false);
            if (detailCloseButton) detailCloseButton.onClick.AddListener(() => detailPanel.SetActive(false));

            if (charactersTab) charactersTab.onClick.AddListener(() => SwitchTab(true));
            if (combosTab) combosTab.onClick.AddListener(() => SwitchTab(false));

            PopulateCharacters();
            PopulateCombos();
            SwitchTab(true);
        }

        void SwitchTab(bool showCharacters)
        {
            if (characterPanel) characterPanel.SetActive(showCharacters);
            if (comboPanel) comboPanel.SetActive(!showCharacters);

            if (charactersTab)
            {
                var img = charactersTab.GetComponent<Image>();
                if (img) img.color = showCharacters ? VTheme.Red : VTheme.Panel;
            }
            if (combosTab)
            {
                var img = combosTab.GetComponent<Image>();
                if (img) img.color = !showCharacters ? VTheme.Red : VTheme.Panel;
            }
        }

        void PopulateCharacters()
        {
            if (allCharacters == null || characterGrid == null || characterCardPrefab == null) return;

            foreach (var data in allCharacters)
            {
                var card = Instantiate(characterCardPrefab, characterGrid);
                var capturedData = data;

                var nameText = card.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText) nameText.text = data.characterName;

                var portrait = card.transform.Find("Portrait")?.GetComponent<Image>();
                if (portrait && data.portrait) portrait.sprite = data.portrait;

                bool unlocked = data.unlockedByDefault ||
                    (CharacterUnlockManager.Instance != null && CharacterUnlockManager.Instance.IsUnlocked(data));

                var lockOverlay = card.transform.Find("LockOverlay");
                if (lockOverlay) lockOverlay.gameObject.SetActive(!unlocked);

                // Card color
                var cardImg = card.GetComponent<Image>();
                if (cardImg) cardImg.color = unlocked ? VTheme.Card : VTheme.ButtonDisabled;

                var btn = card.GetComponent<Button>();
                if (btn) btn.onClick.AddListener(() => ShowCharacterDetail(capturedData, unlocked));
            }
        }

        void PopulateCombos()
        {
            if (allCombos == null || comboGrid == null || comboCardPrefab == null) return;

            foreach (var combo in allCombos)
            {
                var card = Instantiate(comboCardPrefab, comboGrid);
                bool discovered = ComboTracker.Instance != null && ComboTracker.Instance.IsComboDiscovered(combo.comboName);

                var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = discovered ? combo.comboName : "???";
                if (texts.Length > 1)
                {
                    if (discovered)
                    {
                        string seq = "";
                        foreach (var input in combo.inputSequence)
                            seq += input == AttackType.Punch ? "P " : "K ";
                        texts[1].text = $"{seq}  x{combo.damageMultiplier}";
                    }
                    else
                    {
                        texts[1].text = "Not Discovered";
                    }
                }

                var cardImg = card.GetComponent<Image>();
                if (cardImg) cardImg.color = discovered ? VTheme.Card : VTheme.ButtonDisabled;
            }
        }

        void ShowCharacterDetail(CharacterData data, bool unlocked)
        {
            if (detailPanel == null) return;
            detailPanel.SetActive(true);

            if (detailPortrait && data.portrait) detailPortrait.sprite = data.portrait;
            if (detailName) { detailName.text = data.characterName; detailName.color = VTheme.TextPrimary; }
            if (detailSpeed) detailSpeed.value = data.speed / 10f;
            if (detailPower) detailPower.value = data.power / 10f;
            if (detailDefense) detailDefense.value = data.defense / 10f;
            if (detailStatus)
            {
                detailStatus.text = unlocked ? "UNLOCKED" : CharacterUnlockManager.Instance?.GetUnlockDescription(data) ?? "Locked";
                detailStatus.color = unlocked ? VTheme.Green : VTheme.Red;
            }

            UIAudio.Instance?.PlayClick();
        }
    }
}
