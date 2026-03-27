using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Volk.Core;
using Volk.Meta;

namespace Volk.UI
{
    public class ShopUI : MonoBehaviour
    {
        [Header("Navigation")]
        public Button backButton;
        public VTopBar topBar;

        [Header("Currency Display")]
        public TextMeshProUGUI coinText;
        public TextMeshProUGUI gemText;

        [Header("Tab Buttons")]
        public Button battlePassTab;
        public Button cosmeticsTab;
        public Button charactersTab;

        [Header("Tab Panels")]
        public GameObject battlePassPanel;
        public GameObject cosmeticsPanel;
        public GameObject charactersPanel;

        [Header("Battle Pass")]
        public BattlePassUI battlePassUI;

        [Header("Cosmetics Grid")]
        public Transform cosmeticsGrid;
        public GameObject cosmeticCardPrefab;

        [Header("Characters Grid")]
        public Transform charactersGrid;
        public GameObject characterCardPrefab;

        [Header("Confirm Popup")]
        public GameObject confirmPopup;
        public TextMeshProUGUI confirmText;
        public Button confirmYes;
        public Button confirmNo;

        private int activeTabIndex;
        private ShopItemData pendingShopItem;
        private CharacterData pendingCharacter;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        void Start()
        {
            // Back button
            if (backButton)
                backButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

            // Tab buttons
            if (battlePassTab) battlePassTab.onClick.AddListener(() => SwitchTab(0));
            if (cosmeticsTab) cosmeticsTab.onClick.AddListener(() => SwitchTab(1));
            if (charactersTab) charactersTab.onClick.AddListener(() => SwitchTab(2));

            // Confirm popup
            if (confirmPopup) confirmPopup.SetActive(false);
            if (confirmNo) confirmNo.onClick.AddListener(() => confirmPopup?.SetActive(false));
            if (confirmYes) confirmYes.onClick.AddListener(OnConfirmPurchase);

            // Shop events
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnItemPurchased += OnItemPurchased;
                ShopManager.Instance.OnPurchaseFailed += OnFailed;
            }

            // Default tab
            SwitchTab(0);
            UpdateCurrency();
        }

        void OnDestroy()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnItemPurchased -= OnItemPurchased;
                ShopManager.Instance.OnPurchaseFailed -= OnFailed;
            }
        }

        // --- Tab Management ---

        void SwitchTab(int index)
        {
            activeTabIndex = index;
            UIAudio.Instance?.PlayClick();

            if (battlePassPanel) battlePassPanel.SetActive(index == 0);
            if (cosmeticsPanel) cosmeticsPanel.SetActive(index == 1);
            if (charactersPanel) charactersPanel.SetActive(index == 2);

            UpdateTabColors();

            if (index == 0 && battlePassUI) battlePassUI.Refresh();
            if (index == 1) PopulateCosmetics();
            if (index == 2) PopulateCharacters();
        }

        void UpdateTabColors()
        {
            SetTabColor(battlePassTab, activeTabIndex == 0);
            SetTabColor(cosmeticsTab, activeTabIndex == 1);
            SetTabColor(charactersTab, activeTabIndex == 2);
        }

        void SetTabColor(Button tab, bool active)
        {
            if (tab == null) return;
            var img = tab.GetComponent<Image>();
            if (img) img.color = active ? VTheme.Red : VTheme.Card;
            var txt = tab.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.color = active ? VTheme.TextPrimary : VTheme.TextMuted;
        }

        // --- Cosmetics Tab ---

        void PopulateCosmetics()
        {
            if (cosmeticsGrid == null) return;

            // Clear existing
            foreach (Transform child in cosmeticsGrid)
                Destroy(child.gameObject);

            if (ShopManager.Instance == null) return;

            foreach (var item in ShopManager.Instance.shopItems)
            {
                if (cosmeticCardPrefab == null) break;

                var card = Instantiate(cosmeticCardPrefab, cosmeticsGrid);
                var capturedItem = item;

                var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = item.itemName;
                if (texts.Length > 1) texts[1].text = $"{item.price} coin";

                var icon = card.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null && item.icon != null)
                    icon.sprite = item.icon;

                bool owned = ShopManager.Instance.IsOwned(item);
                var ownedBadge = card.transform.Find("OwnedBadge");
                if (ownedBadge != null)
                    ownedBadge.gameObject.SetActive(owned);

                var btn = card.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = !owned;
                    btn.onClick.AddListener(() => ShowConfirmItem(capturedItem));
                }
            }
        }

        // --- Characters Tab ---

        void PopulateCharacters()
        {
            if (charactersGrid == null) return;

            foreach (Transform child in charactersGrid)
                Destroy(child.gameObject);

            var allChars = Resources.LoadAll<CharacterData>("Characters");
            if (allChars == null) return;

            foreach (var charData in allChars)
            {
                if (characterCardPrefab == null) break;

                var card = Instantiate(characterCardPrefab, charactersGrid);
                bool unlocked = charData.unlockedByDefault ||
                    (CharacterUnlockManager.Instance != null && CharacterUnlockManager.Instance.IsUnlocked(charData));

                var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = charData.characterName;
                if (texts.Length > 1)
                    texts[1].text = unlocked ? "OWNED" : GetUnlockCostText(charData);

                var portrait = card.transform.Find("Portrait")?.GetComponent<Image>();
                if (portrait != null && charData.portrait != null)
                    portrait.sprite = charData.portrait;

                // Lock overlay
                var lockOverlay = card.transform.Find("LockOverlay");
                if (lockOverlay != null)
                    lockOverlay.gameObject.SetActive(!unlocked);

                var btn = card.GetComponent<Button>();
                if (btn != null)
                {
                    var captured = charData;
                    btn.interactable = !unlocked;
                    btn.onClick.AddListener(() => ShowConfirmCharacter(captured));
                }
            }
        }

        string GetUnlockCostText(CharacterData charData)
        {
            return charData.unlockType switch
            {
                UnlockCondition.Currency => $"{charData.unlockValue} gem",
                UnlockCondition.StoryProgress => $"Bolum {charData.unlockValue}",
                UnlockCondition.WinCount => $"{charData.unlockValue} wins",
                _ => "LOCKED"
            };
        }

        // --- Confirm Popup ---

        void ShowConfirmItem(ShopItemData item)
        {
            pendingShopItem = item;
            pendingCharacter = null;
            if (confirmPopup == null) { ShopManager.Instance?.TryPurchase(item); return; }
            confirmPopup.SetActive(true);
            if (confirmText) confirmText.text = $"{item.itemName}\n{item.price} coin\nPurchase?";
        }

        void ShowConfirmCharacter(CharacterData charData)
        {
            pendingCharacter = charData;
            pendingShopItem = null;
            if (confirmPopup == null) return;
            confirmPopup.SetActive(true);
            if (confirmText) confirmText.text = $"{charData.characterName}\n{GetUnlockCostText(charData)}\nUnlock?";
        }

        void OnConfirmPurchase()
        {
            if (pendingShopItem != null)
            {
                ShopManager.Instance?.TryPurchase(pendingShopItem);
                pendingShopItem = null;
            }
            else if (pendingCharacter != null)
            {
                TryUnlockCharacter(pendingCharacter);
                pendingCharacter = null;
            }
            confirmPopup?.SetActive(false);
        }

        void TryUnlockCharacter(CharacterData charData)
        {
            if (CharacterUnlockManager.Instance == null) return;
            bool success = CharacterUnlockManager.Instance.TryUnlock(charData);
            if (!success)
            {
                OnFailed("Character unlock failed");
                return;
            }
            PopulateCharacters();
            UpdateCurrency();
        }

        // --- Events ---

        void OnItemPurchased(ShopItemData item)
        {
            PopulateCosmetics();
            UpdateCurrency();
        }

        void OnFailed(string reason)
        {
            Debug.Log($"[Shop] Purchase failed: {reason}");
        }

        // --- Currency ---

        void UpdateCurrency()
        {
            if (CurrencyManager.Instance != null)
            {
                if (coinText) coinText.text = CurrencyManager.Instance.Coins.ToString();
                if (gemText) gemText.text = CurrencyManager.Instance.Gems.ToString();
            }
        }
    }
}
